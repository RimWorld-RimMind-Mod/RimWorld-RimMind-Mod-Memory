using System;
using System.IO;
using System.Linq;
using RimMind.Memory.Core;
using RimMind.Memory.DarkMemory;
using RimMind.Memory.Data;
using RimMind.Memory.Decay;
using RimMind.Testing;
using Xunit;

namespace RimMind.Memory.Tests.Contracts
{
    [Collection("Memory contracts")]
    public sealed class MemoryParsingContextContracts
    {
        [Fact]
        public void Dark_memory_request_input_and_parsing_preserve_contracts()
        {
            ContractCaseRunner.Run(
                ("pawn and narrator requests carry the merge input", DarkMemoryRequestsCarryMergeInput),
                ("public providers retain the side-effect-free bridge brief contract", PublicProvidersRetainBridgeBriefContract),
                ("invalid JSON returns no replacement value", () =>
                    Assert.Null(DarkMemoryResultParserPure.Parse("not-json", 3))),
                ("missing dark field preserves an empty result", () =>
                {
                    var result = DarkMemoryResultParserPure.Parse("{\"other\":true}", 3);
                    Assert.NotNull(result);
                    Assert.Empty(result!);
                }),
                ("partial entries skip empty values", () =>
                {
                    var result = DarkMemoryResultParserPure.Parse(
                        "{\"dark\":[\"kept\",\"\",null,\"also kept\"]}",
                        3);

                    Assert.Equal(new[] { "kept", "also kept" }, result);
                }),
                ("configured maximum bounds accepted entries", () =>
                {
                    var result = DarkMemoryResultParserPure.Parse(
                        "{\"dark\":[\"one\",\"two\",\"three\"]}",
                        2);

                    Assert.Equal(new[] { "one", "two" }, result);
                }),
                ("unrepairable truncation returns no replacement value", () =>
                    Assert.Null(DarkMemoryResultParserPure.Parse(
                        "{\"dark\":[\"unfinished",
                        3))));
        }

        [Fact]
        public void Importance_decay_preserves_threshold_and_pinned_boundaries()
        {
            ContractCaseRunner.Run(
                ("zero and full rates retain calculator bounds", () =>
                {
                    Assert.Equal(0.8f, ImportanceDecayCalculator.Decay(0.8f, 0f));
                    Assert.Equal(0f, ImportanceDecayCalculator.Decay(0.8f, 1f));
                }),
                ("threshold equality is retained while lower values are removed", () =>
                {
                    Assert.False(ImportanceDecayCalculator.ShouldRemove(0.05f, 0.05f));
                    Assert.True(ImportanceDecayCalculator.ShouldRemove(0.049f, 0.05f));
                }),
                ("manager decays active and archive entries", () =>
                {
                    var store = new PawnMemoryStore();
                    store.active.Add(Entry("active", 0.8f));
                    store.archive.Add(Entry("archive", 0.6f));

                    ImportanceDecayManager.ApplyDecay(store, 0.25f, 0.05f);

                    Assert.Equal(0.6f, store.active[0].importance, 3);
                    Assert.Equal(0.45f, store.archive[0].importance, 3);
                }),
                ("pinned and dark state survives decay", () =>
                {
                    var store = new PawnMemoryStore();
                    store.active.Add(Entry("pinned", 0.01f, pinned: true));
                    store.dark.Add(Entry("dark", 0.01f, pinned: true));

                    ImportanceDecayManager.ApplyDecay(store, 0.5f, 0.05f);

                    Assert.Equal(0.01f, Assert.Single(store.active).importance);
                    Assert.Equal(0.01f, Assert.Single(store.dark).importance);
                }));
        }

        private static void DarkMemoryRequestsCarryMergeInput()
        {
            string source = ReadMemorySource("DarkMemory/DarkMemoryUpdater.cs");
            int pawnStart = source.IndexOf(
                "var npcId = $\"NPC-{pawn.thingIDNumber}\";",
                StringComparison.Ordinal);
            int narratorStart = source.IndexOf(
                "var npcId = \"NPC-storyteller\";",
                StringComparison.Ordinal);

            Assert.True(pawnStart >= 0, "Pawn request block was not found.");
            Assert.True(narratorStart > pawnStart, "Narrator request block was not found.");

            AssertRequestCarriesMergeInput(source, pawnStart, narratorStart, "Pawn");
            AssertRequestCarriesMergeInput(source, narratorStart, source.Length, "Narrator");
        }

