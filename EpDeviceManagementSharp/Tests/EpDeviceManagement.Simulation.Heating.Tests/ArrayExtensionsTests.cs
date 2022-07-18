namespace EpDeviceManagementSharp.Simulation.Heating.Tests;

public class ArrayExtensionsTests
{
    private int[] Numbers = Enumerable.Range(0, 10).ToArray();

    [Fact]
    public void IterateAllNumbers()
    {
        Numbers.Iterate(0, 10).Should().BeEquivalentTo(Enumerable.Range(0, 10));
    }

    [Fact]
    public void IteratePartialFromTheBeginning()
    {
        Numbers.Iterate(0, 4).Should().BeEquivalentTo(Enumerable.Range(0, 4));
    }

    [Fact]
    public void IteratePartialToTheEnd()
    {
        Numbers.Iterate(7, 3).Should().BeEquivalentTo(Enumerable.Range(7, 3));
    }

    [Fact]
    public void IteratePartial()
    {
        Numbers.Iterate(2, 5).Should().BeEquivalentTo(Enumerable.Range(2, 5));
    }

    [Fact]
    public void LoopOnce()
    {
        Numbers.Iterate(6, 10).Should().BeEquivalentTo(Enumerable.Range(6, 4).Concat(Enumerable.Range(0, 6)));
    }

    [Fact]
    public void LoopTwice()
    {
        Numbers.Iterate(0, 30).Should()
            .BeEquivalentTo(Enumerable.Range(0, 10).Concat(Enumerable.Range(0, 10)).Concat(Enumerable.Range(0, 10)));
    }

}