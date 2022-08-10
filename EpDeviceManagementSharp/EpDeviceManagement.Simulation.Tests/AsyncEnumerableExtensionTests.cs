using FluentAssertions;

namespace EpDeviceManagement.Simulation.Tests
{
    public class AsyncEnumerableExtensionTests
    {
        [Fact]
        public async Task AsAsync()
        {
            var seq = Enumerable.Range(0, 10).AsAsyncEnumerable();
            var collect = new List<int>();
            await foreach (var i in seq)
            {
                collect.Add(i);
            }

            collect.Should().BeEquivalentTo(Enumerable.Range(0, 10));
        }

        [Fact]
        public async Task Collect()
        {
            var seq = Enumerable.Range(0, 10).AsAsyncEnumerable();
            var collect = await seq.CollectAsync();
            collect.Should().BeEquivalentTo(Enumerable.Range(0, 10));
        }

        [Fact]
        public async Task SkipLastZero()
        {
            var seq = Enumerable.Range(0, 10).AsAsyncEnumerable();
            var skipped = seq.SkipLast(0);
            var collect = await skipped.CollectAsync();
            collect.Should().BeEquivalentTo(Enumerable.Range(0, 10));
        }

        [Fact]
        public async Task SkipLastSome()
        {
            var seq = Enumerable.Range(0, 10).AsAsyncEnumerable();
            var skipped = seq.SkipLast(4);
            var collect = await skipped.CollectAsync();
            collect.Should().BeEquivalentTo(Enumerable.Range(0, 6));
        }

        [Fact]
        public async Task SkipLastAll()
        {
            var seq = Enumerable.Range(0, 10).AsAsyncEnumerable();
            var skipped = seq.SkipLast(10);
            var collect = await skipped.CollectAsync();
            collect.Should().BeEmpty();
        }

        [Fact]
        public async Task SkipLastMoreThanAll()
        {
            var seq = Enumerable.Range(0, 10).AsAsyncEnumerable();
            var skipped = seq.SkipLast(24);
            var collect = await skipped.CollectAsync();
            collect.Should().BeEmpty();
        }
    }
}