using RimMind.Contracts.Extension;

namespace RimMind.Memory
{
    internal sealed class MemoryModCooldown : IModCooldown
    {
        public string Id => "DarkMemory";
        public int CooldownTicks => 60000;
    }
}
