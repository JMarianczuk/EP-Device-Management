﻿using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.UnitsExtensions;
using Humanizer;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public abstract class SingleStateProbabilisticFunctionControl : GuardedStrategy, IEpDeviceController
{
    private readonly Ratio lowerLevel;
    private readonly Ratio upperLevel;
    private readonly RandomNumberGenerator random;

    public SingleStateProbabilisticFunctionControl(
        IStorage battery,
        Energy packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        RandomNumberGenerator random,
        bool withOscillationGuard)
        : base(
            new BatteryCapacityGuard(battery, packetSize),
            new BatteryPowerGuard(battery, packetSize),
            withOscillationGuard ? new OscillationGuard() : DummyGuard.Instance)
    {
        this.lowerLevel = lowerLevel;
        this.upperLevel = upperLevel;
        this.random = random;
        this.Battery = battery;

        this.LowerLimit = battery.TotalCapacity * lowerLevel.DecimalFractions;
        this.UpperLimit = battery.TotalCapacity * upperLevel.DecimalFractions;
        this.MiddleLimit = (LowerLimit + UpperLimit) / 2;
    }

    protected IStorage Battery { get; }
    
    protected Energy LowerLimit { get; }
    
    protected Energy UpperLimit { get; }

    protected Energy MiddleLimit { get; }

    public override string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"[{this.lowerLevel.DecimalFractions:F2}, {this.upperLevel.DecimalFractions:F2}]");

    public override string PrettyConfiguration => $"[{this.lowerLevel}, {this.upperLevel}]";

    protected virtual Energy AssumedCurrentBatterySoC => this.Battery.CurrentStateOfCharge;

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        IEnumerable<ILoad> loads,
        IEnumerable<IGenerator> generators,
        TransferResult lastTransferResult)
    {
        if (this.AssumedCurrentBatterySoC < this.LowerLimit)
        {
            return new ControlDecision.RequestTransfer()
            {
                RequestedDirection = PacketTransferDirection.Incoming,
            };
        }
        else if (this.AssumedCurrentBatterySoC < this.MiddleLimit)
        {
            var probability = this.GetProbabilityForLowerHalf(timeStep);
            if (random.NextDouble() <= probability)
            {
                return new ControlDecision.RequestTransfer()
                {
                    RequestedDirection = PacketTransferDirection.Incoming,
                };
            }
            else
            {
                return new ControlDecision.NoAction();
            }
        }
        else if (this.AssumedCurrentBatterySoC < this.UpperLimit)
        {
            var probability = this.GetProbabilityForUpperHalf(timeStep);
            if (random.NextDouble() <= probability)
            {
                return new ControlDecision.RequestTransfer()
                {
                    RequestedDirection = PacketTransferDirection.Outgoing,
                };
            }
            else
            {
                return new ControlDecision.NoAction();
            }
        }
        else
        {
            var totalGenerationPower = generators.Select(g => g.CurrentGeneration).Sum();
            if (this.Battery.CurrentStateOfCharge + totalGenerationPower * timeStep >= this.Battery.TotalCapacity)
            {
                foreach (var cGen in generators.OfType<IControllableGenerator>())
                {
                    cGen.DisableGenerationForOneTimeStep();
                }
            }
            return new ControlDecision.RequestTransfer()
            {
                RequestedDirection = PacketTransferDirection.Outgoing,
            };
        }
    }

    protected abstract double GetProbabilityForLowerHalf(TimeSpan timeStep);

    protected abstract double GetProbabilityForUpperHalf(TimeSpan timeStep);

    private double GetRelativePositionInInterval(Energy upperLimit, Energy lowerLimit)
    {
        var range = upperLimit - lowerLimit;
        var positionInRange = this.Battery.CurrentStateOfCharge - lowerLimit;
        var relativePosition = positionInRange / range;
        return relativePosition;
        //var meanTimeToResponse = TimeSpan.FromMinutes(30);
        //var mu = (this.upperLimit - this.Battery.CurrentStateOfCharge) /
        //         (this.Battery.CurrentStateOfCharge - this.lowerLimit);
        //return Math.Exp(mu * (timeStep / meanTimeToResponse));
    }
}