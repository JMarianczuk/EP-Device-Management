using EpDeviceManagement.Contracts;
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

    protected static Power GetLoadsPower(IEnumerable<ILoad> loads)
    {
        var powerSum = loads.Select(x => x.CurrentDemand).Aggregate(Power.Zero, (sum, power) => sum + power);
        return powerSum;
    }

    protected static Energy GetLoadsEnergy(TimeSpan timeStep, IEnumerable<ILoad> loads)
    {
        var loadsEnergy = GetLoadsPower(loads) * timeStep;
        return loadsEnergy;
    }

    protected static Power GetGeneratorsPower(IEnumerable<IGenerator> generators)
    {
        var generatorsSum = generators.Select(x => x.CurrentGeneration)
            .Aggregate(Power.Zero, (sum, power) => sum + power);
        return generatorsSum;
    }

    protected static Energy GetGeneratorsEnergy(TimeSpan timeStep, IEnumerable<IGenerator> generators)
    {
        var generatorsSum = GetGeneratorsPower(generators);
        var generatorsEnergy = generatorsSum * timeStep;
        return generatorsEnergy;
    }
}