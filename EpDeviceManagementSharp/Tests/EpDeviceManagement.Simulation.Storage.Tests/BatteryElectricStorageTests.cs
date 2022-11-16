using EpDeviceManagement.UnitsExtensions;

namespace EpDeviceManagement.Simulation.Storage.Tests;

public class BatteryElectricStorageTests
{
    //[Fact]
    //public void StandingLossesTest()
    //{
    //    var bes = new BatteryElectricStorage(
    //        Frequency.FromPerSecond(0.0001),
    //        Ratio.FromPercent(99),
    //        Ratio.FromPercent(101))
    //    {
    //        TotalCapacity = EnergyFast.FromKilowattHours(5),
    //        CurrentStateOfCharge = EnergyFast.FromKilowattHours(3),
    //    };
    //    bes.Simulate(TimeSpan.FromMinutes(1), PowerFast.Zero, PowerFast.Zero);
    //    bes.CurrentStateOfCharge.Should().BeLessThan(EnergyFast.FromKilowattHours(3));
    //}

    //[Fact]
    //public void ChargingLossesTest()
    //{
    //    var bes = new BatteryElectricStorage(
    //        Frequency.FromPerSecond(0),
    //        Ratio.FromPercent(99),
    //        Ratio.FromPercent(101))
    //    {
    //        TotalCapacity = EnergyFast.FromKilowattHours(5),
    //        CurrentStateOfCharge = EnergyFast.FromKilowattHours(3),
    //    };
    //    bes.Simulate(TimeSpan.FromHours(1), PowerFast.FromKilowatts(1), PowerFast.Zero);
    //    bes.CurrentStateOfCharge.Should().BeLessThan(EnergyFast.FromKilowattHours(4));
    //}

    //[Fact]
    //public void ChargingTest()
    //{
    //    var bes = new BatteryElectricStorage(
    //        Frequency.FromPerSecond(0),
    //        Ratio.FromPercent(99),
    //        Ratio.FromPercent(101))
    //    {
    //        TotalCapacity = EnergyFast.FromKilowattHours(5),
    //        CurrentStateOfCharge = EnergyFast.FromKilowattHours(3),
    //    };
    //    bes.Simulate(TimeSpan.FromHours(1), PowerFast.FromKilowatts(1), PowerFast.Zero);
    //    bes.CurrentStateOfCharge.Should().BeGreaterThan(EnergyFast.FromKilowattHours(3));
    //}

    //[Fact]
    //public void DischargingLossesTest()
    //{
    //    var bes = new BatteryElectricStorage(
    //        Frequency.FromPerSecond(0),
    //        Ratio.FromPercent(99),
    //        Ratio.FromPercent(101))
    //    {
    //        TotalCapacity = EnergyFast.FromKilowattHours(5),
    //        CurrentStateOfCharge = EnergyFast.FromKilowattHours(3),
    //    };
    //    bes.Simulate(TimeSpan.FromHours(1), PowerFast.Zero, PowerFast.FromKilowatts(1));
    //    bes.CurrentStateOfCharge.Should().BeLessThan(EnergyFast.FromKilowattHours(2));
    //}

    //[Fact]
    //public void ExceedingLowerLimitTest()
    //{
    //    var bes = new BatteryElectricStorage(
    //        Frequency.FromPerSecond(0),
    //        Ratio.FromPercent(99),
    //        Ratio.FromPercent(101))
    //    {
    //        TotalCapacity = EnergyFast.FromKilowattHours(5),
    //        CurrentStateOfCharge = EnergyFast.FromKilowattHours(1),
    //    };
    //    bes.Simulate(TimeSpan.FromHours(2), PowerFast.Zero, PowerFast.FromKilowatts(1));
    //    bes.CurrentStateOfCharge.Should().BeGreaterThanOrEqualTo(Energy.Zero);
    //}

    //[Fact]
    //public void ExceedingUpperLimitTest()
    //{
    //    var bes = new BatteryElectricStorage(
    //        Frequency.FromPerSecond(0),
    //        Ratio.FromPercent(99),
    //        Ratio.FromPercent(101))
    //    {
    //        TotalCapacity = EnergyFast.FromKilowattHours(5),
    //        CurrentStateOfCharge = EnergyFast.FromKilowattHours(4),
    //    };
    //    bes.Simulate(TimeSpan.FromHours(1), PowerFast.FromKilowatts(2), PowerFast.Zero);
    //    bes.CurrentStateOfCharge.Should().BeLessThanOrEqualTo(Energy.FromKilowattHours(5));
    //}
}