<div align="center">

# RimMind-Memory 🧠
### 3-Tier Cognitive Memory Hierarchy & Temporal Context Engine for RimWorld 1.6 & 1.7

**English** | [简体中文](README_zh.md)

<p>
  <a href="https://rimworldgame.com/"><img src="https://img.shields.io/badge/RimWorld-1.6%20%7C%201.7-brightgreen.svg" alt="RimWorld 1.6 | 1.7"></a>
  <a href="https://github.com/RimWorld-RimMind-Mod/RimWorld-RimMind-Mod-Core"><img src="https://img.shields.io/badge/Dependency-RimMind--Core-blue.svg" alt="Dependency: RimMind-Core"></a>
  <a href="#"><img src="https://img.shields.io/badge/Unit%20Tests-Passing-success.svg" alt="Unit Tests"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
</p>

<p><em>Endow colonists with long-term memory, episodic life chronicles, and natural emotional decay.</em></p>

</div>

---

## 📖 Overview

**RimMind-Memory** implements a biologically inspired, multi-tiered cognitive memory system for RimWorld. Rather than overwhelming LLM prompts with unfiltered event dumps, memories are structured, filtered, and consolidated across distinct cognitive layers.

### 3-Tier Architecture
1. **Active Memory (Tier 1)**: Chronological record of recent work sessions, combat/medical milestones, social relations, injuries, and colony incidents (`maxActive`, default 30).
2. **Archive Memory (Tier 2)**: Importance-ranked episodic store retaining overflowed milestones while pruning lowest-importance noise (`maxArchive`, default 50).
3. **Deep / Dark Memory (Tier 3)**: Pinned long-term psychological impressions synthesized daily via staggered AI consolidation (`DarkMemoryUpdater`, under 50 characters each) and aligned with Core's KV-cache-friendly context layers.

---

## 🎮 In-Game Showcase

![RimMind-Memory Showcase](docs/images/showcase.jpg)
*AI Context Payload Inspector (`Analysis` tab): Inspecting the 4-Zone prompt architecture breakdown (`System 0.9%`, `Static 0.7%`, `Dynamic 4.9%`, `Tools 93.4%`), `~95.1%` KV-cache prefix hit stability, and Zone 2 colonist profile/memory alignment.*

---

## 🏛️ Memory Lifecycle & Decay Flow

```mermaid
flowchart TD
    RawEvent["In-Game Event (Work / Combat / Relations / Incidents)"] --> Policy["JobMemoryPolicy & Event Triggers"]
    Policy --> Active["Active Tier (Chronological Recent Events)"]
    Active --> Overflow{"Capacity Overflow or Daily Decay"}
    Overflow -- Low Importance --> Decay["Natural Importance Decay & Pruning"]
    Overflow -- High Importance --> Archive["Archive Tier (Importance-Ranked Episodes)"]
    Active --> DailyConsolidation["Daily Staggered AI Consolidation (DarkMemoryUpdater)"]
    DailyConsolidation --> Dark["Deep / Dark Tier (Pinned Long-Term Impressions)"]
    Dark --> Prompt["Injected into Core Context (memory_pawn / memory_narrator)"]
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
