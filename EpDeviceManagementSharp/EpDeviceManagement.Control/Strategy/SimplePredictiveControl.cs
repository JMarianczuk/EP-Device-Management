using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Control.Extensions;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.UnitsExtensions;
using Humanizer;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class SimplePredictiveControl : IEpDeviceController
{
    private readonly TimeSpan predictionHorizon;
    private readonly IValuePredictor<PowerFast> loadsPredictor;
    private readonly IValuePredictor<PowerFast> generationPredictor;

    public SimplePredictiveControl(
        IStorage battery,
        Energy packetSize,
        TimeSpan predictionHorizon,
        IValuePredictor<PowerFast> loadsPredictor,
        IValuePredictor<PowerFast> generationPredictor,
        string predictorConfiguration)
    {
        this.predictionHorizon = predictionHorizon;
        this.loadsPredictor = loadsPredictor;
        this.generationPredictor = generationPredictor;

        this.Battery = battery;
        this.PacketSize = packetSize;
        
        this.Configuration = $"{predictionHorizon:hh\\:mm} [{predictorConfiguration}]";
        this.PrettyConfiguration = $"{predictionHorizon:hh\\:mm} {predictorConfiguration}";
    }

    public ControlDecision DoControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        var predictedSteps = (int)(this.predictionHorizon / timeStep);
        var loadPredictions = this.loadsPredictor
            .Predict(predictedSteps, dataPoint)
            .Prepend(load.MomentaryDemand);
        var generationPredictions = this.generationPredictor
            .Predict(predictedSteps, dataPoint)
            .Prepend(generator.MomentaryGeneration);
        var currentBattery = this.Battery.CurrentStateOfCharge;
        var minSoC = this.Battery.TotalCapacity * 0.1;
        var maxSoC = this.Battery.TotalCapacity * 0.9;
        foreach (var (l, gen) in loadPredictions.Zip(generationPredictions))
        {
            var effectiveLoad = l - gen;
            currentBattery -= effectiveLoad * timeStep;
            if (currentBattery <= minSoC)
            {
                return ControlDecision.RequestTransfer.Incoming;
            }

            if (currentBattery >= maxSoC)
            {
                return ControlDecision.RequestTransfer.Outgoing;
            }
        }

        return ControlDecision.NoAction.Instance;
    }

    private Power PowerSum(Power left, Power right) => left + right;

    private IStorage Battery { get; }

    private Energy PacketSize { get; }

    public string Name => "Simple Predictive Control";

    public string Configuration { get; }

    public string PrettyConfiguration { get; }

    public bool RequestsOutgoingPackets => true;
}