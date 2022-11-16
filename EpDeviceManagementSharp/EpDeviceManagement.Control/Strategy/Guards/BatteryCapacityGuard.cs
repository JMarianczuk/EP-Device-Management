using System.Globalization;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Guards;

public sealed class BatteryCapacityGuard : BatteryGuardBase, IControlGuard
{
    public BatteryCapacityGuard(
        IStorage battery,
        EnergyFast packetSize)
        : base(
            battery,
            packetSize)
    {
    }

    public bool CanRequestIncoming(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var expectedSoC = this.Battery.CurrentStateOfCharge
                          + timeStep * generator.MomentaryGeneration
                          - timeStep * load.MomentaryDemand
                          + this.PacketSize;
        return expectedSoC < this.Battery.TotalCapacity;
    }

    public bool CanRequestOutgoing(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var expectedSoC = this.Battery.CurrentStateOfCharge
                       + timeStep * generator.MomentaryGeneration
                       - timeStep * load.MomentaryDemand
                       - this.PacketSize;
        return expectedSoC > EnergyFast.Zero;
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
    }
}