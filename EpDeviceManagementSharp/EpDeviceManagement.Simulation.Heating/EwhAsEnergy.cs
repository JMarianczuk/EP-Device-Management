using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Heating;

public class EwhAsEnergy : ITclAsEnergy
{
    private readonly decimal heatTransferEfficiency;
    private readonly TimeSpan ambientInsulationLossesTimeConstant;
    private readonly Volume waterVolume;
    private readonly Mass waterWeight;
    private readonly Entropy waterEntropy;
    private readonly Temperature referenceWaterTemperature;

    private readonly Func<int, Temperature> getAmbientTemperature;
    private readonly Func<int, VolumeFlow> getHotWaterWithdrawalRate;
    private readonly Func<int, Temperature> getInletTemperature;
    private readonly Func<int, IEnumerable<Temperature>> predictInletTemperatures;
    private readonly Func<int, IEnumerable<Temperature>> predictAmbientTemperatures;
    private readonly Func<int, IEnumerable<VolumeFlow>> predictWaterWithdrawalRates;

    private int currentTimeStep;

    public EwhAsEnergy(
        TimeSpan ambientInsulationLossesTimeConstant,
        SpecificEntropy specificHeatCapacity,
        Density density,
        Volume waterVolume,
        Temperature minimumWaterTemperature,
        Temperature maximumWaterTemperature,
        Temperature initialWaterTemperature,
        Temperature referenceWaterTemperature,
        Power heatPower,
        Ratio heatEfficiency,
        int currentTimeStep,
        Func<int, Temperature> getAmbientTemperature,
        Func<int, VolumeFlow> getHotWaterWithdrawalRate,
        Func<int, Temperature> getInletTemperature,
        Func<int, IEnumerable<Temperature>> predictInletTemperatures,
        Func<int, IEnumerable<Temperature>> predictAmbientTemperatures,
        Func<int, IEnumerable<VolumeFlow>> predictWaterWithdrawalRates)
    {
        this.heatTransferEfficiency = (decimal) heatEfficiency.DecimalFractions;
        this.ambientInsulationLossesTimeConstant = ambientInsulationLossesTimeConstant;
        this.waterVolume = waterVolume;
        this.waterWeight = waterVolume * density;
        this.waterEntropy = specificHeatCapacity * this.waterWeight;
        this.referenceWaterTemperature = referenceWaterTemperature;

        this.getAmbientTemperature = getAmbientTemperature;
        this.getHotWaterWithdrawalRate = getHotWaterWithdrawalRate;
        this.getInletTemperature = getInletTemperature;
        this.predictInletTemperatures = predictInletTemperatures;
        this.predictAmbientTemperatures = predictAmbientTemperatures;
        this.predictWaterWithdrawalRates = predictWaterWithdrawalRates;

        this.currentTimeStep = currentTimeStep;
        this.CurrentTemperature = initialWaterTemperature;

        this.LowerQoSBound = this.ToEnergy(minimumWaterTemperature);
        this.UpperQoSBound = this.ToEnergy(maximumWaterTemperature);
        this.HeatPower = heatPower;
        this.StandingLossRate = Frequency.FromPerSecond(1 / ambientInsulationLossesTimeConstant.TotalSeconds);
        this.HeatEfficiency = heatEfficiency;
    }

    public Temperature CurrentTemperature { get; private set; }
    public Energy CurrentStateOfCharge => this.ToEnergy(this.CurrentTemperature);
    public Energy LowerQoSBound { get; }
    public Energy UpperQoSBound { get; }
    public Frequency StandingLossRate { get; }
    public Ratio HeatEfficiency { get; }
    public Power HeatPower { get; }

    public IEnumerable<Energy> PredictStandingLoss()
    {
        var ambientTemperatures = this.predictAmbientTemperatures(this.currentTimeStep);
        return ambientTemperatures.Select(t => this.waterEntropy * (t - this.referenceWaterTemperature));
    }

    public IEnumerable<Energy> PredictLoss()
    {
        var inletTemperatures = this.predictInletTemperatures(this.currentTimeStep);
        return inletTemperatures.Select(t => this.waterEntropy * (t - this.referenceWaterTemperature));
    }

    public IEnumerable<Frequency> PredictLossRate()
    {
        var waterWithdrawalRates = this.predictWaterWithdrawalRates(this.currentTimeStep);
        return waterWithdrawalRates.Select(r => r.DivideBy(this.waterVolume));
    }

    public void Simulate(
        TimeSpan timeStep,
        bool heat)
    {
        var heatingIncrease = heat
            ? (timeStep * (this.heatTransferEfficiency * this.HeatPower))
              / this.waterEntropy
            : TemperatureDelta.Zero;
        var ambientTemperature = this.getAmbientTemperature(this.currentTimeStep);
        var hotWaterWithdrawalRate = this.getHotWaterWithdrawalRate(this.currentTimeStep);
        var inletTemperature = this.getInletTemperature(this.currentTimeStep);
        var ambientLoss = (this.CurrentTemperature - ambientTemperature)
                          * (timeStep / this.ambientInsulationLossesTimeConstant);
        var inletLosses = (this.CurrentTemperature - inletTemperature)
                          * ((hotWaterWithdrawalRate * timeStep) / this.waterVolume);
        var totalTemperatureChange = heatingIncrease - ambientLoss - inletLosses;
        this.CurrentTemperature += totalTemperatureChange;
        this.currentTimeStep += 1;
    }

    public Energy ToEnergy(Temperature temperature)
    {
        var offset = temperature - this.referenceWaterTemperature;
        return this.waterEntropy
               * offset;
    }
}

public static class UnitsExtensions
{
    public static Frequency DivideBy(this VolumeFlow flow, Volume volume)
    {
        return Frequency.FromPerSecond(flow.LitersPerSecond / volume.Liters);
    }
}