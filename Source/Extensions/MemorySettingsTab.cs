using UnityEngine;
using RimMind.Contracts.Extension;

namespace RimMind.Memory
{
    internal sealed class MemorySettingsTab : ISettingsTab
    {
        private readonly RimMindMemoryMod _mod;
        public MemorySettingsTab(RimMindMemoryMod mod) { _mod = mod; }
        public string Id => "memory";
        public string Label => "RimMind.Memory.Settings.TabLabel".Translate();
        public void Draw(Rect rect) => RimMindMemoryMod.DrawSettingsContent(rect);
    }
}
