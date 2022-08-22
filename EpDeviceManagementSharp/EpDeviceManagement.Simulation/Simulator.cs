using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.Data;
using EpDeviceManagement.Simulation.Loads;
using EpDeviceManagement.Simulation.Storage;
using UnitsNet;
using System.Text.Json;
using UnitsNet.Serialization.SystemTextJson;

using MoreEnumerable = MoreLinq.MoreEnumerable;
using static MoreLinq.Extensions.CartesianExtension;

namespace EpDeviceManagement.Simulation
{
    public class Simulator
    {
        public async Task AnalyzeAsync()
        {
            var (data, handle) = new ReadDataFromCsv().ReadAsync();
            
            Power Min(Power left, Power right) => Power.FromKilowatts(Math.Min(left.Kilowatts, right.Kilowatts));
            Power Max(Power left, Power right) => Power.FromKilowatts(Math.Max(left.Kilowatts, right.Kilowatts));

            var timeStep = TimeSpan.FromMinutes(15);
            var enhanced = await EnhanceAsync(data, timeStep);

            Power maxPower = Power.FromKilowatts(1000000);
            Power minPower = Power.FromKilowatts(-1000000);
            var (dish_min, dish_max) = (maxPower, minPower);
            var (freeze_min, freeze_max) = (maxPower, minPower);
            var (heat_min, heat_max) = (maxPower, minPower);
            var (wash_min, wash_max) = (maxPower, minPower);
            var (pv_min, pv_max) = (maxPower, minPower);
            foreach (var entry in enhanced)
            {
                var dish_power = entry.Residential1_Dishwasher;
                dish_min = Min(dish_min, dish_power);
                dish_max = Max(dish_max, dish_power);

                var freeze_power = entry.Residential1_Freezer;
                freeze_min = Min(freeze_min, freeze_power);
                freeze_max = Max(freeze_max, freeze_power);

                var heat_power = entry.Residential1_HeatPump;
                heat_min = Min(heat_min, heat_power);
                heat_max = Max(heat_max, heat_power);

                var wash_power = entry.Residential1_WashingMachine;
                wash_min = Min(wash_min, wash_power);
                wash_max = Max(wash_max, wash_power);

                var pv_power = entry.Residential1_PV;
                pv_min = Min(pv_min, pv_power);
                pv_max = Max(pv_max, pv_power);
            }

            Console.WriteLine("Stats:");
            Console.WriteLine($"Dishwasher: [{dish_min}, {dish_max}]");
            Console.WriteLine($"Freezer: [{freeze_min}, {freeze_max}]");
            Console.WriteLine($"Heat pump: [{heat_min}, {heat_max}]");
            Console.WriteLine($"Washing machine: [{wash_min}, {wash_max}]");
            Console.WriteLine($"Photovoltaic: [{pv_min}, {pv_max}]");

            handle.Dispose();
        }
        public async Task SimulateAsync()
        {
            var timeStep = TimeSpan.FromMinutes(15);

            IReadOnlyList<EnhancedEnergyDataSet> enhancedData;
            var (data, handle) = new ReadDataFromCsv().ReadAsync();
            enhancedData = await EnhanceAsync(data, timeStep);
            //IReadOnlyList<EnhancedEnergyDataSet> jsonData;
            //using (var stream = File.OpenRead("enhanced.json"))
            //{
            //    var options = new JsonSerializerOptions()
            //    {
            //        Converters =
            //        {
            //            new UnitsNetIQuantityJsonConverterFactory(),
            //        }
            //    };
            //    jsonData = await JsonSerializer.DeserializeAsync<List<EnhancedEnergyDataSet>>(stream);
            //}

            //var zip = enhancedData.Zip(jsonData, MoreEnumerable.Generate(1, x => x + 1));
            //var withDiff = zip.Where(tup =>
            //{
            //    var (left, right, _) = tup;
            //    var isDifferent = left.Residential1_Freezer != right.Residential1_Freezer
            //                      || left.Residential1_Dishwasher != right.Residential1_Dishwasher
            //                      || left.Residential1_HeatPump != right.Residential1_HeatPump
            //                      || left.Residential1_WashingMachine != right.Residential1_WashingMachine
            //                      || left.Residential1_PV != right.Residential1_PV;
            //    return isDifferent;
            //}).ToList();
            //var nonZero = jsonData.Where(x =>
            //{
            //    var isNonZero = x.Residential1_HeatPump != Power.Zero
            //                    || x.Residential1_Dishwasher != Power.Zero
            //                    || x.Residential1_Freezer != Power.Zero
            //                    || x.Residential1_PV != Power.Zero
            //                    || x.Residential1_WashingMachine != Power.Zero;
            //    return isNonZero;
            //}).ToList();
            //var part = enhancedData.Skip(15400).Take(100).ToList();
            var dataSets = new[]
            {
                new DataSet()
                {
                    Configuration = "Res1",
                    Data = enhancedData,
                },
                new DataSet()
                {
                    Configuration = "Res1 noSolar",
                    Data = enhancedData.Select(WithoutSolar).ToList(),
                },
            };
            handle.Dispose();

            var batteries = new List<BatteryConfiguration>
            {
                new BatteryConfiguration()
                {
                    CreateBattery = () => new BatteryElectricStorage(
                        Frequency.FromCyclesPerHour(Ratio.FromPercent(3).DecimalFractions / TimeSpan.FromDays(1).TotalHours),
                        Ratio.FromPercent(90),
                        Ratio.FromPercent(110))
                        {
                            CurrentStateOfCharge = Energy.FromKilowattHours(6.75),
                            TotalCapacity = Energy.FromKilowattHours(13.5),
                            MaximumChargePower = Power.FromKilowatts(4.6),
                            MaximumDischargePower = Power.FromKilowatts(4.6),
                        },
                    Description = "Tesla Powerwall",
                },
            };

            var strategies = new List<Func<Configuration, IEpDeviceController>>()
            {
                config => new AlwaysRequestIncomingPackets(config.Battery, config.PacketSize),
                config => new AlwaysRequestOutgoingPackets(config.Battery, config.PacketSize),
                config => new NoExchangeWithTheCell(),
            };
            var upperLimits = MoreEnumerable
                .Generate(0.5d, x => x + 0.1d)
                .TakeWhile(x => x <= 0.9d)
                .ToList();
            var lowerLimits = MoreEnumerable
                .Generate(0.5d, x => x - 0.1d)
                .TakeWhile(x => x >= 0.1d)
                .ToList();
            foreach (var (upper, lower) in upperLimits.Cartesian(lowerLimits, Tuple.Create))
            {
                strategies.Add(config => new ProbabilisticModelingControl(
                    config.Battery,
                    config.PacketSize,
                    config.Battery.TotalCapacity * upper,
                    config.Battery.TotalCapacity * lower,
                    config.Random));
            }
            foreach (var upper in upperLimits)
            {
                strategies.Add(config => new AimForSpecificBatteryLevel(
                    config.Battery,
                    config.PacketSize,
                    config.Battery.TotalCapacity * upper));
            }

            var packetSizes = MoreEnumerable.Generate(0d, x => x + 0.5)
                .Skip(1)
                .Take(10)
                .Select(x => Energy.FromKilowattHours(x))
                .ToList();

            var packetProbabilities = MoreEnumerable.Sequence(10, 90, 10)
                .Select(x => Ratio.FromPercent(x))
                .Prepend(Ratio.FromPercent(5))
                .Append(Ratio.FromPercent(95))
                .ToList();

            var timeSteps = new[]
            {
                timeStep,
            };

            var seeds = new[]
            {
                13254,
                148354,
            };

            var allCombinations = packetSizes.Cartesian(
                strategies, batteries, packetProbabilities, dataSets, timeSteps, seeds,
                Tuple.Create);
            var numberOfCombinations = GetNumberOfCombinations(
                packetSizes, strategies, batteries, packetProbabilities, dataSets, timeSteps, seeds);
            IProducerConsumerCollection<SimulationResult> results = new ConcurrentBag<SimulationResult>();

            using (var progress = new ConsoleProgressBar())
            {
                progress.Setup(
                    numberOfCombinations,
                    "Running the simulation");
#if DEBUG
                Sequential.ForEach(
#else
                Parallel.ForEach(
#endif
                    allCombinations,
                    new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 16,
                    },
                    combination =>
                    {
                        var (
                            packetSize,
                            strategy,
                            battery,
                            packetProbability,
                            dataSet,
                            simulationStep,
                            seed
                            ) = combination;
                        var res = SimulateSingle(
                            simulationStep,
                            strategy,
                            packetSize,
                            dataSet,
                            packetProbability, 
                            seed,
                            battery);
                        progress.FinishOne();
                        results.TryAdd(res);
                    });
            }

            var resultsFileName = "results.txt";
            File.Delete(resultsFileName);
            using (var progress = new ConsoleProgressBar())
            await using (var stream = File.OpenWrite(resultsFileName))
            await using (var writer = new StreamWriter(stream))
            {
                progress.Setup(results.Count, "Writing the results");
                foreach (var entry in results)
                {
                    await writer.WriteLineAsync(string.Join('\t',
                        "RESULT",
                        $"strategy={entry.StrategyName}",
                        $"configuration={entry.StrategyConfiguration}",
                        $"data={entry.DataConfiguration}",
                        $"battery={entry.BatteryConfiguration}",
                        $"batteryMin={entry.BatteryMinSoC}",
                        $"batteryMax={entry.BatteryMaxSoC}",
                        $"batteryAvg={entry.BatteryAvgSoC}",
                        $"packetSize={entry.PacketSize}",
                        $"probability={entry.PacketProbability}",
                        $"timeStep={entry.TimeStep}",
                        $"success={entry.Success}",
                        $"fail={entry.FailReason}",
                        $"steps={entry.StepsSimulated}",
                        $"seed={entry.Seed}"));
                    progress.FinishOne();
                }
            }
        }

