using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RimMind.Memory.Aggregation
{
    public enum JobMemoryKind
    {
        Unknown = 0,
        Ignored = 1,
        SignificantSingle = 2,
        Aggregatable = 3
    }

    public readonly struct JobMemoryRule
    {
        public JobMemoryKind Kind { get; }
        public float Importance { get; }

        public JobMemoryRule(JobMemoryKind kind, float importance = 0f)
        {
            Kind = kind;
            Importance = importance;
        }
    }

    /// <summary>
    /// Data-driven single source of truth for classifying RimWorld job defNames into
    /// ignored, significant single-event, or aggregatable work-session memories.
    /// External mods may register or override rules at runtime via <see cref="RegisterRule"/>.
    /// </summary>
    public static class JobMemoryPolicy
    {
        private static readonly ConcurrentDictionary<string, JobMemoryRule> Rules = BuildDefaultRules();

        private static ConcurrentDictionary<string, JobMemoryRule> BuildDefaultRules()
        {
            var map = new Dictionary<string, JobMemoryRule>(StringComparer.Ordinal);

            // Ignored ambient / movement / non-work jobs
            string[] ignoredJobs =
            {
                "Wait", "Wait_MaintainPosture", "Wait_Asleep", "Wait_Downed",
                "Wait_SafeTemperature", "Wait_Wander", "Wait_Combat",
                "Goto", "GotoWander", "GotoSafeTemperature",
                "SocialFight", "MarryAdjacentPawn", "Lovin", "StandAndStare",
                "IdleWhileDespawned"
            };
            foreach (var job in ignoredJobs)
                map[job] = new JobMemoryRule(JobMemoryKind.Ignored);

            // Significant single-occurrence jobs recorded immediately as events
            (string Name, float Importance)[] significantJobs =
            {
                ("AttackMelee", 0.9f),
                ("AttackStatic", 0.9f),
                ("Rescue", 0.8f),
                ("TendPatient", 0.8f),
                ("FeedPatient", 0.7f),
                ("DeliverFood", 0.6f),
                ("Slaughter", 0.6f),
                ("Tame", 0.7f),
                ("Train", 0.6f),
                ("Milk", 0.5f),
                ("Shear", 0.5f),
                ("PrisonerAttemptRecruit", 0.7f)
            };
            foreach (var (name, importance) in significantJobs)
                map[name] = new JobMemoryRule(JobMemoryKind.SignificantSingle, importance);

            // Repetitive work jobs aggregated into work sessions
            string[] aggregatableJobs =
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
                "ClearSnow", "ManTurret"
            };
            foreach (var job in aggregatableJobs)
                map[job] = new JobMemoryRule(JobMemoryKind.Aggregatable);

            return new ConcurrentDictionary<string, JobMemoryRule>(map, StringComparer.Ordinal);
        }

        public static void RegisterRule(string jobDefName, JobMemoryRule rule)
        {
            if (string.IsNullOrWhiteSpace(jobDefName)) return;
            Rules[jobDefName] = rule;
        }

        public static JobMemoryRule Resolve(string? jobDefName)
        {
            if (string.IsNullOrEmpty(jobDefName))
                return new JobMemoryRule(JobMemoryKind.Unknown);

            return Rules.TryGetValue(jobDefName!, out var rule)
                ? rule
                : new JobMemoryRule(JobMemoryKind.Unknown);
        }
    }
}
