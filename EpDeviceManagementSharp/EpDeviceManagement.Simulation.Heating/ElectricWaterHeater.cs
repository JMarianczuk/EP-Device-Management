using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Heating;

public class ElectricWaterHeater : IUnidirectionalStorage
{
    private readonly decimal heatTransferEfficiency;
    private readonly TimeSpan ambientInsulationLosses;
    private readonly double specificHeatCapacity;
    private readonly Density density;

    public ElectricWaterHeater(
        decimal heatTransferEfficiency,
        TimeSpan ambientInsulationLosses,
        double specificHeatCapacity,
        Density density)
    {
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
        var heatChange = heat
            ? (this.heatTransferEfficiency * heatElementPowerTransfer).Kilowatts //not yet correct
              / (this.specificHeatCapacity * (this.density * this.TotalWaterCapacity).Kilograms) //kJ/°C
            : 0;
        var ambientLosses = (this.CurrentTemperature - ambientTemperature) * (timeStep / this.ambientInsulationLosses);
        var inletLosses = (this.CurrentTemperature - inletTemperature) *
                          ((hotWaterWithdrawalRate.CubicMetersPerSecond / (60 * this.TotalWaterCapacity.CubicMeters)) * timeStep.TotalSeconds);
        var totalTemperatureLoss = ambientLosses + inletLosses;
        var beforeSoC = this.CurrentStateOfCharge;
        this.CurrentTemperature -= totalTemperatureLoss;
        this.CurrentLoss = (beforeSoC - this.CurrentStateOfCharge) / timeStep;
    }

    public Energy TotalCapacity => this.ToEnergy(this.MaximumWaterTemperature);

    public Energy CurrentStateOfCharge => this.ToEnergy(this.CurrentTemperature);

    public Energy MinimumStateOfCharge => this.ToEnergy(this.MinimumWaterTemperature);
    
    public Power MaximumChargePower { get; init; }
    
    public Power CurrentLoss { get; private set; }

    private Energy ToEnergy(Temperature temperature)
    {
        return Energy.FromKilojoules(
            this.specificHeatCapacity
            * temperature.DegreesCelsius
            * (this.TotalWaterCapacity * this.density).Kilograms);
    }
}