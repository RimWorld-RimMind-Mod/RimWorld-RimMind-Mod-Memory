using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Api;
using RimMind.Memory.Data;
using Verse;

namespace RimMind.Memory.Injection
{
    public static class WorkingMemoryProvider
    {
        public static void Register()
        {
            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "working_memory", ContextLayer.L3_State, 0.3f,
                async (ctx, ct) =>
                {
                    if (ctx.PawnId <= 0) return null;
                    var pawn = Find.WorldPawns.AllPawnsAlive.FirstOrDefault(p => p.thingIDNumber == ctx.PawnId)
                        ?? Find.CurrentMap?.mapPawns?.FreeColonists.FirstOrDefault(p => p.thingIDNumber == ctx.PawnId);
                    if (pawn == null) return null;
                    var wc = RimMindMemoryWorldComponent.Instance;
                    if (wc == null) return null;
                    var wm = wc.GetWorkingMemory(pawn);
                    if (wm == null || wm.IsEmpty) return null;

                    var sb = new StringBuilder();
                    sb.AppendLine("RimMind.Memory.Context.WorkingMemory".Translate(pawn.Name.ToStringShort));
                    foreach (var entry in wm.Entries)
                        sb.AppendLine($"- {entry.Content}");

                    var result = sb.ToString().TrimEnd();
                    return string.IsNullOrEmpty(result) ? null : result;
                }, "RimMind-Memory", stalenessTicks: 750, invalidationTriggers: new[] { "MemoryEvent" }));
        }
    }
}
