using RimMind.Domain.ValueObjects;
using Verse;

namespace RimMind.Memory.Data
{
    public enum MemoryType { Work, Event, Manual, Dark }

    public class MemoryEntry : IExposable
    {
        private static int _nextSeq;

        public string id = string.Empty;
        public string content = string.Empty;
        public MemoryType type;
        public int tick;
        public float importance;
        public bool isPinned;
        public string? pawnId;

        public MemoryEntry() { }

        public static MemoryEntry Create(string content, MemoryType type, int tick, float importance, string? pawnId = null)
        {
            if (content.Length > 2000)
            {
                var originalLength = content.Length;
                content = content.Substring(0, 2000) + "...";
                RimMindErrors.Warn($"[RimMind-Memory] Memory content truncated to 2000 chars (original length: {originalLength})");
            }

            return new MemoryEntry
            {
                id = $"mem-{tick}-{System.Threading.Interlocked.Increment(ref _nextSeq)}",
                content = content,
                type = type,
                tick = tick,
                importance = importance,
                isPinned = type == MemoryType.Dark,
                pawnId = pawnId,
            };
        }

        public void ExposeData()
        {
#pragma warning disable CS8601
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref content, "content", string.Empty);
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref tick, "tick");
            Scribe_Values.Look(ref importance, "importance");
            Scribe_Values.Look(ref isPinned, "isPinned");
            Scribe_Values.Look(ref pawnId, "pawnId", null);
#pragma warning restore CS8601
        }

        public static void ExposeNextSeq()
        {
            Scribe_Values.Look(ref _nextSeq, "memoryNextSeq", 0);
        }
    }
}
