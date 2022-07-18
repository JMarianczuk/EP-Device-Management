using EpDeviceManagement.Simulation.Heating;
using Xunit.Abstractions;

namespace EpDeviceManagementSharp.Simulation.Heating.Tests;

public class EwhAsEnergyTests
{
    private readonly ITestOutputHelper outputHelper;
    private readonly SpecificEntropy waterSpecificEntropy = SpecificEntropy.FromKilojoulesPerKilogramKelvin(4.186);
    private readonly Density waterDensity = Density.FromKilogramsPerLiter(0.99);
    private readonly Volume waterVolume = Volume.FromLiters(1000);
    private readonly Temperature minimumWaterTemperature = Temperature.FromDegreesCelsius(60);
    private readonly Temperature maximumWaterTemperature = Temperature.FromDegreesCelsius(90);
    private readonly Temperature ambientTemperature = Temperature.FromDegreesCelsius(20);
    private readonly Temperature inletTemperature = Temperature.FromDegreesCelsius(15);
    private readonly TimeSpan timeStep = TimeSpan.FromMinutes(5);
    private readonly VolumeFlow[] hotWaterWithdrawalRates;
    private readonly Random random;

    public EwhAsEnergyTests(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
        this.random = new Random(123918623);

        // timestep is 5 minutes
        // day is 24 hours
        int numberOfTimesteps = (int) (TimeSpan.FromDays(1) / timeStep);
        hotWaterWithdrawalRates = new VolumeFlow[numberOfTimesteps];
        for (int i = 0; i < numberOfTimesteps; i += 1)
        {
            hotWaterWithdrawalRates[i] = VolumeFlow.Zero;
        }
        // showering in the morning, 2 people x 15 minutes each
        var showerMorningBegin = (int) (TimeSpan.FromHours(7) / timeStep);
        var showerMorningDuration = (int)(TimeSpan.FromMinutes(30) / timeStep);
        for (int i = 0; i < showerMorningDuration; i += 1)
        {
            hotWaterWithdrawalRates[showerMorningBegin + i] = VolumeFlow.FromLitersPerMinute(8);
        }

        var cookingCleanupWaterTimeStep = (int)(TimeSpan.FromHours(12.5) / timeStep);
        var cookingCleanupWaterDuration = (int)(TimeSpan.FromMinutes(10) / timeStep);
        for (int i = 0; i < cookingCleanupWaterDuration; i += 1)
        {
            hotWaterWithdrawalRates[cookingCleanupWaterTimeStep + i] = VolumeFlow.FromLitersPerMinute(1);
        }
        // showering in the evening, 2 people x 20 minutes each
        var showerEveningBegin = (int)(TimeSpan.FromHours(20) / timeStep);
        var showerEveningDuration = (int)(TimeSpan.FromMinutes(40) / timeStep);
        for (int i = 0; i < showerEveningDuration; i += 1)
        {
            hotWaterWithdrawalRates[showerEveningBegin + i] = VolumeFlow.FromLitersPerMinute(8);
        }
    }

    private EwhAsEnergy CreateEwh(
        TimeSpan? ambientInsulationLossesTimeConstant = null,
        SpecificEntropy? specificHeatCapacity = null,
        Density? density = null,
        Volume? waterVolume = null,
        Temperature? minimumWaterTemperature = null,
        Temperature? maximumWaterTemperature = null,
        Temperature? initialWaterTemperature = null,
        Temperature? referenceWaterTemperature = null,
        Power? heatPower = null,
        Ratio? heatEfficiency = null,
        int? currentTimeStep = null,
        Func<int, Temperature> getAmbientTemperature = null,
        Func<int, VolumeFlow> getHotWaterWithdrawalRate = null,
        Func<int, Temperature> getInletTemperature = null,
        Func<int, IEnumerable<Temperature>> predictInletTemperatures = null,
        Func<int, IEnumerable<Temperature>> predictAmbientTemperatures = null,
        Func<int, IEnumerable<VolumeFlow>> predictWaterWithdrawalRates = null)
    {
        // time-constant: at R-10 this means 1/10 of Temp. difference is lost per hour, so the time constant is 10 hours
        return new EwhAsEnergy(
            ambientInsulationLossesTimeConstant ?? TimeSpan.FromHours(10),
            specificHeatCapacity ?? waterSpecificEntropy,
            density ?? waterDensity,
            waterVolume ?? this.waterVolume,
            minimumWaterTemperature ?? this.minimumWaterTemperature,
            maximumWaterTemperature ?? this.maximumWaterTemperature,
            initialWaterTemperature ?? Temperature.FromDegreesCelsius(70),
            referenceWaterTemperature ?? Temperature.FromDegreesCelsius(0),
            heatPower ?? Power.FromKilowatts(5),
            heatEfficiency ?? Ratio.FromPercent(100),
            currentTimeStep ?? 0,
            getAmbientTemperature ?? AmbientTemperatureFactory,
            getHotWaterWithdrawalRate ?? HotWaterWithdrawalRateFactory,
            getInletTemperature ?? InletTemperatureFactory,
            predictInletTemperatures ?? InletTemperaturesOracle,
            predictAmbientTemperatures ?? AmbientTemperaturesOracle,
            predictWaterWithdrawalRates ?? WaterWithdrawalRatesOracle);
    }

