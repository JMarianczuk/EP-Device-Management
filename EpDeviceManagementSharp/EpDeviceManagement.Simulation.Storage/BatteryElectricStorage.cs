using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Storage;

public class BatteryElectricStorage : IStorage
{
    private readonly Frequency standingLosses;
    private readonly decimal chargingLosses;
    private readonly decimal dischargingLosses;
    private Energy currentStateOfCharge;

    public BatteryElectricStorage(
        Frequency standingLosses,
        decimal chargingLosses,
        decimal dischargingLosses)
    {
        if (chargingLosses is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chargingLosses));
        }

        if (dischargingLosses is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dischargingLosses));
        }
        this.standingLosses = standingLosses;
        this.chargingLosses = chargingLosses;
        this.dischargingLosses = dischargingLosses;
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
                               (this.chargingLosses * chargeRate
                                - this.dischargingLosses * dischargeRate);
        var standingLoss = (this.standingLosses.Multiply(timeStep)) * this.CurrentStateOfCharge;
        this.currentStateOfCharge = this.CurrentStateOfCharge + chargeDifference;
    }
}