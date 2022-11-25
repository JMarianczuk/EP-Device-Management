using System.Globalization;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Guards;

public sealed class BatteryCapacityGuard : BatteryGuardBase, IControlGuard
{
    private readonly EnergyFast bufferedCapacity;
    private readonly EnergyFast bufferedZero;

    public BatteryCapacityGuard(
        IStorage battery,
        EnergyFast packetSize,
        EnergyFast emptyBuffer,
        EnergyFast fullBuffer,
        bool strategyRequestsOutgoingPackets)
        : base(
            battery,
            packetSize)
    {
        this.bufferedCapacity = this.Battery.TotalCapacity - fullBuffer;
        this.bufferedZero = emptyBuffer;
        if (strategyRequestsOutgoingPackets)
        {
            this.Configuration = string.Create(CultureInfo.InvariantCulture,
                $"{emptyBuffer.KilowattHours:F1}, {fullBuffer.KilowattHours:F1}");
        }
        else
        {
            this.Configuration = string.Create(CultureInfo.InvariantCulture,
                $"{fullBuffer.KilowattHours:F1}");
        }
    }

    public string Configuration { get; }

    public bool CanRequestToReceive(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var expectedSoC = this.Battery.CurrentStateOfCharge
                          + timeStep * generator.MomentaryGeneration
                          - timeStep * load.MomentaryDemand
                          + this.PacketSize;
        return expectedSoC < this.bufferedCapacity;
    }

    public bool CanRequestToSend(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var expectedSoC = this.Battery.CurrentStateOfCharge
                       + timeStep * generator.MomentaryGeneration
                       - timeStep * load.MomentaryDemand
                       - this.PacketSize;
        return expectedSoC > bufferedZero;
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
    }
}