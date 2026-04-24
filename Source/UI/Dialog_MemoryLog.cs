using System.Collections.Generic;
using RimMind.Memory.Core;
using RimMind.Memory.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Memory.UI
{
    public class Dialog_MemoryLog : Window
    {
        private readonly Pawn _pawn;
        private Vector2 _scrollPosition;
        private float _lastContentHeight;

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
            var store = wc.GetOrCreatePawnStore(_pawn);
            var settings = RimMindMemoryMod.Settings;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 100f, 30f),
                "RimMind.Memory.UI.MemoryLogTitle".Translate(_pawn.LabelShort));
            Text.Font = GameFont.Small;

            float btnY = inRect.y + 2f;
            if (Widgets.ButtonText(new Rect(inRect.xMax - 96f, btnY, 96f, 28f),
                "RimMind.Memory.UI.AddMemoryManually".Translate()))
            {
                var dialog = new Dialog_InputMemory(_pawn, store, settings);
                Find.WindowStack.Add(dialog);
            }

            float headerH = 36f;
            Rect scrollOuter = new Rect(inRect.x, inRect.y + headerH, inRect.width, inRect.height - headerH);

            int now = Find.TickManager.TicksGame;
            float contentH = CalculateContentHeight(store, settings, now);

            Rect viewRect = new Rect(0f, 0f, scrollOuter.width - 16f, contentH);
            Widgets.BeginScrollView(scrollOuter, ref _scrollPosition, viewRect);

            float y = 0f;
            float w = viewRect.width;

            if (store.dark.Count > 0)
            {
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(0f, y, w, 20f),
                    "RimMind.Memory.UI.DarkMemoryReadonly".Translate());
                y += 22f;
                Text.WordWrap = true;
                try
                {
                    foreach (var d in store.dark)
                    {
                        float textH = Text.CalcHeight(d.content, w - 16f);
                        Widgets.Label(new Rect(8f, y, w - 16f, textH), d.content);
                        y += textH + 2f;
                    }
                }
                finally
                {
                    Text.WordWrap = false;
                }
                y += 6f;
            }

            Widgets.Label(new Rect(0f, y, w, 20f),
                "RimMind.Memory.UI.ActiveMemory".Translate(store.active.Count, settings.maxActive));
            y += 22f;

            for (int i = 0; i < store.active.Count; i++)
            {
                var e = store.active[i];
                string timeStr = TimeFormatter.FormatTimeAgo(e.tick, now);
                string pinMark = e.isPinned ? " [P]" : "";
                Widgets.Label(new Rect(0f, y, w - 60f, 22f),
                    $"{"RimMind.Memory.Time.TimeContent".Translate(timeStr, e.content)}{pinMark}");

                if (Widgets.ButtonText(new Rect(w - 56f, y + 1f, 24f, 20f), "P"))
                    e.isPinned = !e.isPinned;
                if (Widgets.ButtonText(new Rect(w - 28f, y + 1f, 24f, 20f), "X"))
                {
                    store.active.RemoveAt(i);
                    break;
                }
                y += 24f;
            }
            y += 6f;

            if (store.archive.Count > 0)
            {
                Widgets.Label(new Rect(0f, y, w, 20f),
                    "RimMind.Memory.UI.ArchiveMemoryUI".Translate(store.archive.Count, settings.maxArchive));
                y += 22f;

                foreach (var e in store.archive)
                {
                    string timeStr = TimeFormatter.FormatTimeAgo(e.tick, now);
                    Widgets.Label(new Rect(8f, y, w - 16f, 20f),
                        $"{"RimMind.Memory.Time.TimeContent".Translate(timeStr, $"{e.content} (imp={e.importance:F2})")}");
                    y += 20f;
                }
            }

            _lastContentHeight = y;
            Widgets.EndScrollView();
        }

        private float CalculateContentHeight(PawnMemoryStore store, RimMindMemorySettings settings, int now)
        {
            float h = 0f;
            if (store.dark.Count > 0)
            {
                h += 22f;
                foreach (var d in store.dark)
                {
                    bool prevWrap = Text.WordWrap;
                    try
                    {
                        Text.WordWrap = true;
                        float textH = Text.CalcHeight(d.content, 520f);
                        h += textH + 2f;
                    }
                    finally
                    {
                        Text.WordWrap = prevWrap;
                    }
                }
                h += 6f;
            }
            h += 22f + store.active.Count * 24f + 6f;
            if (store.archive.Count > 0)
            {
                h += 22f + store.archive.Count * 20f;
            }
            return h;
        }
    }

    public class Dialog_InputMemory : Window
    {
        private readonly Pawn _pawn;
        private readonly PawnMemoryStore _store;
        private readonly RimMindMemorySettings _settings;
        private string _inputText = "";

        public Dialog_InputMemory(Pawn pawn, PawnMemoryStore store, RimMindMemorySettings settings)
        {
            _pawn = pawn;
            _store = store;
            _settings = settings;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = true;
        }

        public override Vector2 InitialSize => new Vector2(400f, 180f);

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 20f),
                "RimMind.Memory.UI.InputMemoryContent".Translate());
            _inputText = Widgets.TextField(new Rect(inRect.x, inRect.y + 24f, inRect.width, 30f), _inputText);

            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 36f, 100f, 30f),
                "RimMind.Memory.UI.Add".Translate()) &&
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
                    "RimMind.Memory.UI.MemoryAdded".Translate(_pawn.LabelShort),
                    MessageTypeDefOf.SilentInput, historical: false);
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.x + 108f, inRect.yMax - 36f, 80f, 30f),
                "RimMind.Memory.UI.Cancel".Translate()))
                Close();
        }
    }
}
