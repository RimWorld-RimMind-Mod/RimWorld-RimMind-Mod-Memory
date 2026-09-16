using System.Linq;
using Verse;

namespace RimMind.Memory.Core
{
    public static class PawnLookup
    {
        public static Pawn? FindPawnById(string pawnId)
        {
            if (pawnId.NullOrEmpty()) return null;

            var pawn = Find.WorldPawns?.AllPawnsAliveOrDead
                .FirstOrDefault(p => p.ThingID == pawnId);
            if (pawn != null) return pawn;

            foreach (var map in Find.Maps)
            {
                pawn = map.mapPawns?.AllPawns
                    .FirstOrDefault(p => p.ThingID == pawnId);
                if (pawn != null) return pawn;
            }

            foreach (var caravan in Find.WorldObjects.Caravans)
            {
                pawn = caravan.PawnsListForReading
                    .FirstOrDefault(p => p.ThingID == pawnId);
                if (pawn != null) return pawn;
            }

            return null;
        }

        public static Pawn? FindPawnByNumber(int thingIDNumber)
        {
            if (thingIDNumber <= 0) return null;

            var pawn = Find.WorldPawns?.AllPawnsAlive
                .FirstOrDefault(p => p.thingIDNumber == thingIDNumber);
            if (pawn != null) return pawn;

            foreach (var map in Find.Maps)
            {
                pawn = map.mapPawns?.FreeColonists
                    .FirstOrDefault(p => p.thingIDNumber == thingIDNumber);
                if (pawn != null) return pawn;
            }

            return null;
        }
    }
}
