using Verse;

namespace RimMind.Memory.WorkingMemory
{
    public class WorkingMemoryEntry : IExposable
    {
        private string _content = "";
        private int _timestamp;
        private string _source = "";
        private float _relevance;

        public string Content { get => _content; set => _content = value ?? ""; }
        public int Timestamp { get => _timestamp; set => _timestamp = value; }
        public string Source { get => _source; set => _source = value ?? ""; }
        public float Relevance { get => _relevance; set => _relevance = value; }

        public WorkingMemoryEntry() { }

        public WorkingMemoryEntry(string content, string source = "", float relevance = 0.5f)
        {
            _content = content ?? "";
            _timestamp = Find.TickManager?.TicksGame ?? 0;
            _source = source ?? "";
            _relevance = relevance;
        }

        public void ExposeData()
        {
#pragma warning disable CS8601
            Scribe_Values.Look(ref _content, "Content");
            Scribe_Values.Look(ref _timestamp, "Timestamp");
            Scribe_Values.Look(ref _source, "Source");
#pragma warning restore CS8601
            _content ??= "";
            _source ??= "";
            Scribe_Values.Look(ref _relevance, "Relevance");
        }
    }
}
