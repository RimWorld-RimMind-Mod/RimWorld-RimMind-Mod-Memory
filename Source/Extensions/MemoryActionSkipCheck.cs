using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Memory
{
    internal sealed class MemoryActionSkipCheck : ISkipCheck
    {
        public string Id => "memory.action";
        public string OwnerModId => RimMindOwnerConsts.MemoryModId;
        public SkipCheckKind Kind => SkipCheckKind.Action;
        // Memory never vetoes Advisor/Actions when memory recording is disabled;
        // memory enablement only gates Memory's own recording, injection, and consolidation.
        public bool ShouldSkip(in SkipCheckArgs args) => false;
    }
}
