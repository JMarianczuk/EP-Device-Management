using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.UnitsExtensions;
using Humanizer;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public abstract class ProbabilisticRangeControl : IEpDeviceController
{
    private readonly Ratio lowerLevel;
    private readonly Ratio upperLevel;
    private readonly RandomNumberGenerator random;

    public const string DirectionAwarenessAbbreviation = "DIR";
    public const string EstimationAbbreviation = "EST";
    public const string NoSendAbbreviation = "NoSend";

    protected ProbabilisticRangeControl(
        IStorage battery,
        EnergyFast packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        RandomNumberGenerator random,
        bool withGeneration)
    {
        this.lowerLevel = lowerLevel;
        this.upperLevel = upperLevel;
        this.random = random;
        this.Battery = battery;
        this.PacketSize = packetSize;
        this.WithGeneration = withGeneration;

        this.LowerLimit = battery.TotalCapacity * lowerLevel.DecimalFractions;
        this.UpperLimit = battery.TotalCapacity * upperLevel.DecimalFractions;
        this.MiddleLimit = (LowerLimit + UpperLimit) / 2;
    }

    protected IStorage Battery { get; }

    protected EnergyFast PacketSize { get; }

    protected bool WithGeneration { get; }

    protected EnergyFast LowerLimit { get; }
    
    protected EnergyFast UpperLimit { get; }

    protected EnergyFast MiddleLimit { get; }

    public virtual string Name => "PRC";

    public virtual string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"[{this.lowerLevel.DecimalFractions:F1}, {this.upperLevel.DecimalFractions:F1}]");

    public virtual string PrettyConfiguration => $"[{this.lowerLevel}, {this.upperLevel}]";

    public virtual bool RequestsOutgoingPackets => this.WithGeneration;

    protected virtual EnergyFast AssumedCurrentBatterySoC => this.Battery.CurrentStateOfCharge;

    public virtual ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
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
            if (this.WithGeneration
                && random.NextDouble() <= probability)
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
            if (this.WithGeneration)
            {
                return ControlDecision.RequestTransfer.Outgoing;
            }
            else
            {
                return ControlDecision.NoAction.Instance;
            }
        }
    }

    protected abstract double GetProbabilityForLowerHalf(TimeSpan timeStep);

    protected abstract double GetProbabilityForUpperHalf(TimeSpan timeStep);
}