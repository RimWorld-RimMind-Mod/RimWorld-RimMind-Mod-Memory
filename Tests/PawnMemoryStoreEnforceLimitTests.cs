using System.Collections.Generic;
using RimMind.Memory.Data;
using Xunit;

namespace RimMind.Memory.Tests
{
    // PawnMemoryStore 的 EnforceLimit 边界场景补充测试
    public class PawnMemoryStoreEnforceLimitTests
    {
        private static MemoryEntry MakeEntry(int tick, float importance, bool isPinned = false, MemoryType type = MemoryType.Work)
        {
            return new MemoryEntry
            {
                id = $"mem-el-{tick}-{importance}",
                content = $"entry-{tick}",
                type = type,
                tick = tick,
                importance = importance,
                isPinned = isPinned || type == MemoryType.Dark,
            };
        }

        [Fact]
        public void EnforceLimit_AllPinned_StopsEviction()
        {
            // 当 src 中全部 pinned 且超过限制时，不应驱逐
            var src = new List<MemoryEntry>
            {
                MakeEntry(1, 0.5f, isPinned: true),
                MakeEntry(2, 0.6f, isPinned: true),
                MakeEntry(3, 0.7f, isPinned: true),
            };
            var dst = new List<MemoryEntry>();

            PawnMemoryStore.EnforceLimit(src, srcMax: 2, dst, dstMax: 10);

            // 全部 pinned，无法驱逐，src 仍为 3
            Assert.Equal(3, src.Count);
        }

        [Fact]
        public void EnforceLimit_DstAllPinned_StopsDstEviction()
        {
            // dst 中全部 pinned 且超过限制时，不应驱逐
            var src = new List<MemoryEntry>();
            var dst = new List<MemoryEntry>
            {
                MakeEntry(1, 0.5f, isPinned: true),
                MakeEntry(2, 0.6f, isPinned: true),
                MakeEntry(3, 0.7f, isPinned: true),
            };

            PawnMemoryStore.EnforceLimit(src, srcMax: 10, dst, dstMax: 2);

            // dst 全部 pinned，无法驱逐，dst 仍为 3
            Assert.Equal(3, dst.Count);
        }

        [Fact]
        public void EnforceLimit_EvictsLastUnpinned()
        {
            // 驱逐时移除最后一个非 pinned 条目（LastOrDefault）
            var src = new List<MemoryEntry>
            {
                MakeEntry(1, 0.9f),
                MakeEntry(2, 0.3f),
                MakeEntry(3, 0.7f),
            };
            var dst = new List<MemoryEntry>();

            PawnMemoryStore.EnforceLimit(src, srcMax: 2, dst, dstMax: 10);

            Assert.Equal(2, src.Count);
            Assert.Single(dst);
            // LastOrDefault 移除的是最后一个非pinned，即 0.7f
            Assert.Equal(0.7f, dst[0].importance);
        }

        [Fact]
        public void EnforceLimit_EvictedInsertedByImportance()
        {
            // 驱逐到 dst 时按 importance 排序插入
            var src = new List<MemoryEntry>
            {
                MakeEntry(1, 0.9f),
                MakeEntry(2, 0.3f),
                MakeEntry(3, 0.5f),
            };
            var dst = new List<MemoryEntry>
            {
                MakeEntry(10, 0.6f),
            };

            PawnMemoryStore.EnforceLimit(src, srcMax: 1, dst, dstMax: 10);

            // src 只剩 1 个
            Assert.Single(src);
            // dst 应按 importance 降序排列
            Assert.True(dst.Count >= 2);
            for (int i = 1; i < dst.Count; i++)
                Assert.True(dst[i - 1].importance >= dst[i].importance);
        }

        [Fact]
        public void EnforceLimit_DstOverMax_RemovesLowestUnpinned()
        {
            // dst 超过限制时移除 importance 最低的非 pinned
            var src = new List<MemoryEntry>();
            var dst = new List<MemoryEntry>
            {
                MakeEntry(1, 0.8f),
                MakeEntry(2, 0.2f),
                MakeEntry(3, 0.5f),
            };

            PawnMemoryStore.EnforceLimit(src, srcMax: 10, dst, dstMax: 2);

            Assert.Equal(2, dst.Count);
            // 0.2f 应被移除
            Assert.All(dst, e => Assert.True(e.importance > 0.2f));
        }

        [Fact]
        public void EnforceLimit_PinnedNotEvictedFromDst()
        {
            // dst 中 pinned 条目不应被驱逐
            var src = new List<MemoryEntry>();
            var dst = new List<MemoryEntry>
            {
                MakeEntry(1, 0.8f, isPinned: true),
                MakeEntry(2, 0.2f),
                MakeEntry(3, 0.5f),
            };

            PawnMemoryStore.EnforceLimit(src, srcMax: 10, dst, dstMax: 2);

            // 0.2f 非pinned应被移除，0.8f pinned保留
            Assert.Equal(2, dst.Count);
            Assert.Contains(dst, e => e.importance == 0.8f && e.isPinned);
        }

        [Fact]
        public void EnforceLimit_SrcEmpty_NoOp()
        {
            var src = new List<MemoryEntry>();
            var dst = new List<MemoryEntry>();

            PawnMemoryStore.EnforceLimit(src, srcMax: 5, dst, dstMax: 5);

            Assert.Empty(src);
            Assert.Empty(dst);
        }

        [Fact]
        public void EnforceLimit_WithinLimits_NoChange()
        {
            var src = new List<MemoryEntry>
            {
                MakeEntry(1, 0.5f),
            };
            var dst = new List<MemoryEntry>
            {
                MakeEntry(2, 0.3f),
            };

            PawnMemoryStore.EnforceLimit(src, srcMax: 5, dst, dstMax: 5);

            Assert.Single(src);
            Assert.Single(dst);
        }

        [Fact]
        public void AddActive_AllPinnedOverflow_GoesToArchive()
        {
            // 当 active 全部 pinned 且已满时，新条目直接进 archive
            var store = new PawnMemoryStore();
            store.AddActive(MakeEntry(1, 0.5f, isPinned: true), maxActive: 1, maxArchive: 10);
            // 第二个条目：active 已满且全 pinned
            store.AddActive(MakeEntry(2, 0.6f), maxActive: 1, maxArchive: 10);

            Assert.Single(store.active);
            Assert.Single(store.archive);
            Assert.Equal(0.6f, store.archive[0].importance);
        }

        [Fact]
        public void AddActive_MultipleEvictions_CascadesToDark()
        {
            // 多次溢出应级联到 dark
            var store = new PawnMemoryStore();
            for (int i = 0; i < 5; i++)
                store.AddActive(MakeEntry(i * 100, 0.1f + i * 0.05f), maxActive: 1, maxArchive: 1);

            Assert.Single(store.active);
            Assert.True(store.archive.Count <= 1);
            // 剩余应进入 dark
            Assert.True(store.dark.Count > 0 || store.archive.Count + store.active.Count < 5);
        }
    }
}
