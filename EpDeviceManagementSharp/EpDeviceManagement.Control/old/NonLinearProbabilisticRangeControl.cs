using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.old;

public class NonLinearProbabilisticRangeControl : ProbabilisticRangeControl
{
    private readonly TimeSpan mttr;

    public NonLinearProbabilisticRangeControl(
        IStorage battery,
        EnergyFast packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        RandomNumberGenerator random,
        TimeSpan meanTimeToResponse,
        bool withGeneration)
        : base(
            battery,
            packetSize,
            lowerLevel,
            upperLevel,
            random,
            withGeneration)
    {
        mttr = meanTimeToResponse;
    }

    public override string Name => nameof(NonLinearProbabilisticRangeControl);

    protected override double GetProbabilityForLowerHalf(TimeSpan timeStep)
    {
        return GetProbability(timeStep, MiddleLimit, LowerLimit);
    }

    protected override double GetProbabilityForUpperHalf(TimeSpan timeStep)
    {
        return GetProbability(timeStep, UpperLimit, MiddleLimit);
    }

    private double GetProbability(TimeSpan timeStep, EnergyFast upperLimit, EnergyFast lowerLimit)
    {
        var mu = (upperLimit - Battery.CurrentStateOfCharge)
                 / (Battery.CurrentStateOfCharge - lowerLimit);
        var probability = 1 - Math.Exp(-mu * (timeStep / mttr));
        return probability;
    }
}