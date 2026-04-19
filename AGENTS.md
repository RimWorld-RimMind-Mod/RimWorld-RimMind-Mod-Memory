# AGENTS.md — RimMind-Memory

本文件供 AI 编码助手快速理解 RimMind-Memory 项目。

## 项目定位

RimMind-Memory 是 RimMind AI 模组套件的**记忆系统模块**，负责：

1. **记忆采集** — 监听游戏事件（工作、受伤、死亡、技能升级等），生成 MemoryEntry
2. **工作会话聚合** — 将连续同类工作聚合成单条记忆（如"搬运 x12 约4.8游戏时"）
3. **叙事者记忆** — 采集殖民地级别事件（袭击、事件等），形成叙事视角
4. **暗记忆生成** — 每日调用 AI 将当日记忆凝练为长期印象（<=50字/条）
5. **上下文注入** — 通过 RimMind-Core Provider 注册机制将记忆注入 AI Prompt
6. **重要度衰减** — 可选机制，低重要度记忆随时间衰减直至移除
7. **公开 API** — `RimMindMemoryAPI.AddMemory()` 供其他模组（如 Dialogue）写入记忆

## 构建配置

| 属性 | 值 |
|---|---|
| TargetFramework | net48 |
| LangVersion | 9.0 |
| Nullable | enable |
| RootNamespace | RimMind.Memory |
| AssemblyName | RimMindMemory |
| OutputPath | `../$(GameVersion)/Assemblies/` (默认 1.6) |

**NuGet 依赖**: `Krafs.Rimworld.Ref (1.6.*)`, `Lib.Harmony.Ref (2.*)`, `Newtonsoft.Json (13.0.*)`

**项目引用**: `RimMindCore.dll` — 路径 `../../RimMind-Core/$(GameVersion)/Assemblies/RimMindCore.dll`

**部署**: 设置 `RIMWORLD_DIR` 环境变量后构建自动 robocopy 部署；也可用 `script/deploy-single.sh`

## 源码结构

```
Source/
├── RimMindMemoryMod.cs                Mod 入口，Harmony PatchAll，设置 UI
├── RimMindMemoryAPI.cs                公开静态 API
├── Settings/
│   └── RimMindMemorySettings.cs       ModSettings，25 个配置项
├── Data/
│   ├── MemoryEntry.cs                 记忆条目数据结构 + MemoryType 枚举
│   ├── PawnMemoryStore.cs             单个 Pawn 记忆存储（active/archive/dark）
│   ├── NarratorMemoryStore.cs         叙事者记忆存储（结构同 PawnMemoryStore）
│   └── RimMindMemoryWorldComponent.cs WorldComponent 单例，管理所有存储
├── Injection/
│   └── MemoryContextProvider.cs       向 RimMind-Core 注册两个上下文 Provider
├── Aggregation/
│   ├── WorkSessionAggregator.cs       工作会话聚合（GameComponent，不持久化）
│   └── Patch_StartJob_Memory.cs       Pawn_JobTracker.StartJob Postfix
├── Triggers/
│   ├── Patch_AddHediff.cs             Pawn_HealthTracker.AddHediff Postfix
│   ├── Patch_PawnKill.cs              Pawn.Kill Postfix
│   ├── Patch_MentalBreak.cs           MentalStateHandler.TryStartMentalState Postfix
│   ├── Patch_SkillLevelUp.cs          SkillRecord.Learn Prefix+Postfix
│   └── Patch_AddRelation.cs           Pawn_RelationsTracker.AddDirectRelation Postfix
├── Narrator/
│   └── Patch_IncidentWorker.cs        IncidentWorker.TryExecute Postfix
├── DarkMemory/
│   └── DarkMemoryUpdater.cs           每日暗记忆生成（GameComponent）
├── Decay/
│   └── ImportanceDecayManager.cs      重要度衰减管理
├── Core/
│   ├── TimeFormatter.cs               时间格式化（相对时间/游戏日期）
│   └── ImportanceDecayCalculator.cs   衰减计算（纯函数）
├── UI/
│   ├── BioTabMemoryPatch.cs           CharacterCardUtility.DoTopStack Transpiler
│   ├── Dialog_MemoryLog.cs            记忆日志窗口 + Dialog_InputMemory（内嵌类）
│   └── NarratorSettingsTab.cs         叙事者设置页
└── Debug/
    └── MemoryDebugActions.cs          8 个 Dev 菜单调试动作
```

