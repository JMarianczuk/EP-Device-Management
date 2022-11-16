using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
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
        var batteries = new List<BatteryConfiguration>
        {
            new BatteryConfiguration()
            {
                Description = "12 kWh [12 kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = EnergyFast.FromKilowattHours(12);
                    return new BatteryElectricStorage2(
                        PowerFast.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = PowerFast.FromKilowatts(12),
                        MaximumDischargePower = PowerFast.FromKilowatts(12),
                    };
                }
            },

            new BatteryConfiguration()
            {
                Description = "16 kWh [12 kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = EnergyFast.FromKilowattHours(16);
                    return new BatteryElectricStorage2(
                        PowerFast.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = PowerFast.FromKilowatts(12),
                        MaximumDischargePower = PowerFast.FromKilowatts(12),
                    };
                }
            },
            
        };
        if (extended)
        {
            batteries.Add(new BatteryConfiguration()
            {
                Description = "10 kWh [10 kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = EnergyFast.FromKilowattHours(10);
                    return new BatteryElectricStorage2(
                        PowerFast.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = PowerFast.FromKilowatts(10),
                        MaximumDischargePower = PowerFast.FromKilowatts(10),
                    };
                }
            });
            batteries.Add(new BatteryConfiguration()
            {
                Description = "12 kWh [10 kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = EnergyFast.FromKilowattHours(12);
                    return new BatteryElectricStorage2(
                        PowerFast.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = PowerFast.FromKilowatts(12),
                        MaximumDischargePower = PowerFast.FromKilowatts(12),
                    };
                }
            });
            batteries.Add(new BatteryConfiguration()
            {
                Description = "15 kWh [15 kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = EnergyFast.FromKilowattHours(15);
                    return new BatteryElectricStorage2(
                        PowerFast.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = PowerFast.FromKilowatts(15),
                        MaximumDischargePower = PowerFast.FromKilowatts(15),
                    };
                }
            });
            batteries.Add(new BatteryConfiguration()
            {
                Description = "16 kWh [16 kW]",
                CreateBattery = () =>
                {
                    var avgRoundTripEfficiency = Ratio.FromPercent(95);
                    var dischargeEfficiency =
                        Ratio.FromDecimalFractions(2d / (avgRoundTripEfficiency.DecimalFractions + 1));
                    var chargeEfficiency = Ratio.FromPercent(200) - dischargeEfficiency;
                    var capacity = EnergyFast.FromKilowattHours(16);
                    return new BatteryElectricStorage2(
                        PowerFast.FromWatts(20),
                        chargeEfficiency,
                        dischargeEfficiency)
                    {
                        TotalCapacity = capacity,
                        CurrentStateOfCharge = capacity / 2,
                        MaximumChargePower = PowerFast.FromKilowatts(16),
                        MaximumDischargePower = PowerFast.FromKilowatts(16),
                    };
                }
            });
        }
        return batteries;
    }

    public static IList<Simulator.CreateStrategy> GetStrategies()
    {
        var unguardedStrategies = new List<Simulator.CreateStrategy>()
        {
            //(config, o) => new AlwaysRequestIncomingPackets(config.Battery, config.PacketSize),
            //(config, o) => new AlwaysRequestOutgoingPackets(config.Battery, config.PacketSize),
            //(config, o) => new NoExchangeWithTheCell(),
        };
        for (double left = 0d; left <= 1d; left += 0.1d)
        {
            for (double right = left; right <= 1d; right += 0.1d)
            {
                // speed up for now
                if (left == right)
                {
                    continue;
                }

                var (lower, upper) = (Ratio.FromDecimalFractions(left), Ratio.FromDecimalFractions(right));
                unguardedStrategies.Add((config, o) => new AimForSpecificBatteryRange(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.DataSet.HasGeneration));
                //strategies.Add((config, o) => new ProbabilisticModelingControl(
                //    config.Battery,
                //    config.PacketSize,
                //    lower,
                //    upper,
                //    config.Random,
                //    config.DataSet.HasGeneration,
                //    o));
                unguardedStrategies.Add((config, o) => new TclControl2(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    config.DataSet.HasGeneration));
                unguardedStrategies.Add((config, o) => new LinearProbabilisticFunctionControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    config.DataSet.HasGeneration));
                unguardedStrategies.Add((config, o) => new LinearProbabilisticEstimationFunctionControl(
                    config.Battery,
                    config.PacketSize,
                    lower,
                    upper,
                    config.Random,
                    config.DataSet.HasGeneration));
                foreach (var packetPortionPercent in left == right || true
                             ? new [] { 0 }
                             : MoreEnumerable.Sequence(0, 5, 1)
                         //.Append(10)
                         //.Append(20)
                        )
                {
                    foreach (var noOutgoing in new[] { false })//, true })
                    {
                        foreach (var withEstimation in new[] { true, false })
                        {
                            if (noOutgoing && !withEstimation)
                            {
                                continue;
                            }

                            var portion = Ratio.FromPercent(packetPortionPercent);
                            unguardedStrategies.Add((config, o) =>
                            {
                                if (noOutgoing && !config.DataSet.HasGeneration)
                                {
                                    return null;
                                }

                                return new DirectionAwareLinearProbabilisticFunctionControl(
                                    config.Battery,
                                    config.PacketSize,
                                    lower,
                                    upper,
                                    portion,
                                    withEstimation,
                                    config.Random,
                                    config.DataSet.HasGeneration,
                                    noOutgoing);
                            });
                        }
                    }
                }
            }
        }
        var guardedStrategies = new List<Simulator.CreateStrategy>();
        var outgoingGuardPowerBuffers = MoreEnumerable.Sequence(5, 9, 1).Select(x => PowerFast.FromKilowatts(x)).ToList();
        
        foreach (var outgoingPowerBuffer in outgoingGuardPowerBuffers)
        {
            foreach (var createStrategy in unguardedStrategies)
            {
                guardedStrategies.Add((config, o) =>
                {
                    var strategy = createStrategy(config, o);
                    if (strategy is null)
                    {
                        return null;
                    }
                    if (outgoingPowerBuffer != outgoingGuardPowerBuffers[0] && !strategy.RequestsOutgoingPackets)
                    {
                        //no need to put strategy with different outgoing buffers that does not even request outgoing packets
                        return null;
                    }
                    return o
                        ? new GuardedStrategyWrapper(
                            strategy,
                            new BatteryCapacityGuard(config.Battery, config.PacketSize),
                            new BatteryPowerGuard(config.Battery, config.PacketSize, outgoingPowerBuffer),
                            new OscillationGuard())
                        : new GuardedStrategyWrapper(
                            strategy,
                            new BatteryCapacityGuard(config.Battery, config.PacketSize),
                            new BatteryPowerGuard(config.Battery, config.PacketSize, outgoingPowerBuffer));
                });
            }
            
        }

        return guardedStrategies;
    }

    private static IList<EnergyFast> GetPacketSizesInternal()
    {
        return MoreEnumerable
            .Generate(0.05d, x => x + 0.05d)
            //.TakeUntil(x => x > 0.9)
            .TakeUntil(x => x > 1.1)
            //.TakeUntil(x => x > 1.3)
            .Select(x => EnergyFast.FromKilowattHours(x))
            .ToList();
    }

    public static IList<EnergyFast> GetPacketSizes()
    {
        return GetPacketSizesInternal().ToList();
    }

    public static IList<Ratio> GetPacketProbabilities()
    {
        return MoreEnumerable
            //.Sequence(80, 90, 10)
            .Sequence(10, 90, 10)
            .Select(x => (double)x)
            //.Append(98)
            //.Append(99.5)
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
        IReadOnlyList<EnhancedPowerDataSet> enhancedData;
        if (_cachedPowerDataSets != null)
        {
            enhancedData = _cachedPowerDataSets!;
        }
        else
        {
            //var fileName = GetFileName(timeStep);

            //var (data, handle) = new ReadDataFromCsv().ReadAsync2(fileName);
            var (fineResData, handle2) = new ReadDataFromCsv().ReadAsync2("household_data_1min_power.csv");
    #if DEBUG
            var totalEntriesUsed = (int) (2 * oneYearOfTimeSteps);
            progress.Setup(totalEntriesUsed, "reading the data");
            //enhancedData = await EnhanceAsync2(
            //    data,
            //    totalEntriesUsed,
            //    progress);
            enhancedData = await EnhanceAsync2(
                fineResData,
                totalEntriesUsed,
                progress);
    #else
            const int lineCountOf1MinData = 2307134;
            progress.Setup(lineCountOf1MinData, "reading the data");
            //enhancedData = await EnhanceAsync2(
            //    data,
            //    int.MaxValue,
            //    progress,
            //    fineResData: fineResData);
            enhancedData = await EnhanceAsync2(
                fineResData,
                int.MaxValue,
                progress);
    #endif
            _cachedPowerDataSets = enhancedData;
            //handle?.Dispose();
            handle2?.Dispose();

            progress.ProgressComplete();
        }
        // Data set spans five years, reduce to one
        var reducedData =
            enhancedData
                .Skip(oneYearOfTimeSteps)
                .SkipWhile(e => !e.Timestamp.IsDivisibleBy(timeStep))
                .Take(oneYearOfTimeSteps);
        var batchSize = (int) (timeStep / dataTimeStep);
        var batchedData = MoreEnumerable.Batch(reducedData, batchSize, b => b.ToList()).ToList();


        var dataSets = new List<DataSet>()
        {
            new DataSet()
            {
                Configuration = "Res1",
                Data = batchedData,
                GetLoad = d => d.Residential1_Load,
                GetGeneration = d => d.Residential1_Generation,
                HasGeneration = true,
            },
            new DataSet()
            {
                Configuration = "Res4 grid",
                Data = batchedData,
                GetLoad = d => d.Residential4_Import,
                GetGeneration = d => d.Residential4_Export,
                HasGeneration = true,
            },
        };
        if (extended)
        {
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res1 noSolar",
                    Data = batchedData,
                    GetLoad = d => d.Residential1_Load,
                    GetGeneration = _ => PowerFast.Zero,
                    HasGeneration = false,
                });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res2",
                    Data = batchedData,
                    GetLoad = d => d.Residential2_Load,
                    GetGeneration = _ => PowerFast.Zero,
                });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res3",
                    Data = batchedData,
                    GetLoad = d => d.Residential3_Load,
                    GetGeneration = d => d.Residential3_Generation,
                    HasGeneration = true,
                });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res3 noSolar",
                    Data = batchedData,
                    GetLoad = d => d.Residential3_Load,
                    GetGeneration = _ => PowerFast.Zero,
                });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res4",
                    Data = batchedData,
                    GetLoad = d => d.Residential4_Load,
                    //GetLoad = d => d.Residential4_Load + d.Residential4_ControllableLoad,
                    GetGeneration = d => d.Residential4_Generation,
                    HasGeneration = true,
                });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res4 noSolar",
                    Data = batchedData,
                    GetLoad = d => d.Residential4_Load,
                    //GetLoad = d => d.Residential4_Load + d.Residential4_ControllableLoad,
                    GetGeneration = _ => PowerFast.Zero,
                });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Res4 grid noSolar",
            //        Data = batchedData,
            //        GetLoad = d => d.Residential4_Import,
            //        GetGeneration = _ => PowerFast.Zero,
            //    });
            dataSets.Add(
                new DataSet()
                {
                    Configuration = "Res5",
                    Data = batchedData,
                    GetLoad = d => d.Residential5_Load,
                    GetGeneration = _ => PowerFast.Zero,
                });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Ind3",
            //        Data = batchedData,
            //        GetLoad = d => d.Industrial3_Load + d.Industrial3_ControllableLoad,
            //        GetGeneration = d => d.Industrial3_Generation,
            //        HasGeneration = true,
            //    });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Ind3 noSolar",
            //        Data = batchedData,
            //        GetLoad = d => d.Industrial3_Load + d.Industrial3_ControllableLoad,
            //        GetGeneration = _ => PowerFast.Zero,
            //    });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Res4 doubleSolar",
            //        Data = batchedData,
            //        GetLoad = d => d.Residential4_Load + d.Residential4_ControllableLoad,
            //        GetGeneration = d => d.Residential4_Generation * 2,
            //        HasGeneration = true,
            //    });
            //dataSets.Add(
            //    new DataSet()
            //    {
            //        Configuration = "Res1 doubleSolar",
            //        Data = batchedData,
            //        GetLoad = d => d.Residential1_Load,
            //        GetGeneration = d => d.Residential1_Generation * 2,
            //        HasGeneration = true,
            //    });
        }

        return dataSets;
    }

    public static string GetFileName(TimeSpan timeStep)
    {
        string fileName;
        switch (timeStep.TotalMinutes)
        {
            case 1:
            case 5:
            case 15:
            case 60:
            case 240:
            case 360:
            case 1440:
                fileName = $"household_data_{timeStep.TotalMinutes}min_power.csv";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(timeStep), "must be a supported number of minutes");
        }

        return fileName;
    }

    public static async Task<IReadOnlyList<EnhancedEnergyDataSet>> EnhanceAsync(
        IAsyncEnumerable<EnergyDataSet> data,
        TimeSpan timeStep,
        IProgressIndicator progress)
    {
        var result = new List<EnhancedEnergyDataSet>();

        var dish_last = EnergyFast.Zero;
        var freeze_last = EnergyFast.Zero;
        var heat_last = EnergyFast.Zero;
        var wash_last = EnergyFast.Zero;
        var pv_last = EnergyFast.Zero;

        var circulation_last2 = EnergyFast.Zero;
        var freeze_last2 = EnergyFast.Zero;
        var dish_last2 = EnergyFast.Zero;
        var wash_last2 = EnergyFast.Zero;

        var dish_last4 = EnergyFast.Zero;
        var ev_last4 = EnergyFast.Zero;
        var freeze_last4 = EnergyFast.Zero;
        var heat_last4 = EnergyFast.Zero;
        var pv_last4 = EnergyFast.Zero;
        var fridge_last4 = EnergyFast.Zero;
        var wash_last4 = EnergyFast.Zero;

        PowerFast GetPower(double? now, ref EnergyFast last)
        {
            var now_energy = EnergyFast.FromKilowattHours(now ?? last.KilowattHours);
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
        //IAsyncEnumerator<PowerDataSet> fineResEnumerator;
        //bool hasFineResData;
        //if (fineResData != null)
        //{
        //    fineResEnumerator = fineResData.GetAsyncEnumerator();
        //    hasFineResData = await fineResEnumerator.MoveNextAsync();
        //}
        //else
        //{
        //    fineResEnumerator = Enumerable.Empty<PowerDataSet>().AsAsyncEnumerable().GetAsyncEnumerator();
        //    hasFineResData = false;
        //}

        //await using var asyncDisposable = fineResEnumerator;
        await foreach (var entry in data)
        {
            //while (hasFineResData && fineResEnumerator.Current.cet_cest_timestamp < entry.cet_cest_timestamp)
            //{
            //    hasFineResData = await fineResEnumerator.MoveNextAsync();
            //}

            //EnhancedPowerDataSet enhancedPowerDataSet;
            //if (hasFineResData && fineResEnumerator.Current.cet_cest_timestamp == entry.cet_cest_timestamp)
            //{
            //    enhancedPowerDataSet = EnhancePowerDataSet(entry, EnhancePowerDataSet(fineResEnumerator.Current));
            //}
            //else
            //{
            //    enhancedPowerDataSet = EnhancePowerDataSet(entry);
            //}

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

    public static async Task<IReadOnlyList<EnhancedPowerDataSet>> EnhanceAsync3(
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
            if (entry.cet_cest_timestamp == new DateTimeOffset(2016, 10, 28, 8, 35, 0, TimeSpan.FromHours(2)))
            {
                int p = 5;
            }
            while (hasFineResData && fineResEnumerator.Current.cet_cest_timestamp < entry.cet_cest_timestamp)
            {
                hasFineResData = await fineResEnumerator.MoveNextAsync();
            }

            EnhancedPowerDataSet enhancedPowerDataSet;
            if (hasFineResData && fineResEnumerator.Current.cet_cest_timestamp == entry.cet_cest_timestamp)
            {
                enhancedPowerDataSet = EnhancePowerDataSet3(entry, EnhancePowerDataSet3(fineResEnumerator.Current));
            }
            else
            {
                enhancedPowerDataSet = EnhancePowerDataSet3(entry);
            }

            result.Add(enhancedPowerDataSet);
            progress.FinishOne();
            read += 1;
            if (read >= numberOfEntriesToRead)
            {
                break;
            }
        }

        //var histo = new List<(int, Power)>();
        //foreach (var threshold in MoreEnumerable.Sequence(0, 1000, 20))
        //{
        //    var powerT = Power.FromWatts(threshold).ToUnit(PowerUnit.Kilowatt);
        //    histo.Add((gridImports.Count(x => x >= powerT), powerT));
        //}
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
            Industrial3_Load = i3,
            Industrial3_ControllableLoad = PowerFast.FromKilowatts(entry.DE_KN_industrial3_ev),
            Industrial3_Generation = i3g,
            FineResDataSet = fineResDataSet,
        };
        return enhancedPowerDataSet;
    }

    private static int usedExplicitLoads = 0;
    static readonly PowerFast powerThreshold = PowerFast.FromWatts(50);
    private static List<PowerFast> gridImports = new List<PowerFast>();

    public static PowerFast GetLoad(PowerFast gridImport, PowerFast pv, PowerFast? gridExport = null, PowerFast? explicitLoads = null)
    {
        if (gridImport > PowerFast.Zero)
        {
            //if (explicitLoads.HasValue && explicitLoads.Value < powerThreshold && gridImport < powerThreshold)
            //{
            //    // difficult situation, probably the net load was zero, but import has the tiniest bit of power due to not perfect pv tracking
            //}
            gridImports.Add(gridImport);
            return gridImport + pv;
        }
        Debug.Assert(gridImport == PowerFast.Zero);
        
        if (pv == PowerFast.Zero)
        {
            return PowerFast.Zero;
        }
        Debug.Assert(pv > PowerFast.Zero);

        if (gridExport.HasValue)
        {
            return pv - gridExport.Value;
        }
        Debug.Assert(!gridExport.HasValue);

        if (explicitLoads.HasValue)
        {
            usedExplicitLoads += 1;
            return explicitLoads.Value;
        }

        throw new DataException("cannot infer load");
    }

    private static EnhancedPowerDataSet EnhancePowerDataSet3(
        PowerDataSet entry,
        EnhancedPowerDataSet? fineResDataSet = null)
    {
        var import1 = PowerFast.FromKilowatts(entry.DE_KN_residential1_grid_import);
        var explicitLoads1 = PowerFast.FromKilowatts(
            entry.DE_KN_residential1_dishwasher
            + entry.DE_KN_residential1_freezer
            + entry.DE_KN_residential1_heat_pump
            + entry.DE_KN_residential1_washing_machine);
        var pv1 = PowerFast.FromKilowatts(entry.DE_KN_residential1_pv);

        var import2 = PowerFast.FromKilowatts(entry.DE_KN_residential2_grid_import);

        var import3 = PowerFast.FromKilowatts(entry.DE_KN_residential3_grid_import);
        var export3 = PowerFast.FromKilowatts(entry.DE_KN_residential3_grid_export);
        var pv3 = PowerFast.FromKilowatts(entry.DE_KN_residential3_pv);

        var import4 = PowerFast.FromKilowatts(entry.DE_KN_residential4_grid_import);
        var export4 = PowerFast.FromKilowatts(entry.DE_KN_residential4_grid_export);
        var pv4 = PowerFast.FromKilowatts(entry.DE_KN_residential4_pv);

        var import5 = PowerFast.FromKilowatts(entry.DE_KN_residential5_grid_import);
        var enhancedPowerDataSet = new EnhancedPowerDataSet()
        {
            Timestamp = entry.cet_cest_timestamp,
            Residential1_Load = GetLoad(import1, pv1, explicitLoads: explicitLoads1),
            Residential1_Generation = pv1,
            Residential2_Load = import2,
            Residential3_Load = GetLoad(import3, pv3, gridExport: export3),
            Residential3_Generation = pv3,
            Residential4_Load = GetLoad(import4, pv4, gridExport: export4),
            Residential4_Generation = pv4,
            Residential5_Load = import5,
            FineResDataSet = fineResDataSet,
        };
        return enhancedPowerDataSet;
    }
}