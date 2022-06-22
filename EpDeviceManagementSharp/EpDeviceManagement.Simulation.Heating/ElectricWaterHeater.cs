using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Heating;

public class ElectricWaterHeater : IUnidirectionalStorage
{
    private readonly decimal heatTransferEfficiency;
    private readonly TimeSpan ambientInsulationLossesTimeConstant;
    private readonly SpecificEntropy specificHeatCapacity;
    private readonly Density density;
    private Temperature currentTemperature;

    public ElectricWaterHeater(
        Ratio heatTransferEfficiency,
        TimeSpan ambientInsulationLossesTimeConstant,
        SpecificEntropy specificHeatCapacity,
        Density density)
    {
        //var spec = (new Energy() / new Mass()) / new TemperatureDelta();
        if (heatTransferEfficiency.DecimalFractions is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(heatTransferEfficiency));
        }

        if (ambientInsulationLossesTimeConstant == TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ambientInsulationLossesTimeConstant));
        }
        this.heatTransferEfficiency = (decimal) heatTransferEfficiency.DecimalFractions;
        this.ambientInsulationLossesTimeConstant = ambientInsulationLossesTimeConstant;
        this.specificHeatCapacity = specificHeatCapacity;
        this.density = density;
    }

    public Temperature CurrentTemperature
    {
        get => currentTemperature;
        init => currentTemperature = value;
    }

    public Volume TotalWaterCapacity { get; init; }
    
    public Temperature MaximumWaterTemperature { get; init; }
    
    public Temperature MinimumWaterTemperature { get; init; }

    public void Simulate(
        TimeSpan timeStep,
        Power heatElementPowerTransfer,
        bool heat,
        Temperature ambientTemperature,
        VolumeFlow hotWaterWithdrawalRate,
        Temperature inletTemperature)
    {
        var heatingIncrease = heat
            ? (timeStep * (this.heatTransferEfficiency * heatElementPowerTransfer))
            .DivideBy(this.specificHeatCapacity.Multiply(this.density * this.TotalWaterCapacity))
            : TemperatureDelta.Zero;
        var ambientLosses = (this.CurrentTemperature - ambientTemperature)
                            * (timeStep / this.ambientInsulationLossesTimeConstant);
        var inletLosses = (this.CurrentTemperature - inletTemperature)
                          * ((hotWaterWithdrawalRate * timeStep)
                            / this.TotalWaterCapacity);
        var totalTemperatureChange = heatingIncrease - ambientLosses - inletLosses;
        var beforeSoC = this.CurrentStateOfCharge;
        this.currentTemperature += totalTemperatureChange;
        this.CurrentLoss = (beforeSoC - this.CurrentStateOfCharge) / timeStep;
    }

    public Energy TotalCapacity => this.ToEnergy(this.MaximumWaterTemperature);

    public Energy CurrentStateOfCharge => this.ToEnergy(this.CurrentTemperature);

    public Energy MinimumStateOfCharge => this.ToEnergy(this.MinimumWaterTemperature);
    
    public Power MaximumChargePower { get; init; }
    
    public Power CurrentLoss { get; private set; }

    private Energy ToEnergy(Temperature temperature)
    {
        var offset = temperature - this.MinimumWaterTemperature;
        return this.specificHeatCapacity 
            * offset
            *(this.TotalWaterCapacity * this.density);
    }
}