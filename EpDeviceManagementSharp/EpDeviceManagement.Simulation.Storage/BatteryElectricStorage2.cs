using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Storage;

public class BatteryElectricStorage2 : IStorage
{
    private EnergyFast currentStateOfCharge;

    public BatteryElectricStorage2(
        PowerFast standbyPower,
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
        this.StandbyPower = standbyPower;
        this.ChargingEfficiency = chargingEfficiency.DecimalFractions;
        this.DischargingEfficiency = dischargingEfficiency.DecimalFractions;
    }

    public PowerFast StandbyPower { get; }
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
        var newSoC = TrySimulate(timeStep, chargeRate, dischargeRate);

        TrySetNewSoc(newSoC);
    }

    public void TrySetNewSoc(EnergyFast newSoC)
    {
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

    public EnergyFast TrySimulate(TimeSpan timeStep, PowerFast chargeRate, PowerFast dischargeRate)
    {
        var chargeDifference = timeStep *
                               (this.ChargingEfficiency * chargeRate
                                - this.DischargingEfficiency * dischargeRate);
        var standingLoss = this.StandbyPower * timeStep;
        var newSoC = this.CurrentStateOfCharge + chargeDifference - standingLoss;

        return newSoC;
    }

    public override string ToString()
    {
        return $"{this.CurrentStateOfCharge} / {this.TotalCapacity}";
    }
}