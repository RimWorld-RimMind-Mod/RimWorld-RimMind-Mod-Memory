using System.Collections.Generic;
using Verse;

namespace RimMind.Memory.WorkingMemory
{
    public class WorkingMemory : IExposable
    {
        public const int DefaultCapacity = 10;

        private List<WorkingMemoryEntry> _entries = new List<WorkingMemoryEntry>();
        private readonly int _capacity;

        public int Capacity => _capacity;
        public IReadOnlyList<WorkingMemoryEntry> Entries => _entries;
        public bool IsEmpty => _entries.Count == 0;

        public WorkingMemory(int capacity = DefaultCapacity)
        {
            _capacity = capacity > 0 ? capacity : DefaultCapacity;
        }

        public void Add(string content, string source = "", float relevance = 0.5f)
        {
            if (string.IsNullOrEmpty(content)) return;

            var entry = new WorkingMemoryEntry(content, source, relevance);
            _entries.Add(entry);

            while (_entries.Count > _capacity)
                _entries.RemoveAt(0);
        }

        public void Add(WorkingMemoryEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Content)) return;
            _entries.Add(entry);

            while (_entries.Count > _capacity)
                _entries.RemoveAt(0);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref _entries, "entries", LookMode.Deep);
            _entries ??= new List<WorkingMemoryEntry>();
        }
    }
}
