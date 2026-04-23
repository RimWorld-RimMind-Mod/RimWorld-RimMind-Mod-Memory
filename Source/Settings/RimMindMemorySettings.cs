using Verse;

namespace RimMind.Memory
{
    public class RimMindMemorySettings : ModSettings
    {
        public bool enableMemory = true;

        public bool triggerWorkSession   = true;
        public bool triggerInjury        = true;
        public bool triggerMentalBreak   = true;
        public bool triggerDeath         = true;
        public bool triggerSkillLevelUp  = true;
        public bool triggerRelation      = true;

        public int maxActive = 30;
        public int maxArchive = 50;
        public int darkCount = 3;
        public int narratorMaxActive = 30;
        public int narratorMaxArchive = 10;
        public int narratorDarkCount = 10;

        public float activeInjectRatio = 0.5f;
        public float archiveInjectRatio = 0.5f;
        public float narratorActiveInjectRatio = 0.5f;
        public float narratorArchiveInjectRatio = 0.5f;

        public bool enableDecay = false;
        public float decayRate = 0.02f;
        public float minImportanceThreshold = 0.05f;

        public int minAggregationCount = 2;
        public int idleGapThresholdTicks = 6000;
        public float narratorEventThreshold = 0.2f;
        public float pawnToNarratorThreshold = 0.8f;
        public int workingMemoryCapacity = 10;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableMemory,              "enableMemory",              true);

            Scribe_Values.Look(ref triggerWorkSession,        "triggerWorkSession",        true);
            Scribe_Values.Look(ref triggerInjury,             "triggerInjury",             true);
            Scribe_Values.Look(ref triggerMentalBreak,        "triggerMentalBreak",        true);
            Scribe_Values.Look(ref triggerDeath,              "triggerDeath",              true);
            Scribe_Values.Look(ref triggerSkillLevelUp,       "triggerSkillLevelUp",       true);
            Scribe_Values.Look(ref triggerRelation,           "triggerRelation",           true);

            Scribe_Values.Look(ref maxActive,                 "maxActive",                 30);
            Scribe_Values.Look(ref maxArchive,                "maxArchive",                50);
            Scribe_Values.Look(ref darkCount,                 "darkCount",                 3);
            Scribe_Values.Look(ref narratorMaxActive,         "narratorMaxActive",         30);
            Scribe_Values.Look(ref narratorMaxArchive,        "narratorMaxArchive",        10);
            Scribe_Values.Look(ref narratorDarkCount,         "narratorDarkCount",         10);
            Scribe_Values.Look(ref activeInjectRatio,         "activeInjectRatio",         0.5f);
            Scribe_Values.Look(ref archiveInjectRatio,        "archiveInjectRatio",        0.5f);
            Scribe_Values.Look(ref narratorActiveInjectRatio, "narratorActiveInjectRatio", 0.5f);
            Scribe_Values.Look(ref narratorArchiveInjectRatio,"narratorArchiveInjectRatio",0.5f);
            Scribe_Values.Look(ref enableDecay,               "enableDecay",               false);
            Scribe_Values.Look(ref decayRate,                 "decayRate",                 0.02f);
            Scribe_Values.Look(ref minImportanceThreshold,    "minImportanceThreshold",    0.05f);
            Scribe_Values.Look(ref minAggregationCount,       "minAggregationCount",       2);
            Scribe_Values.Look(ref idleGapThresholdTicks,     "idleGapThresholdTicks",     6000);
            Scribe_Values.Look(ref narratorEventThreshold,    "narratorEventThreshold",    0.2f);
            Scribe_Values.Look(ref pawnToNarratorThreshold,   "pawnToNarratorThreshold",   0.8f);
            Scribe_Values.Look(ref workingMemoryCapacity,     "workingMemoryCapacity",     10);
        }
    }
}