        private SimulationResult SimulateSingle(
            TimeSpan timeStep,
            Func<Configuration, IEpDeviceController> createStrategy,
            Energy packetSize,
            DataSet dataSet,
            Ratio packetProbability,
            int seed,
            BatteryConfiguration batteryConfig)
        {
            var random = new SeededRandomNumberGenerator(seed);
            TransferResult lastDecision = new TransferResult.NoTransferRequested();
            var battery = batteryConfig.CreateBattery();
            var strategy = createStrategy(
                new Configuration()
                {
                    Battery = battery,
                    PacketSize = packetSize,
                    Random = random,
                });
//#if DEBUG
//            if (strategy is ProbabilisticModelingControl pmc
//                && pmc.Configuration == "[5.4 kWh, 12.15 kWh]"
//                && dataSet.Configuration == "Res1"
//                && packetSize.Equals(Energy.FromKilowattHours(3.5), 0.01, ComparisonType.Relative)
//                && packetProbability.Equals(Ratio.FromPercent(80), 0.01, ComparisonType.Relative))
//            {
//                int p = 5;
//            }
//            else
//            {
//                return new SimulationResult();
//            }
//#endif

            UncontrollableLoad[] res1 = new UncontrollableLoad[4];
            for (int i = 0; i < res1.Length; i += 1)
            {
                res1[i] = new UncontrollableLoad()
                {
                    LastStatus = Energy.Zero,
                };
            }

            UncontrollableGeneration[] res1gen = new UncontrollableGeneration[1];
            res1gen[0] = new UncontrollableGeneration()
            {
                LastStatus = Energy.Zero,
            };

            int step = 0;
            var result = new SimulationResult()
            {
                PacketProbability = packetProbability,
                PacketSize = packetSize,
                TimeStep = timeStep,
                StrategyName = strategy.Name,
                StrategyConfiguration = strategy.Configuration,
                DataConfiguration = dataSet.Configuration,
                BatteryConfiguration = $"{batteryConfig.Description} [{battery.TotalCapacity}]",
                Seed = seed,
            };
            var batteryMinSoC = battery.CurrentStateOfCharge;
            var batteryMaxSoC = battery.CurrentStateOfCharge;
            var batterySoCSum = battery.CurrentStateOfCharge;
            foreach (var entry in dataSet.Data)
            {
                res1[0].CurrentDemand = entry.Residential1_Dishwasher;
                res1[1].CurrentDemand = entry.Residential1_Freezer;
                res1[2].CurrentDemand = entry.Residential1_HeatPump;
                res1[3].CurrentDemand = entry.Residential1_WashingMachine;
                res1gen[0].CurrentGeneration = entry.Residential1_PV;

//#if DEBUG
//                if (step >= 15490)
//                {
//                    int p = 6;
//                }
//#endif
                
                var decision = strategy.DoControl(timeStep, res1, res1gen, lastDecision);
                Energy packet;
                if (decision is ControlDecision.RequestTransfer rt)
                {
                    var success = random.NextDouble() <= packetProbability.DecimalFractions;
                    packet = success
                        ? (rt.RequestedDirection == PacketTransferDirection.Outgoing
                            ? packetSize
                            : -packetSize)
                        : Energy.Zero;
                    lastDecision = success
                        ? new TransferResult.Success()
                        {
                            PerformedDirection = rt.RequestedDirection,
                        }
                        : new TransferResult.Failure()
                        {
                            RequestedDirection = rt.RequestedDirection,
                        };
                }
                else
                {
                    packet = Energy.Zero;
                    lastDecision = new TransferResult.NoTransferRequested();
                }

                var power = res1.Aggregate(Power.Zero, (left, right) => left + right.CurrentDemand) -
                    res1gen[0].CurrentGeneration + packet / timeStep;
                if (power > Power.Zero)
                {
                    battery.Simulate(timeStep, Power.Zero, power);
                }
                else
                {
                    battery.Simulate(timeStep, -power, Power.Zero);
                }
                step += 1;

                batteryMinSoC = Units.Min(batteryMinSoC, battery.CurrentStateOfCharge);
                batteryMaxSoC = Units.Max(batteryMaxSoC, battery.CurrentStateOfCharge);
                batterySoCSum += battery.CurrentStateOfCharge;

                var belowZero = battery.CurrentStateOfCharge <= Energy.Zero;
                var exceedCapacity = battery.CurrentStateOfCharge >= battery.TotalCapacity;
                if (belowZero || exceedCapacity)
                {
                    result.Success = false;
                    result.StepsSimulated = step;
                    if (belowZero)
                    {
                        result.FailReason = BatteryOutOfBoundsReason.BelowZero;
                    }
                    else if (exceedCapacity)
                    {
                        result.FailReason = BatteryOutOfBoundsReason.ExceedCapacity;
                    }
                    return result;
                }

            }

            result.Success = true;
            result.StepsSimulated = step;
            result.BatteryMinSoC = batteryMinSoC;
            result.BatteryMaxSoC = batteryMaxSoC;
            result.BatteryAvgSoC = batterySoCSum / step;
            return result;
        }