        private static void PublicProvidersRetainBridgeBriefContract()
        {
            string worldSource = ReadMemorySource("Data/RimMindMemoryWorldComponent.cs");
            string providerSource = ReadMemorySource("Injection/MemoryContextProvider.cs");

            string pawnStoreQuery = SliceSource(
                worldSource,
                "internal PawnMemoryStore? GetPawnStore(Pawn pawn)",
                "public NarratorMemoryStore NarratorStore");
            Assert.Contains(
                "return _pawnStores.TryGetValue(pawn.thingIDNumber, out var store) ? store : null;",
                pawnStoreQuery,
                StringComparison.Ordinal);
            Assert.DoesNotContain("_pawnStores[", pawnStoreQuery, StringComparison.Ordinal);

            Assert.DoesNotContain("GetOrCreatePawnStore", providerSource, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(providerSource, "GetPawnStore(pawn)"));
            Assert.Contains("var store = wc.GetPawnStore(pawn);", providerSource, StringComparison.Ordinal);
            Assert.Contains(
                "var store = RimMindMemoryWorldComponent.Instance?.GetPawnStore(pawn);",
                providerSource,
                StringComparison.Ordinal);

            string registerMethod = SliceSource(
                providerSource,
                "public static void Register()",
                "private static void RegisterPublicProviders()");
            Assert.Equal(
                2,
                CountOccurrences(registerMethod, "RimMindAPI.Context.ContextKeys.Register"));
            int secondContextRegistration = registerMethod.LastIndexOf(
                "RimMindAPI.Context.ContextKeys.Register",
                StringComparison.Ordinal);
            int publicProviderRegistration = registerMethod.IndexOf(
                "RegisterPublicProviders();",
                StringComparison.Ordinal);
            Assert.True(
                publicProviderRegistration > secondContextRegistration,
                "Public providers must be registered after both context providers.");

            Assert.Contains(
                "private const string PublicProviderOwner = \"RimMind.Memory\";",
                providerSource,
                StringComparison.Ordinal);
            Assert.Contains(
                "private const int PublicProviderPriority = 100;",
                providerSource,
                StringComparison.Ordinal);

            string publicProviders = RemoveWhitespace(SliceSource(
                providerSource,
                "private static void RegisterPublicProviders()",
                "private static string BuildPawnBrief(Pawn pawn)"));
            Assert.Contains(
                "RimMindAPI.Providers.RegisterPawnProvider(\"memory.pawn_brief\",PublicProviderOwner,BuildPawnBrief,PublicProviderPriority,overrideExisting:true);",
                publicProviders,
                StringComparison.Ordinal);
            Assert.Contains(
                "RimMindAPI.Providers.RegisterStaticProvider(\"memory.narrator_brief\",PublicProviderOwner,BuildNarratorBrief,PublicProviderPriority);",
                publicProviders,
                StringComparison.Ordinal);

            string pawnBrief = SliceSource(
                providerSource,
                "private static string BuildPawnBrief(Pawn pawn)",
                "private static string BuildNarratorBrief()");
            string narratorBrief = providerSource.Substring(providerSource.IndexOf(
                "private static string BuildNarratorBrief()",
                StringComparison.Ordinal));

            Assert.Equal(3, CountOccurrences(providerSource, "Take(5)"));
            Assert.Contains("store.active.Take(5)", pawnBrief, StringComparison.Ordinal);
            Assert.Contains("if (store.dark.Count > 0)", pawnBrief, StringComparison.Ordinal);
            Assert.Contains("store.dark.Take(5)", pawnBrief, StringComparison.Ordinal);
            Assert.Contains(
                "new StringBuilder(\"[RimMind Memory]\")",
                pawnBrief,
                StringComparison.Ordinal);
            Assert.Contains("sb.AppendLine(\"[Long-term]\");", pawnBrief, StringComparison.Ordinal);
            Assert.Contains("store.active.Take(5)", narratorBrief, StringComparison.Ordinal);
            Assert.Contains(
                "new StringBuilder(\"[RimMind Storyteller]\")",
                narratorBrief,
                StringComparison.Ordinal);
            Assert.Equal(3, CountOccurrences(providerSource, "sb.AppendLine($\"- {memory.content}\");"));
            Assert.Equal(1, CountOccurrences(pawnBrief, "return sb.ToString().TrimEnd();"));
            Assert.Equal(1, CountOccurrences(narratorBrief, "return sb.ToString().TrimEnd();"));
        }

