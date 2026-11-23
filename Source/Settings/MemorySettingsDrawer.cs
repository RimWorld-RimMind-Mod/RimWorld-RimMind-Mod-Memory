using RimMind.Memory.Core;
using RimMind.Memory.Data;
using RimMind.Presentation.Api;
using RimMind.Presentation.UI;
using UnityEngine;
using Verse;

namespace RimMind.Memory
{
    internal static class MemorySettingsDrawer
    {
        private static Vector2 _settingsScrollPos = Vector2.zero;
        private static float _measuredHeight = 900f;

        private static RimMindMemorySettings Settings => RimMindMemoryMod.Settings;

        private static string Tr(string key, string fallback, params object[] args)
            => RimMindAPI.Text.TrOr(key, fallback, args);

        internal static void Draw(Rect inRect)
        {
            SettingsUIDrawer.DrawScrollableSettingsPage(
                inRect,
                ref _settingsScrollPos,
                ref _measuredHeight,
                DrawContent,
                ResetDefaults,
                () => Settings.Write());
        }

        private static void DrawContent(Listing_Standard listing)
        {
            listing.CheckboxLabeled(
                Tr("RimMind.Memory.Settings.EnableMemory", "Enable Memory System"),
                ref Settings.enableMemory,
                Tr("RimMind.Memory.Settings.EnableMemory.Desc", "When enabled, colonists automatically record game events as personal memories."));

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Memory.Settings.TriggerSources", "Trigger Sources"));
            listing.CheckboxLabeled(
                Tr("RimMind.Memory.Settings.TriggerWorkSession", "Work Session End"),
                ref Settings.triggerWorkSession,
                Tr("RimMind.Memory.Settings.TriggerWorkSession.Desc", "Automatically record work content as memory when a work session ends."));
            listing.CheckboxLabeled(
                Tr("RimMind.Memory.Settings.TriggerInjury", "Injury / Illness"),
                ref Settings.triggerInjury,
                Tr("RimMind.Memory.Settings.TriggerInjury.Desc", "Automatically record as memory when a colonist is injured or falls ill."));
            listing.CheckboxLabeled(
                Tr("RimMind.Memory.Settings.TriggerMentalBreak", "Mental Break"),
                ref Settings.triggerMentalBreak,
                Tr("RimMind.Memory.Settings.TriggerMentalBreak.Desc", "Automatically record as memory when a colonist has a mental break."));
            listing.CheckboxLabeled(
                Tr("RimMind.Memory.Settings.TriggerDeath", "Close One Died"),
                ref Settings.triggerDeath,
                Tr("RimMind.Memory.Settings.TriggerDeath.Desc", "Automatically record as memory when a colonist's friend or family member dies."));
            listing.CheckboxLabeled(
                Tr("RimMind.Memory.Settings.TriggerSkillLevelUp", "Skill Level Up"),
                ref Settings.triggerSkillLevelUp,
                Tr("RimMind.Memory.Settings.TriggerSkillLevelUp.Desc", "Automatically record as memory when a colonist levels up a skill."));
            listing.CheckboxLabeled(
                Tr("RimMind.Memory.Settings.TriggerRelation", "Relationship Change"),
                ref Settings.triggerRelation,
                Tr("RimMind.Memory.Settings.TriggerRelation.Desc", "Automatically record as memory when a colonist establishes or changes a relationship."));

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Memory.Settings.PawnCapacity", "Pawn Memory Capacity"));
            string maxActiveTip = Tr("RimMind.Memory.Settings.MaxActive.Desc", "Active memories are recent, high-importance memories injected into AI context.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.MaxActive", "Active Memory Limit: {0}", Settings.maxActive), maxActiveTip);
            Settings.maxActive = (int)listing.SliderWithTooltip(Settings.maxActive, 10, 100, maxActiveTip);

            string maxArchiveTip = Tr("RimMind.Memory.Settings.MaxArchive.Desc", "Archive memories are older memories with reduced importance, injected proportionally.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.MaxArchive", "Archive Memory Limit: {0}", Settings.maxArchive), maxArchiveTip);
            Settings.maxArchive = (int)listing.SliderWithTooltip(Settings.maxArchive, 10, 100, maxArchiveTip);

