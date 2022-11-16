using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Storage;

public class BatteryElectricStorage : IStorage
{
    private EnergyFast currentStateOfCharge;

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
        this.ChargingEfficiency = chargingEfficiency.DecimalFractions;
        this.DischargingEfficiency = dischargingEfficiency.DecimalFractions;
    }

    public Frequency StandingLosses { get; }
    public double ChargingEfficiency { get; }
    public double DischargingEfficiency { get; }
    
    public EnergyFast TotalCapacity { get; init; }

    public EnergyFast CurrentStateOfCharge
    {
        get => currentStateOfCharge;
        init => currentStateOfCharge = value;
    }

    public PowerFast MaximumChargePower { get; init; }
    public PowerFast MaximumDischargePower { get; init; }

    public void Simulate(TimeSpan timeStep, PowerFast chargeRate, PowerFast dischargeRate)
    {
        var chargeDifference = timeStep *
                               (this.ChargingEfficiency * chargeRate
                                - this.DischargingEfficiency * dischargeRate);
        var standingLossRate = this.StandingLosses * this.CurrentStateOfCharge;
        var standingLoss = standingLossRate * timeStep;
        var newSoC = this.CurrentStateOfCharge + chargeDifference - standingLoss;
        if (newSoC < EnergyFast.Zero)
        {
            newSoC = EnergyFast.Zero;
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