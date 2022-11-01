using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Storage;

public class BatteryElectricStorage2 : IStorage
{
    private Energy currentStateOfCharge;

    public BatteryElectricStorage2(
        Power standbyPower,
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
        this.ChargingEfficiency = (decimal) chargingEfficiency.DecimalFractions;
        this.DischargingEfficiency = (decimal) dischargingEfficiency.DecimalFractions;
    }

    public Power StandbyPower { get; }
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
        var newSoC = TrySimulate(timeStep, chargeRate, dischargeRate);

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

    public Energy TrySimulate(TimeSpan timeStep, Power chargeRate, Power dischargeRate)
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