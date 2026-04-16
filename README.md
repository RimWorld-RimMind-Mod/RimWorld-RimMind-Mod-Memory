# RimMind - Memory

三层记忆系统，自动采集游戏事件生成记忆条目，通过 AI 凝练为长期印象，为其他 RimMind 模组提供上下文支持。

## RimMind 是什么

RimMind 是一套 AI 驱动的 RimWorld 模组套件，通过接入大语言模型（LLM），让殖民者拥有人格、记忆、对话和自主决策能力。

## 子模组列表与依赖关系

| 模组 | 职责 | 依赖 | GitHub |
|------|------|------|--------|
| RimMind-Core | API 客户端、请求调度、上下文打包 | Harmony | [链接](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Core) |
| RimMind-Actions | AI 控制小人的动作执行库 | Core | [链接](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Actions) |
| RimMind-Advisor | AI 扮演小人做出工作决策 | Core, Actions | [链接](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Advisor) |
| RimMind-Dialogue | AI 驱动的对话系统 | Core | [链接](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Dialogue) |
| **RimMind-Memory** | **记忆采集与上下文注入** | Core | [链接](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Memory) |
| RimMind-Personality | AI 生成人格与想法 | Core | [链接](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Personality) |
| RimMind-Storyteller | AI 叙事者，智能选择事件 | Core | [链接](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Storyteller) |

```
Core ── Actions ── Advisor
  ├── Dialogue
  ├── Memory
  ├── Personality
  └── Storyteller
```

## 安装步骤

### 从源码安装

**Linux/macOS:**
```bash
git clone git@github.com:mcocdaa/RimWorld-RimMind-Mod-Memory.git
cd RimWorld-RimMind-Mod-Memory
./script/deploy-single.sh <your RimWorld path>
```

**Windows:**
```powershell
git clone git@github.com:mcocdaa/RimWorld-RimMind-Mod-Memory.git
cd RimWorld-RimMind-Mod-Memory
./script/deploy-single.ps1 <your RimWorld path>
```

### 从 Steam 安装

1. 安装 [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) 前置模组
2. 安装 RimMind-Core
3. 安装 RimMind-Memory
4. 在模组管理器中确保加载顺序：Harmony → Core → Memory

<!-- ![安装步骤](images/install-steps.png) -->

## 快速开始

### 填写 API Key

1. 启动游戏，进入主菜单
2. 点击 **选项 → 模组设置 → RimMind-Core**
3. 填写你的 **API Key** 和 **API 端点**
4. 填写 **模型名称**（如 `gpt-4o-mini`）
5. 点击 **测试连接**，确认显示"连接成功"

### 查看记忆

1. 进入游戏，选择一个殖民者
2. 打开 Bio 页面，点击顶部的 **"记忆"** 按钮
3. 查看活跃记忆、存档记忆和长期印象

<!-- ![记忆界面](images/screenshot-memory-bio.png) -->

## 截图展示

<!-- ![记忆日志窗口](images/screenshot-memory-log.png) -->
<!-- ![三层记忆结构](images/screenshot-memory-layers.png) -->
<!-- ![叙事者记忆](images/screenshot-narrator-memory.png) -->

## 核心功能

### 三层存储结构

每个殖民者和叙事者都有三层记忆：

- **活跃记忆**：近期事件，按时间排序，有容量上限
- **存档记忆**：按重要度排序，容量满时淘汰低重要度条目
- **长期印象（暗记忆）**：AI 每日生成，凝练为 50 字以内的摘要，永久保留

### 记忆采集

自动监听多种游戏事件：

| 事件类型 | 说明 |
|---------|------|
| 工作会话 | 搬运、建造、种植等连续工作聚合成单条记忆 |
| 受伤/患病 | 记录受伤来源和部位 |
| 精神崩溃 | 记录崩溃类型 |
| 亲近者死亡 | 有关系的小人会记录 |
| 技能升级 | 高等级技能升级更重要 |
| 关系建立 | 配偶、恋人、家人等重要关系 |
| 叙事者事件 | 袭击、婚礼、商队等殖民地级别事件 |

### 工作会话聚合

将连续同类工作聚合成单条记忆，例如"搬运 x12 约4.8游戏时"，避免记忆列表被细粒度 Job 刷屏。

### 暗记忆生成

每日调用 AI 将当日记忆凝练为长期印象。暗记忆只读展示，用于 AI 理解人物的长期状态和性格演变。

### 上下文注入

通过 RimMind-Core 的 Provider 机制，将记忆自动注入 AI Prompt，让所有子模块都能参考殖民者的历史。

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
| 暗记忆条数 | 3 | AI 生成的长期印象条数 |
| 活跃注入比例 | 50% | 活跃记忆注入 Prompt 的比例 |
| 存档注入比例 | 50% | 存档记忆注入 Prompt 的比例 |

## 常见问题

**Q: 记忆会占用很多 Token 吗？**
A: 注入比例可调。默认只注入 50% 的活跃和存档记忆，暗记忆条数也有限制（默认 3 条）。可根据 API 费用调整。

**Q: 记忆随存档保存吗？**
A: 是的。所有记忆通过 WorldComponent 随存档序列化，载入存档后自动恢复。

