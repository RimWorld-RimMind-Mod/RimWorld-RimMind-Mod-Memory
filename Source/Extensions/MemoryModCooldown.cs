using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Memory.DarkMemory;
using RimMind.Presentation.Api;

namespace RimMind.Memory
{
    internal sealed class MemoryModCooldown : IModCooldown
    {
        // Mod-level pacing between consecutive dark-memory consolidation calls across colonists.
        // Per-pawn daily cadence (60,000 ticks) is enforced by DarkMemoryUpdater via TickStagger;
        // keeping the mod-global pacing short (3,000 ticks = 1.2 in-game hours) prevents multi-colonist
        // queue starvation while avoiding simultaneous LLM bursts.
        public const int BaseModCooldownTicks = 3000;
        public const int MinModCooldownTicks = 600;

        public string Id => DarkMemoryUpdater.RequestModId;
        public string OwnerModId => RimMindOwnerConsts.MemoryModId;
        public int CooldownTicks => RimMindAPI.Settings.ScaleCooldown(BaseModCooldownTicks, MinModCooldownTicks);
    }
}
