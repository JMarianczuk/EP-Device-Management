using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

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

    protected bool CanRequestIncoming(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var expectedSoC = this.Battery.CurrentStateOfCharge
            + GetGeneratorsEnergy(timeStep, generators)
            - GetLoadsEnergy(timeStep, loads)
            + this.PacketSize;
        return expectedSoC < this.Battery.TotalCapacity;
    }

    protected bool CanRequestOutgoing(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var expectedSoC = this.Battery.CurrentStateOfCharge
            + GetGeneratorsEnergy(timeStep, generators)
            - GetLoadsEnergy(timeStep, loads)
            - this.PacketSize;
        return expectedSoC > Energy.Zero;
    }

    private static Energy GetLoadsEnergy(TimeSpan timeStep, IEnumerable<ILoad> loads)
    {
        var powerSum = loads.Select(x => x.CurrentDemand).Aggregate(Power.Zero, (sum, power) => sum + power);
        var loadsEnergy = powerSum * timeStep;
        return loadsEnergy;
    }

    private static Energy GetGeneratorsEnergy(TimeSpan timeStep, IEnumerable<IGenerator> generators)
    {
        var generatorsSum = generators.Select(x => x.CurrentGeneration)
            .Aggregate(Power.Zero, (sum, power) => sum + power);
        var generatorsEnergy = generatorsSum * timeStep;
        return generatorsEnergy;
    }
}