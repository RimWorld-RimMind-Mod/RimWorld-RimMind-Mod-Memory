using System.Collections.Generic;
using RimMind.Memory.Core;
using RimMind.Memory.Data;
using RimMind.Memory.Injection;
using RimMind.Presentation.Api;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Memory.UI
{
    public class Dialog_MemoryLog : Window
    {
        private readonly Pawn _pawn;
        private Vector2 _scrollPosition;

        public Dialog_MemoryLog(Pawn pawn)
        {
            _pawn = pawn;
            doCloseX = true;
            draggable = true;
            closeOnAccept = false;
            closeOnCancel = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
        }

        public override Vector2 InitialSize => new Vector2(560f, 620f);

        public override void DoWindowContents(Rect inRect)
        {
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;
            var store = wc.GetPawnStore(_pawn);
            var settings = RimMindMemoryMod.Settings;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 100f, 30f),
                RimMindAPI.Text.TrOr("RimMind.Memory.UI.MemoryLogTitle", "{0}'s Memories", _pawn.LabelShort));
            Text.Font = GameFont.Small;

            float btnY = inRect.y + 2f;
            if (Widgets.ButtonText(new Rect(inRect.xMax - 96f, btnY, 96f, 28f),
                RimMindAPI.Text.TrOr("RimMind.Memory.UI.AddMemoryManually", "Add Memory Manually")))
            {
                var dialog = new Dialog_InputMemory(_pawn, settings);
                Find.WindowStack.Add(dialog);
            }

            float headerH = 36f;
            Rect scrollOuter = new Rect(inRect.x, inRect.y + headerH, inRect.width, inRect.height - headerH);

            int now = Find.TickManager.TicksGame;
            var (activeList, archiveList, darkList) = store != null
                ? store.GetSnapshots()
                : (new List<MemoryEntry>(), new List<MemoryEntry>(), new List<MemoryEntry>());
            float viewWidth = scrollOuter.width - 16f;
            float contentH = CalculateContentHeight(activeList, archiveList, darkList, viewWidth);

            Rect viewRect = new Rect(0f, 0f, viewWidth, contentH);
            Widgets.BeginScrollView(scrollOuter, ref _scrollPosition, viewRect);

            float y = 0f;
            float w = viewRect.width;

            if (darkList.Count > 0)
            {
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(0f, y, w, 20f),
                    RimMindAPI.Text.TrOr("RimMind.Memory.UI.DarkMemoryReadonly", "Long-term Impressions (Read-only, AI-generated)"));
                y += 22f;
                bool prevWrap = Text.WordWrap;
                Text.WordWrap = true;
                try
                {
                    foreach (var d in darkList)
                    {
                        float textH = Text.CalcHeight(d.content, w - 16f);
                        Widgets.Label(new Rect(8f, y, w - 16f, textH), d.content);
                        y += textH + 2f;
                    }
                }
                finally
                {
                    Text.WordWrap = prevWrap;
                }
                y += 6f;
            }

            Widgets.Label(new Rect(0f, y, w, 20f),
                RimMindAPI.Text.TrOr("RimMind.Memory.UI.ActiveMemory", "Active Memories ({0}/{1})", activeList.Count, settings.maxActive));
            y += 22f;

            for (int i = 0; i < activeList.Count; i++)
            {
                var e = activeList[i];
                string timeStr = TimeFormatter.FormatTimeAgo(e.tick, now);
                string pinMark = e.isPinned ? " [P]" : "";
                string lineText = RimMindAPI.Text.TrOr("RimMind.Memory.Time.TimeContent", "{0}: {1}", timeStr, e.content);
                Widgets.Label(new Rect(0f, y, w - 60f, 22f), $"{lineText}{pinMark}");

                if (store != null && Widgets.ButtonText(new Rect(w - 56f, y + 1f, 24f, 20f), "P"))
                {
                    lock (store.SyncRoot)
                    {
                        e.isPinned = !e.isPinned;
                    }
                    MemoryChangedEvent.Publish(_pawn.thingIDNumber);
                }
                if (store != null && Widgets.ButtonText(new Rect(w - 28f, y + 1f, 24f, 20f), "X"))
                {
                    lock (store.SyncRoot)
                    {
                        store.active.Remove(e);
                    }
                    MemoryChangedEvent.Publish(_pawn.thingIDNumber);
                    break;
                }
                y += 24f;
            }
            y += 6f;

            if (archiveList.Count > 0)
            {
                Widgets.Label(new Rect(0f, y, w, 20f),
                    RimMindAPI.Text.TrOr("RimMind.Memory.UI.ArchiveMemoryUI", "Archived Memories ({0}/{1})", archiveList.Count, settings.maxArchive));
                y += 22f;

                foreach (var e in archiveList)
                {
                    string timeStr = TimeFormatter.FormatTimeAgo(e.tick, now);
                    string lineText = RimMindAPI.Text.TrOr("RimMind.Memory.Time.TimeContent", "{0}: {1}", timeStr, $"{e.content} (imp={e.importance:F2})");
                    Widgets.Label(new Rect(8f, y, w - 16f, 20f), lineText);
                    y += 20f;
                }
            }

            Widgets.EndScrollView();
        }

        private float CalculateContentHeight(
            List<MemoryEntry> activeList,
            List<MemoryEntry> archiveList,
            List<MemoryEntry> darkList,
            float viewWidth)
        {
            float h = 0f;
            float darkWrapWidth = Mathf.Max(60f, viewWidth - 16f);
            if (darkList.Count > 0)
            {
                h += 22f;
                bool prevWrap = Text.WordWrap;
                try
                {
                    Text.WordWrap = true;
                    foreach (var d in darkList)
                    {
                        float textH = Text.CalcHeight(d.content, darkWrapWidth);
                        h += textH + 2f;
                    }
                }
                finally
                {
                    Text.WordWrap = prevWrap;
                }
                h += 6f;
            }
            h += 22f + activeList.Count * 24f + 6f;
            if (archiveList.Count > 0)
            {
                h += 22f + archiveList.Count * 20f;
            }
            return h;
        }
    }

    public class Dialog_InputMemory : Window
    {
        private readonly Pawn _pawn;
        private readonly RimMindMemorySettings _settings;
        private string _inputText = "";

        public Dialog_InputMemory(Pawn pawn, RimMindMemorySettings settings)
        {
            _pawn = pawn;
            _settings = settings;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = true;
        }

        public override Vector2 InitialSize => new Vector2(400f, 180f);

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 20f),
                RimMindAPI.Text.TrOr("RimMind.Memory.UI.InputMemoryContent", "Enter memory content:"));
            _inputText = Widgets.TextField(new Rect(inRect.x, inRect.y + 24f, inRect.width, 30f), _inputText);

            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 36f, 100f, 30f),
                RimMindAPI.Text.TrOr("RimMind.Memory.UI.Add", "Add")) &&
                !_inputText.Trim().NullOrEmpty())
            {
                int now = Find.TickManager.TicksGame;
                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc != null)
                {
                    wc.AddPawnMemory(_pawn,
                        MemoryEntry.Create(_inputText.Trim(), MemoryType.Manual, now, 0.6f),
                        _settings.maxActive, _settings.maxArchive);
                }
                Messages.Message(
                    RimMindAPI.Text.TrOr("RimMind.Memory.UI.MemoryAdded", "[RimMind] Memory added for {0}", _pawn.LabelShort),
                    MessageTypeDefOf.SilentInput, historical: false);
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.x + 108f, inRect.yMax - 36f, 80f, 30f),
                RimMindAPI.Text.TrOr("RimMind.Memory.UI.Cancel", "Cancel")))
                Close();
        }
    }
}
