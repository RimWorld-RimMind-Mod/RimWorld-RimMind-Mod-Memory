using System.Collections.Generic;
using Verse;

namespace RimMind.Memory.WorkingMemory
{
    // Save compatibility only. New memories use PawnMemoryStore / NarratorMemoryStore.
    // Keep the type and Scribe labels so existing entries remain readable and resavable.
    public class WorkingMemory : IExposable
    {
        private const int DefaultCapacity = 10;
        private List<WorkingMemoryEntry> _entries = new List<WorkingMemoryEntry>();
        private int _capacity = DefaultCapacity;

        public int Capacity => _capacity;
        public IReadOnlyList<WorkingMemoryEntry> Entries => _entries;
        public bool IsEmpty => _entries.Count == 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref _capacity, "capacity", DefaultCapacity);
            Scribe_Collections.Look(ref _entries, "entries", LookMode.Deep);
            _entries ??= new List<WorkingMemoryEntry>();
        }
    }
}
