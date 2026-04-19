# RimMind - Memory

三层记忆系统，让 AI 拥有"往事历历"的时间感知能力。

## 核心能力

**活跃记忆 (Active)** - 最近发生的重要事件，完整保留细节，随时可供 AI 查询。

**归档记忆 (Archive)** - 较久之前的事件，保留核心信息但精简细节，平衡信息量与 Token 消耗。

**深层记忆 (Dark)** - 永久保留的关键印象（AI 每日凝练生成），随时间沉淀但不会遗忘。条数由设置项 `darkCount` 控制，代码与 prompt 双重保障。

**工作会话聚合** - 将连续的同类工作（如"挖矿 3 小时"）聚合成一条记忆，避免信息爆炸。最小聚合次数和空闲间隔阈值均可配置。

**重要性衰减** - 模拟真实记忆，普通琐事随时间淡化（每游戏日按比例衰减），重大事件长久留存。

**上下文注入** - 记忆自动注入 AI Prompt，让所有 RimMind 模块都能参考殖民者的历史。Pawn 和叙事者的注入比例均可独立配置。

## 记录内容

- 工作会话（搬运、建造、种植等连续工作）
- 击杀记录（谁杀死了什么）
- 关系变化（成为恋人、分手、宿敌）
- 技能升级里程碑
- 健康问题（疾病、受伤）
- 精神崩溃事件
- 殖民地级别事件（袭击、商队等）
- 自定义手动记录

## 设置项

| 设置 | 默认值 | 说明 |
|------|--------|------|
| 启用记忆系统 | 开启 | 总开关 |
| 工作会话 | 开启 | 采集工作相关记忆 |
| 受伤/患病 | 开启 | 采集健康相关记忆 |
| 精神崩溃 | 开启 | 采集精神事件 |
| 亲近者死亡 | 开启 | 采集死亡事件 |
| 技能升级 | 开启 | 采集技能提升 |
| 关系变化 | 开启 | 采集关系建立 |
| 活跃记忆上限 | 30 | 每人近期记忆条数 |
| 存档记忆上限 | 50 | 每人存档记忆条数 |
| 暗记忆条数 | 3 | AI 生成的长期印象条数（代码+prompt 双重限制） |
| 活跃叙事上限 | 30 | 叙事者活跃叙事条数 |
| 存档叙事上限 | 10 | 叙事者存档叙事条数 |
| 暗叙事条数 | 10 | 叙事者 AI 压缩长期叙事条数 |
| 活跃注入比例 | 50% | Pawn 活跃记忆注入 Prompt 的比例 |
| 存档注入比例 | 50% | Pawn 存档记忆注入 Prompt 的比例 |
| 叙事者活跃注入比例 | 50% | 叙事者活跃叙事注入 Prompt 的比例 |
| 叙事者存档注入比例 | 50% | 叙事者存档叙事注入 Prompt 的比例 |
| 最小聚合次数 | 2 | 同类工作达到此次数后才聚合为一条记忆 |
| 空闲间隔阈值 | 2.4 游戏时 | 间隔超过此时长记录为休息/待机 |
| 启用重要度衰减 | 关闭 | 记忆重要度随时间降低 |
| 衰减速率 | 2%/天 | 每游戏日重要度降低的百分比 |
| 最低阈值 | 0.05 | 低于此阈值的记忆自动归档 |
| 叙事者事件阈值 | 0.2 | 事件重要度达到此阈值才被叙事者记录 |
| 小人→叙事者阈值 | 0.8 | Pawn 记忆重要度达到此阈值才同步到叙事者 |
| 请求过期 | 0.5 游戏天 | 暗记忆生成请求超时自动取消 |

## 建议配图

1. 记忆日志窗口截图（展示三层记忆结构）
2. 生物标签页中记忆面板的展示
3. 暗记忆（AI 凝练的长期印象）展示

---

# RimMind - Memory (English)

A three-layer memory system giving AI the ability to perceive time through "vivid recollections of the past".

## Key Features

**Active Memory** - Recent important events, fully preserved with details, always available for AI queries.

**Archive Memory** - Older events, retaining core information but with streamlined details, balancing information volume and token consumption.

**Dark Memory** - Permanently preserved key impressions (AI-distilled daily) that settle over time but are never forgotten. Count controlled by `darkCount` setting with dual enforcement via code and prompt.

**Work Session Aggregation** - Aggregates continuous similar work (e.g., "mining for 3 hours") into a single memory entry, preventing information overload. Min aggregation count and idle gap threshold are configurable.

**Importance Decay** - Simulates real memory: mundane matters fade over time (percentage reduction per game day), while significant events remain.

**Context Injection** - Memories are automatically injected into AI prompts, allowing all RimMind modules to reference colonist history. Pawn and narrator injection ratios are independently configurable.

## Recorded Content

- Work sessions (hauling, building, planting, etc.)
- Kill records (who killed what)
- Relationship changes (lovers, breakups, rivals)
- Skill upgrade milestones
- Health issues (illnesses, injuries)
- Mental break events
- Colony-level events (raids, traders, etc.)
- Custom manual entries

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Enable Memory System | On | Master switch |
| Work Session | On | Collect work-related memories |
| Injury / Illness | On | Collect health-related memories |
| Mental Break | On | Collect mental break events |
| Close One Died | On | Collect death events |
| Skill Level Up | On | Collect skill upgrades |
| Relationship Change | On | Collect relationship changes |
| Active Memory Limit | 30 | Max active memories per pawn |
| Archive Memory Limit | 50 | Max archived memories per pawn |
| Dark Memory Count | 3 | AI-generated long-term impressions (code + prompt dual limit) |
| Active Narrative Limit | 30 | Max active narrator narratives |
| Archive Narrative Limit | 10 | Max archived narrator narratives |
| Dark Narrative Count | 10 | AI-compressed long-term narratives for narrator |
| Active Injection Ratio | 50% | Ratio of pawn active memories injected into prompt |
| Archive Injection Ratio | 50% | Ratio of pawn archived memories injected into prompt |
| Narrator Active Injection Ratio | 50% | Ratio of narrator active narratives injected into prompt |
| Narrator Archive Injection Ratio | 50% | Ratio of narrator archived narratives injected into prompt |
| Min Aggregation Count | 2 | Min same-type jobs before aggregating into one memory |
| Idle Gap Threshold | 2.4 game hours | Gaps exceeding this are recorded as idle time |
| Enable Importance Decay | Off | Memory importance decreases over time |
| Decay Rate | 2%/day | Importance reduction percentage per game day |
| Minimum Threshold | 0.05 | Memories below this are auto-archived |
| Narrator Event Threshold | 0.2 | Min importance for narrator to record an event |
| Pawn→Narrator Threshold | 0.8 | Min importance for pawn memory to sync to narrator |
| Request Expiry | 0.5 game days | Dark memory requests auto-cancel after this time |

## Suggested Screenshots

1. Memory log window (showing three-layer structure)
2. Memory panel in bio tab
3. Dark memory (AI-distilled long-term impressions)
