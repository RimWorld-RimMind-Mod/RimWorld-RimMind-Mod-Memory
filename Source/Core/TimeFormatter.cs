using System;
using RimMind.Domain.Common;
using Verse;

namespace RimMind.Memory.Core
{
    public static class TimeFormatter
    {
        private const int TicksPerHour = RimMindTime.TicksPerHour;
        private const int TicksPerDay = RimMindTime.TicksPerDay;

        public static string FormatTimeAgo(int eventTick, int nowTick)
        {
            int delta = nowTick - eventTick;
            if (delta < 0) delta = 0;

            if (delta < TicksPerHour)
                return "RimMind.Memory.Time.JustNow".Translate();

            if (delta < 6 * TicksPerHour)
            {
                float hours = delta / (float)TicksPerHour;
                return "RimMind.Memory.Time.HoursAgo".Translate($"{hours:F0}");
            }

            if (delta < TicksPerDay)
                return "RimMind.Memory.Time.Today".Translate();

            int days = delta / TicksPerDay;
            if (days <= 3)
                return "RimMind.Memory.Time.DaysAgo".Translate(days);

            return FormatGameDate(nowTick);
        }

        public static string FormatGameDate(int tick)
        {
            int day = tick / TicksPerDay;
            int hour = (tick % TicksPerDay) / TicksPerHour;
            return "RimMind.Memory.Time.GameDate".Translate((day + 1).ToString(), $"{hour:D2}");
        }
    }
}
