using RimMind.Application.Common.Constants;
using RimMind.Presentation.Api;
using RimMind.Presentation.Settings;
using UnityEngine;

namespace RimMind.Memory
{
    internal sealed class MemorySettingsTab : ISettingsTab
    {
        public string Id => "memory";
        public string OwnerModId => RimMindOwnerConsts.MemoryModId;
        public string Label => RimMindAPI.Text.TrOr("RimMind.Memory.Settings.TabLabel", "Memory");
        public void Draw(Rect rect) => MemorySettingsDrawer.Draw(rect);
    }
}
