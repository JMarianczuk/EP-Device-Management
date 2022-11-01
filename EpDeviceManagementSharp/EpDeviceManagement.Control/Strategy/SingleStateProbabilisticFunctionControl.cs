using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
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

    protected SingleStateProbabilisticFunctionControl(
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
        PacketSize = packetSize;

        this.LowerLimit = battery.TotalCapacity * lowerLevel.DecimalFractions;
        this.UpperLimit = battery.TotalCapacity * upperLevel.DecimalFractions;
        this.MiddleLimit = (LowerLimit + UpperLimit) / 2;
    }

    protected IStorage Battery { get; }

    protected Energy PacketSize { get; }

    protected Energy LowerLimit { get; }
    
    protected Energy UpperLimit { get; }

    protected Energy MiddleLimit { get; }

    public override string Name => "Probabilistic Range Control";

    public override string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"[{this.lowerLevel.DecimalFractions:F2}, {this.upperLevel.DecimalFractions:F2}]");

    public override string PrettyConfiguration => $"[{this.lowerLevel}, {this.upperLevel}]";

    protected virtual Energy AssumedCurrentBatterySoC => this.Battery.CurrentStateOfCharge;

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
        TransferResult lastTransferResult)
    {
        if (this.AssumedCurrentBatterySoC < this.LowerLimit)
        {
            return ControlDecision.RequestTransfer.Incoming;
        }
        else if (this.AssumedCurrentBatterySoC < this.MiddleLimit)
        {
            var probability = this.GetProbabilityForLowerHalf(timeStep);
            if (random.NextDouble() <= probability)
            {
                return ControlDecision.RequestTransfer.Incoming;
            }
            else
            {
                return ControlDecision.NoAction.Instance;
            }
        }
        else if (this.AssumedCurrentBatterySoC < this.UpperLimit)
        {
            var probability = this.GetProbabilityForUpperHalf(timeStep);
            if (random.NextDouble() <= probability)
            {
                return ControlDecision.RequestTransfer.Outgoing;
            }
            else
            {
                return ControlDecision.NoAction.Instance;
            }
        }
        else
        {
            return ControlDecision.RequestTransfer.Outgoing;
        }
    }

    protected abstract double GetProbabilityForLowerHalf(TimeSpan timeStep);

    protected abstract double GetProbabilityForUpperHalf(TimeSpan timeStep);
}