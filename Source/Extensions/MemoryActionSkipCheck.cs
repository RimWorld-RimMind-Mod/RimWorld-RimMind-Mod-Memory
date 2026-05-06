using RimMind.Contracts.Extension;

namespace RimMind.Memory
{
    internal sealed class MemoryActionSkipCheck : ISkipCheck
    {
        public string Id => "memory.action";
        public SkipCheckKind Kind => SkipCheckKind.Action;
        public bool ShouldSkip(in SkipCheckArgs args) => !RimMindMemoryMod.Settings.enableMemory;
    }
}
