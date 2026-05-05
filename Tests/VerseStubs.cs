using System.Collections.Generic;

namespace Verse
{
    public struct TaggedString
    {
        public string Value;
        public static implicit operator string(TaggedString ts) => ts.Value;
        public static implicit operator TaggedString(string s) => new TaggedString { Value = s };
        public override string ToString() => Value ?? "";
    }

    public interface IExposable
    {
        void ExposeData();
    }

    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string label, T defaultValue = default) { }
    }

    public static class Scribe_Collections
    {
        public static void Look<T>(ref System.Collections.Generic.List<T> list, string label, LookMode lookMode) { }
        public static void Look<T>(ref System.Collections.Generic.List<T> list, string label) { }
        public static void Look<TKey, TValue>(ref System.Collections.Generic.Dictionary<TKey, TValue> dict, string label, LookMode keyLookMode, LookMode valueLookMode) { }
    }

    public static class Scribe_Deep
    {
        public static void Look<T>(ref T target, string label) where T : IExposable, new() { }
    }

    public enum LookMode { Value, Deep }

    public static class Log
    {
        public static void Warning(string msg) { }
        public static void Message(string msg) { }
        public static void Error(string msg) { }
    }

    public static class TranslationStubs
    {
        private static readonly Dictionary<string, string> Translations = new Dictionary<string, string>
        {
            { "RimMind.Memory.Time.JustNow", "Just now" },
            { "RimMind.Memory.Time.HoursAgo", "About {0}h ago" },
            { "RimMind.Memory.Time.Today", "Today" },
            { "RimMind.Memory.Time.DaysAgo", "{0} days ago" },
            { "RimMind.Memory.Time.GameDate", "Day {0} {1}:00" },
            { "RimMind.Memory.Time.TimeContent", "{0}: {1}" },
        };

        public static TaggedString Translate(this string key)
        {
            if (Translations.TryGetValue(key, out var value))
                return new TaggedString { Value = value };
            return new TaggedString { Value = key };
        }

        public static TaggedString Translate(this string key, object arg0)
        {
            string template = Translations.TryGetValue(key, out var value) ? value : key;
            return new TaggedString { Value = string.Format(template, arg0) };
        }

        public static TaggedString Translate(this string key, object arg0, object arg1)
        {
            string template = Translations.TryGetValue(key, out var value) ? value : key;
            return new TaggedString { Value = string.Format(template, arg0, arg1) };
        }

        public static bool CanTranslate(this string key)
        {
            return Translations.ContainsKey(key);
        }
    }
}

namespace RimWorld.Planet
{
    public class WorldComponent
    {
        public WorldComponent(World world) { }
        public virtual void ExposeData() { }
    }

    public class World { }
}

namespace Verse
{
    public class Pawn
    {
        public int thingIDNumber;
    }

    public static class Find
    {
        public static TickManager TickManager = new TickManager();
    }

    public class TickManager
    {
        public int TicksGame = 100000;
    }
}
