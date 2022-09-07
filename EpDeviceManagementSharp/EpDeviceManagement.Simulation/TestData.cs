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

        for (double left = 0.1d; left <= 0.9d; left += 0.1d)
        {
            for (double right = left; right <= 0.9d; right += 0.1d)
            {
                var (lower, upper) = (Ratio.FromDecimalFractions(left), Ratio.FromDecimalFractions(right));
                strategies.Add(config => new AimForSpecificBatteryRange(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper));
                strategies.Add(config => new ProbabilisticModelingControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random));
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
        var oneYearOfTimeSteps = (int) (TimeSpan.FromDays(365) / timeStep);

        // Data set spans five years, reduce to one
        enhancedData = enhancedData.Skip(oneYearOfTimeSteps).Take(oneYearOfTimeSteps).ToList();
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
            },
            new DataSet()
            {
                Configuration = "Res4",
                Data = enhancedData,
                GetLoadsTotalPower = d => d.Residential4_Load + d.Residential4_ControllableLoad,
                GetGeneratorsTotalPower = d => d.Residential4_Generation,
            },
            new DataSet()
            {
                Configuration = "Res4 noSolar",
                Data = enhancedData,
                GetLoadsTotalPower = d => d.Residential4_Load + d.Residential4_ControllableLoad,
                GetGeneratorsTotalPower = _ => Power.Zero,
            },
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

        var dish_last4 = Energy.Zero;
        var ev_last4 = Energy.Zero;
        var freeze_last4 = Energy.Zero;
        var heat_last4 = Energy.Zero;
        var pv_last4 = Energy.Zero;
        var fridge_last4 = Energy.Zero;
        var wash_last4 = Energy.Zero;

        Power GetPower(double? now, ref Energy last)
        {
            var now_energy = Energy.FromKilowattHours(now ?? last.KilowattHours);
            var result = (now_energy - last) / timeStep;
            last = now_energy;
            return result;
        }
        await foreach (var entry in data)
        {
            var dish = GetPower(entry.DE_KN_residential1_dishwasher, ref dish_last);
            var freeze = GetPower(entry.DE_KN_residential1_freezer, ref freeze_last);
            var heat = GetPower(entry.DE_KN_residential1_heat_pump, ref heat_last);
            var wash = GetPower(entry.DE_KN_residential1_washing_machine, ref wash_last);
            var pv = GetPower(entry.DE_KN_residential1_pv, ref pv_last);

            var circulation2 = GetPower(entry.DE_KN_residential2_circulation_pump, ref circulation_last2);
            var freeze2 = GetPower(entry.DE_KN_residential2_freezer, ref freeze_last2);
            var dish2 = GetPower(entry.DE_KN_residential2_dishwasher, ref dish_last2);
            var wash2 = GetPower(entry.DE_KN_residential2_washing_machine, ref wash_last2);

            var dish4 = GetPower(entry.DE_KN_residential4_dishwasher, ref dish_last4);
            var ev4 = GetPower(entry.DE_KN_residential4_ev, ref ev_last4);
            var freeze4 = GetPower(entry.DE_KN_residential4_freezer, ref freeze_last4);
            var heat4 = GetPower(entry.DE_KN_residential4_heat_pump, ref heat_last4);
            var pv4 = GetPower(entry.DE_KN_residential4_pv, ref pv_last4);
            var fridge4 = GetPower(entry.DE_KN_residential4_refrigerator, ref fridge_last4);
            var wash4 = GetPower(entry.DE_KN_residential4_washing_machine, ref wash_last4);

            var l1 = dish + freeze + heat + wash;

            var l2 = circulation2 + freeze2 + dish2 + wash2;

            var l4 = dish4 + freeze4 + heat4 + fridge4 + wash4;
            result.Add(new EnhancedEnergyDataSet()
            {
                Timestamp = entry.cet_cest_timestamp,
                Residential1_Load = l1,
                Residential1_Generation = pv,
                Residential2_Load = l2,
                Residential4_Load = l4,
                Residential4_ControllableLoad = ev4,
                Residential4_Generation = pv,
            });

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