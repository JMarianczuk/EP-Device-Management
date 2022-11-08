using System.Collections.Concurrent;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.Data;
using EpDeviceManagement.Prediction;
using EpDeviceManagement.Simulation.Storage;
using UnitsNet;

using MoreEnumerable = MoreLinq.MoreEnumerable;
using static MoreLinq.Extensions.CartesianExtension;
using static MoreLinq.Extensions.TakeUntilExtension;

namespace EpDeviceManagement.Simulation;

public class TestData
{
    public static IList<BatteryConfiguration> GetBatteries()
    {
        // battery data partly from https://solar.htw-berlin.de/studien/speicher-inspektion-2022/
        return new List<BatteryConfiguration>
        {
            new BatteryConfiguration()
            {
                Description = "10 kWh [10 kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = Energy.FromKilowattHours(10);
                    return new BatteryElectricStorage2(
                        Power.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = Power.FromKilowatts(10),
                        MaximumDischargePower = Power.FromKilowatts(10),
                    };
                }
            },
            new BatteryConfiguration()
            {
                Description = "15 kWh [15 kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = Energy.FromKilowattHours(15);
                    return new BatteryElectricStorage2(
                        Power.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = Power.FromKilowatts(15),
                        MaximumDischargePower = Power.FromKilowatts(15),
                    };
                }
            }
        };
    }

    public static IList<Simulator.CreateStrategy> GetStrategies()
    {
        var strategies = new List<Simulator.CreateStrategy>()
        {
            //(config, o) => new AlwaysRequestIncomingPackets(config.Battery, config.PacketSize),
            //(config, o) => new AlwaysRequestOutgoingPackets(config.Battery, config.PacketSize),
            //(config, o) => new NoExchangeWithTheCell(),
        };

        for (double left = 0d; left <= 1d; left += 0.1d)
        {
            for (double right = left; right <= 1d; right += 0.1d)
            {
                var (lower, upper) = (Ratio.FromDecimalFractions(left), Ratio.FromDecimalFractions(right));
                strategies.Add((config, o) => new AimForSpecificBatteryRange(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    o));
                strategies.Add((config, o) => new ProbabilisticModelingControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    o));
                strategies.Add((config, o) => new LinearProbabilisticFunctionControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    o));
                strategies.Add((config, o) => new LinearProbabilisticEstimationFunctionControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    o));
                for (int packetPortionPercent = 0; packetPortionPercent <= (left == right ? 0 : 5); packetPortionPercent += 1)
                {
                    foreach (var withEstimation in new[] { true, false })
                    {
                        var portion = Ratio.FromPercent(packetPortionPercent);
                        strategies.Add((config, o) => new DirectionAwareLinearProbabilisticFunctionControl(
                            config.Battery,
                            config.PacketSize,
                            lower,
                            upper,
                            portion,
                            withEstimation,
                            config.Random,
                            o));
                    }
                }
            }
        }

        return strategies;
    }

    public static IList<Func<IValuePredictor<Power>>> GetPredictors()
    {
        var predictors = new List<Func<IValuePredictor<Power>>>()
        {
            () => new LastValuePredictor<Power>()
        };

        return predictors;
    }

    public static IList<Energy> GetPacketSizes()
    {
        //return MoreEnumerable
        //    .Generate(0.1d, x => x + 0.1d)
        //    .TakeUntil(x => x > 4.5)
        //    .Select(x => Energy.FromKilowattHours(x))
        //    .ToList();
        return MoreEnumerable
            .Generate(0.05d, x => x + 0.05d)
            //.TakeUntil(x => x > 1.3)
            .TakeUntil(x => x > 0.85)
            .Select(x => Energy.FromKilowattHours(x))
            .ToList();
    }

    public static IList<Ratio> GetPacketProbabilities()
    {
        return MoreEnumerable
            .Sequence(5, 95, 5)
            .Select(x => (double)x)
            .Append(98)
            .Append(99.5)
            .Select(x => Ratio.FromPercent(x))
            .ToList();
    }

    public static async Task<IList<DataSet>> GetDataSetsAsync(TimeSpan timeStep, IProgressIndicator progress, bool extended = false)
    {
        //IReadOnlyList<EnhancedEnergyDataSet> enhancedData;
        IReadOnlyList<EnhancedPowerDataSet> enhancedData;
        //var (data, handle) = new ReadDataFromCsv().ReadAsync();
        //enhancedData = await EnhanceAsync(data, timeStep, progress);
        var oneYearOfTimeSteps = (int)(TimeSpan.FromDays(365) / timeStep);

        var (data, handle) = new ReadDataFromCsv().ReadAsync2("household_data_5min_power.csv");
        var (fineResData, handle2) = new ReadDataFromCsv().ReadAsync2("household_data_1min_power.csv");
#if DEBUG
        var totalEntriesUsed = 2 * oneYearOfTimeSteps;
        progress.Setup(totalEntriesUsed, "reading the data");
        enhancedData = await EnhanceAsync2(
            data,
            totalEntriesUsed,
            progress,
            fineResData: fineResData);
#else
        const int lineCountOf15MinData = 153811;
        const int lineCountOf5MinData = 461428;
        int lineCount;
        switch (timeStep.TotalMinutes)
        {
            case 5:
                lineCount = lineCountOf5MinData;
                break;
            default:
            case 15:
                lineCount = lineCountOf15MinData;
                break;
        }
        progress.Setup(lineCount, "reading the data");
        enhancedData = await EnhanceAsync2(
            data,
            int.MaxValue,
            progress,
            fineResData: fineResData);
#endif
        // Data set spans five years, reduce to one
        enhancedData = enhancedData.Skip(oneYearOfTimeSteps).Take(oneYearOfTimeSteps).ToList();
        handle.Dispose();
        handle2.Dispose();
        progress.ProgressComplete();

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
            //new DataSet()
            //{
            //    Configuration = "Res4",
            //    Data = enhancedData,
            //    GetLoadsTotalPower = d => d.Residential4_Load + d.Residential4_ControllableLoad,
            //    GetGeneratorsTotalPower = d => d.Residential4_Generation,
            //},
            //new DataSet()
            //{
            //    Configuration = "Res4 noSolar",
            //    Data = enhancedData,
            //    GetLoadsTotalPower = d => d.Residential4_Load + d.Residential4_ControllableLoad,
            //    GetGeneratorsTotalPower = _ => Power.Zero,
            //},
        };
        if (extended)
        {
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res2",
                    Data = enhancedData,
                    GetLoadsTotalPower = d => d.Residential2_Load,
                    GetGeneratorsTotalPower = _ => Power.Zero,
                });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res3",
                    Data = enhancedData,
                    GetLoadsTotalPower = d => d.Residential3_Load,
                    GetGeneratorsTotalPower = d => d.Residential3_Generation,
                });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res3 noSolar",
                    Data = enhancedData,
                    GetLoadsTotalPower = d => d.Residential3_Load,
                    GetGeneratorsTotalPower = _ => Power.Zero,
                });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res5",
                    Data = enhancedData,
                    GetLoadsTotalPower = d => d.Residential5_Load,
                    GetGeneratorsTotalPower = _ => Power.Zero,
                });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Ind3",
            //        Data = enhancedData,
            //        GetLoadsTotalPower = d => d.Industrial3_Load + d.Industrial3_ControllableLoad,
            //        GetGeneratorsTotalPower = d => d.Industrial3_Generation,
            //    });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Ind3 noSolar",
            //        Data = enhancedData,
            //        GetLoadsTotalPower = d => d.Industrial3_Load + d.Industrial3_ControllableLoad,
            //        GetGeneratorsTotalPower = _ => Power.Zero,
            //    });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Res4 doubleSolar",
            //        Data = enhancedData,
            //        GetLoadsTotalPower = d => d.Residential4_Load + d.Residential4_ControllableLoad,
            //        GetGeneratorsTotalPower = d => d.Residential4_Generation * 2,
            //    });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Res1 doubleSolar",
            //        Data = enhancedData,
            //        GetLoadsTotalPower = d => d.Residential1_Load,
            //        GetGeneratorsTotalPower = d => d.Residential1_Generation * 2,
            //    });
        }

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
                Residential4_Generation = pv4,
            });

            progress.FinishOne();
        }

        return result;
    }

    public static async Task<IReadOnlyList<EnhancedPowerDataSet>> EnhanceAsync2(
        IAsyncEnumerable<PowerDataSet> data,
        int numberOfEntriesToRead,
        IProgressIndicator progress,
        IAsyncEnumerable<PowerDataSet>? fineResData = null)
    {
        var result = new List<EnhancedPowerDataSet>();
        int read = 0;
        IAsyncEnumerator<PowerDataSet> fineResEnumerator;
        bool hasFineResData;
        if (fineResData != null)
        {
            fineResEnumerator = fineResData.GetAsyncEnumerator();
            hasFineResData = await fineResEnumerator.MoveNextAsync();
        }
        else
        {
            fineResEnumerator = Enumerable.Empty<PowerDataSet>().AsAsyncEnumerable().GetAsyncEnumerator();
            hasFineResData = false;
        }

        await using var asyncDisposable = fineResEnumerator;
        await foreach (var entry in data)
        {
            while (hasFineResData && fineResEnumerator.Current.cet_cest_timestamp < entry.cet_cest_timestamp)
            {
                hasFineResData = await fineResEnumerator.MoveNextAsync();
            }

            EnhancedPowerDataSet enhancedPowerDataSet;
            if (hasFineResData && fineResEnumerator.Current.cet_cest_timestamp == entry.cet_cest_timestamp)
            {
                enhancedPowerDataSet = EnhancePowerDataSet(entry, EnhancePowerDataSet(fineResEnumerator.Current));
            }
            else
            {
                enhancedPowerDataSet = EnhancePowerDataSet(entry);
            }

            result.Add(enhancedPowerDataSet);
            progress.FinishOne();
            read += 1;
            if (read >= numberOfEntriesToRead)
            {
                break;
            }
        }

        return result;
    }

    private static EnhancedPowerDataSet EnhancePowerDataSet(
        PowerDataSet entry,
        EnhancedPowerDataSet? fineResDataSet = null)
    {
        var l1 = Power.FromKilowatts(
            entry.DE_KN_residential1_dishwasher
            + entry.DE_KN_residential1_freezer
            + entry.DE_KN_residential1_heat_pump
            + entry.DE_KN_residential1_washing_machine);
        var l2 = Power.FromKilowatts(
            entry.DE_KN_residential2_circulation_pump
            + entry.DE_KN_residential2_freezer
            + entry.DE_KN_residential2_dishwasher
            + entry.DE_KN_residential2_washing_machine);
        var l3 = Power.FromKilowatts(
            entry.DE_KN_residential3_circulation_pump
            + entry.DE_KN_residential2_dishwasher
            + entry.DE_KN_residential3_freezer
            + entry.DE_KN_residential3_refrigerator
            + entry.DE_KN_residential3_washing_machine);
        var l4 = Power.FromKilowatts(
            entry.DE_KN_residential4_dishwasher
            + entry.DE_KN_residential4_freezer
            + entry.DE_KN_residential4_heat_pump
            + entry.DE_KN_residential4_refrigerator
            + entry.DE_KN_residential4_washing_machine);
        var l5 = Power.FromKilowatts(
            entry.DE_KN_residential5_dishwasher
            + entry.DE_KN_residential5_refrigerator
            + entry.DE_KN_residential5_washing_machine);
        var i3 = Power.FromKilowatts(
            entry.DE_KN_industrial3_area_offices
            + entry.DE_KN_industrial3_area_room_1
            + entry.DE_KN_industrial3_area_room_2
            + entry.DE_KN_industrial3_area_room_3
            + entry.DE_KN_industrial3_area_room_4
            + entry.DE_KN_industrial3_compressor
            + entry.DE_KN_industrial3_cooling_aggregate
            + entry.DE_KN_industrial3_cooling_pumps
            + entry.DE_KN_industrial3_dishwasher
            + entry.DE_KN_industrial3_refrigerator
            + entry.DE_KN_industrial3_ventilation
            + entry.DE_KN_industrial3_machine_1
            + entry.DE_KN_industrial3_machine_2
            + entry.DE_KN_industrial3_machine_3
            + entry.DE_KN_industrial3_machine_4
            + entry.DE_KN_industrial3_machine_5);
        var i3g = Power.FromKilowatts(
            entry.DE_KN_industrial3_pv_facade
            + entry.DE_KN_industrial3_pv_roof);
        var enhancedPowerDataSet = new EnhancedPowerDataSet()
        {
            Timestamp = entry.cet_cest_timestamp,
            Residential1_Load = l1,
            Residential1_Generation = Power.FromKilowatts(entry.DE_KN_residential1_pv),
            Residential2_Load = l2,
            Residential3_Load = l3,
            Residential3_Generation = Power.FromKilowatts(entry.DE_KN_residential3_pv),
            Residential4_Load = l4,
            Residential4_ControllableLoad = Power.FromKilowatts(entry.DE_KN_residential4_ev),
            Residential4_Generation = Power.FromKilowatts(entry.DE_KN_residential4_pv),
            Residential5_Load = l5,
            Industrial3_Load = i3,
            Industrial3_ControllableLoad = Power.FromKilowatts(entry.DE_KN_industrial3_ev),
            Industrial3_Generation = i3g,
            FineResDataSet = fineResDataSet,
        };
        return enhancedPowerDataSet;
    }
}