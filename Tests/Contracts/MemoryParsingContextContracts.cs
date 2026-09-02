using System;
using System.IO;
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
                ("public providers retain the bridge brief contract", PublicProvidersRetainBridgeBriefContract),
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
            string source = ReadMemorySource("Injection/MemoryContextProvider.cs");

            Assert.Contains("\"memory.pawn_brief\"", source, StringComparison.Ordinal);
            Assert.Contains("\"memory.narrator_brief\"", source, StringComparison.Ordinal);
            Assert.Contains("RimMindAPI.Providers.RegisterPawnProvider", source, StringComparison.Ordinal);
            Assert.Contains("RimMindAPI.Providers.RegisterStaticProvider", source, StringComparison.Ordinal);
            Assert.Contains("[RimMind Memory]", source, StringComparison.Ordinal);
            Assert.Contains("[Long-term]", source, StringComparison.Ordinal);
            Assert.Contains("[RimMind Storyteller]", source, StringComparison.Ordinal);
            Assert.Contains("Take(5)", source, StringComparison.Ordinal);
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
