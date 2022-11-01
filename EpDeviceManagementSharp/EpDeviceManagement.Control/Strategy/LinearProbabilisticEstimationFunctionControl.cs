using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
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

    public override string Name => base.Name + " + Estimation";

    protected override Energy AssumedCurrentBatterySoC => this.assumedCurrentBatterySoC;

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
        TransferResult lastTransferResult)
    {
        this.assumedCurrentBatterySoC = CalculateAssumedCurrentBatterySoC(timeStep, loads, generators, this.Battery.CurrentStateOfCharge);
        return base.DoUnguardedControl(dataPoint, timeStep, loads, generators, lastTransferResult);
    }

    public static Energy CalculateAssumedCurrentBatterySoC(
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
        Energy actualCurrentStateOfCharge)
    {
        var l = loads.Sum();
        var g = generators.Sum();
        var net = l - g;
        var batteryCurrentStateOfCharge = actualCurrentStateOfCharge - (net * timeStep);
        return batteryCurrentStateOfCharge;
    }
}