using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Guards;

public abstract class BatteryGuardBase
{
    protected BatteryGuardBase(
        IStorage battery,
        EnergyFast packetSize)
    {
        Battery = battery;
        PacketSize = packetSize;
    }

    protected IStorage Battery { get; }

    protected EnergyFast PacketSize { get; }
}