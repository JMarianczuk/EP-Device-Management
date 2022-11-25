using System.Globalization;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Guards;

public sealed class BatteryPowerGuard : BatteryGuardBase, IControlGuard
{
    private readonly PowerFast receiveGuardMargin;
    private readonly PowerFast sendGuardMargin;

    public BatteryPowerGuard(
        IStorage battery,
        EnergyFast packetSize,
        PowerFast receiveGuardMargin,
        PowerFast sendGuardMargin,
        bool strategyRequestsOutgoing)
        : base(
            battery,
            packetSize)
    {
        this.receiveGuardMargin = receiveGuardMargin;
        this.sendGuardMargin = sendGuardMargin;
        if (strategyRequestsOutgoing)
        {
            this.Configuration = string.Create(CultureInfo.InvariantCulture,
                $"{this.receiveGuardMargin.Kilowatts:F1}, {this.sendGuardMargin.Kilowatts:F1}");
        }
        else
        {
            this.Configuration = string.Create(CultureInfo.InvariantCulture,
                $"{this.receiveGuardMargin.Kilowatts:F1}");
        }
    }

    public string Configuration { get; }

    public bool CanRequestToReceive(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var expectedDischargePower = load.MomentaryDemand
                                     - generator.MomentaryGeneration
                                     - this.PacketSize / timeStep;
        if (expectedDischargePower > PowerFast.Zero)
        {
            return true;
        }
        else
        {
            var expectedChargePower = -expectedDischargePower;
            return expectedChargePower + this.receiveGuardMargin < this.Battery.MaximumChargePower;
        }
    }

    public bool CanRequestToSend(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var expectedDischargePower = load.MomentaryDemand
                                     - generator.MomentaryGeneration
                                     + this.PacketSize / timeStep;
        if (expectedDischargePower > PowerFast.Zero)
        {
            return expectedDischargePower + this.sendGuardMargin < this.Battery.MaximumDischargePower;
        }
        else
        {
            return true;
        }
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
    }
}