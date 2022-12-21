using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.Data;
using EpDeviceManagement.Prediction;
using EpDeviceManagement.Simulation.Extensions;
using EpDeviceManagement.Simulation.Storage;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;
using UnitsNet.Units;
using MoreEnumerable = MoreLinq.MoreEnumerable;
using static MoreLinq.Extensions.CartesianExtension;
using static MoreLinq.Extensions.SkipUntilExtension;
using static MoreLinq.Extensions.TakeUntilExtension;

namespace EpDeviceManagement.Simulation;

public class TestData
{
    public static IList<BatteryConfiguration> GetBatteries(bool extended = false)
    {
        // battery data partly from https://solar.htw-berlin.de/studien/speicher-inspektion-2022/
        BatteryConfiguration MakeBattery(int cap, int power, string? description = null)
        {
            return new BatteryConfiguration()
            {
                Description = description ?? $"{cap} kWh [{power} kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = EnergyFast.FromKilowattHours(cap);
                    return new BatteryElectricStorage2(
                        PowerFast.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = PowerFast.FromKilowatts(power),
                        MaximumDischargePower = PowerFast.FromKilowatts(power),
                    };
                },
            };
        }
        var batteries = new List<BatteryConfiguration>
        {
            MakeBattery(12, 12, "A"),
#if !EXPERIMENTAL
            MakeBattery(16, 16, "B"),
#endif
        };
        if (extended)
        {
            batteries.AddRange(new [] {
                MakeBattery(8, 8),
                MakeBattery(10, 10),
                MakeBattery(12, 10),
                MakeBattery(10, 12),
                MakeBattery(15, 15),
                MakeBattery(16, 12),
            });
        }
        return batteries;
    }

    private static List<Func<Configuration, IEpDeviceController>> GetUnguardedStrategies()
    {
        var unguardedStrategies = new List<Func<Configuration, IEpDeviceController>>();
        for (double left = 0d; left <= 1d; left += 0.1d)
        {
            for (double right = left; right <= 1d; right += 0.1d)
            {
                // speed up for now
                //if (left == right)
                //{
                //    continue;
                //}

                var (lower, upper) = (Ratio.FromDecimalFractions(left), Ratio.FromDecimalFractions(right));
#if !EXPERIMENTAL
                unguardedStrategies.Add(config => new ThreePointController(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.DataSet.HasGeneration));
#endif
                if (left == right)
                {
                    //only test for 3SSC
                    continue;
                }
#if !EXPERIMENTAL
                unguardedStrategies.Add(config => new LinearProbabilisticRangeControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    config.DataSet.HasGeneration));
                unguardedStrategies.Add(config => new LinearProbabilisticEstimationRangeControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    config.DataSet.HasGeneration));
#endif
                unguardedStrategies.Add(config => new PemControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    config.DataSet.HasGeneration));
                //unguardedStrategies.Add(config => new EstimationPemControl(
                //    config.Battery,
                //    config.PacketSize,
                //    lower,
                //    upper,
                //    config.Random,
                //    config.DataSet.HasGeneration));
                var sendThresholds = new[] { -0.5, 0 };
                var receiveThresholds = new[] { 0d };
                var noOutgoings = new[]
                {
                    false,
#if !EXPERIMENTAL
                    true,
#endif
                };
                var withEstimations = new[]
                {
                    true,
#if !EXPERIMENTAL
                    false,
#endif
                };
                foreach (var (sendThreshold, receiveThreshold) in sendThresholds.Cartesian(receiveThresholds, ValueTuple.Create))
                {
                    #if EXPERIMENTAL
                    //break;
                    #endif
                    foreach (var (noOutgoing, withEstimation) in noOutgoings.Cartesian(withEstimations, ValueTuple.Create))
                    {
                        if (noOutgoing && !withEstimation)
                        {
                            continue;
                        }

                        if (noOutgoing && sendThreshold != sendThresholds[0])
                        {
                            continue;
                        }
                        
                        var receiveThresholdRatio = Ratio.FromDecimalFractions(receiveThreshold);
                        var sendThresholdRatio = Ratio.FromDecimalFractions(sendThreshold);
                        unguardedStrategies.Add(config =>
                        {
                            if (noOutgoing && !config.DataSet.HasGeneration)
                            {
                                return null;
                            }

                            if (!config.DataSet.HasGeneration && sendThreshold != sendThresholds[0])
                            {
                                return null;
                            }

                            return new DirectionAwareLinearProbabilisticRangeControl(
                                config.Battery,
                                config.PacketSize,
                                lower,
                                upper,
                                receiveThresholdRatio,
                                sendThresholdRatio,
                                withEstimation,
                                config.Random,
                                config.DataSet.HasGeneration,
                                noOutgoing);
                        });
                    }
                }
            }
        }
        
        return unguardedStrategies;
    }

    public static IList<Simulator.CreateStrategy> GetStrategies()
    {
        var unguardedStrategies = GetUnguardedStrategies();
        var guardedStrategies = new List<Simulator.CreateStrategy>();
        var emptyMargins = new[] { 0.7 }.Select(EnergyFast.FromKilowattHours);
        var fullMargins = new[] { 0.5 }.Select(EnergyFast.FromKilowattHours);
        var outgoingGuardPowerBuffers =
#if EXPERIMENTAL
            //MoreEnumerable.Sequence(4, 9, 1).Select(x => PowerFast.FromKilowatts(x)).ToList();
            new[] { PowerFast.FromKilowatts(0) };
#else
            //MoreEnumerable.Sequence(4, 9, 1).Select(x => PowerFast.FromKilowatts(x)).ToList();
            new[] { PowerFast.FromKilowatts(9) };
#endif

        foreach (var (emptyMargin, fullMargin) in emptyMargins.Cartesian(fullMargins, ValueTuple.Create))
        foreach (var outgoingPowerBuffer in outgoingGuardPowerBuffers)
        {
            foreach (var createStrategy in unguardedStrategies)
            {
                guardedStrategies.Add((config, o) =>
                {
                    var strategy = createStrategy(config);
                    if (strategy is null)
                    {
                        return null;
                    }
                    if (outgoingPowerBuffer != outgoingGuardPowerBuffers.Last() && !strategy.RequestsOutgoingPackets)
                    {
                        //no need to put strategy with different outgoing buffers that does not even request outgoing packets
                        //use only the buffer with 9kW because that is the good one used in the evaluation
                        return null;
                    }

                    var strategyRequestsOutgoingPackets = config.DataSet.HasGeneration;
                    return o
                        ? new GuardedStrategyWrapper(
                            strategy,
                            new BatteryCapacityGuard(config.Battery, config.PacketSize, emptyMargin, fullMargin, strategyRequestsOutgoingPackets),
                            new BatteryPowerGuard(config.Battery, config.PacketSize, PowerFast.Zero, outgoingPowerBuffer, strategyRequestsOutgoingPackets),
                            new OscillationGuard())
                        : new GuardedStrategyWrapper(
                            strategy,
                            new BatteryCapacityGuard(config.Battery, config.PacketSize, emptyMargin, fullMargin, strategyRequestsOutgoingPackets),
                            new BatteryPowerGuard(config.Battery, config.PacketSize, PowerFast.Zero, outgoingPowerBuffer, strategyRequestsOutgoingPackets));
                });
            }
            
        }

        return guardedStrategies;
    }

    private static IList<EnergyFast> GetPacketSizesInternal()
    {
        return MoreEnumerable
#if EXPERIMENTAL
            .Generate(0.15d, x => x + 0.05d)
            //.TakeUntil(x => x > 0.9)
            //.TakeUntil(x => x > 0.95)
            .TakeUntil(x => x > 1.1)
            //.TakeUntil(x => x > 1.3)
#else
            .Generate(0.05d, x => x + 0.05d)
            .TakeUntil(x => x > 1.4)
#endif
            .Select(EnergyFast.FromKilowattHours)
            .ToList();
    }

    public static IList<EnergyFast> GetPacketSizes()
    {
        return GetPacketSizesInternal().ToList();
    }

    public static IList<Ratio> GetPacketProbabilities()
    {
        return MoreEnumerable
#if EXPERIMENTAL
            .Sequence(20, 90, 10)
#else
            .Sequence(10, 90, 10)
#endif
            .Select(x => (double)x)
            .Append(99)
            .Select(x => Ratio.FromPercent(x).ToUnit(RatioUnit.DecimalFraction))
            .ToList();
    }

    public static IList<int> GetSeeds()
    {
        return new List<int>()
        {
            //13254,
            148354,
            //71712,
            //23730,
        };
    }

    private static IReadOnlyList<EnhancedPowerDataSet>? _cachedPowerDataSets;

    public static async Task<IList<DataSet>> GetDataSetsAsync(TimeSpan timeStep, IProgressIndicator progress, bool extended = false)
    {
        var dataTimeStep = TimeSpan.FromMinutes(1);
        var oneYearOfTimeSteps = (int)(TimeSpan.FromDays(365) / dataTimeStep);
        var oneDayOfTimesteps = (int)(TimeSpan.FromDays(1) / dataTimeStep);
        var twoYearsOfTimeSteps = 2 * oneYearOfTimeSteps;
        IReadOnlyList<EnhancedPowerDataSet> enhancedData;
        if (_cachedPowerDataSets != null)
        {
            enhancedData = _cachedPowerDataSets!;
        }
        else
        {
            var (fineResData, handle) = new ReadDataFromCsv()
#if EXPERIMENTAL
                .ReadAsync2("household_data_1min_power_reduced.csv");
#else
                .ReadAsync2("household_data_1min_power.csv");
#endif
    #if DEBUG
            var totalEntriesUsed = (int) (2 * oneYearOfTimeSteps);
            progress.Setup(totalEntriesUsed, "reading the data");
            enhancedData = await EnhanceAsync2(
                fineResData,
                totalEntriesUsed,
                progress);
    #else
            const int lineCountOf1MinData = 2307134;
            progress.Setup(lineCountOf1MinData, "reading the data");
            enhancedData = await EnhanceAsync2(
                fineResData,
                int.MaxValue,
                progress);
    #endif
            _cachedPowerDataSets = enhancedData;
            handle?.Dispose();

            progress.ProgressComplete();
        }
        // Data set spans five years, reduce to one
        var reducedData =
            enhancedData
                .Skip(oneYearOfTimeSteps)
                .SkipWhile(e => !e.Timestamp.IsDivisibleBy(timeStep))
                .Take(oneYearOfTimeSteps + oneDayOfTimesteps); // because 2016 was a leap year
        var batchSize = (int) (timeStep / dataTimeStep);
        var batchedData = MoreEnumerable.Batch(reducedData, batchSize, b => b.ToList()).ToList();


        var dataSets = new List<DataSet>()
        {
            new DataSet()
            {
                Configuration = "R1LG",
                Data = batchedData,
                GetLoad = d => d.Residential1_Load,
                GetGeneration = d => d.Residential1_Generation,
                HasGeneration = true,
            },
            new DataSet()
            {
                Configuration = "R4 Grid",
                Data = batchedData,
                GetLoad = d => d.Residential4_Import,
                GetGeneration = d => d.Residential4_Export,
                HasGeneration = true,
            },
#if !EXPERIMENTAL
            new DataSet()
            {
                Configuration = "R1L",
                Data = batchedData,
                GetLoad = d => d.Residential1_Load,
                GetGeneration = _ => PowerFast.Zero,
                HasGeneration = false,
            },
#endif
        };
        if (extended)
        {
            dataSets.AddRange(new[]
            {
                new DataSet()
                {
                    Configuration = "R2L",
                    Data = batchedData,
                    GetLoad = d => d.Residential2_Load,
                    GetGeneration = _ => PowerFast.Zero,
                },
                new DataSet()
                {
                    Configuration = "R3LG",
                    Data = batchedData,
                    GetLoad = d => d.Residential3_Load,
                    GetGeneration = d => d.Residential3_Generation,
                    HasGeneration = true,
                },
                new DataSet()
                {
                    Configuration = "R3L",
                    Data = batchedData,
                    GetLoad = d => d.Residential3_Load,
                    GetGeneration = _ => PowerFast.Zero,
                },
                new DataSet()
                {
                    Configuration = "R4LG",
                    Data = batchedData,
                    GetLoad = d => d.Residential4_Load,
                    //GetLoad = d => d.Residential4_Load + d.Residential4_ControllableLoad,
                    GetGeneration = d => d.Residential4_Generation,
                    HasGeneration = true,
                },
                new DataSet()
                {
                    Configuration = "R4L",
                    Data = batchedData,
                    GetLoad = d => d.Residential4_Load,
                    //GetLoad = d => d.Residential4_Load + d.Residential4_ControllableLoad,
                    GetGeneration = _ => PowerFast.Zero,
                },
                new DataSet()
                {
                    Configuration = "R5L",
                    Data = batchedData,
                    GetLoad = d => d.Residential5_Load,
                    GetGeneration = _ => PowerFast.Zero,
                },
                new DataSet()
                {
                    Configuration = "R6LG",
                    Data = batchedData,
                    GetLoad = d => d.Residential6_Load,
                    GetGeneration = d => d.Residential6_Generation,
                },
                new DataSet()
                {
                    Configuration = "R6L",
                    Data = batchedData,
                    GetLoad = d => d.Residential6_Load,
                    GetGeneration = _ => PowerFast.Zero,
                },
                new DataSet()
                {
                    Configuration = "R6 Grid",
                    Data = batchedData,
                    GetLoad = d => d.Residential6_Import,
                    GetGeneration = d => d.Residential6_Export,
                }
                //new DataSet()
                //{
                //    Configuration = "Ind3",
                //    Data = batchedData,
                //    GetLoad = d => d.Industrial3_Load + d.Industrial3_ControllableLoad,
                //    GetGeneration = d => d.Industrial3_Generation,
                //    HasGeneration = true,
                //},
                //new DataSet()
                //{
                //    Configuration = "Ind3 noSolar",
                //    Data = batchedData,
                //    GetLoad = d => d.Industrial3_Load + d.Industrial3_ControllableLoad,
                //    GetGeneration = _ => PowerFast.Zero,
                //},
                //new DataSet()
                //{
                //    Configuration = "Res4 doubleSolar",
                //    Data = batchedData,
                //    GetLoad = d => d.Residential4_Load + d.Residential4_ControllableLoad,
                //    GetGeneration = d => d.Residential4_Generation * 2,
                //    HasGeneration = true,
                //},
                //new DataSet()
                //{
                //    Configuration = "Res1 doubleSolar",
                //    Data = batchedData,
                //    GetLoad = d => d.Residential1_Load,
                //    GetGeneration = d => d.Residential1_Generation * 2,
                //    HasGeneration = true,
                //});
        });
        }

        return dataSets;
    }

    public static async Task<IReadOnlyList<EnhancedPowerDataSet>> EnhanceAsync2(
        IAsyncEnumerable<PowerDataSet> data,
        int numberOfEntriesToRead,
        IProgressIndicator progress,
        IAsyncEnumerable<PowerDataSet>? fineResData = null)
    {
        var result = new List<EnhancedPowerDataSet>();
        int read = 0;
        await foreach (var entry in data)
        {
            var enhancedPowerDataSet = EnhancePowerDataSet(entry);

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
        var l1 = PowerFast.FromKilowatts(
            entry.DE_KN_residential1_dishwasher
            + entry.DE_KN_residential1_freezer
            + entry.DE_KN_residential1_heat_pump
            + entry.DE_KN_residential1_washing_machine);
        var l2 = PowerFast.FromKilowatts(
            entry.DE_KN_residential2_circulation_pump
            + entry.DE_KN_residential2_freezer
            + entry.DE_KN_residential2_dishwasher
            + entry.DE_KN_residential2_washing_machine);
        var l3 = PowerFast.FromKilowatts(
            entry.DE_KN_residential3_circulation_pump
            + entry.DE_KN_residential3_dishwasher
            + entry.DE_KN_residential3_freezer
            + entry.DE_KN_residential3_refrigerator
            + entry.DE_KN_residential3_washing_machine);
        var l4 = PowerFast.FromKilowatts(
            entry.DE_KN_residential4_dishwasher
            + entry.DE_KN_residential4_freezer
            + entry.DE_KN_residential4_heat_pump
            + entry.DE_KN_residential4_refrigerator
            + entry.DE_KN_residential4_washing_machine
            + entry.DE_KN_residential4_ev);
        var l5 = PowerFast.FromKilowatts(
            entry.DE_KN_residential5_dishwasher
            + entry.DE_KN_residential5_refrigerator
            + entry.DE_KN_residential5_washing_machine);
        var l6 = PowerFast.FromKilowatts(
            entry.DE_KN_residential6_circulation_pump
            + entry.DE_KN_residential6_dishwasher
            + entry.DE_KN_residential6_freezer
            + entry.DE_KN_residential6_washing_machine);
        var i3 = PowerFast.FromKilowatts(
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
        var i3g = PowerFast.FromKilowatts(
            entry.DE_KN_industrial3_pv_facade
            + entry.DE_KN_industrial3_pv_roof);
        var enhancedPowerDataSet = new EnhancedPowerDataSet()
        {
            Timestamp = entry.cet_cest_timestamp,
            Residential1_Load = l1,
            Residential1_Generation = PowerFast.FromKilowatts(entry.DE_KN_residential1_pv),
            Residential2_Load = l2,
            Residential3_Load = l3,
            Residential3_Generation = PowerFast.FromKilowatts(entry.DE_KN_residential3_pv),
            Residential4_Load = l4,
            //Residential4_Load = l4,
            //Residential4_ControllableLoad = Power.FromKilowatts(entry.DE_KN_residential4_ev),
            Residential4_Generation = PowerFast.FromKilowatts(entry.DE_KN_residential4_pv),
            Residential4_Import = PowerFast.FromKilowatts(entry.DE_KN_residential4_grid_import),
            Residential4_Export = PowerFast.FromKilowatts(entry.DE_KN_residential4_grid_export),
            Residential5_Load = l5,
            Residential6_Load = l6,
            Residential6_Generation = PowerFast.FromKilowatts(entry.DE_KN_residential6_pv),
            Residential6_Import = PowerFast.FromKilowatts(entry.DE_KN_residential6_grid_import),
            Residential6_Export = PowerFast.FromKilowatts(entry.DE_KN_residential6_grid_export),
            Industrial3_Load = i3,
            Industrial3_ControllableLoad = PowerFast.FromKilowatts(entry.DE_KN_industrial3_ev),
            Industrial3_Generation = i3g,
            FineResDataSet = fineResDataSet,
        };
        return enhancedPowerDataSet;
    }
}