using System.Threading.Tasks;
using RimMind.Domain.ValueObjects;
using RimMind.Memory.Data;
using RimMind.Testing;
using Xunit;

namespace RimMind.Memory.Tests.Contracts
{
    public sealed class RemoteSyncBoundaryContracts
    {
        [Fact]
        public async Task Remote_sync_applies_only_successful_nonempty_snapshots()
        {
            await ContractCaseRunner.RunAsync(
                ("remote failure leaves local memory intact", async () =>
                {
                    int scheduled = 0;
                    int merged = 0;
                    await MemoryRemoteSyncCoordinator.PullAndMergeAsync(
                        () => Task.FromResult(Result<string?, RimMindError>.Err(
                            RimMindErrors.RemoteBackendFailed("offline"))),
                        action => scheduled++,
                        json => merged++,
                        _ => { });
                    Assert.Equal(0, scheduled);
                    Assert.Equal(0, merged);
                }),
                ("blank success is a no-op", async () =>
                {
                    int merged = 0;
                    await MemoryRemoteSyncCoordinator.PullAndMergeAsync(
                        () => Task.FromResult(Result<string?, RimMindError>.Ok(" ")),
                        action => action(),
                        json => merged++,
                        _ => { });
                    Assert.Equal(0, merged);
                }),
                ("successful payload is scheduled before merge", async () =>
                {
                    bool scheduled = false;
                    string? merged = null;
                    await MemoryRemoteSyncCoordinator.PullAndMergeAsync(
                        () => Task.FromResult(Result<string?, RimMindError>.Ok("{\"ok\":true}")),
                        action =>
                        {
                            scheduled = true;
                            action();
                        },
                        json => merged = json,
                        _ => { });
                    Assert.True(scheduled);
                    Assert.Equal("{\"ok\":true}", merged);
                }),
                ("transport exceptions are isolated", async () =>
                {
                    int merged = 0;
                    string? warning = null;
                    await MemoryRemoteSyncCoordinator.PullAndMergeAsync(
                        () => Task.FromException<Result<string?, RimMindError>>(
                            new System.InvalidOperationException("boom")),
                        action => action(),
                        json => merged++,
                        message => warning = message);
                    Assert.Equal(0, merged);
                    Assert.Contains("boom", warning);
                }));
        }
    }
}