## 关键类与数据结构

### MemoryEntry

```csharp
// 命名空间: RimMind.Memory.Data
public enum MemoryType { Work, Event, Manual, Dark }

public class MemoryEntry : IExposable
{
    public string id;           // "mem-{tick}"
    public string content;      // 记忆内容（中文）
    public MemoryType type;
    public int tick;            // 游戏刻
    public float importance;    // 0-1
    public bool isPinned;       // 固定（不被淘汰/衰减），Dark 类型自动 true
    public string? pawnId;      // 叙事者记忆关联 Pawn
    public string? notes;

    static MemoryEntry Create(string content, MemoryType type, int tick, float importance, string? pawnId = null);
}
```

### PawnMemoryStore / NarratorMemoryStore

三层存储，NarratorMemoryStore 结构相同，复用 `PawnMemoryStore.EnforceLimit`：

```csharp
// 命名空间: RimMind.Memory.Data
public class PawnMemoryStore : IExposable
{
    public List<MemoryEntry> active;    // 按时间倒序，头部插入
    public List<MemoryEntry> archive;   // 按重要度排序
    public List<MemoryEntry> dark;      // AI 生成，只读展示

    public void AddActive(MemoryEntry e, int maxActive, int maxArchive);
    // 头部插入 active → EnforceLimit 溢出到 archive → archive 溢出丢弃最低非 pinned

    public static void EnforceLimit(List<MemoryEntry> src, int srcMax, List<MemoryEntry> dst, int dstMax);
    public bool IsEmpty { get; }
}
```

### RimMindMemoryWorldComponent

```csharp
// 命名空间: RimMind.Memory.Data
public class RimMindMemoryWorldComponent : WorldComponent
{
    public static RimMindMemoryWorldComponent? Instance { get; }  // 单例
    Dictionary<int, PawnMemoryStore> _pawnStores;  // key = pawn.thingIDNumber
    NarratorMemoryStore _narratorStore;

    public PawnMemoryStore GetOrCreatePawnStore(Pawn pawn);
    public NarratorMemoryStore NarratorStore { get; }
    public IEnumerable<PawnMemoryStore> AllPawnStores { get; }
    public void ClearPawnStore(Pawn pawn);
}
```

### RimMindMemoryAPI

```csharp
// 命名空间: RimMind.Memory
public static class RimMindMemoryAPI
{
    // memoryType 通过 Enum.TryParse<MemoryType> 解析，返回 false 表示 pawn 未找到
    public static bool AddMemory(string content, string memoryType, int tick, float importance, string? pawnId = null);
}
```

## Harmony 补丁汇总

Harmony ID: `mcocdaa.RimMindMemory`

| 类 | 目标方法 | 补丁类型 | 命名空间 |
|---|---|---|---|
| Patch_StartJob_Memory | `Pawn_JobTracker.StartJob` | Postfix | Aggregation |
| Patch_AddHediff | `Pawn_HealthTracker.AddHediff(Hediff, BodyPartRecord, DamageInfo?, DamageResult)` | Postfix | Triggers |
| Patch_PawnKill | `Pawn.Kill` | Postfix | Triggers |
| Patch_MentalBreak | `MentalStateHandler.TryStartMentalState` | Postfix | Triggers |
| Patch_SkillLevelUp | `SkillRecord.Learn` | Prefix + Postfix | Triggers |
| Patch_AddRelation | `Pawn_RelationsTracker.AddDirectRelation` | Postfix | Triggers |
| Patch_IncidentWorker | `IncidentWorker.TryExecute` | Postfix | Narrator |
| BioTabMemoryPatch | `CharacterCardUtility.DoTopStack` | Transpiler | UI |

## 记忆触发来源

