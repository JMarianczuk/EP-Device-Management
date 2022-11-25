using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class LinearProbabilisticEstimationRangeControl : LinearProbabilisticRangeControl
{
    public LinearProbabilisticEstimationRangeControl(
        IStorage battery,
        EnergyFast packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        RandomNumberGenerator random,
        bool withGeneration)
        : base(
            battery,
            packetSize,
            lowerLevel,
            upperLevel,
            random,
            withGeneration)
    {
        this.assumedCurrentBatterySoC = battery.CurrentStateOfCharge;
    }

    private EnergyFast assumedCurrentBatterySoC;

    public override string Name => base.Name + " + EST";

    protected override EnergyFast AssumedCurrentBatterySoC => this.assumedCurrentBatterySoC;

    public override ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        this.assumedCurrentBatterySoC = CalculateAssumedCurrentBatterySoC(timeStep, load, generator, this.Battery.CurrentStateOfCharge);
        return base.DoControl(timeStep, load, generator, lastTransferResult);
    }

    public static EnergyFast CalculateAssumedCurrentBatterySoC(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        EnergyFast actualCurrentStateOfCharge)
    {
        var l = load.MomentaryDemand;
        var g = generator.MomentaryGeneration;
        var net = l - g;
        var batteryCurrentStateOfCharge = actualCurrentStateOfCharge - (net * timeStep);
        return batteryCurrentStateOfCharge;
    }
}