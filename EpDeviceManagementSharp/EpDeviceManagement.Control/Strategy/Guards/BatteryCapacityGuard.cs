using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Guards;

public sealed class BatteryCapacityGuard : BatteryGuardBase, IControlGuard
{
    public BatteryCapacityGuard(
        IStorage battery,
        Energy packetSize)
        : base(
            battery,
            packetSize)
    {
    }

    public bool CanRequestIncoming(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var expectedSoC = this.Battery.CurrentStateOfCharge
                          + GetGeneratorsEnergy(timeStep, generators)
                          - GetLoadsEnergy(timeStep, loads)
                          + this.PacketSize;
        return expectedSoC < this.Battery.TotalCapacity;
    }

    public bool CanRequestOutgoing(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var expectedSoC = this.Battery.CurrentStateOfCharge
                       + GetGeneratorsEnergy(timeStep, generators)
                       - GetLoadsEnergy(timeStep, loads)
                       - this.PacketSize;
        return expectedSoC > Energy.Zero;
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
    }
}