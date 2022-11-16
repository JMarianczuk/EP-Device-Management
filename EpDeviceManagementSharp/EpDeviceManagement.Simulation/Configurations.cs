using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Simulation.Storage;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation;

public class Configuration
{
    public IStorage Battery { get; init; }

    public EnergyFast PacketSize { get; init; }

    public DataSet DataSet { get; init; }

    public RandomNumberGenerator Random { get; init; }
}

public class DataSet
{
    public IReadOnlyList<List<EnhancedPowerDataSet>> Data { get; init; } =
        Array.Empty<List<EnhancedPowerDataSet>>();

    public string Configuration { get; init; } = string.Empty;

    public bool HasGeneration { get; init; } = false;

    public Func<EnhancedPowerDataSet, PowerFast> GetLoad { get; init; }

    public Func<EnhancedPowerDataSet, PowerFast> GetGeneration { get; init; }
}

public readonly struct BatteryConfiguration
{
    public Func<BatteryElectricStorage2> CreateBattery { get; init; }

    public string Description { get; init; }
}

public class SimulationResult
{
    public string BatteryConfiguration { get; init; } = string.Empty;

    public int StepsSimulated { get; set; }

    public EnergyFast PacketSize { get; init; }

    public Ratio PacketProbability { get; init; }

    public TimeSpan TimeStep { get; init; }

    public bool Success { get; set; }

    public BatteryFailReason FailReason { get; set; }

    public string StrategyName { get; init; } = string.Empty;

    public string StrategyConfiguration { get; init; } = string.Empty;

    public string StrategyPrettyConfiguration { get; init; } = string.Empty;

    public string DataConfiguration { get; init; } = string.Empty;

    public string GuardConfiguration { get; init; } = string.Empty;

    public int Seed { get; init; }

    public EnergyFast BatteryMinSoC { get; set; }

    public EnergyFast BatteryMaxSoC { get; set; }

    public EnergyFast BatteryAvgSoC { get; set; }

    public double TotalKilowattHoursIncoming { get; set; }

    public double TotalKilowattHoursOutgoing { get; set; }

    public double TotalKilowattHoursForfeited { get; set; }

    public IGuardSummary GuardSummary { get; set; }
}

public enum BatteryFailReason
{
    None,
    BelowZero,
    ExceedCapacity,
    ExceedDischargePower,
    ExceedChargePower,
    Exception
}