        private static EnhancedEnergyDataSet WithoutSolar(EnhancedEnergyDataSet data)
        {
            return new EnhancedEnergyDataSet()
            {
                Timestamp = data.Timestamp,
                Residential1_Dishwasher = data.Residential1_Dishwasher,
                Residential1_Freezer = data.Residential1_Freezer,
                Residential1_HeatPump = data.Residential1_HeatPump,
                Residential1_WashingMachine = data.Residential1_WashingMachine,
                Residential1_PV = Power.Zero,
            };
        }

        public async Task<IReadOnlyList<EnhancedEnergyDataSet>> EnhanceAsync(
            IAsyncEnumerable<EnergyDataSet> data,
            TimeSpan timeStep)
        {
            var result = new List<EnhancedEnergyDataSet>();

            var dish_last = Energy.Zero;
            var freeze_last = Energy.Zero;
            var heat_last = Energy.Zero;
            var wash_last = Energy.Zero;
            var pv_last = Energy.Zero;
            await foreach (var entry in data)
            {
                var dish = Energy.FromKilowattHours(entry.DE_KN_residential1_dishwasher ?? dish_last.KilowattHours);
                var freeze = Energy.FromKilowattHours(entry.DE_KN_residential1_freezer ?? freeze_last.KilowattHours);
                var heat = Energy.FromKilowattHours(entry.DE_KN_residential1_heat_pump ?? heat_last.KilowattHours);
                var wash = Energy.FromKilowattHours(entry.DE_KN_residential1_washing_machine ?? wash_last.KilowattHours);
                var pv = Energy.FromKilowattHours(entry.DE_KN_residential1_pv ?? pv_last.KilowattHours);
                result.Add(new EnhancedEnergyDataSet()
                {
                    Timestamp = entry.cet_cest_timestamp,
                    Residential1_Dishwasher = (dish - dish_last) / timeStep,
                    Residential1_Freezer = (freeze - freeze_last) / timeStep,
                    Residential1_HeatPump = (heat - heat_last) / timeStep,
                    Residential1_WashingMachine = (wash - wash_last) / timeStep,
                    Residential1_PV = (pv - pv_last) / timeStep,
                });
                dish_last = dish;
                freeze_last = freeze;
                heat_last = heat;
                wash_last = wash;
                pv_last = pv;
            }

            return result;
        }

        private static int GetNumberOfCombinations(params ICollection[] collections)
        {
            return collections.Aggregate(1, (num, collection) => num * collection.Count);
        }
    }
}