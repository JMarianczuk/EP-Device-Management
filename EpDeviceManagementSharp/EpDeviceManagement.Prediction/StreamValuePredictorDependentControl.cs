using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;

using UnitsNet;

namespace EpDeviceManagement.Prediction;

public class StreamValuePredictorDependentControl : IEpDeviceController
{
    private readonly IEpDeviceController strategy;
    private readonly StreamValuePredictor<PowerFast> loadsPredictor;
    private readonly StreamValuePredictor<PowerFast> generationPredictor;

    public StreamValuePredictorDependentControl(
        IEpDeviceController strategy,
        StreamValuePredictor<PowerFast> loadsPredictor,
        StreamValuePredictor<PowerFast> generationPredictor)
    {
        this.strategy = strategy;
        this.loadsPredictor = loadsPredictor;
        this.generationPredictor = generationPredictor;
    }

    public string Name => this.strategy.Name;
    public string Configuration => this.strategy.Configuration;
    public string PrettyConfiguration => this.strategy.PrettyConfiguration;
    public bool RequestsOutgoingPackets => this.strategy.RequestsOutgoingPackets;

    public ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        var currentLoad = load.MomentaryDemand;
        var currentGeneration = generator.MomentaryGeneration;
        this.loadsPredictor.ReportCurrentValue(currentLoad);
        this.generationPredictor.ReportCurrentValue(currentGeneration);
        return this.strategy.DoControl(
            timeStep,
            load,
            generator,
            lastTransferResult);
    }

    private static PowerFast Power(ILoad load) => load.MomentaryDemand;

    private static PowerFast Power(IGenerator gen) => gen.MomentaryGeneration;
}