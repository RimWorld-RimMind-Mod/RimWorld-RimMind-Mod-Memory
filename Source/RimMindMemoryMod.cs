using HarmonyLib;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation.Api;
using RimMind.Presentation.Settings;
using RimMind.Memory.Core;
using RimMind.Memory.Injection;
using UnityEngine;
using Verse;

namespace RimMind.Memory
{
    public class RimMindMemoryMod : Mod
    {
        public static RimMindMemorySettings Settings = null!;

        public RimMindMemoryMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimMindMemorySettings>();
            new Harmony("mcocdaa.RimMindMemory").PatchAll();

            MemoryContextProvider.Register();
            WorkingMemoryProvider.Register();
            RimMindAPI.Memory.RegisterBridge(new RimMindMemoryBridge());
            RimMindAPI.Extensions<ISettingsTab>().Register(new MemorySettingsTab());
            RimMindAPI.Extensions<IToggleBehavior>().Register(new MemoryToggleBehavior());
            RimMindAPI.Extensions<IModCooldown>().Register(new MemoryModCooldown());
            RimMindAPI.Extensions<ISkipCheck>().Register(new MemoryActionSkipCheck());
            Log.Message("[RimMind-Memory] Initialized.");
        }

        public override string SettingsCategory() => "RimMind - Memory";

        public override void DoSettingsWindowContents(Rect rect) =>
            MemorySettingsDrawer.Draw(rect);
    }
}
