using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Memory
{
    internal sealed class MemoryModCooldown : IModCooldown
    {
        public string Id => "DarkMemory";
        public string OwnerModId => "RimMind.Memory";
        public int CooldownTicks => 60000;
    }
}
