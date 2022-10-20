using EpDeviceManagement.Contracts;
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

    public bool CanRequestIncoming(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var expectedDischargePower = GetLoadsPower(loads)
                                     - GetGeneratorsPower(generators)
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

    public bool CanRequestOutgoing(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var expectedDischargePower = GetLoadsPower(loads)
                                     - GetGeneratorsPower(generators)
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