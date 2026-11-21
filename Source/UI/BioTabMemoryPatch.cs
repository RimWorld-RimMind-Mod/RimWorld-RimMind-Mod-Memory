using RimMind.Application.Common.Constants;
using RimMind.Memory.Data;
using RimMind.Presentation.Api;
using RimMind.Presentation.UI;
using Verse;

namespace RimMind.Memory.UI
{
    /// <summary>
    /// Bio-tab badge provider for RimMind-Memory registered via Core's unified
    /// <c>CharacterCardUtility.DoTopStack</c> host (<see cref="RimMindAPI.UI.RegisterBioTabBadge"/>).
    /// Uses strictly read-only <see cref="RimMindMemoryWorldComponent.GetPawnStore"/> during hover/draw.
    /// </summary>
    public sealed class MemoryBioTabBadgeProvider : IBioTabBadgeProvider
    {
        public const string BadgeId = "rimmind.memory.bio_tab";

        public string Id => BadgeId;
        public string OwnerModId => RimMindOwnerConsts.MemoryModId;
        public int Order => 20;

        public BioTabBadgeDescriptor? TryGetBadge(Pawn pawn)
        {
            if (pawn == null) return null;

            string label = RimMindAPI.Text.TrOr("RimMind.Memory.UI.MemoryTab", "Memory");
            return new BioTabBadgeDescriptor(
                Label: label,
                TooltipProvider: () => BuildTooltip(pawn),
                OnClick: () => Find.WindowStack.Add(new Dialog_MemoryLog(pawn)),
                ExtraWidth: 16f);
        }

        private static string BuildTooltip(Pawn pawn)
        {
            var store = RimMindMemoryWorldComponent.Instance?.GetPawnStore(pawn);
            if (store == null)
                return RimMindAPI.Text.TrOr("RimMind.Memory.UI.NoMemory", "No memory records");

            var (activeList, archiveList, darkList) = store.GetSnapshots();
            int count = activeList.Count + archiveList.Count + darkList.Count;
            return count > 0
                ? RimMindAPI.Text.TrOr(
                    "RimMind.Memory.UI.MemorySummary",
                    "Active {0} / Archived {1} / Dark {2}",
                    activeList.Count, archiveList.Count, darkList.Count)
                : RimMindAPI.Text.TrOr("RimMind.Memory.UI.NoMemory", "No memory records");
        }
    }
}