**Q: 可以手动添加记忆吗？**
A: 可以。在记忆日志窗口中可手动添加自定义记忆条目。

**Q: 暗记忆可以编辑吗？**
A: 暗记忆由 AI 生成，只读展示。你可以固定（Pin）活跃记忆防止被淘汰。

**Q: 配合其他模块效果如何？**
A: Memory 为所有 AI 评估提供历史上下文。配合 Personality、Advisor、Dialogue 使用时，AI 决策会更连贯。

## 致谢

本项目开发过程中参考了以下优秀的 RimWorld 模组：

- [RimTalk](https://github.com/jlibrary/RimTalk.git) - 对话系统参考
- [RimTalk-ExpandActions](https://github.com/sanguodxj-byte/RimTalk-ExpandActions.git) - 动作扩展参考
- [NewRatkin](https://github.com/solaris0115/NewRatkin.git) - 种族模组架构参考
- [VanillaExpandedFramework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework.git) - 框架设计参考

## 贡献

欢迎提交 Issue 和 Pull Request！如果你有任何建议或发现 Bug，请通过 GitHub Issues 反馈。


---

# RimMind - Memory (English)

A three-layer memory system that automatically collects game events, generates memory entries, and uses AI to distill long-term impressions, providing context for other RimMind modules.

## What is RimMind

RimMind is an AI-driven RimWorld mod suite that connects to Large Language Models (LLMs), giving colonists personality, memory, dialogue, and autonomous decision-making.

## Sub-Modules & Dependencies

| Module | Role | Depends On | GitHub |
|--------|------|------------|--------|
| RimMind-Core | API client, request dispatch, context packaging | Harmony | [Link](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Core) |
| RimMind-Actions | AI-controlled pawn action execution | Core | [Link](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Actions) |
| RimMind-Advisor | AI role-plays colonists for work decisions | Core, Actions | [Link](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Advisor) |
| RimMind-Dialogue | AI-driven dialogue system | Core | [Link](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Dialogue) |
| **RimMind-Memory** | **Memory collection & context injection** | Core | [Link](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Memory) |
| RimMind-Personality | AI-generated personality & thoughts | Core | [Link](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Personality) |
| RimMind-Storyteller | AI storyteller, smart event selection | Core | [Link](https://github.com/mcocdaa/RimWorld-RimMind-Mod-Storyteller) |

## Installation

### Install from Source

**Linux/macOS:**
```bash
git clone git@github.com:mcocdaa/RimWorld-RimMind-Mod-Memory.git
cd RimWorld-RimMind-Mod-Memory
./script/deploy-single.sh <your RimWorld path>
```

**Windows:**
```powershell
git clone git@github.com:mcocdaa/RimWorld-RimMind-Mod-Memory.git
cd RimWorld-RimMind-Mod-Memory
./script/deploy-single.ps1 <your RimWorld path>
```

### Install from Steam

1. Install [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
2. Install RimMind-Core
3. Install RimMind-Memory
4. Ensure load order: Harmony → Core → Memory

## Quick Start

### API Key Setup

1. Launch the game, go to main menu
2. Click **Options → Mod Settings → RimMind-Core**
3. Enter your **API Key** and **API Endpoint**
4. Enter your **Model Name** (e.g., `gpt-4o-mini`)
5. Click **Test Connection** to confirm

### View Memories

1. In-game, select a colonist
2. Open the Bio tab, click the **"Memory"** button at the top
3. View active memories, archive memories, and dark impressions

## Key Features

- **Three-Layer Storage**: Active (recent), Archive (by importance), Dark (AI-generated long-term impressions)
- **Auto Collection**: Monitors work sessions, injuries, mental breaks, deaths, skill ups, relationships, and narrator events
- **Work Session Aggregation**: Groups continuous similar work into single entries (e.g., "Hauling x12, ~4.8 game hours")
- **Dark Memory**: AI distills daily memories into permanent 50-char summaries
- **Context Injection**: Automatically injects memories into AI prompts for all sub-modules

## FAQ

**Q: Will memories use too many tokens?**
A: Injection ratios are adjustable. Default is 50% for active/archive, and dark memory is limited (default: 3 entries). Adjust based on API costs.

**Q: Are memories saved with the save file?**
A: Yes. All memories are serialized via WorldComponent and restored when loading saves.

**Q: Can I manually add memories?**
A: Yes. You can add custom memory entries in the memory log window.

**Q: Can I edit dark memories?**
A: Dark memories are AI-generated and read-only. You can pin active memories to prevent them from being evicted.

**Q: How does it work with other modules?**
A: Memory provides historical context for all AI evaluations. Combined with Personality, Advisor, and Dialogue, AI decisions become more coherent.

## Acknowledgments

This project references the following excellent RimWorld mods:

- [RimTalk](https://github.com/jlibrary/RimTalk.git) - Dialogue system reference
- [RimTalk-ExpandActions](https://github.com/sanguodxj-byte/RimTalk-ExpandActions.git) - Action expansion reference
- [NewRatkin](https://github.com/solaris0115/NewRatkin.git) - Race mod architecture reference
- [VanillaExpandedFramework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework.git) - Framework design reference

## Contributing

Issues and Pull Requests are welcome! If you have any suggestions or find bugs, please feedback via GitHub Issues.
