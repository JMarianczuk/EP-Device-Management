using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Guards;

public sealed class BatteryPowerGuard : BatteryGuardBase, IControlGuard
{
    public BatteryPowerGuard(
        IStorage battery,
        Energy packetSize)
        : base(
            battery,
            packetSize)
    {
    }

    public bool CanRequestIncoming(TimeSpan timeStep, ILoad[] loads, IGenerator[] generators)
    {
        var expectedDischargePower = loads.Sum()
                                     - generators.Sum()
                                     - this.PacketSize / timeStep;
        if (expectedDischargePower > Power.Zero)
        {
            return expectedDischargePower <= this.Battery.MaximumDischargePower;
        }
        else
        {
            var expectedChargePower = -expectedDischargePower;
            return expectedChargePower <= this.Battery.MaximumChargePower;
        }
    }

    public bool CanRequestOutgoing(TimeSpan timeStep, ILoad[] loads, IGenerator[] generators)
    {
        var expectedDischargePower = loads.Sum()
                                     - generators.Sum()
                                     + this.PacketSize / timeStep;
        if (expectedDischargePower > Power.Zero)
        {
            return expectedDischargePower <= this.Battery.MaximumDischargePower;
        }
        else
        {
            var expectedChargePower = -expectedDischargePower;
            return expectedChargePower <= this.Battery.MaximumChargePower;
        }
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
    }
}