| 来源 | 触发器 | 重要度 | 写入目标 |
|---|---|---|---|
| 工作会话 | WorkSessionAggregator | 时长>15000tick=0.5, 否则0.4 | Pawn + 条件升级叙事者 |
| 重要工作 | WorkSessionAggregator (SignificantJobs) | 0.5-0.9 (Rescue=0.8, Attack=0.9) | Pawn + 叙事者 |
| 受伤/患病 | Patch_AddHediff | lethal=0.9, chronic=0.8, tendable=0.7, sick=0.6, else=0.5 | Pawn + 条件升级叙事者 |
| 精神崩溃 | Patch_MentalBreak | Berserk=0.95, Manhunter=0.9, Extreme=0.9, Serious=0.8, else=0.7 | Pawn + 叙事者 |
| 亲近者死亡 | Patch_PawnKill | 有关系=1.0, 无关系=0.85, 叙事者=1.0 | 相关 Pawn + 叙事者 |
| 技能升级 | Patch_SkillLevelUp | >=15级=0.7, 否则0.5 | Pawn |
| 关系建立 | Patch_AddRelation | Spouse/Lover=0.95, Fiance=0.9, Parent/Child=0.9, Sibling/Bond=0.85, else=0.6 | 双向 Pawn + 条件升级叙事者 |
| 叙事者事件 | Patch_IncidentWorker | 0.3-1.0 (Raid=0.9, Wedding=0.85) | 仅叙事者 |
| 手动添加 | Dialog_InputMemory | 固定 0.6, type=Manual | Pawn |
| 外部模组 | RimMindMemoryAPI.AddMemory | 调用方指定 | Pawn |

**Pawn→叙事者升级**: 当 Pawn 记忆重要度 >= `pawnToNarratorThreshold`（默认0.8）时，同步写入 NarratorStore。

## 工作会话聚合

WorkSessionAggregator（GameComponent，不持久化，新游戏/读档时清空 `_sessions`）：

```csharp
class PawnSession
{
    string? currentJobDef;
    int startTick;
    int lastJobTick;
    int count;
    int totalTicks;
    int lastMeaningfulJobTick;  // 用于空闲间隔检测
}
```

**会话结束条件**:
1. 工作类型切换 → FlushSession 写入聚合记忆
2. 单次会话超时（硬编码 2500 ticks）
3. 空闲间隔超过阈值（默认 6000 ticks）→ 写入"休息/待机"记忆

**工作分类**:
- `BlacklistedJobs`（16 个）— 忽略，不记录
- `WhitelistedJobs`（33 个）— 可聚合，连续同类合并
- `SignificantJobs`（12 个）— 直接单独记录，不聚合

**工作标签**: 通过翻译键 `RimMind.Memory.Work.{defName}` 动态查找，`CanTranslate()` 时使用翻译值，否则回退到 defName。重要工作标签同理，使用 `RimMind.Memory.Work.Significant.{defName}`。

## 暗记忆生成

DarkMemoryUpdater（GameComponent）每日执行，带抖动机制分散 API 请求：

- **Pawn 暗记忆**: 每个 Pawn 有独立 jitter 偏移，按 `IsHashIntervalTick(DailyInterval + jitter)` 触发
- **叙事者暗记忆**: 有独立 `_narratorOffset` 偏移

流程：
1. 构建输入：今日记忆（active 中今天产生的）+ 现有暗记忆
2. 通过 `RimMindAPI.RequestAsync` 发送 AI 请求（使用 `StructuredPromptBuilder.FromKeyPrefix` 构建 System Prompt）
3. 解析 JSON 响应 `{"dark": ["...", ...]}`，替换 store.dark

**注意**: `darkCount` 参数在 `ApplyPawnDarkMemory`/`ApplyNarratorDarkMemory` 中用于限制实际写入 store.dark 的条目数，同时 prompt 中也约束 AI 输出对应条数。双重保障确保暗记忆条数符合设置。

## 上下文注入

MemoryContextProvider.Register() 注册两个 Provider：

| Provider ID | 注册方式 | 优先级 | 内容 |
|---|---|---|---|
| `memory_pawn` | RegisterPawnContextProvider | PromptSection.PriorityMemory | Pawn 的 active + archive + dark |
| `memory_narrator` | RegisterStaticProvider | PromptSection.PriorityAuxiliary | 叙事者 active + archive + dark |

注入比例由设置控制：`activeInjectRatio`/`archiveInjectRatio`（Pawn）和 `narratorActiveInjectRatio`/`narratorArchiveInjectRatio`（叙事者），截取对应比例的条目数。

## 重要度衰减

默认关闭（`enableDecay = false`）：

```csharp
ImportanceDecayCalculator.Decay(importance, rate)  => importance * (1 - rate)
ImportanceDecayCalculator.ShouldRemove(importance, threshold) => importance < threshold

ImportanceDecayManager.ApplyDecay(store, decayRate, minThreshold);
// 衰减 active + archive，移除 archive 中低于阈值且非 pinned 的条目
```

