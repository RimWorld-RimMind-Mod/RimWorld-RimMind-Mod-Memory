<div align="center">

# RimMind-Memory 🧠
### 3-Tier Cognitive Memory Hierarchy & Temporal Context Engine for RimWorld 1.6

**English** | [简体中文](README_zh.md)

<p>
  <a href="https://rimworldgame.com/"><img src="https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg" alt="RimWorld 1.6"></a>
  <a href="https://github.com/mcocdaa/RimWorld-RimMind-Mod-Core"><img src="https://img.shields.io/badge/Dependency-RimMind--Core-blue.svg" alt="Dependency: RimMind-Core"></a>
  <a href="#"><img src="https://img.shields.io/badge/Unit%20Tests-Passing-success.svg" alt="Unit Tests"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
</p>

<p><em>Endow colonists with long-term memory, episodic life chronicles, and natural emotional decay.</em></p>

</div>

---

## 📖 Overview

**RimMind-Memory** implements a biologically inspired, multi-tiered cognitive memory system for RimWorld. Rather than overwhelming LLM prompts with unfiltered event dumps, memories are structured, filtered, and consolidated across distinct cognitive layers.

### 3-Tier Architecture
1. **Sensory & Working Buffer (L1)**: Captures immediate environmental sensations and short-lived interactions. Automatically decays after several hours.
2. **Episodic Daily Chronicle (L2)**: Records daily milestones, battle triumphs, tragic losses, and relationship shifts in concise narrative entries.
3. **Reflective & Dark Memories (L3)**: Enduring convictions, traumas, and life philosophy shaped by major colony events (e.g., witnessing cannibalism, surviving a betrayal, or falling in love).

---

## 🎮 In-Game Showcase

![RimMind-Memory Showcase](docs/images/showcase.jpg)
*Memory inspection window: Viewing a colonist's multi-layered memory chronicle, showing decayed sensory impressions alongside pinned life milestones.*

---

## 🏛️ Memory Lifecycle & Decay Flow

```mermaid
flowchart TD
    RawEvent["In-Game Event (Raid / Wedding / Trauma)"] --> L1["Sensory Buffer (Immediate Context)"]
    L1 --> Filter{"Importance Filter"}
    Filter -- Low Significance --> Decay["Natural Forgetting & Pruning"]
    Filter -- High Significance --> L2["Episodic Memory Consolidation"]
    L2 --> Synthesis["Sleep / Meditation Synthesis"]
    Synthesis --> L3["Deep Reflection & Permanent Personality Imprint"]
    L3 --> Prompt["Injected into Core Zone 2 Context"]
```

---

## 🛠️ Installation & Load Order

```text
1. Harmony
2. Core (Vanilla RimWorld)
3. RimMind-Core
4. RimMind-Memory
```

---

## 🧪 Developer Guide & Testing

Run unit tests directly:

```powershell
dotnet test RimMind-Memory/Tests/RimMindMemory.Tests.csproj -c Release
```

---

## 📜 License

Licensed under the [MIT License](LICENSE).
