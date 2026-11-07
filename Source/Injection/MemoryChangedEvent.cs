using System;
using RimMind.Domain.Events;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;

namespace RimMind.Memory.Injection
{
    /// <summary>
    /// Published on the Core bus whenever pawn or narrator memories are added, merged or cleared.
    /// The <c>memory_pawn</c> and <c>memory_narrator</c> context keys name <see cref="EventName"/>
    /// as their invalidation trigger, so new memories reach the next prompt immediately.
    /// <see cref="AgentBusEvent.PawnId"/> is 0 for narrator memories.
    /// </summary>
    public sealed class MemoryChangedEvent : AgentBusEvent
    {
        /// <summary>Bus event name used by context-key invalidation triggers.</summary>
        public const string EventName = "MemoryEvent";

        public MemoryChangedEvent(int pawnId)
        {
            PawnId = pawnId;
            NpcId = pawnId > 0 ? "NPC-" + pawnId : string.Empty;
        }

        /// <summary>Registers <see cref="EventName"/> with the Core bus. Call before registering context keys.</summary>
        internal static void Register()
        {
            try { RimMindAPI.Bus.RegisterEventType<MemoryChangedEvent>(EventName); }
            catch (Exception ex) { RimMindErrors.Warn($"[RimMind-Memory] Event type registration failed: {ex.Message}"); }
        }

        /// <summary>Publishes from any thread; never throws.</summary>
        internal static void Publish(int pawnId)
        {
            try { RimMindAPI.Bus.PublishOnMainThread(new MemoryChangedEvent(pawnId)); }
            catch (Exception ex) { RimMindErrors.Warn($"[RimMind-Memory] Memory change notification failed: {ex.Message}"); }
        }
    }
}
