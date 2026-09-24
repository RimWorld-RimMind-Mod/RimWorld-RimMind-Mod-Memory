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
    public static class WorkingMemoryProvider
    {
        // Compatibility reader for saved entries, not an active memory-production path.
        public static void Register()
        {
            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "working_memory", ContextLayer.L3_State, 0.3f,
                async (ctx, ct) =>
                {
                    if (ctx.PawnId <= 0) return null;
                    var pawn = PawnLookup.FindPawnByNumber(ctx.PawnId);
                    if (pawn == null) return null;
                    var wc = RimMindMemoryWorldComponent.Instance;
                    if (wc == null) return null;
                    var wm = wc.GetWorkingMemory(pawn);
                    if (wm == null || wm.IsEmpty) return null;

                    var sb = new StringBuilder();
                    sb.AppendLine("RimMind.Memory.Context.WorkingMemory".Translate(pawn.Name.ToStringShort));
                    string? currentJobReport = pawn.CurJob?.GetReport(pawn);
                    bool hasEntries = false;
                    foreach (var entry in wm.Entries)
                    {
                        if (!string.IsNullOrEmpty(currentJobReport) && !string.IsNullOrEmpty(entry.Content)
                            && entry.Content.IndexOf(currentJobReport, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }
                        sb.AppendLine($"- {entry.Content}");
                        hasEntries = true;
                    }

                    if (!hasEntries) return null;
                    var result = sb.ToString().TrimEnd();
                    return string.IsNullOrEmpty(result) ? null : result;
                }, RimMindOwnerConsts.MemoryModId, stalenessTicks: 750, invalidationTriggers: new[] { "MemoryEvent" }));
        }
    }
}
