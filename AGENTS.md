# AGENTS.md — RimMind-Memory

三层记忆系统(Active/Archive/Dark)，追踪小人活动与地图事件，为AI Prompt提供时间上下文。

## Start here

先读 `Source/README.md`。通常只需继续打开公共 Bridge、WorldComponent、目标 Store、生产者或 Provider 中的一条路径，不要从所有 Harmony Patch 开始搜索。

## 项目定位

监听游戏事件(工作/受伤/死亡/技能/关系/叙事者)生成MemoryEntry → PawnMemoryStore(三层存储+溢出淘汰) + NarratorMemoryStore → 工作会话聚合(连续同类合并) → 暗记忆每日AI生成(≤50字/条) → 重要度衰减(可选) → ContextKeyRegistry注入上下文。公开 `RimMindMemoryAPI.AddMemory` 供其他模组写入，`GetNarratorMemories` 读取叙事者记忆。

依赖: Core(编译期)，其他模组通过API/反射桥接消费记忆。

## 构建

| 项 | 值 |
|----|-----|
| Target | net48, C#9.0, Nullable enable |
| Output | `../1.6/Assemblies/` |
| Assembly | RimMindMemory, RootNS: RimMind.Memory |
| 依赖 | RimMindCore.dll, Krafs.Rimworld.Ref, Lib.Harmony.Ref, Newtonsoft.Json |

## 源码结构

```
Source/
├── README.md                                      入口到测试的运行时阅读地图
├── RimMindMemoryMod.cs / RimMindMemoryAPI.cs      组合根 + 公开兼容API
├── Settings/
│   ├── RimMindMemorySettings.cs                   25项设置与存档字段
│   └── MemorySettingsDrawer.cs                    原生/Core设置页共享绘制实现
├── Data/
│   ├── MemoryEntry.cs                           记忆条目(MemoryType: Work/Event/Manual/Dark, content≤2000字)
│   ├── MemoryStoreBase.cs                       三层存储基类(active/archive/dark + AddActive + EnforceLimit)
│   ├── PawnMemoryStore.cs                       继承MemoryStoreBase(默认AddIfNotExists→active)
│   ├── NarratorMemoryStore.cs                   继承MemoryStoreBase(AddIfNotExists带isActive参数,Storyteller反射依赖)
│   └── RimMindMemoryWorldComponent.cs           WorldComponent管理所有存储+WorkingMemory+远端同步
├── WorkingMemory/                               工作记忆缓冲区(容量可配置, 已序列化, UpdateCapacity)
│   ├── WorkingMemory.cs                         滚动缓冲区(capacity可动态更新)
│   └── WorkingMemoryEntry.cs                    工作记忆条目(content/source/relevance)
├── Injection/MemoryContextProvider.cs           注册ContextKey(memory_pawn/memory_narrator)
│       WorkingMemoryProvider.cs                 注册ContextKey(working_memory)
├── Aggregation/
│   ├── WorkSessionAggregator.cs                 GameComponent工作聚合(不持久化, CleanupPawnJitter)
│   └── Patch_StartJob_Memory.cs                 JobTracker Postfix
├── Triggers/                                    5个Patch(AddHediff/Kill/MentalBreak/SkillLevelUp/AddRelation)
│       MemoryTriggerHelper.cs                   触发器辅助(ShouldProcess+WriteMemory+TryUpgradeToNarrator)
├── Narrator/Patch_IncidentWorker.cs             叙事者事件Postfix
├── DarkMemory/DarkMemoryUpdater.cs              每日暗记忆生成(ScenarioIds.Memory, RimMindAPI.RequestStructured)
│                 DarkMemoryResultParserPure.cs    纯逻辑解析器(生产+测试共用,JSON修复+解析)
├── Decay/ImportanceDecayManager.cs              衰减管理(默认关闭,单MemoryStoreBase重载)
├── Core/TimeFormatter.cs + ImportanceDecayCalculator.cs + PawnLookup.cs
├── UI/BioTabMemoryPatch.cs + Dialog_MemoryLog.cs
└── Debug/MemoryDebugActions.cs
```

## 记忆触发来源

