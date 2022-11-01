using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Simulation.Storage;
using UnitsNet;

namespace EpDeviceManagement.Simulation;

public class Configuration
{
    public IStorage Battery { get; init; }

    public Energy PacketSize { get; init; }

    public DataSet DataSet { get; init; }

    public RandomNumberGenerator Random { get; init; }
}

public class DataSet
{
    private static Func<EnhancedPowerDataSet, Power> ZeroPower = _ => Power.Zero;
    private readonly Func<EnhancedPowerDataSet, Power> getLoadsTotalPower = ZeroPower;
    private readonly Func<EnhancedPowerDataSet, Power> getGeneratorsTotalPower = ZeroPower;

    public IReadOnlyCollection<EnhancedPowerDataSet> Data { get; init; } = Array.Empty<EnhancedPowerDataSet>();

    public string Configuration { get; init; } = string.Empty;

    public Func<EnhancedPowerDataSet, Power> GetLoadsTotalPower
    {
        get => this.getLoadsTotalPower;
        init
        {
            this.getLoadsTotalPower = value;
            this.GetMomentaneousLoadsPower = GetMomentaneousPowerFromFineRes(value);
        }
    }

    public Func<EnhancedPowerDataSet, Power> GetMomentaneousLoadsPower { get; init; } = ZeroPower;

    public Func<EnhancedPowerDataSet, Power> GetGeneratorsTotalPower
    {
        get => this.getGeneratorsTotalPower;
        init
        {
            this.getGeneratorsTotalPower = value;
            this.GetMomentaneousGeneratorsPower = GetMomentaneousPowerFromFineRes(value);
        }
    }

    public Func<EnhancedPowerDataSet, Power> GetMomentaneousGeneratorsPower { get; init; } = ZeroPower;

    public static Func<EnhancedPowerDataSet, Power> GetMomentaneousPowerFromFineRes(
        Func<EnhancedPowerDataSet, Power> getPowerFunc)
    {
        return d =>
        {
            if (d.FineResDataSet != null)
            {
                return getPowerFunc(d.FineResDataSet);
            }
            else
            {
                return getPowerFunc(d);
            }
        };
    }
}

public readonly struct BatteryConfiguration
{
    public Func<BatteryElectricStorage2> CreateBattery { get; init; }

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