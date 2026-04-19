using System;
using System.Collections.Generic;
using RimMind.Memory.Data;
using RimMind.Memory.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimMind.Memory.Aggregation
{
    public class WorkSessionAggregator : GameComponent
    {
        private static WorkSessionAggregator? _instance;
        public static WorkSessionAggregator Instance => _instance!;

        private readonly Dictionary<int, PawnSession> _sessions = new Dictionary<int, PawnSession>();

        private const int SessionTimeoutTicks = 2500;

        public WorkSessionAggregator(Game game) { _instance = this; }

        public override void StartedNewGame() { _sessions.Clear(); }
        public override void LoadedGame()     { _sessions.Clear(); }

        public void OnJobStarted(Pawn pawn, Job job, int maxActive, int maxArchive, int idleGapThresholdTicks, int minAggregationCount)
        {
            if (pawn == null || job == null) return;
            int now = Find.TickManager.TicksGame;
            int pawnId = pawn.thingIDNumber;
            string jobDefName = job.def.defName;

            if (IsBlacklisted(jobDefName))
            {
                if (IsSignificantSingleJob(jobDefName))
                {
                    RecordSignificantJob(pawn, jobDefName, now, maxActive, maxArchive);
                    UpdateLastMeaningfulJob(pawnId, now);
                }
                return;
            }

            if (!IsWhitelisted(jobDefName))
            {
                CheckIdleGap(pawn, pawnId, now, idleGapThresholdTicks, maxActive, maxArchive);
                return;
            }

            if (!_sessions.TryGetValue(pawnId, out var session))
            {
                session = new PawnSession();
                _sessions[pawnId] = session;
            }

            CheckIdleGap(pawn, pawnId, now, idleGapThresholdTicks, maxActive, maxArchive);

            if (session.currentJobDef != null && session.currentJobDef == jobDefName
                && session.totalTicks >= SessionTimeoutTicks)
            {
                FlushSession(pawn, session, now, maxActive, maxArchive, minAggregationCount);
            }

            if (session.currentJobDef != null && session.currentJobDef != jobDefName)
            {
                FlushSession(pawn, session, now, maxActive, maxArchive, minAggregationCount);
            }

            if (session.currentJobDef == null || now - session.lastJobTick > SessionTimeoutTicks)
            {
                if (session.currentJobDef != null)
                    FlushSession(pawn, session, now, maxActive, maxArchive, minAggregationCount);
                session.currentJobDef = jobDefName;
                session.startTick = now;
                session.count = 0;
                session.totalTicks = 0;
            }

            session.count++;
            session.lastJobTick = now;
            session.totalTicks = now - session.startTick;

            UpdateLastMeaningfulJob(pawnId, now);
        }

        private void FlushSession(Pawn pawn, PawnSession session, int now, int maxActive, int maxArchive, int minAggregationCount)
        {
            if (session.currentJobDef == null) return;
            if (session.count < minAggregationCount) { session.Reset(); return; }

            float hours = session.totalTicks / 2500f;
            float importance = session.totalTicks > 15000 ? 0.5f : 0.4f;
            string content = "RimMind.Memory.Work.Session".Translate(
                JobLabel(session.currentJobDef), session.count.ToString(), $"{hours:F1}");

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { session.Reset(); return; }

            var store = wc.GetOrCreatePawnStore(pawn);
            store.AddActive(MemoryEntry.Create(content, MemoryType.Work, now, importance), maxActive, maxArchive);

            TryUpgradeToNarrator(pawn, content, now, importance, wc);

            session.Reset();
        }

        private void RecordSignificantJob(Pawn pawn, string jobDefName, int now, int maxActive, int maxArchive)
        {
            float importance = GetSignificantJobImportance(jobDefName);
            string content = GetSignificantJobLabel(jobDefName);

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            var store = wc.GetOrCreatePawnStore(pawn);
            store.AddActive(MemoryEntry.Create(content, MemoryType.Event, now, importance), maxActive, maxArchive);

            TryUpgradeToNarrator(pawn, content, now, importance, wc);
        }

        private void CheckIdleGap(Pawn pawn, int pawnId, int now, int idleGapThresholdTicks, int maxActive, int maxArchive)
        {
            if (!_sessions.TryGetValue(pawnId, out var session)) return;
            if (session.lastMeaningfulJobTick <= 0) return;

            int gap = now - session.lastMeaningfulJobTick;
            if (gap > idleGapThresholdTicks)
            {
                float hours = gap / 2500f;
                string content = "RimMind.Memory.Work.Idle".Translate($"{hours:F1}");
                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc != null)
                {
                    var store = wc.GetOrCreatePawnStore(pawn);
                    store.AddActive(MemoryEntry.Create(content, MemoryType.Work, now, 0.3f), maxActive, maxArchive);
                }
                session.lastMeaningfulJobTick = now;
            }
        }

        private void UpdateLastMeaningfulJob(int pawnId, int now)
        {
            if (!_sessions.TryGetValue(pawnId, out var session)) return;
            session.lastMeaningfulJobTick = now;
        }

        private void TryUpgradeToNarrator(Pawn pawn, string content, int now, float importance, RimMindMemoryWorldComponent wc)
        {
            var settings = RimMindMemoryMod.Settings;
            if (importance >= settings.pawnToNarratorThreshold)
            {
                string narratorContent = $"[{pawn.Name.ToStringShort}] {content}";
                wc.NarratorStore.AddActive(
                    MemoryEntry.Create(narratorContent, MemoryType.Event, now, importance, pawn.ThingID),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
        }

        private static readonly HashSet<string> BlacklistedJobs = new HashSet<string>
        {
            "Wait", "Wait_MaintainPosture", "Wait_Asleep", "Wait_Downed",
            "Wait_SafeTemperature", "Wait_Wander", "Wait_Combat",
            "Goto", "GotoWander", "GotoSafeTemperature",
            "AttackMelee", "AttackStatic", "SocialFight",
            "MarryAdjacentPawn", "Lovin", "StandAndStare",
            "IdleWhileDespawned",
        };

        private static readonly HashSet<string> SignificantJobs = new HashSet<string>
        {
            "AttackMelee", "AttackStatic",
            "Rescue", "TendPatient", "FeedPatient", "DeliverFood",
            "Slaughter", "Tame", "Train", "Milk", "Shear",
            "PrisonerAttemptRecruit",
        };

        private static readonly HashSet<string> WhitelistedJobs = new HashSet<string>
        {
            "Sow", "PlantSeed", "Replant",
            "Harvest", "HarvestDesignated",
            "CutPlant", "CutPlantDesignated",
            "FinishFrame", "PlaceNoCostFrame",
            "Deconstruct", "Uninstall",
            "Repair", "FixBrokenDownBuilding",
            "Mine", "Clean",
            "HaulToCell", "HaulToContainer",
            "HaulToTransporter", "HaulToPortal",
            "HaulCorpseToPublicPlace",
            "DoBill", "Research", "Hunt",
            "BeatFire", "ExtinguishSelf",
            "SmoothFloor", "RemoveFloor", "SmoothWall",
            "BuildRoof", "RemoveRoof",
            "PaintBuilding", "PaintFloor",
            "RemovePaintBuilding", "RemovePaintFloor",
            "OperateDeepDrill", "OperateScanner",
            "ExtractTree", "Refuel", "RearmTurret",
            "ClearSnow", "ManTurret",
        };

        private static readonly Dictionary<string, float> SignificantJobImportanceMap = new Dictionary<string, float>
        {
            { "AttackMelee", 0.9f }, { "AttackStatic", 0.9f },
            { "Rescue", 0.8f }, { "TendPatient", 0.8f },
            { "FeedPatient", 0.7f }, { "DeliverFood", 0.6f },
            { "Slaughter", 0.6f }, { "Tame", 0.7f },
            { "Train", 0.6f }, { "Milk", 0.5f }, { "Shear", 0.5f },
            { "PrisonerAttemptRecruit", 0.7f },
        };

        private static bool IsBlacklisted(string defName) => BlacklistedJobs.Contains(defName);

        private static bool IsWhitelisted(string defName) => WhitelistedJobs.Contains(defName);

        private static bool IsSignificantSingleJob(string defName) => SignificantJobs.Contains(defName);

        private static float GetSignificantJobImportance(string defName)
            => SignificantJobImportanceMap.TryGetValue(defName, out var v) ? v : 0.6f;

        private static string GetSignificantJobLabel(string defName)
        {
            string key = $"RimMind.Memory.Work.{defName}";
            return key.CanTranslate() ? key.Translate() : defName;
        }

        private static string JobLabel(string defName)
        {
            string key = $"RimMind.Memory.Work.{defName}";
            return key.CanTranslate() ? key.Translate() : defName;
        }

        private class PawnSession
        {
            public string? currentJobDef;
            public int startTick;
            public int lastJobTick;
            public int count;
            public int totalTicks;
            public int lastMeaningfulJobTick;

            public void Reset()
            {
                currentJobDef = null;
                startTick = 0;
                lastJobTick = 0;
                count = 0;
                totalTicks = 0;
            }
        }
    }
}
