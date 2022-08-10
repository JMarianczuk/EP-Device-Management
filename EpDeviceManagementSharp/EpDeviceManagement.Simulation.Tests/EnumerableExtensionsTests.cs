namespace EpDeviceManagement.Simulation.Tests;

public class EnumerableExtensionsTests
{
    [Fact]
    public void CartesianProduct()
    {
        var first = Enumerable.Range(0, 5);
        var second = Enumerable.Range(0, 2);
    }
}