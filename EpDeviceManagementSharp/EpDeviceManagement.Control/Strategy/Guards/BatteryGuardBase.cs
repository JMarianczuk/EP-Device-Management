using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Guards;

public abstract class BatteryGuardBase
{
    protected BatteryGuardBase(
        IStorage battery,
        Energy packetSize)
    {
        Battery = battery;
        PacketSize = packetSize;
    }

    protected IStorage Battery { get; }

    protected Energy PacketSize { get; }

    protected static Energy GetLoadsEnergy(TimeSpan timeStep, ILoad[] loads)
    {
        var loadsEnergy = loads.Sum() * timeStep;
        return loadsEnergy;
    }

    protected static Energy GetGeneratorsEnergy(TimeSpan timeStep, IGenerator[] generators)
    {
        var generatorsSum = generators.Sum();
        var generatorsEnergy = generatorsSum * timeStep;
        return generatorsEnergy;
    }
}