        private static string SliceSource(string source, string startMarker, string endMarker)
        {
            int start = source.IndexOf(startMarker, StringComparison.Ordinal);
            int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);

            Assert.True(start >= 0, $"Source marker was not found: {startMarker}");
            Assert.True(end > start, $"Source marker was not found after {startMarker}: {endMarker}");
            return source.Substring(start, end - start);
        }

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static string RemoveWhitespace(string value)
        {
            return string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
        }

        private static void AssertRequestCarriesMergeInput(
            string source,
            int start,
            int end,
            string requestName)
        {
            int builderStart = source.IndexOf(
                "var envelope = LlmRequestEnvelopeBuilder",
                start,
                StringComparison.Ordinal);
            Assert.True(
                builderStart >= start && builderStart < end,
                $"{requestName} request envelope builder was not found in its request block.");

            int npcIdCall = source.IndexOf(
                ".WithNpcId(npcId)",
                builderStart,
                StringComparison.Ordinal);
            int inputCall = source.IndexOf(
                ".WithGameStateInfo(currentQuery)",
                builderStart,
                StringComparison.Ordinal);
            int buildCall = source.IndexOf(
                ".Build();",
                builderStart,
                StringComparison.Ordinal);

            Assert.True(
                npcIdCall >= builderStart && npcIdCall < end,
                $"{requestName} request does not set npcId in its envelope builder.");
            Assert.True(
                inputCall >= builderStart && inputCall < end,
                $"{requestName} request does not carry currentQuery in its envelope builder.");
            Assert.True(
                buildCall >= builderStart && buildCall < end,
                $"{requestName} request envelope build was not found in its request block.");
            Assert.True(
                builderStart < npcIdCall,
                $"{requestName} request sets npcId before its envelope builder.");
            Assert.True(
                npcIdCall < inputCall,
                $"{requestName} request carries currentQuery before setting npcId.");
            Assert.True(
                inputCall < buildCall,
                $"{requestName} request builds before carrying currentQuery.");
        }

        private static string ReadMemorySource(string relativePath)
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null &&
                   !Directory.Exists(Path.Combine(directory.FullName, "RimMind-Memory", "Source")))
            {
                directory = directory.Parent;
            }

            Assert.NotNull(directory);
            return File.ReadAllText(Path.Combine(
                directory!.FullName,
                "RimMind-Memory",
                "Source",
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        [Fact]
        public void Time_context_preserves_elapsed_and_game_date_boundaries()
        {
            ContractCaseRunner.Run(
                ("future events clamp to just now", () =>
                    Assert.Equal("Just now", TimeFormatter.FormatTimeAgo(1000, 0))),
                ("elapsed hours round through the short window", () =>
                {
                    Assert.Equal("About 1h ago", TimeFormatter.FormatTimeAgo(0, 2500));
                    Assert.Equal("About 5h ago", TimeFormatter.FormatTimeAgo(0, 12500));
                }),
                ("same-day history remains today", () =>
                    Assert.Equal("Today", TimeFormatter.FormatTimeAgo(0, 59999))),
                ("three days remains relative", () =>
                    Assert.Equal("3 days ago", TimeFormatter.FormatTimeAgo(0, 180000))),
                ("older history uses a stable game date", () =>
                    Assert.Equal("Day 5 00:00", TimeFormatter.FormatTimeAgo(0, 240000))),
                ("game date preserves day and hour", () =>
                    Assert.Equal("Day 2 06:00", TimeFormatter.FormatGameDate(75000))));
        }

        private static MemoryEntry Entry(string id, float importance, bool pinned = false)
        {
            return new MemoryEntry
            {
                id = id,
                content = id,
                type = MemoryType.Work,
                tick = 1,
                importance = importance,
                isPinned = pinned
            };
        }
    }
}
