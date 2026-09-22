using System.Linq;
using Verse;

namespace RimMind.Memory.Core
{
    public static class PawnLookup
    {
        public static Pawn? FindPawnById(string pawnId)
            => RimMind.Presentation.Api.RimMindPawnLookup.FindPawnById(pawnId);

        public static Pawn? FindPawnByNumber(int thingIDNumber)
            => RimMind.Presentation.Api.RimMindPawnLookup.FindPawnByNumber(thingIDNumber);
    }
}
