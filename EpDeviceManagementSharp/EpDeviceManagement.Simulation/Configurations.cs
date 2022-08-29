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

public class DataSet
{
    public IReadOnlyCollection<EnhancedEnergyDataSet> Data { get; init; }

    public string Configuration { get; init; }

    public Func<EnhancedEnergyDataSet, Power> GetLoadsTotalPower { get; init; }

    public Func<EnhancedEnergyDataSet, Power> GetGeneratorsTotalPower { get; init; }
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

    public TimeSpan TimeStep { get; set; }

    public bool Success { get; set; }

    public BatteryFailReason FailReason { get; set; }

    public string StrategyName { get; set; }

    public string StrategyConfiguration { get; set; }

    public string StrategyPrettyConfiguration { get; set; }

    public string DataConfiguration { get; set; }

    public int Seed { get; set; }

    public Energy BatteryMinSoC { get; set; }

    public Energy BatteryMaxSoC { get; set; }

    public Energy BatteryAvgSoC { get; set; }

    public int TotalPacketsTransferred { get; set; }
}

public enum BatteryFailReason
{
    None,
    BelowZero,
    ExceedCapacity,
    ExceedDischargePower,
    ExceedChargePower
}