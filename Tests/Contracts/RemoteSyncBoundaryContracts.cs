using System;
using System.IO;
using System.Threading.Tasks;
using RimMind.Testing;
using Xunit;

namespace RimMind.Memory.Tests.Contracts
{
    public sealed class RemoteSyncBoundaryContracts
    {
        [Fact]
        public Task Remote_sync_uses_the_public_Core_capability()
        {
            return ContractCaseRunner.RunAsync(
                ("unconfigured sync is a no-op", () => VerifySource(source =>
                    Assert.Contains("RimMindAPI.RemoteSync.IsConfigured", source, StringComparison.Ordinal))),
                ("load delegates through RimMindAPI", () => VerifySource(source =>
                    Assert.Contains("RimMindAPI.RemoteSync.SyncOnLoadAsync", source, StringComparison.Ordinal))),
                ("push delegates through RimMindAPI", () => VerifySource(source =>
                    Assert.Contains("RimMindAPI.RemoteSync.EnqueuePushAsync", source, StringComparison.Ordinal))),
                ("remote failure leaves local memory intact", () => VerifySource(source =>
                {
                    Assert.Contains("if (result.IsErr)", source, StringComparison.Ordinal);
                    Assert.Contains("return;", source, StringComparison.Ordinal);
                })),
                ("Memory has no service locator escape hatch", () => VerifySource(source =>
                {
                    Assert.DoesNotContain("RimMindServiceLocator", source, StringComparison.Ordinal);
                    Assert.DoesNotContain("GetRemoteSync", source, StringComparison.Ordinal);
                    Assert.DoesNotContain("IRemoteSyncService", source, StringComparison.Ordinal);
                })));
        }

        private static Task VerifySource(Action<string> assertion)
        {
            assertion(File.ReadAllText(ComponentPath()));
            return Task.CompletedTask;
        }

        private static string ComponentPath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "RimMind-Memory", "Source", "Data", "RimMindMemoryWorldComponent.cs")))
                directory = directory.Parent;
            return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."),
                "RimMind-Memory", "Source", "Data", "RimMindMemoryWorldComponent.cs");
        }
    }
}
