using UnityEngine;
using RimMind.Presentation.Settings;
using Verse;

namespace RimMind.Memory
{
    internal sealed class MemorySettingsTab : ISettingsTab
    {
        public string Id => "memory";
        public string OwnerModId => "RimMindMemory";
        public string Label => "RimMind.Memory.Settings.TabLabel".CanTranslate() ? (string)"RimMind.Memory.Settings.TabLabel".Translate() : "Memory";
        public void Draw(Rect rect) => MemorySettingsDrawer.Draw(rect);
    }
}
