using HarmonyLib;
using RimMind.Core;
using RimMind.Core.UI;
using RimMind.Memory.Core;
using RimMind.Memory.Data;
using RimMind.Memory.Injection;
using UnityEngine;
using Verse;

namespace RimMind.Memory
{
    public class RimMindMemoryMod : Mod
    {
        public static RimMindMemorySettings Settings = null!;
        private static Vector2 _settingsScrollPos = Vector2.zero;

        public RimMindMemoryMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimMindMemorySettings>();
            new Harmony("mcocdaa.RimMindMemory").PatchAll();

            MemoryContextProvider.Register();
            RimMindAPI.RegisterSettingsTab("memory", () => "RimMind.Memory.Settings.TabLabel".Translate(), DrawSettingsContent);
            RimMindAPI.RegisterModCooldown("DarkMemory", () => 60000);
            Log.Message("[RimMind-Memory] Initialized.");
        }

        public override string SettingsCategory() => "RimMind - Memory";

        public override void DoSettingsWindowContents(Rect rect)
        {
            DrawSettingsContent(rect);
        }

        internal static void DrawSettingsContent(Rect inRect)
        {
            Rect contentArea = SettingsUIHelper.SplitContentArea(inRect);
            Rect bottomBar  = SettingsUIHelper.SplitBottomBar(inRect);

            float contentH = EstimateSettingsHeight();
            Rect viewRect = new Rect(0f, 0f, contentArea.width - 16f, contentH);
            Widgets.BeginScrollView(contentArea, ref _settingsScrollPos, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.CheckboxLabeled("RimMind.Memory.Settings.EnableMemory".Translate(), ref Settings.enableMemory,
                "RimMind.Memory.Settings.EnableMemory.Desc".Translate());

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.TriggerSources".Translate());
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

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.PawnCapacity".Translate());
            listing.Label("RimMind.Memory.Settings.MaxActive".Translate(Settings.maxActive));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.MaxActive.Desc".Translate());
            GUI.color = Color.white;
            Settings.maxActive = (int)listing.Slider(Settings.maxActive, 10, 100);
            listing.Label("RimMind.Memory.Settings.MaxArchive".Translate(Settings.maxArchive));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.MaxArchive.Desc".Translate());
            GUI.color = Color.white;
            Settings.maxArchive = (int)listing.Slider(Settings.maxArchive, 10, 100);
            listing.Label("RimMind.Memory.Settings.DarkCount".Translate(Settings.darkCount));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.DarkCount.Desc".Translate());
            GUI.color = Color.white;
            Settings.darkCount = (int)listing.Slider(Settings.darkCount, 1, 10);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.NarratorCapacity".Translate());
            listing.Label("RimMind.Memory.Settings.NarratorMaxActive".Translate(Settings.narratorMaxActive));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.NarratorMaxActive.Desc".Translate());
            GUI.color = Color.white;
            Settings.narratorMaxActive = (int)listing.Slider(Settings.narratorMaxActive, 10, 100);
            listing.Label("RimMind.Memory.Settings.NarratorMaxArchive".Translate(Settings.narratorMaxArchive));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.NarratorMaxArchive.Desc".Translate());
            GUI.color = Color.white;
            Settings.narratorMaxArchive = (int)listing.Slider(Settings.narratorMaxArchive, 5, 50);
            listing.Label("RimMind.Memory.Settings.NarratorDarkCount".Translate(Settings.narratorDarkCount));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.NarratorDarkCount.Desc".Translate());
            GUI.color = Color.white;
            Settings.narratorDarkCount = (int)listing.Slider(Settings.narratorDarkCount, 1, 20);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.InjectRatio".Translate());
            listing.Label("RimMind.Memory.Settings.ActiveInjectRatio".Translate($"{Settings.activeInjectRatio:F2}"));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.ActiveInjectRatio.Desc".Translate());
            GUI.color = Color.white;
            Settings.activeInjectRatio = listing.Slider(Settings.activeInjectRatio, 0f, 1f);
            listing.Label("RimMind.Memory.Settings.ArchiveInjectRatio".Translate($"{Settings.archiveInjectRatio:F2}"));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.ArchiveInjectRatio.Desc".Translate());
            GUI.color = Color.white;
            Settings.archiveInjectRatio = listing.Slider(Settings.archiveInjectRatio, 0f, 1f);
            listing.Label("RimMind.Memory.Settings.NarratorActiveInjectRatio".Translate($"{Settings.narratorActiveInjectRatio:F2}"));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.NarratorActiveInjectRatio.Desc".Translate());
            GUI.color = Color.white;
            Settings.narratorActiveInjectRatio = listing.Slider(Settings.narratorActiveInjectRatio, 0f, 1f);
            listing.Label("RimMind.Memory.Settings.NarratorArchiveInjectRatio".Translate($"{Settings.narratorArchiveInjectRatio:F2}"));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.NarratorArchiveInjectRatio.Desc".Translate());
            GUI.color = Color.white;
            Settings.narratorArchiveInjectRatio = listing.Slider(Settings.narratorArchiveInjectRatio, 0f, 1f);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.Section.Aggregation".Translate());
            listing.Label("RimMind.Memory.Settings.MinAggregationCount".Translate(Settings.minAggregationCount));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.MinAggregationCount.Desc".Translate());
            GUI.color = Color.white;
            Settings.minAggregationCount = (int)listing.Slider(Settings.minAggregationCount, 1, 10);
            listing.Label("RimMind.Memory.Settings.IdleGapThreshold".Translate($"{Settings.idleGapThresholdTicks / 2500f:F1}"));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.IdleGapThreshold.Desc".Translate());
            GUI.color = Color.white;
            Settings.idleGapThresholdTicks = (int)listing.Slider(Settings.idleGapThresholdTicks, 1500f, 30000f);
            Settings.idleGapThresholdTicks = (Settings.idleGapThresholdTicks / 500) * 500;

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.Section.Decay".Translate());
            listing.CheckboxLabeled("RimMind.Memory.Settings.EnableDecay".Translate(), ref Settings.enableDecay,
                "RimMind.Memory.Settings.EnableDecay.Desc".Translate());
            if (Settings.enableDecay)
            {
                listing.Label("RimMind.Memory.Settings.DecayRate".Translate($"{Settings.decayRate * 100f:F1}"));
                GUI.color = Color.gray;
                listing.Label("  " + "RimMind.Memory.Settings.DecayRate.Desc".Translate());
                GUI.color = Color.white;
                Settings.decayRate = listing.Slider(Settings.decayRate, 0f, 0.2f);
                listing.Label("RimMind.Memory.Settings.MinThreshold".Translate($"{Settings.minImportanceThreshold:F3}"));
                GUI.color = Color.gray;
                listing.Label("  " + "RimMind.Memory.Settings.MinThreshold.Desc".Translate());
                GUI.color = Color.white;
                Settings.minImportanceThreshold = listing.Slider(Settings.minImportanceThreshold, 0f, 0.2f);
            }

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.CollectionControl".Translate());
            listing.Label("RimMind.Memory.Settings.NarratorEventThreshold".Translate($"{Settings.narratorEventThreshold:F2}"));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.NarratorEventThreshold.Desc".Translate());
            GUI.color = Color.white;
            Settings.narratorEventThreshold = listing.Slider(Settings.narratorEventThreshold, 0f, 1f);
            listing.Label("RimMind.Memory.Settings.PawnToNarratorThreshold".Translate($"{Settings.pawnToNarratorThreshold:F2}"));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.PawnToNarratorThreshold.Desc".Translate());
            GUI.color = Color.white;
            Settings.pawnToNarratorThreshold = listing.Slider(Settings.pawnToNarratorThreshold, 0f, 1f);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.Section.Request".Translate());
            listing.Label("RimMind.Memory.Settings.RequestExpire".Translate($"{Settings.requestExpireTicks / 60000f:F2}"));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Memory.Settings.RequestExpire.Desc".Translate());
            GUI.color = Color.white;
            Settings.requestExpireTicks = (int)listing.Slider(Settings.requestExpireTicks, 3600f, 120000f);
            Settings.requestExpireTicks = (Settings.requestExpireTicks / 1500) * 1500;

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Memory.Settings.NarratorMemoryReadonly".Translate());
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

            SettingsUIHelper.DrawBottomBar(bottomBar, () =>
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
                Settings.enableDecay = false;
                Settings.decayRate = 0.02f;
                Settings.minImportanceThreshold = 0.05f;
                Settings.minAggregationCount = 2;
                Settings.idleGapThresholdTicks = 6000;
                Settings.narratorEventThreshold = 0.2f;
                Settings.pawnToNarratorThreshold = 0.8f;
                Settings.requestExpireTicks = 30000;
            });

            Settings.Write();
        }

        private static float EstimateSettingsHeight()
        {
            float h = 30f;
            h += 24f;
            h += 24f + 24f * 6;
            h += 24f + 24f + 32f + 24f + 32f + 24f + 32f;
            h += 24f + 24f + 32f + 24f + 32f + 24f + 32f;
            h += 24f + 24f + 32f + 24f + 32f + 24f + 32f + 24f + 32f;
            h += 24f + 24f + 32f;
            if (Settings.enableDecay)
                h += 24f + 32f + 24f + 32f;
            h += 24f + 24f + 32f + 24f + 32f;
            h += 24f + 24f;

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
