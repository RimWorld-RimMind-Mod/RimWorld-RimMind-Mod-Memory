using System.Linq;
using System.Text;
using RimMind.Core;
using RimMind.Core.Prompt;
using RimMind.Memory.Core;
using RimMind.Memory.Data;
using Verse;

namespace RimMind.Memory.Injection
{
    public static class MemoryContextProvider
    {
        public static void Register()
        {
            RimMindAPI.RegisterPawnContextProvider("memory_pawn", pawn =>
            {
                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return null;
                var store = wc.GetOrCreatePawnStore(pawn);
                if (store.IsEmpty) return null;

                var settings = RimMindMemoryMod.Settings;
                var sb = new StringBuilder("RimMind.Memory.Context.RecentMemory".Translate(pawn.Name.ToStringShort));
                sb.AppendLine();

                int activeInject = (int)(settings.maxActive * settings.activeInjectRatio);
                var fromActive = store.active.Take(activeInject).ToList();

                int archiveInject = (int)(settings.maxArchive * settings.archiveInjectRatio);
                var fromArchive = store.archive.Take(archiveInject).ToList();

                int now = Find.TickManager.TicksGame;
                foreach (var e in fromActive)
                    sb.AppendLine($"- {"RimMind.Memory.Time.TimeContent".Translate(TimeFormatter.FormatTimeAgo(e.tick, now), e.content)}");

                if (fromArchive.Count > 0)
                {
                    sb.AppendLine("RimMind.Memory.Context.ArchiveMemory".Translate(pawn.Name.ToStringShort));
                    foreach (var e in fromArchive)
                        sb.AppendLine($"- {"RimMind.Memory.Time.TimeContent".Translate(TimeFormatter.FormatTimeAgo(e.tick, now), e.content)}");
                }

                if (store.dark.Count > 0)
                {
                    sb.AppendLine("RimMind.Memory.Context.DarkMemory".Translate(pawn.Name.ToStringShort));
                    foreach (var d in store.dark)
                        sb.AppendLine($"- {d.content}");
                }

                return sb.ToString().TrimEnd();
            }, PromptSection.PriorityMemory, "RimMind-Memory");

            RimMindAPI.RegisterStaticProvider("memory_narrator", () =>
            {
                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return null!;
                var store = wc.NarratorStore;
                if (store.IsEmpty) return null!;

                var settings = RimMindMemoryMod.Settings;
                var sb = new StringBuilder("RimMind.Memory.Context.NarratorMemory".Translate());
                sb.AppendLine();

                int activeInject = (int)(settings.narratorMaxActive * settings.narratorActiveInjectRatio);
                var fromActive = store.active.Take(activeInject).ToList();

                int archiveInject = (int)(settings.narratorMaxArchive * settings.narratorArchiveInjectRatio);
                var fromArchive = store.archive.Take(archiveInject).ToList();

                int now = Find.TickManager.TicksGame;
                foreach (var e in fromActive)
                    sb.AppendLine($"- {"RimMind.Memory.Time.TimeContent".Translate(TimeFormatter.FormatTimeAgo(e.tick, now), e.content)}");

                if (fromArchive.Count > 0)
                {
                    sb.AppendLine("RimMind.Memory.Context.NarratorArchive".Translate());
                    foreach (var e in fromArchive)
                        sb.AppendLine($"- {"RimMind.Memory.Time.TimeContent".Translate(TimeFormatter.FormatTimeAgo(e.tick, now), e.content)}");
                }

                if (store.dark.Count > 0)
                {
                    sb.AppendLine("RimMind.Memory.Context.NarratorDark".Translate());
                    foreach (var d in store.dark)
                        sb.AppendLine($"- {d.content}");
                }

                return sb.ToString().TrimEnd();
            }, PromptSection.PriorityAuxiliary, "RimMind-Memory");
        }
    }
}
