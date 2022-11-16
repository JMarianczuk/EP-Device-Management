using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Extensions;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class DirectionAwareLinearProbabilisticFunctionControl : LinearProbabilisticFunctionControl
{
    private readonly bool withEstimation;
    private readonly bool noOutgoing;
    private readonly double loadLimitPortionOfPacketSize;

    public DirectionAwareLinearProbabilisticFunctionControl(
        IStorage battery,
        EnergyFast packetSize,
        Ratio lowerLevel,
        Ratio upperLevel,
        Ratio loadLimitPortionOfPacketSize,
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
        this.loadLimitPortionOfPacketSize = loadLimitPortionOfPacketSize.DecimalFractions;
    }

    public override string Name => base.Name + " + Direction" + (this.withEstimation ? " + Estimation" : "") + (this.noOutgoing ? " + NoOutgoing" : "");

    public override string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"{base.Configuration} ({this.loadLimitPortionOfPacketSize:F2})");

    public override string PrettyConfiguration => $"{base.PrettyConfiguration} ({this.loadLimitPortionOfPacketSize * 100:F0}%)";

    private EnergyFast assumedCurrentBatterySoC;

    protected override EnergyFast AssumedCurrentBatterySoC => this.assumedCurrentBatterySoC;

    public override ControlDecision DoControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        if (withEstimation)
        {
            this.assumedCurrentBatterySoC =
                LinearProbabilisticEstimationFunctionControl.CalculateAssumedCurrentBatterySoC(timeStep, load,
                    generator, this.Battery.CurrentStateOfCharge);
        }
        else
        {
            this.assumedCurrentBatterySoC = this.Battery.CurrentStateOfCharge;
        }
        if (this.AssumedCurrentBatterySoC > this.LowerLimit
            && this.AssumedCurrentBatterySoC < this.UpperLimit)
        {
            var totalLoad = load.MomentaryDemand -
                            generator.MomentaryGeneration;
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

        return base.DoControl(dataPoint, timeStep, load, generator, lastTransferResult);
    }
}