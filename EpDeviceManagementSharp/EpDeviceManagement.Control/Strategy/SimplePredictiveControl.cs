using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.UnitsExtensions;
using Humanizer;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class SimplePredictiveControl : GuardedStrategy, IEpDeviceController
{
    private readonly TimeSpan predictionHorizon;
    private readonly IValuePredictor<Power> loadsPredictor;
    private readonly IValuePredictor<Power> generationPredictor;

    public SimplePredictiveControl(
        IStorage battery,
        Energy packetSize,
        TimeSpan predictionHorizon,
        IValuePredictor<Power> loadsPredictor,
        IValuePredictor<Power> generationPredictor,
        string predictorConfiguration)
        : base(
            new BatteryCapacityGuard(battery, packetSize),
            new BatteryPowerGuard(battery, packetSize),
            new OscillationGuard())
    {
        this.predictionHorizon = predictionHorizon;
        this.loadsPredictor = loadsPredictor;
        this.generationPredictor = generationPredictor;

        this.Battery = battery;
        this.PacketSize = packetSize;
        
        this.Configuration = $"{predictionHorizon:hh\\:mm} [{predictorConfiguration}]";
        this.PrettyConfiguration = $"{predictionHorizon:hh\\:mm} {predictorConfiguration}";
    }

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        IEnumerable<ILoad> loads,
        IEnumerable<IGenerator> generators,
        TransferResult lastTransferResult)
    {
        var predictedSteps = (int)(this.predictionHorizon / timeStep);
        var loadPredictions = this.loadsPredictor
            .Predict(predictedSteps, dataPoint)
            .Prepend(loads.Select(x => x.CurrentDemand).Sum());
        var generationPredictions = this.generationPredictor
            .Predict(predictedSteps, dataPoint)
            .Prepend(generators.Select(x => x.CurrentGeneration).Sum());
        var currentBattery = this.Battery.CurrentStateOfCharge;
        var minSoC = this.Battery.TotalCapacity * 0.1;
        var maxSoC = this.Battery.TotalCapacity * 0.9;
        foreach (var (load, gen) in loadPredictions.Zip(generationPredictions))
        {
            var effectiveLoad = load - gen;
            currentBattery -= effectiveLoad * timeStep;
            if (currentBattery <= minSoC)
            {
                return new ControlDecision.RequestTransfer()
                {
                    RequestedDirection = PacketTransferDirection.Incoming,
                };
            }

            if (currentBattery >= maxSoC)
            {
                return new ControlDecision.RequestTransfer()
                {
                    RequestedDirection = PacketTransferDirection.Outgoing,
                };
            }
        }

        return new ControlDecision.NoAction();
    }

    private Power PowerSum(Power left, Power right) => left + right;

    private IStorage Battery { get; }

    private Energy PacketSize { get; }

    public override string Name => nameof(SimplePredictiveControl);

    public override string Configuration { get; }

    public override string PrettyConfiguration { get; }
}