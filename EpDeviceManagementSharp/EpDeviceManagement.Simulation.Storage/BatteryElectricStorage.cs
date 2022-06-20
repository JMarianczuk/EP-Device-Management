using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Storage;

public class BatteryElectricStorage : IStorage
{
    private readonly Frequency standingLosses;
    private readonly decimal chargingEfficiency;
    private readonly decimal dischargingEfficiency;
    private Energy currentStateOfCharge;

    public BatteryElectricStorage(
        Frequency standingLosses,
        Ratio chargingEfficiency,
        Ratio dischargingEfficiency)
    {
        if (chargingEfficiency.DecimalFractions is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chargingEfficiency));
        }

        if (dischargingEfficiency.DecimalFractions is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dischargingEfficiency));
        }
        this.standingLosses = standingLosses;
        this.chargingEfficiency = (decimal) chargingEfficiency.DecimalFractions;
        this.dischargingEfficiency = (decimal) dischargingEfficiency.DecimalFractions;
    }
    
    public Energy TotalCapacity { get; init; }

    public Energy CurrentStateOfCharge
    {
        get => currentStateOfCharge;
        init => currentStateOfCharge = value;
    }

    public Power MaximumChargePower { get; init; }
    public Power MaximumDischargePower { get; init; }

    public void Simulate(TimeSpan timeStep, Power chargeRate, Power dischargeRate)
    {
        var chargeDifference = timeStep *
                               (this.chargingEfficiency * chargeRate
                                - this.dischargingEfficiency * dischargeRate);
        var standingLossRate = this.standingLosses.Times(this.CurrentStateOfCharge);
        var standingLoss = standingLossRate * timeStep;
        var newSoC = this.CurrentStateOfCharge + chargeDifference - standingLoss;
        if (newSoC < Energy.Zero)
        {
            newSoC = Energy.Zero;
        }

        if (newSoC > this.TotalCapacity)
        {
            newSoC = this.TotalCapacity;
        }

        this.currentStateOfCharge = newSoC;
    }
}