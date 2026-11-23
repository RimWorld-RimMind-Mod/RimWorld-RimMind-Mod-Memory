using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Memory
{
    internal sealed class MemoryToggleBehavior : IToggleBehavior
    {
        public string Id => "memory.toggle";
        public string OwnerModId => RimMindOwnerConsts.MemoryModId;
        public bool IsActive => RimMindMemoryMod.Settings.enableMemory;
        public void Toggle()
        {
            RimMindMemoryMod.Settings.enableMemory = !RimMindMemoryMod.Settings.enableMemory;
            RimMindMemoryMod.Settings.Write();
        }
    }
}
