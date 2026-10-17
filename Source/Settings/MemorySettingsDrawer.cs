using RimMind.Memory.Core;
using RimMind.Memory.Data;
using RimMind.Presentation.UI;
using UnityEngine;
using Verse;

namespace RimMind.Memory
{
    internal static class MemorySettingsDrawer
    {
        private static Vector2 _settingsScrollPos = Vector2.zero;

        private static RimMindMemorySettings Settings => RimMindMemoryMod.Settings;

        internal static void Draw(Rect inRect)
        {
            Rect contentArea = SettingsUIDrawer.SplitContentArea(inRect);
            Rect bottomBar = SettingsUIDrawer.SplitBottomBar(inRect);

            float contentH = EstimateSettingsHeight();
            Rect viewRect = new Rect(0f, 0f, contentArea.width - 16f, contentH);
            Widgets.BeginScrollView(contentArea, ref _settingsScrollPos, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.CheckboxLabeled("RimMind.Memory.Settings.EnableMemory".Translate(), ref Settings.enableMemory,
                "RimMind.Memory.Settings.EnableMemory.Desc".Translate());

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Memory.Settings.TriggerSources".Translate());
            listing.CheckboxLabeled("RimMind.Memory.Settings.TriggerWorkSession".Translate(), ref Settings.triggerWorkSession,
                "RimMind.Memory.Settings.TriggerWorkSession.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Memory.Settings.TriggerInjury".Translate(), ref Settings.triggerInjury,
                "RimMind.Memory.Settings.TriggerInjury.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Memory.Settings.TriggerMentalBreak".Translate(), ref Settings.triggerMentalBreak,
                "RimMind.Memory.Settings.TriggerMentalBreak.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Memory.Settings.TriggerDeath".Translate(), ref Settings.triggerDeath,
                "RimMind.Memory.Settings.TriggerDeath.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Memory.Settings.TriggerSkillLevelUp".Translate(), ref Settings.triggerSkillLevelUp,
                "RimMind.Memory.Settings.TriggerSkillLevelUp.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Memory.Settings.TriggerRelation".Translate(), ref Settings.triggerRelation,
                "RimMind.Memory.Settings.TriggerRelation.Desc".Translate());

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Memory.Settings.PawnCapacity".Translate());
            string maxActiveTip = "RimMind.Memory.Settings.MaxActive.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.MaxActive".Translate(Settings.maxActive), maxActiveTip);
            Settings.maxActive = (int)listing.SliderWithTooltip(Settings.maxActive, 10, 100, maxActiveTip);

            string maxArchiveTip = "RimMind.Memory.Settings.MaxArchive.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.MaxArchive".Translate(Settings.maxArchive), maxArchiveTip);
            Settings.maxArchive = (int)listing.SliderWithTooltip(Settings.maxArchive, 10, 100, maxArchiveTip);

