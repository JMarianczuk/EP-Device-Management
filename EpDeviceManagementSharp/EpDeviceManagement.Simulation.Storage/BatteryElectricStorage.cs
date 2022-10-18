using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Storage;

public class BatteryElectricStorage : IStorage
{
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
        this.StandingLosses = standingLosses;
        this.ChargingEfficiency = (decimal) chargingEfficiency.DecimalFractions;
        this.DischargingEfficiency = (decimal) dischargingEfficiency.DecimalFractions;
    }

    public Frequency StandingLosses { get; }
    public decimal ChargingEfficiency { get; }
    public decimal DischargingEfficiency { get; }
    
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
                               (this.ChargingEfficiency * chargeRate
                                - this.DischargingEfficiency * dischargeRate);
        var standingLossRate = this.StandingLosses * this.CurrentStateOfCharge;
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

    public override string ToString()
    {
        return $"{this.CurrentStateOfCharge} / {this.TotalCapacity}";
    }
}