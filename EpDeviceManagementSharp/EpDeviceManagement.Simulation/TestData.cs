using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.Data;
using EpDeviceManagement.Simulation.Storage;
using UnitsNet;

using MoreEnumerable = MoreLinq.MoreEnumerable;
using static MoreLinq.Extensions.CartesianExtension;

namespace EpDeviceManagement.Simulation;

public class TestData
{
    public static IList<BatteryConfiguration> GetBatteries()
    {
        // battery data partly from https://solar.htw-berlin.de/studien/speicher-inspektion-2022/
        return new List<BatteryConfiguration>
        {
            //new BatteryConfiguration()
            //{
            //    Description = "Tesla Powerwall",
            //    CreateBattery = () => new BatteryElectricStorage(
            //        Frequency.FromCyclesPerHour(Ratio.FromPercent(3).DecimalFractions / TimeSpan.FromDays(1).TotalHours),
            //        Ratio.FromPercent(90),
            //        Ratio.FromPercent(110))
            //        {
            //            TotalCapacity = Energy.FromKilowattHours(13.5),
            //            CurrentStateOfCharge = Energy.FromKilowattHours(6.75),
            //            MaximumChargePower = Power.FromKilowatts(4.6),
            //            MaximumDischargePower = Power.FromKilowatts(4.6),
            //        },
            //},
            //new BatteryConfiguration()
            //{
            //    Description = "Kostal Piko HVS 7.7", // D1
            //    CreateBattery = () =>
            //    {
            //        //var nominalVoltage = ElectricPotential.FromVolts(307.2);
            //        //var chargeCurrent = ElectricCurrent.FromAmperes(25);
            //        //var dischargeCurrent = ElectricCurrent.FromAmperes(25);
            //        var averageRoundtripEfficiency = Ratio.FromPercent(96.9);
            //        var averageRoundtripLoss = Ratio.FromPercent(100) - averageRoundtripEfficiency;
            //        var capacity = Energy.FromKilowattHours(7.68);
            //        return new BatteryElectricStorage(
            //            Power.FromWatts(5).DivideBy(capacity),
            //            Ratio.FromPercent(100) - averageRoundtripLoss / 2,
            //            Ratio.FromPercent(100) + averageRoundtripLoss / 2)
            //        {
            //            TotalCapacity = capacity,
            //            CurrentStateOfCharge = capacity / 2,
            //            MaximumChargePower = Power.FromKilowatts(6),
            //            MaximumDischargePower = Power.FromKilowatts(6),
            //        };
            //    },
            //},
            new BatteryConfiguration()
            {
                Description = "RCT Power Battery 11.5", // G2
                CreateBattery = () =>
                {
                    //var nominalVoltage = ElectricPotential.FromVolts(461);
                    //var chargeCurrent = ElectricCurrent.FromAmperes(25);
                    //var dischargeCurrent = ElectricCurrent.FromAmperes(25);
                    var averageRoundtripEfficiency = Ratio.FromPercent(95.6);
                    var averageRoundtripLoss = Ratio.FromPercent(100) - averageRoundtripEfficiency;
                    var capacity = Energy.FromKilowattHours(10.37);
                    return new BatteryElectricStorage(
                        Power.FromWatts(10.5).DivideBy(capacity),
                        Ratio.FromPercent(100) - averageRoundtripLoss / 2,
                        Ratio.FromPercent(100) + averageRoundtripLoss / 2)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = Power.FromKilowatts(10.5),
                        MaximumDischargePower = Power.FromKilowatts(10.5),
                    };
                },
            },
            new BatteryConfiguration()
            {
                Description = "Fenecon Home", // I1
                CreateBattery = () =>
                {
                    var averageRoundtripEfficiency = Ratio.FromPercent(95.5);
                    var averageRoundtripLoss = Ratio.FromPercent(100) - averageRoundtripEfficiency;
                    var capacity = Energy.FromKilowattHours(16.1);
                    return new BatteryElectricStorage(
                        Power.FromWatts(20.5).DivideBy(capacity),
                        Ratio.FromPercent(100) - averageRoundtripLoss / 2,
                        Ratio.FromPercent(100) + averageRoundtripLoss / 2)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = Power.FromKilowatts(7.84),
                        MaximumDischargePower = Power.FromKilowatts(7.84),
                    };
                },
            },
            new BatteryConfiguration()
            {
                Description = "Solax T-Bat H 23", // L1
                CreateBattery = () =>
                {
                    var averageRoundtripEfficiency = Ratio.FromPercent(94.7);
                    var averageRoundtripLoss = Ratio.FromPercent(100) - averageRoundtripEfficiency;
                    var capacity = Energy.FromKilowattHours(20.6);
                    return new BatteryElectricStorage(
                        Power.FromWatts(37).DivideBy(capacity),
                        Ratio.FromPercent(100) - averageRoundtripLoss / 2,
                        Ratio.FromPercent(100) + averageRoundtripLoss / 2)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = Power.FromKilowatts(13.8),
                        MaximumDischargePower = Power.FromKilowatts(13.8),
                    };
                },
            },
        };
    }

    public static IList<Func<Configuration, IEpDeviceController>> GetStrategies()
    {
        var strategies = new List<Func<Configuration, IEpDeviceController>>()
        {
            config => new AlwaysRequestIncomingPackets(config.Battery, config.PacketSize),
            config => new AlwaysRequestOutgoingPackets(config.Battery, config.PacketSize),
            config => new NoExchangeWithTheCell(),
        };
        var upperLimits = MoreEnumerable
            .Generate(0.5d, x => x + 0.1d)
            .TakeWhile(x => x <= 0.9d)
            .Select(x => Ratio.FromDecimalFractions(x))
            .ToList();
        var lowerLimits = MoreEnumerable
            .Generate(0.5d, x => x - 0.1d)
            .TakeWhile(x => x >= 0.1d)
            .Select(x => Ratio.FromDecimalFractions(x))
            .ToList();
        foreach (var (upper, lower) in upperLimits.Cartesian(lowerLimits, ValueTuple.Create))
        {
            if (upper != lower)
            {
                strategies.Add(config => new ProbabilisticModelingControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random));
            }
        }
        
        for (double lower = 0.1d; lower <= 0.9d; lower += 0.1d)
        {
            for (double upper = lower; upper <= 0.9d; upper += 0.1d)
            {
                var (min, max) = (Ratio.FromDecimalFractions(lower), Ratio.FromDecimalFractions(upper));
                strategies.Add(config => new AimForSpecificBatteryRange(
                    config.Battery,
                    config.PacketSize,
                    min,
                    max));
            }
        }

        return strategies;
    }

    public static IList<Energy> GetPacketSizes()
    {
        return MoreEnumerable
            .Generate(0d, x => x + 0.1d)
            .Skip(1)
            .Take(4)
            .Concat(MoreEnumerable
                .Generate(0d, x => x + 0.5)
                .Skip(1)
                .Take(10))
            .Select(x => Energy.FromKilowattHours(x))
            .ToList();
    }

    public static IList<Ratio> GetPacketProbabilities()
    {
        return MoreEnumerable
            .Sequence(10, 90, 10)
            .Prepend(5)
            .Append(95)
            .Append(98)
            .Select(x => Ratio.FromPercent(x))
            .ToList();
    }

    public static async Task<IList<DataSet>> GetDataSetsAsync(TimeSpan timeStep, IProgressIndicator progress)
    {
        IReadOnlyList<EnhancedEnergyDataSet> enhancedData;
        var (data, handle) = new ReadDataFromCsv().ReadAsync();
        const int lineCountOf15MinData = 153811;
        progress.Setup(lineCountOf15MinData, "reading the data");
        enhancedData = await EnhanceAsync(data, timeStep, progress);
        handle.Dispose();
        var dataSets = new List<DataSet>()
        {
            new DataSet()
            {
                Configuration = "Res1",
                Data = enhancedData,
                GetLoadsTotalPower = d => d.Residential1_Load,
                GetGeneratorsTotalPower = d => d.Residential1_Generation,
            },
            new DataSet()
            {
                Configuration = "Res1 noSolar",
                Data = enhancedData,
                GetLoadsTotalPower = d => d.Residential1_Load,
                GetGeneratorsTotalPower = _ => Power.Zero,
            },
            new DataSet()
            {
                Configuration = "Res2",
                Data = enhancedData,
                GetLoadsTotalPower = d => d.Residential2_Load,
                GetGeneratorsTotalPower = _ => Power.Zero,
            }
        };

        return dataSets;
    }

    public static async Task<IReadOnlyList<EnhancedEnergyDataSet>> EnhanceAsync(
        IAsyncEnumerable<EnergyDataSet> data,
        TimeSpan timeStep,
        IProgressIndicator progress)
    {
        var result = new List<EnhancedEnergyDataSet>();

        var dish_last = Energy.Zero;
        var freeze_last = Energy.Zero;
        var heat_last = Energy.Zero;
        var wash_last = Energy.Zero;
        var pv_last = Energy.Zero;

        var circulation_last2 = Energy.Zero;
        var freeze_last2 = Energy.Zero;
        var dish_last2 = Energy.Zero;
        var wash_last2 = Energy.Zero;
        await foreach (var entry in data)
        {
            var dish = Energy.FromKilowattHours(entry.DE_KN_residential1_dishwasher ?? dish_last.KilowattHours);
            var freeze = Energy.FromKilowattHours(entry.DE_KN_residential1_freezer ?? freeze_last.KilowattHours);
            var heat = Energy.FromKilowattHours(entry.DE_KN_residential1_heat_pump ?? heat_last.KilowattHours);
            var wash = Energy.FromKilowattHours(entry.DE_KN_residential1_washing_machine ?? wash_last.KilowattHours);
            var pv = Energy.FromKilowattHours(entry.DE_KN_residential1_pv ?? pv_last.KilowattHours);

            var circulation2 = Energy.FromKilowattHours(entry.DE_KN_residential2_circulation_pump ?? circulation_last2.KilowattHours);
            var freeze2 = Energy.FromKilowattHours(entry.DE_KN_residential2_freezer ?? freeze_last2.KilowattHours);
            var dish2 = Energy.FromKilowattHours(entry.DE_KN_residential2_dishwasher ?? dish_last2.KilowattHours);
            var wash2 = Energy.FromKilowattHours(entry.DE_KN_residential2_washing_machine ?? wash_last2.KilowattHours);

            var l1 = (dish - dish_last) / timeStep;
            l1 += (freeze - freeze_last) / timeStep;
            l1 += (heat - heat_last) / timeStep;
            l1 += (wash - wash_last) / timeStep;
            var l2 = (circulation2 - circulation_last2) / timeStep;
            l2 += (freeze2 - freeze_last2) / timeStep;
            l2 += (dish2 - dish_last2) / timeStep;
            l2 += (wash2 - wash_last2) / timeStep;
            result.Add(new EnhancedEnergyDataSet()
            {
                Timestamp = entry.cet_cest_timestamp,
                Residential1_Load = l1,
                Residential1_Generation = (pv - pv_last) / timeStep,
                Residential2_Load = l2,
            });
            dish_last = dish;
            freeze_last = freeze;
            heat_last = heat;
            wash_last = wash;
            pv_last = pv;

            circulation_last2 = circulation2;
            freeze_last2 = freeze2;
            dish_last2 = dish2;
            wash_last2 = wash2;

            progress.FinishOne();
        }

        return result;
    }

    public static async Task AnalyzeAsync()
    {
        var (data, handle) = new ReadDataFromCsv().ReadAsync();
        
        Power Min(Power left, Power right) => Power.FromKilowatts(Math.Min(left.Kilowatts, right.Kilowatts));
        Power Max(Power left, Power right) => Power.FromKilowatts(Math.Max(left.Kilowatts, right.Kilowatts));

        var timeStep = TimeSpan.FromMinutes(15);
        var enhanced = await EnhanceAsync(data, timeStep, new NoProgress());

        Power maxPower = Power.FromKilowatts(1000000);
        Power minPower = Power.FromKilowatts(-1000000);
        var (dish_min, dish_max) = (maxPower, minPower);
        var (freeze_min, freeze_max) = (maxPower, minPower);
        var (heat_min, heat_max) = (maxPower, minPower);
        var (wash_min, wash_max) = (maxPower, minPower);
        var (pv_min, pv_max) = (maxPower, minPower);
        foreach (var entry in enhanced)
        {
            //var dish_power = entry.Residential1_Dishwasher;
            //dish_min = Min(dish_min, dish_power);
            //dish_max = Max(dish_max, dish_power);

            //var freeze_power = entry.Residential1_Freezer;
            //freeze_min = Min(freeze_min, freeze_power);
            //freeze_max = Max(freeze_max, freeze_power);

            //var heat_power = entry.Residential1_HeatPump;
            //heat_min = Min(heat_min, heat_power);
            //heat_max = Max(heat_max, heat_power);

            //var wash_power = entry.Residential1_WashingMachine;
            //wash_min = Min(wash_min, wash_power);
            //wash_max = Max(wash_max, wash_power);

            //var pv_power = entry.Residential1_PV;
            //pv_min = Min(pv_min, pv_power);
            //pv_max = Max(pv_max, pv_power);
        }

        Console.WriteLine("Stats:");
        Console.WriteLine($"Dishwasher: [{dish_min}, {dish_max}]");
        Console.WriteLine($"Freezer: [{freeze_min}, {freeze_max}]");
        Console.WriteLine($"Heat pump: [{heat_min}, {heat_max}]");
        Console.WriteLine($"Washing machine: [{wash_min}, {wash_max}]");
        Console.WriteLine($"Photovoltaic: [{pv_min}, {pv_max}]");

        handle.Dispose();
    }
}