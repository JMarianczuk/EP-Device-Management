using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class LinearProbabilisticEstimationFunctionControl : LinearProbabilisticFunctionControl
{
    public LinearProbabilisticEstimationFunctionControl(IStorage battery, Energy packetSize, Ratio lowerLevel,
        Ratio upperLevel,
        RandomNumberGenerator random,
        bool withOscillationGuard)
        : base(battery, packetSize, lowerLevel, upperLevel, random, withOscillationGuard)
    {
        this.assumedCurrentBatterySoC = battery.CurrentStateOfCharge;
    }

    private Energy assumedCurrentBatterySoC;

    public override string Name => nameof(LinearProbabilisticEstimationFunctionControl);

    protected override Energy AssumedCurrentBatterySoC => this.assumedCurrentBatterySoC;

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        IEnumerable<ILoad> loads,
        IEnumerable<IGenerator> generators,
        TransferResult lastTransferResult)
    {
        var l = loads.Select(x => x.CurrentDemand).Sum();
        var g = generators.Select(x => x.CurrentGeneration).Sum();
        var net = l - g;
        this.assumedCurrentBatterySoC = this.Battery.CurrentStateOfCharge - (net * timeStep);
        return base.DoUnguardedControl(dataPoint, timeStep, loads, generators, lastTransferResult);
    }
}