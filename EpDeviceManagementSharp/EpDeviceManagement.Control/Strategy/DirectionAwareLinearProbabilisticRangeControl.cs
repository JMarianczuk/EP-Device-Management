using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class DirectionAwareLinearProbabilisticRangeControl : LinearProbabilisticRangeControl
{
    private readonly bool withEstimation;
    private readonly bool noOutgoing;
    private readonly double receiveThresholdFactor;
    private readonly double sendThresholdFactor;

    public DirectionAwareLinearProbabilisticRangeControl(
        IStorage battery,
        EnergyFast packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        Ratio receiveThresholdRatio,
        Ratio sendThresholdRatio,
        bool withEstimation,
        RandomNumberGenerator random,
        bool withGeneration,
        bool noOutgoing)
        : base(
            battery,
            packetSize,
            lowerLevel,
            upperLevel,
            random,
            withGeneration && !noOutgoing)
    {
        this.withEstimation = withEstimation;
        this.noOutgoing = noOutgoing;
        this.receiveThresholdFactor = receiveThresholdRatio.DecimalFractions;
        this.sendThresholdFactor = sendThresholdRatio.DecimalFractions;
    }

    public override string Name => StringExtensions.JoinIf(" + ", base.Name, DirectionAwarenessAbbreviation, this.withEstimation ? EstimationAbbreviation : "", this.noOutgoing ? NoSendAbbreviation : "");

    public override string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"{base.Configuration} ({this.receiveThresholdFactor:+0.00;-0.00;+0.00}") + 
        (this.noOutgoing ? ")" : string.Create(CultureInfo.InvariantCulture, $",{this.sendThresholdFactor:+0.00;-0.00;+0.00})"));

    public override string PrettyConfiguration => $"{base.PrettyConfiguration} ({this.receiveThresholdFactor * 100:F0}%)";

    private EnergyFast assumedCurrentBatterySoC;

    protected override EnergyFast AssumedCurrentBatterySoC => this.assumedCurrentBatterySoC;

    public override ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        if (withEstimation)
        {
            this.assumedCurrentBatterySoC =
                LinearProbabilisticEstimationRangeControl.CalculateAssumedCurrentBatterySoC(timeStep, load,
                    generator, this.Battery.CurrentStateOfCharge);
        }
        else
        {
            this.assumedCurrentBatterySoC = this.Battery.CurrentStateOfCharge;
        }
        if (this.AssumedCurrentBatterySoC > this.LowerLimit
            && this.AssumedCurrentBatterySoC < this.UpperLimit)
        {
            var effectivePower = load.MomentaryDemand -
                            generator.MomentaryGeneration;
            var packetPower = this.PacketSize / timeStep;
            if (this.AssumedCurrentBatterySoC < this.MiddleLimit)
            {
                // would like to request to receive
                // check if generation is >= 1/10 of packet
                var netGeneration = -effectivePower;
                if (netGeneration >= packetPower * this.receiveThresholdFactor)
                {
                    return ControlDecision.NoAction.Instance;
                }
            }
            else
            {
                // would like to request to send
                // check if load is >= 1/10 of packet
                if (effectivePower >= packetPower * this.sendThresholdFactor)
                {
                    return ControlDecision.NoAction.Instance;
                }
            }
        }

        return base.DoControl(timeStep, load, generator, lastTransferResult);
    }
}