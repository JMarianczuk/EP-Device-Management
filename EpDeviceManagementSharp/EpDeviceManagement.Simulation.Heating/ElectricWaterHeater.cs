using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Heating;

public class ElectricWaterHeater : IUnidirectionalStorage
{
    private readonly decimal heatTransferEfficiency;
    private readonly TimeSpan ambientInsulationLosses;
    private readonly SpecificEntropy specificHeatCapacity;
    private readonly Density density;

    public ElectricWaterHeater(
        decimal heatTransferEfficiency,
        TimeSpan ambientInsulationLosses,
        SpecificEntropy specificHeatCapacity,
        Density density)
    {
        var spec = (new Energy() / new Mass()) / new TemperatureDelta(); 
        if (heatTransferEfficiency is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(heatTransferEfficiency));
        }
        this.heatTransferEfficiency = heatTransferEfficiency;
        this.ambientInsulationLosses = ambientInsulationLosses;
        this.specificHeatCapacity = specificHeatCapacity;
        this.density = density;
    }

    public Temperature CurrentTemperature { get; private set; }
    
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
        var ambientLosses = (this.CurrentTemperature - ambientTemperature) * (timeStep / this.ambientInsulationLosses);
        var inletLosses = (this.CurrentTemperature - inletTemperature) *
                          ((hotWaterWithdrawalRate.CubicMetersPerSecond / (60 * this.TotalWaterCapacity.CubicMeters)) * timeStep.TotalSeconds);
        var totalTemperatureChange = heatingIncrease - ambientLosses - inletLosses;
        var beforeSoC = this.CurrentStateOfCharge;
        this.CurrentTemperature += totalTemperatureChange;
        this.CurrentLoss = (beforeSoC - this.CurrentStateOfCharge) / timeStep;
    }

    public Energy TotalCapacity => this.ToEnergy(this.MaximumWaterTemperature);

    public Energy CurrentStateOfCharge => this.ToEnergy(this.CurrentTemperature);

    public Energy MinimumStateOfCharge => this.ToEnergy(this.MinimumWaterTemperature);
    
    public Power MaximumChargePower { get; init; }
    
    public Power CurrentLoss { get; private set; }

    private Energy ToEnergy(Temperature temperature)
    {
        var offset = temperature - MinimumWaterTemperature;
        return this.specificHeatCapacity 
            * offset
            *(this.TotalWaterCapacity * this.density);
    }
}