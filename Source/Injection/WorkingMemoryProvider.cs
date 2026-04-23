using System.Text;
using RimMind.Core;
using RimMind.Core.Prompt;
using RimMind.Memory.Data;
using Verse;

namespace RimMind.Memory.Injection
{
    public static class WorkingMemoryProvider
    {
        public static void Register()
        {
            RimMindAPI.RegisterPawnContextProvider("working_memory", pawn =>
            {
                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return null;
                var wm = wc.GetWorkingMemory(pawn);
                if (wm == null || wm.IsEmpty) return null;

                var sb = new StringBuilder();
                sb.AppendLine("RimMind.Memory.Context.WorkingMemory".Translate(pawn.Name.ToStringShort));
                foreach (var entry in wm.Entries)
                    sb.AppendLine($"- {entry.Content}");

                return sb.ToString().TrimEnd();
            }, PromptSection.PriorityCurrentInput, "RimMind.Memory");
        }
    }
}
