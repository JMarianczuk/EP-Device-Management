using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class LinearProbabilisticFunctionControl : SingleStateProbabilisticFunctionControl
{
    public LinearProbabilisticFunctionControl(
        IStorage battery,
        Energy packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        RandomNumberGenerator random,
        bool withOscillationGuard)
        : base(battery, packetSize, lowerLevel, upperLevel, random, withOscillationGuard)
    {
    }

    public override string Name => nameof(LinearProbabilisticFunctionControl);

    protected override double GetProbabilityForLowerHalf(TimeSpan timeStep)
    {
        var relPos = GetRelativePositionInInterval(this.MiddleLimit, this.LowerLimit);
        var probability = 1 - relPos; // because full prob at lower end
        return probability;
    }

    protected override double GetProbabilityForUpperHalf(TimeSpan timeStep)
    {
        var relPos = GetRelativePositionInInterval(this.UpperLimit, this.MiddleLimit);
        var probability = relPos;
        return probability;
    }

    private double GetRelativePositionInInterval(Energy upperLimit, Energy lowerLimit)
    {
        var range = upperLimit - lowerLimit;
        var positionInRange = this.AssumedCurrentBatterySoC - lowerLimit;
        var relativePosition = positionInRange / range;
        return relativePosition;
        //var meanTimeToResponse = TimeSpan.FromMinutes(30);
        //var mu = (this.upperLimit - this.Battery.CurrentStateOfCharge) /
        //         (this.Battery.CurrentStateOfCharge - this.lowerLimit);
        //return Math.Exp(mu * (timeStep / meanTimeToResponse));
    }
}