            string darkCountTip = Tr("RimMind.Memory.Settings.DarkCount.Desc", "Dark memories are AI-compressed long-term impressions, each no more than 50 characters.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.DarkCount", "Dark Memory Count: {0}", Settings.darkCount), darkCountTip);
            Settings.darkCount = (int)listing.SliderWithTooltip(Settings.darkCount, 1, 10, darkCountTip);

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Memory.Settings.NarratorCapacity", "Narrator Memory Capacity"));
            string nMaxActiveTip = Tr("RimMind.Memory.Settings.NarratorMaxActive.Desc", "Maximum active event records for the narrator.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.NarratorMaxActive", "Active Narrative Limit: {0}", Settings.narratorMaxActive), nMaxActiveTip);
            Settings.narratorMaxActive = (int)listing.SliderWithTooltip(Settings.narratorMaxActive, 10, 100, nMaxActiveTip);

            string nMaxArchiveTip = Tr("RimMind.Memory.Settings.NarratorMaxArchive.Desc", "Maximum archived event records for the narrator.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.NarratorMaxArchive", "Archive Narrative Limit: {0}", Settings.narratorMaxArchive), nMaxArchiveTip);
            Settings.narratorMaxArchive = (int)listing.SliderWithTooltip(Settings.narratorMaxArchive, 5, 50, nMaxArchiveTip);

            string nDarkCountTip = Tr("RimMind.Memory.Settings.NarratorDarkCount.Desc", "Number of AI-compressed long-term narratives for the narrator.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.NarratorDarkCount", "Dark Narrative Count: {0}", Settings.narratorDarkCount), nDarkCountTip);
            Settings.narratorDarkCount = (int)listing.SliderWithTooltip(Settings.narratorDarkCount, 1, 20, nDarkCountTip);

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Memory.Settings.InjectRatio", "Injection Ratios"));
            string activeInjectTip = Tr("RimMind.Memory.Settings.ActiveInjectRatio.Desc", "Ratio of active memories injected into AI context.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.ActiveInjectRatio", "Active Injection Ratio: {0}", $"{Settings.activeInjectRatio:F2}"), activeInjectTip);
            Settings.activeInjectRatio = listing.SliderWithTooltip(Settings.activeInjectRatio, 0f, 1f, activeInjectTip);

            string archiveInjectTip = Tr("RimMind.Memory.Settings.ArchiveInjectRatio.Desc", "Ratio of archive memories injected into AI context.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.ArchiveInjectRatio", "Archive Injection Ratio: {0}", $"{Settings.archiveInjectRatio:F2}"), archiveInjectTip);
            Settings.archiveInjectRatio = listing.SliderWithTooltip(Settings.archiveInjectRatio, 0f, 1f, archiveInjectTip);

            string nActiveInjectTip = Tr("RimMind.Memory.Settings.NarratorActiveInjectRatio.Desc", "Ratio of active narrator narratives injected into AI context.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.NarratorActiveInjectRatio", "Narrator Active Injection Ratio: {0}", $"{Settings.narratorActiveInjectRatio:F2}"), nActiveInjectTip);
            Settings.narratorActiveInjectRatio = listing.SliderWithTooltip(Settings.narratorActiveInjectRatio, 0f, 1f, nActiveInjectTip);

            string nArchiveInjectTip = Tr("RimMind.Memory.Settings.NarratorArchiveInjectRatio.Desc", "Ratio of archived narrator narratives injected into AI context.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.NarratorArchiveInjectRatio", "Narrator Archive Injection Ratio: {0}", $"{Settings.narratorArchiveInjectRatio:F2}"), nArchiveInjectTip);
            Settings.narratorArchiveInjectRatio = listing.SliderWithTooltip(Settings.narratorArchiveInjectRatio, 0f, 1f, nArchiveInjectTip);

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Memory.Settings.Section.Aggregation", "Work Session Aggregation"));
            string minAggTip = Tr("RimMind.Memory.Settings.MinAggregationCount.Desc", "Minimum number of same-type jobs before aggregating into a single memory.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.MinAggregationCount", "Min Aggregation Count: {0}", Settings.minAggregationCount), minAggTip);
            Settings.minAggregationCount = (int)listing.SliderWithTooltip(Settings.minAggregationCount, 1, 10, minAggTip);

            string idleGapTip = Tr("RimMind.Memory.Settings.IdleGapThreshold.Desc", "Gaps between meaningful jobs exceeding this duration are recorded as idle time.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.IdleGapThreshold", "Idle Gap Threshold: {0} game hours", $"{Settings.idleGapThresholdTicks / 2500f:F1}"), idleGapTip);
            Settings.idleGapThresholdTicks = (int)listing.SliderWithTooltip(Settings.idleGapThresholdTicks, 1500f, 30000f, idleGapTip);
            Settings.idleGapThresholdTicks = (Settings.idleGapThresholdTicks / 500) * 500;

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Memory.Settings.Section.Decay", "Memory Decay"));
            listing.CheckboxLabeled(
                Tr("RimMind.Memory.Settings.EnableDecay", "Enable Importance Decay"),
                ref Settings.enableDecay,
                Tr("RimMind.Memory.Settings.EnableDecay.Desc", "When enabled, memory importance decreases over time and memories below the threshold are removed."));
            if (Settings.enableDecay)
            {
                string decayRateTip = Tr("RimMind.Memory.Settings.DecayRate.Desc", "Percentage of importance reduction per game day.");
                listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.DecayRate", "Decay Rate: {0}%/day", $"{Settings.decayRate * 100f:F1}"), decayRateTip);
                Settings.decayRate = listing.SliderWithTooltip(Settings.decayRate, 0f, 0.2f, decayRateTip);

                string minThreshTip = Tr("RimMind.Memory.Settings.MinThreshold.Desc", "Memories with importance below this threshold are automatically removed.");
                listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.MinThreshold", "Minimum Threshold: {0}", $"{Settings.minImportanceThreshold:F3}"), minThreshTip);
                Settings.minImportanceThreshold = listing.SliderWithTooltip(Settings.minImportanceThreshold, 0f, 0.2f, minThreshTip);
            }

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Memory.Settings.CollectionControl", "Collection Control"));
            string nEventThreshTip = Tr("RimMind.Memory.Settings.NarratorEventThreshold.Desc", "Events must reach this importance threshold to be recorded by the narrator.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.NarratorEventThreshold", "Narrator Event Threshold: {0}", $"{Settings.narratorEventThreshold:F2}"), nEventThreshTip);
            Settings.narratorEventThreshold = listing.SliderWithTooltip(Settings.narratorEventThreshold, 0f, 1f, nEventThreshTip);

            string p2nThreshTip = Tr("RimMind.Memory.Settings.PawnToNarratorThreshold.Desc", "Pawn memories must reach this importance threshold to be synced to the narrator.");
            listing.LabelWithTooltip(Tr("RimMind.Memory.Settings.PawnToNarratorThreshold", "Pawn->Narrator Threshold: {0}", $"{Settings.pawnToNarratorThreshold:F2}"), p2nThreshTip);
            Settings.pawnToNarratorThreshold = listing.SliderWithTooltip(Settings.pawnToNarratorThreshold, 0f, 1f, p2nThreshTip);

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Memory.Settings.NarratorMemoryReadonly", "Narrator Memory (Read-only):"));
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null || Current.Game == null || Find.TickManager == null)
            {
                listing.Label(Tr("RimMind.Memory.Settings.NeedLoadGame", "  Load a game to view"));
            }
            else
            {
                var (activeList, archiveList, darkList) = wc.NarratorStore.GetSnapshots();
                int now = Find.TickManager.TicksGame;
                if (darkList.Count > 0)
                {
                    listing.Label(Tr("RimMind.Memory.Settings.LongTermNarrative", "  Long-term Narratives (AI-generated):"));
                    foreach (var d in darkList)
                        listing.Label($"    {d.content}");
                }
                listing.Label(Tr("RimMind.Memory.Settings.ActiveNarrative", "  Active Narratives ({0}/{1}):", activeList.Count, Settings.narratorMaxActive));
                for (int i = 0; i < activeList.Count && i < 10; i++)
                {
                    var e = activeList[i];
                    string timeStr = TimeFormatter.FormatTimeAgo(e.tick, now);
                    listing.Label($"    {Tr("RimMind.Memory.Time.TimeContent", "{0}: {1}", timeStr, e.content)}");
                }
                if (archiveList.Count > 0)
                    listing.Label(Tr("RimMind.Memory.Settings.ArchiveNarrativeCount", "  Archive Narratives: {0} entries", archiveList.Count));
            }
        }

        private static void ResetDefaults()
        {
            Settings.enableMemory = true;
            Settings.triggerWorkSession = true;
            Settings.triggerInjury = true;
            Settings.triggerMentalBreak = true;
            Settings.triggerDeath = true;
            Settings.triggerSkillLevelUp = true;
            Settings.triggerRelation = true;
            Settings.maxActive = 30;
            Settings.maxArchive = 50;
            Settings.darkCount = 3;
            Settings.narratorMaxActive = 30;
            Settings.narratorMaxArchive = 10;
            Settings.narratorDarkCount = 10;
            Settings.activeInjectRatio = 0.5f;
            Settings.archiveInjectRatio = 0.5f;
            Settings.narratorActiveInjectRatio = 0.5f;
            Settings.narratorArchiveInjectRatio = 0.5f;
            Settings.enableDecay = true;
            Settings.decayRate = 0.02f;
            Settings.minImportanceThreshold = 0.05f;
            Settings.minAggregationCount = 2;
            Settings.idleGapThresholdTicks = 6000;
            Settings.narratorEventThreshold = 0.2f;
            Settings.pawnToNarratorThreshold = 0.8f;
        }
    }
}