    private Temperature AmbientTemperatureFactory(int timeStep)
    {
        return Temperature.FromDegreesCelsius(20);
    }

    private VolumeFlow HotWaterWithdrawalRateFactory(int timeStep)
    {
        return this.hotWaterWithdrawalRates[timeStep];
    }

    private VolumeFlow NoHotWaterWithdrawal(int timeStep)
    {
        return VolumeFlow.Zero;
    }

    private Temperature InletTemperatureFactory(int timeStep)
    {
        return Temperature.FromDegreesCelsius(15);
    }

    private double GetTemperatureDeviation()
    {
        var range = 0.5d;
        var rand = this.random.NextDouble();
        return (rand * range) - (range / 2);
    }

    private VolumeFlow GetVolumeFlowDeviation()
    {
        var range = Volume.FromLiters(0.5) / TimeSpan.FromMinutes(5);
        var rand = this.random.NextDouble();
        return (rand * range) - (range / 2);
    }

    private IEnumerable<Temperature> InletTemperaturesOracle(int timeStep)
    {
        return Enumerable.Repeat(Temperature.FromDegreesCelsius(15 + GetTemperatureDeviation()), 100);
    }

    private IEnumerable<Temperature> AmbientTemperaturesOracle(int timeStep)
    {
        return Enumerable.Repeat(Temperature.FromDegreesCelsius(20 + GetTemperatureDeviation()), 100);
    }

    private IEnumerable<VolumeFlow> WaterWithdrawalRatesOracle(int timeStep)
    {
        return this.hotWaterWithdrawalRates.Iterate(timeStep, 100).Select(x => x + GetVolumeFlowDeviation());
    }

    [Fact]
    public void StandingLossesTest()
    {
        var ewh = this.CreateEwh(
            initialWaterTemperature: Temperature.FromDegreesCelsius(75),
            getHotWaterWithdrawalRate: this.NoHotWaterWithdrawal);
        ewh.Simulate(this.timeStep, false);
        ewh.CurrentTemperature.Should().BeLessThan(Temperature.FromDegreesCelsius(75));
    }

    [Fact]
    public void HeatingTest()
    {
        var ewh = this.CreateEwh(
            heatEfficiency: Ratio.FromPercent(99),
            initialWaterTemperature: Temperature.FromDegreesCelsius(75),
            ambientInsulationLossesTimeConstant: TimeSpan.MaxValue,
            getHotWaterWithdrawalRate: this.NoHotWaterWithdrawal);
        ewh.Simulate(this.timeStep, true);
        ewh.CurrentTemperature.Should().BeGreaterThan(Temperature.FromDegreesCelsius(75));
    }

    [Fact]
    public void HeatingLossesTest()
    {
        var ewh = this.CreateEwh(
            heatEfficiency: Ratio.FromPercent(80),
            heatPower: Power.FromKilowatts(5),
            ambientInsulationLossesTimeConstant: TimeSpan.MaxValue,
            getHotWaterWithdrawalRate: this.NoHotWaterWithdrawal);
        var initialSoC = ewh.CurrentStateOfCharge;
        ewh.Simulate(this.timeStep, true);
        ewh.CurrentStateOfCharge.Should().BeLessThan(initialSoC + Power.FromKilowatts(5) * timeStep);
    }

    [Fact]
    public void ExceedingLowerLimitAmbientTest()
    {
        var ewh = this.CreateEwh(
            initialWaterTemperature: Temperature.FromDegreesCelsius(25),
            getAmbientTemperature: _ => Temperature.FromDegreesCelsius(20),
            getHotWaterWithdrawalRate: this.NoHotWaterWithdrawal);
        var totalSimulationTime = TimeSpan.FromDays(20);
        for (int step = 0; step < totalSimulationTime / timeStep; step += 1)
        {
            ewh.Simulate(timeStep, false);
        }

        ewh.CurrentTemperature.Should().BeGreaterOrEqualTo(Temperature.FromDegreesCelsius(20));
        outputHelper.WriteLine($"temperature at the end of simulation: {ewh.CurrentTemperature.DegreesCelsius} °C");
    }

    [Fact]
    public void ExceedingLowerLimitInletTest()
    {
        var ewh = this.CreateEwh(
            initialWaterTemperature: Temperature.FromDegreesCelsius(25),
            getAmbientTemperature: _ => Temperature.FromDegreesCelsius(20),
            getInletTemperature: _ => Temperature.FromDegreesCelsius(15),
            getHotWaterWithdrawalRate: _ => VolumeFlow.FromLitersPerMinute(8));
        var totalSimulationTime = TimeSpan.FromDays(20);
        for (int step = 0; step < totalSimulationTime / timeStep; step += 1)
        {
            ewh.Simulate(timeStep, false);
        }
        
        ewh.CurrentTemperature.Should().BeLessThan(Temperature.FromDegreesCelsius(20));
        ewh.CurrentTemperature.Should().BeGreaterThanOrEqualTo(Temperature.FromDegreesCelsius(15));
        outputHelper.WriteLine($"temperature at the end of simulation: {ewh.CurrentTemperature.DegreesCelsius} °C");
    }
}