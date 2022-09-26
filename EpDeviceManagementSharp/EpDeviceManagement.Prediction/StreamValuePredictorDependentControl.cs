using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;

using UnitsNet;

namespace EpDeviceManagement.Prediction;

public class StreamValuePredictorDependentControl : IEpDeviceController
{
    private readonly IEpDeviceController strategy;
    private readonly StreamValuePredictor<Power> loadsPredictor;
    private readonly StreamValuePredictor<Power> generationPredictor;

    public StreamValuePredictorDependentControl(
        IEpDeviceController strategy,
        StreamValuePredictor<Power> loadsPredictor,
        StreamValuePredictor<Power> generationPredictor)
    {
        this.strategy = strategy;
        this.loadsPredictor = loadsPredictor;
        this.generationPredictor = generationPredictor;
    }

    public string Name => this.strategy.Name;
    public string Configuration => this.strategy.Configuration;
    public string PrettyConfiguration => this.strategy.PrettyConfiguration;

    public ControlDecision DoControl(
        TimeSpan timeStep,
        IEnumerable<ILoad> loads,
        IEnumerable<IGenerator> generators,
        TransferResult lastTransferResult)
    {
        var currentLoad = loads.Select(Power).Sum();
        var currentGeneration = generators.Select(Power).Sum();
        this.loadsPredictor.ReportCurrentValue(currentLoad);
        this.generationPredictor.ReportCurrentValue(currentGeneration);
        return this.strategy.DoControl(
            timeStep,
            loads,
            generators,
            lastTransferResult);
    }

    private static Power Power(ILoad load) => load.CurrentDemand;

    private static Power Power(IGenerator gen) => gen.CurrentGeneration;
}