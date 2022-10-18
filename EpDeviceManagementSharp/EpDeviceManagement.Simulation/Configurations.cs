using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Simulation.Storage;
using UnitsNet;

namespace EpDeviceManagement.Simulation;

public class Configuration
{
    private readonly IValuePredictor<Power> loadsPredictor;
    private readonly IValuePredictor<Power> generationPredictor;

    public IStorage Battery { get; init; }

    public Energy PacketSize { get; init; }

    public DataSet DataSet { get; init; }

    public IValuePredictor<Power> LoadsPredictor
    {
        get
        {
            this.PredictorUsed = true;
            return loadsPredictor;
        }
        init => loadsPredictor = value;
    }

    public IValuePredictor<Power> GenerationPredictor
    {
        get
        {
            this.PredictorUsed = true;
            return generationPredictor;
        }
        init => generationPredictor = value;
    }

    public RandomNumberGenerator Random { get; init; }

    public bool PredictorUsed { get; private set; } = false;
}

public class DataSet
{
    private static Func<EnhancedPowerDataSet, Power> ZeroPower = _ => Power.Zero;

    public IReadOnlyCollection<EnhancedPowerDataSet> Data { get; init; } = Array.Empty<EnhancedPowerDataSet>();

    public string Configuration { get; init; } = string.Empty;

    public Func<EnhancedPowerDataSet, Power> GetLoadsTotalPower { get; init; } = ZeroPower;

    public Func<EnhancedPowerDataSet, Power> GetGeneratorsTotalPower { get; init; } = ZeroPower;
}

public readonly struct BatteryConfiguration
{
    public Func<BatteryElectricStorage> CreateBattery { get; init; }

    public string Description { get; init; }
}

public class SimulationResult
{
    public string BatteryConfiguration { get; set; } = string.Empty;

    public int StepsSimulated { get; set; }

    public Energy PacketSize { get; set; }

    public Ratio PacketProbability { get; set; }

    public TimeSpan TimeStep { get; set; }

    public bool Success { get; set; }

    public BatteryFailReason FailReason { get; set; }

    public string StrategyName { get; set; } = string.Empty;

    public string StrategyConfiguration { get; set; } = string.Empty;

    public string StrategyPrettyConfiguration { get; set; } = string.Empty;

    public string DataConfiguration { get; set; } = string.Empty;

    public int Seed { get; set; }

    public Energy BatteryMinSoC { get; set; }

    public Energy BatteryMaxSoC { get; set; }

    public Energy BatteryAvgSoC { get; set; }

    public double TotalKilowattHoursIncoming { get; set; }

    public double TotalKilowattHoursOutgoing { get; set; }

    public double TotalKilowattHoursGenerationMissed { get; set; }
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