| 来源 | 重要度 | 写入目标 |
|------|--------|---------|
| 工作会话 | 0.4-0.5 | Pawn + 条件升级叙事者(≥0.8) |
| 重要工作(Rescue/Attack等) | 0.5-0.9 | Pawn + 条件升级 |
| 受伤/患病 | 0.5-0.9 | Pawn + 条件升级 |
| 精神崩溃 | 0.7-0.95 | Pawn + 条件升级 |
| 亲近者死亡 | 0.85-1.0 | 相关Pawn + 叙事者 |
| 技能升级 | 0.5-0.7 | Pawn + 条件升级 |
| 关系建立 | 0.6-0.95 | 双向Pawn + 条件升级 |
| 叙事者事件 | 0.3-1.0 | 仅叙事者 |
| 手动/外部API | 调用方指定 | Pawn |

## ContextKey 注册

| Key | Layer | Priority | 内容 |
|-----|-------|----------|------|
| memory_pawn | L3_State | 0.25 | Pawn的active+archive+dark |
| memory_narrator | L4_History | 0.6 | 叙事者active+archive+dark |
| working_memory | L3_State | 0.3 | Pawn工作记忆 |

## Core API 使用情况

| API | 用途 | 状态 |
|-----|------|------|
| ContextKeyRegistry.Register | 注入记忆上下文 | ✅ 使用中 |
| RimMindAPI.RequestStructured | 暗记忆AI生成 | ✅ 使用中 |
| SchemaRegistry.DarkMemoryOutput | 暗记忆JSON Schema | ✅ 使用中 |
| RimMindAPI.RegisterSettingsTab | 设置页标签 | ✅ 使用中 |
| RimMindAPI.RegisterModCooldown | 冷却注册 | ✅ 使用中 |
| RimMindAPI.IsConfigured | API可用性检查 | ✅ 使用中 |
| PromptSanitizer.Sanitize | Prompt清洗 | ✅ 使用中 |
| StorageDriverFactory.GetDriver | 远端存储 | ✅ 使用中(SaveAll+LoadAll+PutAsync) |
| JsonRepairHelper.TryRepairTruncatedJson | JSON修复 | ✅ 使用中 |
| ScenarioIds.Memory | 暗记忆场景 | ✅ 使用中 |
| AgentBus.Publish/Subscribe | 事件总线 | ❌ 未使用 |
| RimMindAPI.PublishPerception | 感知广播 | ❌ 未使用 |
| RimMindAPI.RegisterAgentIdentityProvider | 身份注册 | ❌ 未使用 |
| TaskInstructionBuilder.Build | 结构化Prompt | ❌ 未使用 |
| IStorageDriver.QueryMemoriesAsync | 语义搜索 | ❌ 未使用 |

## 代码约定

- Harmony ID: `mcocdaa.RimMindMemory`，PostFix优先
- 所有触发器try-catch包裹，日志前缀 `[RimMind-Memory]`
- 新触发器在 `RimMindMemorySettings` 添加 `triggerXxx` 开关
- 记忆写入: `MemoryTriggerHelper.WriteMemory(pawn, content, type, importance)` (自动处理wc获取+Pawn写入+条件Narrator升级)
- 直接写入(无Narrator升级): `MemoryTriggerHelper.WriteMemory(pawn, content, type, importance, upgradeNarrator: false)`
- 存储基类: `MemoryStoreBase`(AddActive/EnforceLimit/IsEmpty/ContainsId/AddIfNotExists/ExposeData)
- 重要度≥`pawnToNarratorThreshold`(默认0.8)时同步写入 `NarratorStore`
- 单例模式: `Instance => _instance ?? throw new InvalidOperationException(...)`
- 暗记忆场景: `ScenarioIds.Memory`（非 Personality/Storyteller）
- MemoryEntry.Create 自动截断 content 至 2000 字符
- 设置入口只转发到 `MemorySettingsDrawer`；Mod 入口不承载控件布局

## 操作边界

### ✅ 必须做
- 新触发器在Settings添加开关 + UI暴露控件
- 新工作类型在对应HashSet添加 + 翻译XML添加 `RimMind.Memory.Work.{defName}`
- 记忆写入用try-catch

### ⚠️ 先询问
- 修改 `memory_narrator` Provider优先级(当前L4_History 0.6)
- 修改容量默认值(`maxActive=30`/`maxArchive=50`)
- 修改暗记忆生成的AI请求参数

### 🚫 绝对禁止
- 后台线程调用 `RimMindAPI.RequestStructured`
- 修改 `MemoryType` 枚举值(影响已持久化数据)
- 绕过 `RimMindMemoryAPI.AddMemory` 直接操作WorldComponent
- DarkMemoryUpdater Prompt构建硬编码文本(应通过翻译键)
