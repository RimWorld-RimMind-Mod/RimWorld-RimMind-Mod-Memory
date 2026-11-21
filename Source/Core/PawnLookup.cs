using System;
using RimMind.Presentation.Api;
using Verse;

namespace RimMind.Memory.Core
{
    [Obsolete("Use RimMind.Presentation.Api.RimMindPawnLookup directly.")]
    public static class PawnLookup
    {
        public static Pawn? FindPawnById(string pawnId)
            => RimMindPawnLookup.FindPawnById(pawnId);

        public static Pawn? FindPawnByNumber(int thingIDNumber)
            => RimMindPawnLookup.FindPawnByNumber(thingIDNumber);
    }
}
