using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class DirectionAwareLinearProbabilisticFunctionControl : LinearProbabilisticFunctionControl
{
    private readonly bool withEstimation;
    private readonly decimal loadLimitPortionOfPacketSize;

    public DirectionAwareLinearProbabilisticFunctionControl(
        IStorage battery,
        Energy packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        Ratio loadLimitPortionOfPacketSize,
        bool withEstimation,
        RandomNumberGenerator random,
        bool withGeneration,
        bool withOscillationGuard)
        : base(battery, packetSize, lowerLevel, upperLevel, random, withGeneration, withOscillationGuard)
    {
        this.withEstimation = withEstimation;
        this.loadLimitPortionOfPacketSize = (decimal) loadLimitPortionOfPacketSize.DecimalFractions;
    }

    public override string Name => base.Name + " + Direction" + (this.withEstimation ? " + Estimation" : "");

    public override string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"{base.Configuration} ({this.loadLimitPortionOfPacketSize:F2})");

    public override string PrettyConfiguration => $"{base.PrettyConfiguration} ({this.loadLimitPortionOfPacketSize * 100:F0}%)";

    private Energy assumedCurrentBatterySoC;

    protected override Energy AssumedCurrentBatterySoC => this.assumedCurrentBatterySoC;

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
        TransferResult lastTransferResult)
    {
        if (withEstimation)
        {
            this.assumedCurrentBatterySoC =
                LinearProbabilisticEstimationFunctionControl.CalculateAssumedCurrentBatterySoC(timeStep, loads,
                    generators, this.Battery.CurrentStateOfCharge);
        }
        else
        {
            this.assumedCurrentBatterySoC = this.Battery.CurrentStateOfCharge;
        }
        if (this.AssumedCurrentBatterySoC > this.LowerLimit
            && this.AssumedCurrentBatterySoC < this.UpperLimit)
        {
            var totalLoad = loads.Sum() -
                            generators.Sum();
            var packetPower = this.PacketSize / timeStep;
            var loadLimit = packetPower * loadLimitPortionOfPacketSize;
            if (this.AssumedCurrentBatterySoC < this.MiddleLimit)
            {
                // would like to request incoming
                // check if generation is >= 1/10 of packet
                var generation = -totalLoad;
                if (generation >= loadLimit)
                {
                    return ControlDecision.NoAction.Instance;
                }
            }
            else
            {
                // would like to request outgoing
                // check if load is >= 1/10 of packet
                if (totalLoad >= loadLimit)
                {
                    return ControlDecision.NoAction.Instance;
                }
            }
        }

        return base.DoUnguardedControl(dataPoint, timeStep, loads, generators, lastTransferResult);
    }
}