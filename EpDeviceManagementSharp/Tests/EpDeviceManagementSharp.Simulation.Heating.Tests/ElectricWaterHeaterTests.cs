using EpDeviceManagement.Simulation.Heating;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Xunit.Abstractions;

namespace EpDeviceManagementSharp.Simulation.Heating.Tests
{
    public class ElectricWaterHeaterTests
    {
        private readonly ITestOutputHelper outputHelper;
        private readonly SpecificEntropy waterSpecificEntropy = SpecificEntropy.FromKilojoulesPerKilogramKelvin(4.186);
        private readonly Density waterDensity = Density.FromKilogramsPerLiter(0.99);
        private readonly Temperature ambientTemperature = Temperature.FromDegreesCelsius(20);
        private readonly Temperature inletTemperature = Temperature.FromDegreesCelsius(15);

        public ElectricWaterHeaterTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void StandingLossesTest()
        {
            var ewh = new ElectricWaterHeater(
                Ratio.FromPercent(100),
                TimeSpan.FromHours(1),
                waterSpecificEntropy,
                waterDensity)
            {
                MaximumChargePower = Power.FromKilowatts(5),
                MinimumWaterTemperature = Temperature.FromDegreesCelsius(0),
                MaximumWaterTemperature = Temperature.FromDegreesCelsius(90),
                TotalWaterCapacity = Volume.FromLiters(1000),
                CurrentTemperature = Temperature.FromDegreesCelsius(50),
            };
            ewh.Simulate(
                TimeSpan.FromMinutes(1),
                Power.Zero,
                false,
                ambientTemperature,
                VolumeFlow.Zero,
                inletTemperature);
            ewh.CurrentTemperature.Should().BeLessThan(Temperature.FromDegreesCelsius(50));
        }

        [Fact]
        public void HeatingTest()
        {
            var ewh = new ElectricWaterHeater(
                Ratio.FromPercent(99),
                TimeSpan.MaxValue,
                waterSpecificEntropy,
                waterDensity)
            {
                MaximumChargePower = Power.FromKilowatts(5),
                MinimumWaterTemperature = Temperature.FromDegreesCelsius(0),
                MaximumWaterTemperature = Temperature.FromDegreesCelsius(90),
                TotalWaterCapacity = Volume.FromLiters(1000),
                CurrentTemperature = Temperature.FromDegreesCelsius(50),
            };
            ewh.Simulate(
                TimeSpan.FromHours(1),
                Power.FromKilowatts(1),
                true,
                ambientTemperature,
                VolumeFlow.Zero,
                inletTemperature);
            ewh.CurrentTemperature.Should().BeGreaterThan(Temperature.FromDegreesCelsius(50));
        }

        [Fact]
        public void HeatingLossesTest()
        {
            var ewh = new ElectricWaterHeater(
                Ratio.FromPercent(99),
                TimeSpan.MaxValue,
                waterSpecificEntropy,
                waterDensity)
            {
                MaximumChargePower = Power.FromKilowatts(5),
                MinimumWaterTemperature = Temperature.FromDegreesCelsius(0),
                MaximumWaterTemperature = Temperature.FromDegreesCelsius(90),
                TotalWaterCapacity = Volume.FromLiters(1000),
                CurrentTemperature = Temperature.FromDegreesCelsius(50),
            };
            var initialSoC = ewh.CurrentStateOfCharge;
            ewh.Simulate(
                TimeSpan.FromHours(1),
                Power.FromKilowatts(1),
                true,
                ambientTemperature,
                VolumeFlow.Zero,
                inletTemperature);
            ewh.CurrentStateOfCharge.Should()
                .BeLessThan(initialSoC + Energy.FromKilowattHours(1)); //TODO optimize using initialSoC
        }

        [Fact]
        public void HeatLossTest()
        {
            var ewh = new ElectricWaterHeater(
                Ratio.FromPercent(99),
                TimeSpan.MaxValue,
                waterSpecificEntropy,
                waterDensity)
            {
                MaximumChargePower = Power.FromKilowatts(5),
                MinimumWaterTemperature = Temperature.FromDegreesCelsius(0),
                MaximumWaterTemperature = Temperature.FromDegreesCelsius(90),
                TotalWaterCapacity = Volume.FromLiters(1000),
                CurrentTemperature = Temperature.FromDegreesCelsius(50),
            };
            ewh.Simulate(
                TimeSpan.FromMinutes(1),
                Power.Zero,
                false,
                ambientTemperature,
                VolumeFlow.FromLitersPerSecond(0.1),
                inletTemperature);
            ewh.CurrentTemperature.Should().BeLessThan(Temperature.FromDegreesCelsius(50));
        }

        [Fact]
        public void ExceedingLowerLimitAmbientTest()
        {
            var ewh = new ElectricWaterHeater(
                Ratio.FromPercent(99),
                TimeSpan.FromHours(8),
                waterSpecificEntropy,
                waterDensity)
            {
                MaximumChargePower = Power.FromKilowatts(5),
                MinimumWaterTemperature = Temperature.FromDegreesCelsius(0),
                MaximumWaterTemperature = Temperature.FromDegreesCelsius(90),
                TotalWaterCapacity = Volume.FromLiters(1000),
                CurrentTemperature = Temperature.FromDegreesCelsius(25),
            };
            var totalSimTime = TimeSpan.FromDays(20);
            var step = TimeSpan.FromMinutes(5);
            for (int s = 0; s < totalSimTime / step; s += 1)
            {
                ewh.Simulate(
                    step,
                    Power.Zero,
                    false,
                    Temperature.FromDegreesCelsius(20),
                    VolumeFlow.Zero,
                    inletTemperature);
            }
            ewh.CurrentTemperature.Should().BeGreaterThanOrEqualTo(Temperature.FromDegreesCelsius(20));
            outputHelper.WriteLine($"temperature at the end of simulation: {ewh.CurrentTemperature.DegreesCelsius} °C");
        }

        [Fact]
        public void ExceedingLowerLimitInletTest()
        {
            var ewh = new ElectricWaterHeater(
                Ratio.FromPercent(99),
                TimeSpan.FromHours(8),
                waterSpecificEntropy,
                waterDensity)
            {
                MaximumChargePower = Power.FromKilowatts(5),
                MinimumWaterTemperature = Temperature.FromDegreesCelsius(0),
                MaximumWaterTemperature = Temperature.FromDegreesCelsius(90),
                TotalWaterCapacity = Volume.FromLiters(1000),
                CurrentTemperature = Temperature.FromDegreesCelsius(25),
            };
            var totalSimTime = TimeSpan.FromDays(20);
            var step = TimeSpan.FromMinutes(5);
            for (int s = 0; s < totalSimTime / step; s += 1)
            {
                ewh.Simulate(
                    step,
                    Power.Zero,
                    false,
                    Temperature.FromDegreesCelsius(20),
                    VolumeFlow.FromLitersPerMinute(40),
                    Temperature.FromDegreesCelsius(15));
            }
            ewh.CurrentTemperature.Should().BeLessThan(Temperature.FromDegreesCelsius(20));
            ewh.CurrentTemperature.Should().BeGreaterThanOrEqualTo(Temperature.FromDegreesCelsius(15));
            outputHelper.WriteLine($"temperature at the end of simulation: {ewh.CurrentTemperature.DegreesCelsius} °C");
        }
}
}