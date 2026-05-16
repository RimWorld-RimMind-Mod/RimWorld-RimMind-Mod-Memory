using System.Collections.Generic;
using System.Text;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation;
using RimMind.Presentation.Context;
using RimMind.Application.Features.Context;
using RimMind.Memory.Data;
using Verse;

namespace RimMind.Memory.Injection
{
    public static class WorkingMemoryProvider
    {
        public static void Register()
        {
            ContextKeyRegistry.Register("working_memory", ContextLayer.L3_State, 0.3f,
                pawnObj =>
                {
                    var pawn = pawnObj as Pawn;
                    if (pawn == null) return new List<ContextEntry>();
                    var wc = RimMindMemoryWorldComponent.Instance;
                    if (wc == null) return new List<ContextEntry>();
                    var wm = wc.GetWorkingMemory(pawn);
                    if (wm == null || wm.IsEmpty) return new List<ContextEntry>();

                    var sb = new StringBuilder();
                    sb.AppendLine("RimMind.Memory.Context.WorkingMemory".Translate(pawn.Name.ToStringShort));
                    foreach (var entry in wm.Entries)
                        sb.AppendLine($"- {entry.Content}");

                    return string.IsNullOrEmpty(sb.ToString()) ? new List<ContextEntry>() : new List<ContextEntry> { new ContextEntry(sb.ToString().TrimEnd()) };
                }, "RimMind.Memory");
        }
    }
}
