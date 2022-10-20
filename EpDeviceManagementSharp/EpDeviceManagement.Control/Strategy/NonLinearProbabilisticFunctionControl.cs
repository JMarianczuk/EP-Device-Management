using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class NonLinearProbabilisticFunctionControl : SingleStateProbabilisticFunctionControl
{
    private readonly TimeSpan mttr;
    public NonLinearProbabilisticFunctionControl(
        IStorage battery,
        Energy packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        RandomNumberGenerator random,
        TimeSpan meanTimeToResponse,
        bool withOscillationGuard)
        : base(battery, packetSize, lowerLevel, upperLevel, random, withOscillationGuard)
    {
        this.mttr = meanTimeToResponse;
    }

    public override string Name => nameof(NonLinearProbabilisticFunctionControl);

    protected override double GetProbabilityForLowerHalf(TimeSpan timeStep)
    {
        return this.GetProbability(timeStep, this.MiddleLimit, this.LowerLimit);
    }

    protected override double GetProbabilityForUpperHalf(TimeSpan timeStep)
    {
        return this.GetProbability(timeStep, this.UpperLimit, this.MiddleLimit);
    }

    private double GetProbability(TimeSpan timeStep, Energy upperLimit, Energy lowerLimit)
    {
        var mu = (upperLimit - this.Battery.CurrentStateOfCharge)
                 / (this.Battery.CurrentStateOfCharge - lowerLimit);
        var probability = 1 - Math.Exp(-mu * (timeStep / this.mttr));
        return probability;
    }
}