using System.Globalization;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Guards;

public sealed class BatteryPowerGuard : BatteryGuardBase, IControlGuard
{
    private readonly PowerFast outgoingGuardBuffer;

    public BatteryPowerGuard(
        IStorage battery,
        EnergyFast packetSize,
        PowerFast outgoingGuardBuffer)
        : base(
            battery,
            packetSize)
    {
        this.outgoingGuardBuffer = outgoingGuardBuffer;
    }

    public string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"{this.outgoingGuardBuffer.Kilowatts:F0}");

    public bool CanRequestIncoming(TimeSpan timeStep, ILoad load, IGenerator generator)
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
            return expectedChargePower < this.Battery.MaximumChargePower;
        }
    }

    public bool CanRequestOutgoing(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var expectedDischargePower = load.MomentaryDemand
                                     - generator.MomentaryGeneration
                                     + this.PacketSize / timeStep;
        if (expectedDischargePower > PowerFast.Zero)
        {
            return expectedDischargePower + this.outgoingGuardBuffer < this.Battery.MaximumDischargePower;
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