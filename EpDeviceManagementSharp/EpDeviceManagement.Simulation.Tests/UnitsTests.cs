using FluentAssertions;
using UnitsNet;
using UnitsNet.Units;

namespace EpDeviceManagement.Simulation.Tests;

public class UnitsTests
{
    [Fact]
    public void EnergyMinTest()
    {
        var left = Energy.FromKilowattHours(3);
        var right = Energy.FromKilowattHours(2);
        Units.Min(left, right).Should().Be(Energy.FromKilowattHours(2));
    }

    [Fact]
    public void PowerMaxTest()
    {
        var left = Power.FromKilowatts(1);
        var right = Power.FromKilojoulesPerHour(40);
        Units.Max(left, right).Should().Be(Power.FromKilowatts(1));
    }
}