namespace EpDeviceManagement.Simulation.Storage.Tests;

public class BatteryElectricStorageTests
{
    [Fact]
    public void StandingLossesTest()
    {
        var bes = new BatteryElectricStorage(
            Frequency.FromPerSecond(0.0001),
            Ratio.FromPercent(99),
            Ratio.FromPercent(101))
        {
            TotalCapacity = Energy.FromKilowattHours(5),
            CurrentStateOfCharge = Energy.FromKilowattHours(3),
        };
        bes.Simulate(TimeSpan.FromMinutes(1), Power.Zero, Power.Zero);
        bes.CurrentStateOfCharge.Should().BeLessThan(Energy.FromKilowattHours(3));
    }

    [Fact]
    public void ChargingLossesTest()
    {
        var bes = new BatteryElectricStorage(
            Frequency.FromPerSecond(0),
            Ratio.FromPercent(99),
            Ratio.FromPercent(101))
        {
            TotalCapacity = Energy.FromKilowattHours(5),
            CurrentStateOfCharge = Energy.FromKilowattHours(3),
        };
        bes.Simulate(TimeSpan.FromHours(1), Power.FromKilowatts(1), Power.Zero);
        bes.CurrentStateOfCharge.Should().BeLessThan(Energy.FromKilowattHours(4));
    }

    [Fact]
    public void ChargingTest()
    {
        var bes = new BatteryElectricStorage(
            Frequency.FromPerSecond(0),
            Ratio.FromPercent(99),
            Ratio.FromPercent(101))
        {
            TotalCapacity = Energy.FromKilowattHours(5),
            CurrentStateOfCharge = Energy.FromKilowattHours(3),
        };
        bes.Simulate(TimeSpan.FromHours(1), Power.FromKilowatts(1), Power.Zero);
        bes.CurrentStateOfCharge.Should().BeGreaterThan(Energy.FromKilowattHours(3));
    }

    [Fact]
    public void DischargingLossesTest()
    {
        var bes = new BatteryElectricStorage(
            Frequency.FromPerSecond(0),
            Ratio.FromPercent(99),
            Ratio.FromPercent(101))
        {
            TotalCapacity = Energy.FromKilowattHours(5),
            CurrentStateOfCharge = Energy.FromKilowattHours(3),
        };
        bes.Simulate(TimeSpan.FromHours(1), Power.Zero, Power.FromKilowatts(1));
        bes.CurrentStateOfCharge.Should().BeLessThan(Energy.FromKilowattHours(2));
    }

    [Fact]
    public void ExceedingLowerLimitTest()
    {
        var bes = new BatteryElectricStorage(
            Frequency.FromPerSecond(0),
            Ratio.FromPercent(99),
            Ratio.FromPercent(101))
        {
            TotalCapacity = Energy.FromKilowattHours(5),
            CurrentStateOfCharge = Energy.FromKilowattHours(1),
        };
        bes.Simulate(TimeSpan.FromHours(2), Power.Zero, Power.FromKilowatts(1));
        bes.CurrentStateOfCharge.Should().BeGreaterThanOrEqualTo(Energy.Zero);
    }

    [Fact]
    public void ExceedingUpperLimitTest()
    {
        var bes = new BatteryElectricStorage(
            Frequency.FromPerSecond(0),
            Ratio.FromPercent(99),
            Ratio.FromPercent(101))
        {
            TotalCapacity = Energy.FromKilowattHours(5),
            CurrentStateOfCharge = Energy.FromKilowattHours(4),
        };
        bes.Simulate(TimeSpan.FromHours(1), Power.FromKilowatts(2), Power.Zero);
        bes.CurrentStateOfCharge.Should().BeLessThanOrEqualTo(Energy.FromKilowattHours(5));
    }
}