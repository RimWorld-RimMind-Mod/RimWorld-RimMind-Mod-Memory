using System;
using System.Collections.Generic;
using RimMind.Memory.Data;
using RimMind.Memory.Injection;
using RimMind.Memory.Triggers;
using RimMind.Presentation.Api;
using Verse;
using Verse.AI;

namespace RimMind.Memory.Aggregation
{
    public class WorkSessionAggregator : GameComponent
    {
        private static WorkSessionAggregator? _instance;
        public static WorkSessionAggregator? Instance => Current.Game != null ? _instance : null;

        private readonly Dictionary<int, PawnSession> _sessions = new Dictionary<int, PawnSession>();

        private const int SessionTimeoutTicks = 2500;
        private const int PruneIntervalTicks = 60000;
        private int _lastPruneTick;

        public WorkSessionAggregator(Game game) { _instance = this; }

        public override void StartedNewGame()
        {
            _sessions.Clear();
            _lastPruneTick = 0;
            Patch_AddHediff.ClearCooldowns();
        }

        public override void LoadedGame()
        {
            _sessions.Clear();
            _lastPruneTick = 0;
            Patch_AddHediff.ClearCooldowns();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (_sessions.Count == 0) return;
            int now = Find.TickManager?.TicksGame ?? 0;
            if (_lastPruneTick == 0)
            {
                _lastPruneTick = now;
                return;
            }
            if (now >= _lastPruneTick && (now - _lastPruneTick) < PruneIntervalTicks)
                return;

            _lastPruneTick = now;
            var activeIds = new HashSet<int>();
            if (Find.Maps != null)
            {
                foreach (var map in Find.Maps)
                {
                    var colonists = RimMindPawnLookup.GetSpawnedColonistsSafe(map);
                    for (int i = 0; i < colonists.Count; i++)
                    {
                        var p = colonists[i];
                        if (p != null && RimMindPawnLookup.IsActiveSpawnedColonist(p))
                            activeIds.Add(p.thingIDNumber);
                    }
                }
            }
            PruneInactiveSessions(activeIds);
        }

        public int PruneInactiveSessions(ISet<int> retainedPawnIds)
        {
            return PruneSessionDictionary(_sessions, retainedPawnIds);
        }

        public static int PruneSessionDictionary<TSession>(Dictionary<int, TSession> sessions, ISet<int> retainedPawnIds)
        {
            if (sessions == null || sessions.Count == 0) return 0;
            var toRemove = new List<int>();
            foreach (var id in sessions.Keys)
            {
                if (retainedPawnIds == null || !retainedPawnIds.Contains(id))
                    toRemove.Add(id);
            }
            for (int i = 0; i < toRemove.Count; i++)
                sessions.Remove(toRemove[i]);
            return toRemove.Count;
        }

        public void OnJobStarted(Pawn pawn, Job job, int maxActive, int maxArchive, int idleGapThresholdTicks, int minAggregationCount)
        {
            if (pawn == null || job == null || job.def == null) return;
            int now = Find.TickManager.TicksGame;
            int pawnId = pawn.thingIDNumber;
            string jobDefName = job.def.defName;

            JobMemoryRule rule = JobMemoryPolicy.Resolve(jobDefName);
            if (rule.Kind == JobMemoryKind.Ignored)
                return;

            if (rule.Kind == JobMemoryKind.SignificantSingle)
            {
                RecordSignificantJob(pawn, jobDefName, rule.Importance, now, maxActive, maxArchive);
                UpdateLastMeaningfulJob(pawnId, now);
                return;
            }

            if (rule.Kind != JobMemoryKind.Aggregatable)
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
            string content = RimMindAPI.Text.TrOr(
                "RimMind.Memory.Work.Session",
                "{0} x{1}  ~{2} game hours",
                JobLabel(session.currentJobDef),
                session.count.ToString(),
                $"{hours:F1}");

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { session.Reset(); return; }

            wc.AddPawnMemory(pawn, MemoryEntry.Create(content, MemoryType.Work, now, importance), maxActive, maxArchive);

            TryUpgradeToNarrator(pawn, content, now, importance, wc);

            session.Reset();
        }

        private void RecordSignificantJob(Pawn pawn, string jobDefName, float importance, int now, int maxActive, int maxArchive)
        {
            string content = JobLabel(jobDefName);

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            wc.AddPawnMemory(pawn, MemoryEntry.Create(content, MemoryType.Event, now, importance), maxActive, maxArchive);

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
                string content = RimMindAPI.Text.TrOr(
                    "RimMind.Memory.Work.Idle",
                    "Idle/Standby ~{0} game hours",
                    $"{hours:F1}");
                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc != null)
                {
                    var store = wc.GetPawnStore(pawn);
                    bool updatedExisting = false;
                    if (store != null && !string.IsNullOrEmpty(session.lastIdleEntryId))
                    {
                        lock (store.SyncRoot)
                        {
                            if (store.active.Count > 0
                                && store.active[0].type == MemoryType.Work
                                && store.active[0].id == session.lastIdleEntryId)
                            {
                                store.active[0].content = content;
                                store.active[0].tick = now;
                                updatedExisting = true;
                            }
                        }
                    }

                    if (updatedExisting)
                    {
                        MemoryChangedEvent.Publish(pawnId);
                    }
                    else
                    {
                        var idleEntry = MemoryEntry.Create(content, MemoryType.Work, now, 0.1f);
                        session.lastIdleEntryId = idleEntry.id;
                        wc.AddPawnMemory(pawn, idleEntry, maxActive, maxArchive);
                    }
                }
                session.lastMeaningfulJobTick = now;
            }
        }

        private void UpdateLastMeaningfulJob(int pawnId, int now)
        {
            if (!_sessions.TryGetValue(pawnId, out var session))
            {
                session = new PawnSession();
                _sessions[pawnId] = session;
            }
            session.lastMeaningfulJobTick = now;
            session.lastIdleEntryId = null;
        }

        private void TryUpgradeToNarrator(Pawn pawn, string content, int now, float importance, RimMindMemoryWorldComponent wc)
        {
            var settings = RimMindMemoryMod.Settings;
            if (importance >= settings.pawnToNarratorThreshold)
            {
                string pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort;
                string narratorContent = $"[{pawnName}] {content}";
                wc.AddNarratorMemory(
                    MemoryEntry.Create(narratorContent, MemoryType.Event, now, importance, pawn.ThingID),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
        }

        private static string JobLabel(string defName)
        {
            return RimMindAPI.Text.TrOr($"RimMind.Memory.Work.{defName}", defName);
        }

        private class PawnSession
        {
            public string? currentJobDef;
            public int startTick;
            public int lastJobTick;
            public int count;
            public int totalTicks;
            public int lastMeaningfulJobTick;
            public string? lastIdleEntryId;

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
