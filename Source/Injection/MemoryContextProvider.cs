using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimMind.Memory.Core;
using RimMind.Memory.Data;
using Verse;

namespace RimMind.Memory.Injection
{
    public static class MemoryContextProvider
    {
        private const string PublicProviderOwner = "RimMind.Memory";
        private const int PublicProviderPriority = 100;

        private static string Tr(string key, params object[] args)
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return string.Empty;
                string template = key.CanTranslate() ? key.TranslateSimple() : key;
                return (args == null || args.Length == 0) ? template : string.Format(template, args);
            }
            catch { return key ?? string.Empty; }
        }

        public static void Register()
        {
            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "memory_pawn", ContextLayer.L3_State, 0.25f,
                async (ctx, ct) =>
                {
                    try
                    {
                        if (ctx.PawnId <= 0) return null;
                        if (Current.Game == null) return null;
                        var pawn = PawnLookup.FindPawnByNumber(ctx.PawnId);
                        if (pawn == null) return null;
                        var wc = RimMindMemoryWorldComponent.Instance;
                        if (wc == null) return null;
                        var store = wc.GetPawnStore(pawn);
                        if (store == null || store.IsEmpty) return null;

                        var (activeList, archiveList, darkList) = store.GetSnapshots();
                        if (activeList.Count == 0 && archiveList.Count == 0 && darkList.Count == 0) return null;

                        var settings = RimMindMemoryMod.Settings;
                        string pawnShortName = pawn.Name?.ToStringShort ?? ("Pawn#" + pawn.thingIDNumber);
                        var sb = new StringBuilder(Tr("RimMind.Memory.Context.RecentMemory", pawnShortName));
                        sb.AppendLine();

                        int? recipientId = RimMindAPI.Chat.GetActiveDialogueRecipient(pawn.thingIDNumber);
                        Pawn? recipient = recipientId.HasValue && recipientId.Value > 0 ? PawnLookup.FindPawnByNumber(recipientId.Value) : null;
                        string? targetPawnId = recipient?.thingIDNumber.ToString();
                        string? targetName = recipient?.Name?.ToStringShort;

                        int now = Find.TickManager?.TicksGame ?? 0;
                        int activeInject = (int)(settings.maxActive * settings.activeInjectRatio);
                        var fromActive = MemoryRetrievalScorer.SelectTopScored(activeList, activeInject, now, targetPawnId, targetName);

                        int archiveInject = (int)(settings.maxArchive * settings.archiveInjectRatio);
                        var fromArchive = MemoryRetrievalScorer.SelectTopScored(archiveList, archiveInject, now, targetPawnId, targetName);

                        var seenActiveLines = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var e in fromActive)
                        {
                            string line = $"- {Tr("RimMind.Memory.Time.TimeContent", TimeFormatter.FormatTimeAgo(e.tick, now), e.content)}";
                            if (seenActiveLines.Add(line))
                                sb.AppendLine(line);
                        }

                        if (fromArchive.Count > 0)
                        {
                            var seenArchiveLines = new HashSet<string>(StringComparer.Ordinal);
                            sb.AppendLine(Tr("RimMind.Memory.Context.ArchiveMemory", pawnShortName));
                            foreach (var e in fromArchive)
                            {
                                string line = $"- {Tr("RimMind.Memory.Time.TimeContent", TimeFormatter.FormatTimeAgo(e.tick, now), e.content)}";
                                if (seenArchiveLines.Add(line))
                                    sb.AppendLine(line);
                            }
                        }

                        if (darkList.Count > 0)
                        {
                            var seenDarkLines = new HashSet<string>(StringComparer.Ordinal);
                            sb.AppendLine(Tr("RimMind.Memory.Context.DarkMemory", pawnShortName));
                            foreach (var d in darkList)
                            {
                                string line = $"- {d.content}";
                                if (seenDarkLines.Add(line))
                                    sb.AppendLine(line);
                            }
                        }

                        return sb.ToString().TrimEnd();
                    }
                    catch (Exception ex)
                    {
                        RimMindErrors.Warn($"[RimMind-Memory] memory_pawn provider error: {ex.Message}");
                        return null;
                    }
                }, RimMindOwnerConsts.MemoryModId, stalenessTicks: 1500, invalidationTriggers: new[] { "MemoryEvent" }));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "memory_narrator", ContextLayer.L4_History, 0.6f,
                async (ctx, ct) =>
                {
                    try
                    {
                        if (Current.Game == null) return null;
                        var wc = RimMindMemoryWorldComponent.Instance;
                        if (wc == null) return null;
                        var store = wc.NarratorStore;
                        if (store == null || store.IsEmpty) return null;

                        var (activeList, archiveList, darkList) = store.GetSnapshots();
                        if (activeList.Count == 0 && archiveList.Count == 0 && darkList.Count == 0) return null;

                        var settings = RimMindMemoryMod.Settings;
                        var sb = new StringBuilder(Tr("RimMind.Memory.Context.NarratorMemory"));
                        sb.AppendLine();

                        int now = Find.TickManager?.TicksGame ?? 0;
                        int activeInject = (int)(settings.narratorMaxActive * settings.narratorActiveInjectRatio);
                        var fromActive = MemoryRetrievalScorer.SelectTopScored(activeList, activeInject, now);

                        int archiveInject = (int)(settings.narratorMaxArchive * settings.narratorArchiveInjectRatio);
                        var fromArchive = MemoryRetrievalScorer.SelectTopScored(archiveList, archiveInject, now);

                        var seenNarratorActive = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var e in fromActive)
                        {
                            string line = $"- {Tr("RimMind.Memory.Time.TimeContent", TimeFormatter.FormatTimeAgo(e.tick, now), e.content)}";
                            if (seenNarratorActive.Add(line))
                                sb.AppendLine(line);
                        }

                        if (fromArchive.Count > 0)
                        {
                            var seenNarratorArchive = new HashSet<string>(StringComparer.Ordinal);
                            sb.AppendLine(Tr("RimMind.Memory.Context.NarratorArchive"));
                            foreach (var e in fromArchive)
                            {
                                string line = $"- {Tr("RimMind.Memory.Time.TimeContent", TimeFormatter.FormatTimeAgo(e.tick, now), e.content)}";
                                if (seenNarratorArchive.Add(line))
                                    sb.AppendLine(line);
                            }
                        }

                        if (darkList.Count > 0)
                        {
                            var seenNarratorDark = new HashSet<string>(StringComparer.Ordinal);
                            sb.AppendLine(Tr("RimMind.Memory.Context.NarratorDark"));
                            foreach (var d in darkList)
                            {
                                string line = $"- {d.content}";
                                if (seenNarratorDark.Add(line))
                                    sb.AppendLine(line);
                            }
                        }

                        return sb.ToString().TrimEnd();
                    }
                    catch (Exception ex)
                    {
                        RimMindErrors.Warn($"[RimMind-Memory] memory_narrator provider error: {ex.Message}");
                        return null;
                    }
                }, RimMindOwnerConsts.MemoryModId, stalenessTicks: 3000, invalidationTriggers: new[] { "MemoryEvent" }));

            RegisterPublicProviders();
        }

        private static void RegisterPublicProviders()
        {
            RimMindAPI.Providers.RegisterPawnProvider(
                "memory.pawn_brief",
                PublicProviderOwner,
                BuildPawnBrief,
                PublicProviderPriority,
                overrideExisting: true);

            RimMindAPI.Providers.RegisterStaticProvider(
                "memory.narrator_brief",
                PublicProviderOwner,
                BuildNarratorBrief,
                PublicProviderPriority);
        }

        private static string BuildPawnBrief(Pawn pawn)
        {
            if (pawn == null || Current.Game == null) return string.Empty;
            var store = RimMindMemoryWorldComponent.Instance?.GetPawnStore(pawn);
            if (store == null || store.IsEmpty) return string.Empty;

            var sb = new StringBuilder("[RimMind Memory]");
            foreach (var memory in store.active.Take(5))
                sb.AppendLine($"- {memory.content}");

            if (store.dark.Count > 0)
            {
                sb.AppendLine("[Long-term]");
                foreach (var memory in store.dark.Take(5))
                    sb.AppendLine($"- {memory.content}");
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildNarratorBrief()
        {
            if (Current.Game == null) return string.Empty;
            var store = RimMindMemoryWorldComponent.Instance?.NarratorStore;
            if (store == null || store.IsEmpty) return string.Empty;

            var sb = new StringBuilder("[RimMind Storyteller]");
            foreach (var memory in store.active.Take(5))
                sb.AppendLine($"- {memory.content}");
            return sb.ToString().TrimEnd();
        }
    }
}
