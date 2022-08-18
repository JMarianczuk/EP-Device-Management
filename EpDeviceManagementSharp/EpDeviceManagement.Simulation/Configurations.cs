using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Simulation.Storage;
using UnitsNet;

namespace EpDeviceManagement.Simulation;

public readonly struct Configuration
{
    public IStorage Battery { get; init; }

    public Energy PacketSize { get; init; }

    public RandomNumberGenerator Random { get; init; }
}

public readonly struct DataSet
{
    public IEnumerable<EnhancedEnergyDataSet> Data { get; init; }

    public string Configuration { get; init; }
}

public readonly struct BatteryConfiguration
{
    public Func<BatteryElectricStorage> CreateBattery { get; init; }

    public string Description { get; init; }
}

public class SimulationResult
{
    public string BatteryConfiguration { get; set; }

    public int StepsSimulated { get; set; }

    public Energy PacketSize { get; set; }

    public Ratio PacketProbability { get; set; }

    public bool Success { get; set; }

    public BatteryOutOfBoundsReason FailReason { get; set; }

    public string StrategyName { get; set; }

    public string StrategyConfiguration { get; set; }

    public string DataConfiguration { get; set; }

    public int Seed { get; set; }
}

public enum BatteryOutOfBoundsReason
{
    None,
    BelowZero,
    ExceedCapacity,
}