            string darkCountTip = "RimMind.Memory.Settings.DarkCount.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.DarkCount".Translate(Settings.darkCount), darkCountTip);
            Settings.darkCount = (int)listing.SliderWithTooltip(Settings.darkCount, 1, 10, darkCountTip);

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Memory.Settings.NarratorCapacity".Translate());
            string nMaxActiveTip = "RimMind.Memory.Settings.NarratorMaxActive.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.NarratorMaxActive".Translate(Settings.narratorMaxActive), nMaxActiveTip);
            Settings.narratorMaxActive = (int)listing.SliderWithTooltip(Settings.narratorMaxActive, 10, 100, nMaxActiveTip);

            string nMaxArchiveTip = "RimMind.Memory.Settings.NarratorMaxArchive.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.NarratorMaxArchive".Translate(Settings.narratorMaxArchive), nMaxArchiveTip);
            Settings.narratorMaxArchive = (int)listing.SliderWithTooltip(Settings.narratorMaxArchive, 5, 50, nMaxArchiveTip);

            string nDarkCountTip = "RimMind.Memory.Settings.NarratorDarkCount.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.NarratorDarkCount".Translate(Settings.narratorDarkCount), nDarkCountTip);
            Settings.narratorDarkCount = (int)listing.SliderWithTooltip(Settings.narratorDarkCount, 1, 20, nDarkCountTip);

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Memory.Settings.InjectRatio".Translate());
            string activeInjectTip = "RimMind.Memory.Settings.ActiveInjectRatio.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.ActiveInjectRatio".Translate($"{Settings.activeInjectRatio:F2}"), activeInjectTip);
            Settings.activeInjectRatio = listing.SliderWithTooltip(Settings.activeInjectRatio, 0f, 1f, activeInjectTip);

            string archiveInjectTip = "RimMind.Memory.Settings.ArchiveInjectRatio.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.ArchiveInjectRatio".Translate($"{Settings.archiveInjectRatio:F2}"), archiveInjectTip);
            Settings.archiveInjectRatio = listing.SliderWithTooltip(Settings.archiveInjectRatio, 0f, 1f, archiveInjectTip);

            string nActiveInjectTip = "RimMind.Memory.Settings.NarratorActiveInjectRatio.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.NarratorActiveInjectRatio".Translate($"{Settings.narratorActiveInjectRatio:F2}"), nActiveInjectTip);
            Settings.narratorActiveInjectRatio = listing.SliderWithTooltip(Settings.narratorActiveInjectRatio, 0f, 1f, nActiveInjectTip);

            string nArchiveInjectTip = "RimMind.Memory.Settings.NarratorArchiveInjectRatio.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.NarratorArchiveInjectRatio".Translate($"{Settings.narratorArchiveInjectRatio:F2}"), nArchiveInjectTip);
            Settings.narratorArchiveInjectRatio = listing.SliderWithTooltip(Settings.narratorArchiveInjectRatio, 0f, 1f, nArchiveInjectTip);

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Memory.Settings.Section.Aggregation".Translate());
            string minAggTip = "RimMind.Memory.Settings.MinAggregationCount.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.MinAggregationCount".Translate(Settings.minAggregationCount), minAggTip);
            Settings.minAggregationCount = (int)listing.SliderWithTooltip(Settings.minAggregationCount, 1, 10, minAggTip);

            string idleGapTip = "RimMind.Memory.Settings.IdleGapThreshold.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.IdleGapThreshold".Translate($"{Settings.idleGapThresholdTicks / 2500f:F1}"), idleGapTip);
            Settings.idleGapThresholdTicks = (int)listing.SliderWithTooltip(Settings.idleGapThresholdTicks, 1500f, 30000f, idleGapTip);
            Settings.idleGapThresholdTicks = (Settings.idleGapThresholdTicks / 500) * 500;

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Memory.Settings.Section.Decay".Translate());
            listing.CheckboxLabeled("RimMind.Memory.Settings.EnableDecay".Translate(), ref Settings.enableDecay,
                "RimMind.Memory.Settings.EnableDecay.Desc".Translate());
            if (Settings.enableDecay)
            {
                string decayRateTip = "RimMind.Memory.Settings.DecayRate.Desc".Translate();
                listing.LabelWithTooltip("RimMind.Memory.Settings.DecayRate".Translate($"{Settings.decayRate * 100f:F1}"), decayRateTip);
                Settings.decayRate = listing.SliderWithTooltip(Settings.decayRate, 0f, 0.2f, decayRateTip);

                string minThreshTip = "RimMind.Memory.Settings.MinThreshold.Desc".Translate();
                listing.LabelWithTooltip("RimMind.Memory.Settings.MinThreshold".Translate($"{Settings.minImportanceThreshold:F3}"), minThreshTip);
                Settings.minImportanceThreshold = listing.SliderWithTooltip(Settings.minImportanceThreshold, 0f, 0.2f, minThreshTip);
            }

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Memory.Settings.CollectionControl".Translate());
            string nEventThreshTip = "RimMind.Memory.Settings.NarratorEventThreshold.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.NarratorEventThreshold".Translate($"{Settings.narratorEventThreshold:F2}"), nEventThreshTip);
            Settings.narratorEventThreshold = listing.SliderWithTooltip(Settings.narratorEventThreshold, 0f, 1f, nEventThreshTip);

            string p2nThreshTip = "RimMind.Memory.Settings.PawnToNarratorThreshold.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Memory.Settings.PawnToNarratorThreshold".Translate($"{Settings.pawnToNarratorThreshold:F2}"), p2nThreshTip);
            Settings.pawnToNarratorThreshold = listing.SliderWithTooltip(Settings.pawnToNarratorThreshold, 0f, 1f, p2nThreshTip);

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Memory.Settings.NarratorMemoryReadonly".Translate());
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null)
            {
                listing.Label("RimMind.Memory.Settings.NeedLoadGame".Translate());
            }
            else
            {
                var nStore = wc.NarratorStore;
                int now = Find.TickManager.TicksGame;
                if (nStore.dark.Count > 0)
                {
                    listing.Label("RimMind.Memory.Settings.LongTermNarrative".Translate());
                    foreach (var d in nStore.dark)
                        listing.Label($"    {d.content}");
                }
                listing.Label("RimMind.Memory.Settings.ActiveNarrative".Translate(nStore.active.Count, Settings.narratorMaxActive));
                for (int i = 0; i < nStore.active.Count && i < 10; i++)
                {
                    var e = nStore.active[i];
                    string timeStr = TimeFormatter.FormatTimeAgo(e.tick, now);
                    listing.Label($"    {"RimMind.Memory.Time.TimeContent".Translate(timeStr, e.content)}");
                }
                if (nStore.archive.Count > 0)
                    listing.Label("RimMind.Memory.Settings.ArchiveNarrativeCount".Translate(nStore.archive.Count));
            }

            listing.End();
            Widgets.EndScrollView();

            SettingsUIDrawer.DrawBottomBar(bottomBar, () =>
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
            });

            Settings.Write();
        }

        private static float EstimateSettingsHeight()
        {
            float h = 30f;
            h += 24f;
            h += 24f + 24f * 6;
            h += 24f + 56f * 3;
            h += 24f + 56f * 3;
            h += 24f + 56f * 4;
            h += 24f + 56f * 2;
            h += 24f + 24f;
            if (Settings.enableDecay)
                h += 56f * 2;
            h += 24f + 56f * 2;

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc != null)
            {
                var nStore = wc.NarratorStore;
                if (nStore.dark.Count > 0)
                    h += 24f + nStore.dark.Count * 24f;
                h += 24f + Mathf.Min(nStore.active.Count, 10) * 24f;
                if (nStore.archive.Count > 0)
                    h += 24f;
            }
            else
            {
                h += 24f;
            }

            return h + 40f;
        }
    }
}