## RimMind-Core API 使用点

| API | 使用位置 | 用途 |
|---|---|---|
| `RimMindAPI.RegisterPawnContextProvider` | MemoryContextProvider | 注册 Pawn 记忆上下文 |
| `RimMindAPI.RegisterStaticProvider` | MemoryContextProvider | 注册叙事者上下文 |
| `RimMindAPI.RegisterSettingsTab` | RimMindMemoryMod | 注册叙事者设置标签页 |
| `RimMindAPI.RegisterModCooldown` | RimMindMemoryMod | 注册模组冷却 |
| `RimMindAPI.IsConfigured()` | DarkMemoryUpdater | 检查 API 是否已配置 |
| `RimMindAPI.RequestAsync` | DarkMemoryUpdater | 异步 AI 请求 |
| `StructuredPromptBuilder.FromKeyPrefix` | DarkMemoryUpdater | 构建 System Prompt |
| `SettingsUIHelper.*` | RimMindMemoryMod | UI 辅助方法 |
| `PromptSection.PriorityMemory/Auxiliary` | MemoryContextProvider | 优先级常量 |

## 设置项

```csharp
// 命名空间: RimMind.Memory
public class RimMindMemorySettings : ModSettings
{
    // 触发器开关
    bool enableMemory = true;           // 总开关
    bool triggerWorkSession = true;
    bool triggerInjury = true;
    bool triggerMentalBreak = true;
    bool triggerDeath = true;
    bool triggerSkillLevelUp = true;
    bool triggerRelation = true;

    // 容量
    int maxActive = 30;                 // Pawn 活跃记忆上限
    int maxArchive = 50;                // Pawn 存档记忆上限
    int darkCount = 3;                  // Pawn 暗记忆条数（prompt 约束 + 代码强制限制）
    int narratorMaxActive = 30;
    int narratorMaxArchive = 10;
    int narratorDarkCount = 10;

    // 注入比例
    float activeInjectRatio = 0.5f;
    float archiveInjectRatio = 0.5f;
    float narratorActiveInjectRatio = 0.5f;
    float narratorArchiveInjectRatio = 0.5f;

    // 衰减
    bool enableDecay = false;
    float decayRate = 0.02f;            // 每游戏日衰减百分比
    float minImportanceThreshold = 0.05f;

    // 聚合
    int minAggregationCount = 2;
    int idleGapThresholdTicks = 6000;

    // 叙事者
    float narratorEventThreshold = 0.2f;
    float pawnToNarratorThreshold = 0.8f;

    // 请求
    int requestExpireTicks = 30000;
}
```

**所有设置项均已在 UI 中暴露控件**，包括 `narratorActiveInjectRatio`、`narratorArchiveInjectRatio`、`minAggregationCount`、`idleGapThresholdTicks`。

## 代码约定

### 命名空间

| 命名空间 | 目录 | 职责 |
|---|---|---|
| `RimMind.Memory` | Source/ 根 | Mod 入口、Settings、公开 API |
| `RimMind.Memory.Data` | Data/ | 数据结构和存储 |
| `RimMind.Memory.Injection` | Injection/ | 上下文注入 |
| `RimMind.Memory.Aggregation` | Aggregation/ | 工作会话聚合 |
| `RimMind.Memory.Triggers` | Triggers/ | 事件触发器 |
| `RimMind.Memory.Narrator` | Narrator/ | 叙事者事件 |
| `RimMind.Memory.DarkMemory` | DarkMemory/ | 暗记忆生成 |
| `RimMind.Memory.Decay` | Decay/ | 衰减管理 |
| `RimMind.Memory.Core` | Core/ | 工具类 |
| `RimMind.Memory.UI` | UI/ | 界面 |
| `RimMind.Memory.Debug` | Debug/ | 调试动作 |

### Harmony 补丁

- Harmony ID: `mcocdaa.RimMindMemory`
- 优先使用 Postfix，唯一例外: Patch_SkillLevelUp 使用 Prefix+Postfix
- UI 注入使用 Transpiler（BioTabMemoryPatch）
- 所有 Patch 类放在 `Triggers/`、`Aggregation/` 或 `Narrator/` 目录

### 错误处理

所有触发器使用 try-catch 包裹，日志前缀 `[RimMind-Memory]`：

