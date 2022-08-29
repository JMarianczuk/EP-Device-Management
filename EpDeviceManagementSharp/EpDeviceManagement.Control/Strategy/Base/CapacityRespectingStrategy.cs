using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Base;

public abstract class CapacityRespectingStrategy
{
    protected CapacityRespectingStrategy(
        IStorage battery,
        Energy packetSize)
    {
        Battery = battery;
        PacketSize = packetSize;
    }

    protected IStorage Battery { get; }

    protected Energy PacketSize { get; }

    protected virtual bool CanRequestIncoming(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var expectedSoC = Battery.CurrentStateOfCharge
            + GetGeneratorsEnergy(timeStep, generators)
            - GetLoadsEnergy(timeStep, loads)
            + PacketSize;
        return expectedSoC < Battery.TotalCapacity;
    }

    protected virtual bool CanRequestOutgoing(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var expectedSoC = Battery.CurrentStateOfCharge
            + GetGeneratorsEnergy(timeStep, generators)
            - GetLoadsEnergy(timeStep, loads)
            - PacketSize;
        return expectedSoC > Energy.Zero;
    }

    protected static Power GetLoadsPower(IEnumerable<ILoad> loads)
    {
        var powerSum = loads.Select(x => x.CurrentDemand).Aggregate(Power.Zero, (sum, power) => sum + power);
        return powerSum;
    }

    private static Energy GetLoadsEnergy(TimeSpan timeStep, IEnumerable<ILoad> loads)
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

    private static Energy GetGeneratorsEnergy(TimeSpan timeStep, IEnumerable<IGenerator> generators)
    {
        var generatorsSum = GetGeneratorsPower(generators);
        var generatorsEnergy = generatorsSum * timeStep;
        return generatorsEnergy;
    }
}