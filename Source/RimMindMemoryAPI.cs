using RimMind.Application.Common.Models.Memory;
using RimMind.Presentation.Api;

namespace RimMind.Memory
{
    public static class RimMindMemoryAPI
    {
        public static bool AddMemory(string content, string memoryType, int tick, float importance, string? pawnId = null)
        {
            var kind = System.Enum.TryParse<MemoryKind>(memoryType, out var parsed)
                ? parsed
                : MemoryKind.Event;
            return RimMindAPI.Memory.AddPawnMemory(content, kind, tick, importance, pawnId);
        }
    }
}