```csharp
try { /* 记忆采集逻辑 */ }
catch (Exception ex) { Log.Warning($"[RimMind-Memory] Patch_XXX error: {ex.Message}"); }
```

### 生命周期

| 类 | 基类 | 生命周期 | 持久化 |
|---|---|---|---|
| RimMindMemoryWorldComponent | WorldComponent | World 级别 | 是（Scribe） |
| WorkSessionAggregator | GameComponent | Game 级别 | 否（新游戏/读档清空） |
| DarkMemoryUpdater | GameComponent | Game 级别 | 仅 _narratorOffset 和 _pawnJitter |

## 扩展指南

### 添加新的记忆触发器

1. 在 `Triggers/` 创建 Patch 类
2. 监听目标事件，构造 MemoryEntry
3. 调用 `store.AddActive()` 写入 Pawn 记忆
4. 如重要度 >= `settings.pawnToNarratorThreshold`，同步写入 `wc.NarratorStore`
5. 在 `RimMindMemorySettings` 添加对应 `triggerXxx` 开关
6. 在设置 UI（`RimMindMemoryMod.DrawSettingsContent`）添加开关控件

```csharp
[HarmonyPatch(typeof(TargetType), "TargetMethod")]
public static class Patch_MyTrigger
{
    static void Postfix(TargetType __instance)
    {
        if (!RimMindMemoryMod.Settings.enableMemory) return;
        if (!RimMindMemoryMod.Settings.triggerMyFeature) return;
        try
        {
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;
            var settings = RimMindMemoryMod.Settings;
            int now = Find.TickManager.TicksGame;
            float importance = 0.7f;
            string content = "事件描述";
            var store = wc.GetOrCreatePawnStore(pawn);
            store.AddActive(MemoryEntry.Create(content, MemoryType.Event, now, importance),
                settings.maxActive, settings.maxArchive);
            if (importance >= settings.pawnToNarratorThreshold)
            {
                wc.NarratorStore.AddActive(
                    MemoryEntry.Create(content, MemoryType.Event, now, importance, pawn.ThingID.ToString()),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"[RimMind-Memory] Patch_MyTrigger error: {ex.Message}");
        }
    }
}
```

### 通过 API 添加记忆（外部模组）

```csharp
RimMindMemoryAPI.AddMemory("与艾丽斯进行了深度交谈", "Dialogue", Find.TickManager.TicksGame, 0.6f, pawnId: pawn.ThingID.ToString());
```

### 添加新的工作类型到聚合

**可聚合工作**: 在 `WorkSessionAggregator.WhitelistedJobs` 添加 JobDef 名称，标签通过翻译键 `RimMind.Memory.Work.{defName}` 定义（在 `Languages/ChineseSimplified/Keyed/RimMind_Memory.xml` 中添加）。

**重要工作（不聚合）**: 在 `WorkSessionAggregator.SignificantJobs` 添加 JobDef 名称，在 `SignificantJobImportanceMap` 添加重要度映射，标签翻译键为 `RimMind.Memory.Work.Significant.{defName}`。

**忽略的工作**: 在 `WorkSessionAggregator.BlacklistedJobs` 添加 JobDef 名称。

## 依赖关系

```
RimMind-Memory
    ├── RimMind-Core (RimMindAPI, AIRequest, StructuredPromptBuilder, PromptSection, SettingsUIHelper)
    ├── Harmony (Lib.Harmony.Ref 2.*)
    ├── RimWorld 1.6 (Krafs.Rimworld.Ref)
    └── Newtonsoft.Json 13.0.* (DarkMemoryUpdater 反序列化)
```

RimMind-Memory 不依赖其他 RimMind 子模组，但会向它们提供记忆上下文。其他模组可通过 `RimMindMemoryAPI.AddMemory` 写入记忆。

## 测试

- 测试项目: `Tests/`，xUnit，目标 `net10.0`
- 直接 Compile Include 源码（非项目引用），仅覆盖不依赖 RimWorld 运行时的纯逻辑类
- `VerseStubs.cs` 提供 IExposable、Scribe_*/TaggedString/WorldComponent/Pawn 等类型桩
- 已有测试: `PawnMemoryStoreTests`（8 个）、`ImportanceDecayTests`（5 个）、`TimeFormatterTests`（7 个）

## 已知问题

（无）
