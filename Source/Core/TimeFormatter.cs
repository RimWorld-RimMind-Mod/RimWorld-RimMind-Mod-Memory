using System;
using RimMind.Domain.Common;
using Verse;

namespace RimMind.Memory.Core
{
    public static class TimeFormatter
    {
        private const int TicksPerHour = RimMindTime.TicksPerHour;
        private const int TicksPerDay = RimMindTime.TicksPerDay;

        private static string Tr(string key, params object[] args)
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return string.Empty;
                string template = key.CanTranslate() ? key.TranslateSimple() : key;
                return (args == null || args.Length == 0) ? template : string.Format(template, args);
            }
            catch { return key ?? string.Empty; }
        }

        public static string FormatTimeAgo(int eventTick, int nowTick)
        {
            if (eventTick < 0) eventTick = 0;
            if (nowTick < 0) nowTick = 0;

            int delta = nowTick - eventTick;
            if (delta < 0) delta = 0;

            if (delta < TicksPerHour)
                return Tr("RimMind.Memory.Time.JustNow");

            if (delta < 6 * TicksPerHour)
            {
                float hours = delta / (float)TicksPerHour;
                return Tr("RimMind.Memory.Time.HoursAgo", $"{hours:F0}");
            }

            if (delta < TicksPerDay)
                return Tr("RimMind.Memory.Time.Today");

            int days = delta / TicksPerDay;
            if (days <= 3)
                return Tr("RimMind.Memory.Time.DaysAgo", days);

            return FormatGameDate(nowTick);
        }

        public static string FormatGameDate(int tick)
        {
            if (tick < 0) tick = 0;
            int day = tick / TicksPerDay;
            int hour = (tick % TicksPerDay) / TicksPerHour;
            return Tr("RimMind.Memory.Time.GameDate", (day + 1).ToString(), $"{hour:D2}");
        }
    }
}
