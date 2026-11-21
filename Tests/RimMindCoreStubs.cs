using RimMind.Application.Features.Json;

namespace RimMind.Presentation.Api
{
    /// <summary>
    /// Thin test-only facade forwarding <see cref="RimMindAPI.Json"/> directly to Core Application's
    /// real <see cref="LlmPayloadParser"/> and <see cref="JsonRepairer"/> implementations.
    /// </summary>
    public static class RimMindAPI
    {
        public static class Json
        {
            public static string StripThinkingAndCodeFences(string? raw)
                => LlmPayloadParser.StripThinkingAndCodeFences(raw);

            public static string? ExtractJsonPayload(string? raw)
                => LlmPayloadParser.ExtractJsonPayload(raw);

            public static bool TryDeserializeWithRepair<T>(string? raw, out T? value) where T : class
                => LlmPayloadParser.TryDeserializeWithRepair<T>(raw, out value);

            public static string? TryRepairTruncatedJson(string input)
                => JsonRepairer.TryRepairTruncatedJson(input);
        }
    }
}
