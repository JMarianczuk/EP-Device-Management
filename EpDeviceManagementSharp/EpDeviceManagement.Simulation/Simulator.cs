using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Simulation.Loads;
using EpDeviceManagement.Simulation.Storage;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;
using Humanizer;
using JetBrains.Profiler.Api;
using static MoreLinq.Extensions.CartesianExtension;
using static EpDeviceManagement.Simulation.Extensions.DataSetExtensions;

namespace EpDeviceManagement.Simulation
{
    public class Simulator
    {
        public async Task SimulateAsync()
        {
            var timeStep = TimeSpan.FromMinutes(5);

            var batteries = TestData.GetBatteries();
            var strategies = TestData.GetStrategies();
            var packetSizes = TestData.GetPacketSizes();
            var packetProbabilities = TestData.GetPacketProbabilities();
            var seeds = TestData.GetSeeds();

            var timeSteps = new[]
            {
                timeStep,
                //TimeSpan.FromMinutes(3),
            };

            var shortTimestep = timeSteps.Skip(1).Take(1)
                .Cartesian(packetSizes.Where(ps => ps <= EnergyFast.FromKilowattHours(0.8)), ValueTuple.Create);
            var longTimestep = timeSteps.Take(1)
                .Cartesian(packetSizes, ValueTuple.Create);
            //var timestepCombinations = shortTimestep.Concat(longTimestep).ToList();
            var timestepCombinations = longTimestep.ToList();


            IList<DataSet> dataSets;
            using (var progress = new ConsoleProgressBar())
            {
                dataSets = await TestData.GetDataSetsAsync(timeStep, progress);
            }
            
            var precomputedLength = dataSets.Max(d => d.Data.Count) * 2;
            List<(int s, int strategySeed, Func<CachedDoublesRandomNumberGenerator>)> precomputedRandomValues;
            using (var progress = new ConsoleProgressBar())
            {
                progress.Setup(precomputedLength * seeds.Count, "precalculating random values");
                precomputedRandomValues = seeds.Select<int, (int, int, Func<CachedDoublesRandomNumberGenerator>)>(s =>
                    {
                        var precomputed_buffer = new double[precomputedLength];
                        var rng = new SeededRandomNumberGenerator(s);
                        var strategySeed = rng.Next(0, int.MaxValue);
                        for (int i = 0; i < precomputed_buffer.Length; i += 1)
                        {
                            precomputed_buffer[i] = rng.NextDouble();
                            progress.FinishOne();
                        }

                        return (s, strategySeed, () => new CachedDoublesRandomNumberGenerator(precomputed_buffer));
                    })
                    .ToList();
            }

            var allCombinations =
                dataSets
                    .Cartesian(
                        batteries,
                        strategies,
                        packetProbabilities,
                        precomputedRandomValues,
                        ValueTuple.Create)
                    .Cartesian(
                        timestepCombinations,
                        ValueTupleExtensions.Combine);
            var numberOfCombinations = GetNumberOfCombinations(
                dataSets, batteries, strategies, packetProbabilities, precomputedRandomValues, timestepCombinations);
            IProducerConsumerCollection<SimulationResult> results = new ConcurrentQueue<SimulationResult>();
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            static Task WriteEntry(TextWriter streamWriter, SimulationResult simulationResult)
            {
                return streamWriter.WriteLineAsync(string.Join('\t',
                    "RESULT",
                    $"strategy={simulationResult.StrategyName}",
                    $"configuration={simulationResult.StrategyConfiguration}",
                    $"prettyConfiguration={simulationResult.StrategyPrettyConfiguration}",
                    $"data={simulationResult.DataConfiguration}",
                    $"guardConfiguration={simulationResult.GuardConfiguration}",
                    $"battery={simulationResult.BatteryConfiguration}",
                    string.Create(CultureInfo.InvariantCulture,
                        $"batteryMin_kwh={simulationResult.BatteryMinSoC.KilowattHours:0.00#}"),
                    string.Create(CultureInfo.InvariantCulture,
                        $"batteryMax_kwh={simulationResult.BatteryMaxSoC.KilowattHours:0.00#}"),
                    string.Create(CultureInfo.InvariantCulture,
                        $"batteryAvg_kwh={simulationResult.BatteryAvgSoC.KilowattHours:0.00#}"),
                    string.Create(CultureInfo.InvariantCulture,
                        $"packetSize={simulationResult.PacketSize.KilowattHours:0.00#}"),
                    string.Create(CultureInfo.InvariantCulture,
                        $"probability={simulationResult.PacketProbability.DecimalFractions:0.###'}"),
                    $"timeStep={simulationResult.TimeStep}",
                    $"success={simulationResult.Success}",
                    $"fail={simulationResult.FailReason}",
                    $"incomingPowerGuards={simulationResult.GuardSummary?.IncomingPowerGuards ?? 0}",
                    $"outgoingPowerGuards={simulationResult.GuardSummary?.OutgoingPowerGuards ?? 0}",
                    $"emptyCapacityGuards={simulationResult.GuardSummary?.EmptyCapacityGuards ?? 0}",
                    $"fullCapacityGuards={simulationResult.GuardSummary?.FullCapacityGuards ?? 0}",
                    $"oscillationGuards={simulationResult.GuardSummary?.OscillationGuards ?? 0}",
                    $"steps={simulationResult.StepsSimulated}",
                    string.Create(CultureInfo.InvariantCulture,
                        $"energy_kwh_in={simulationResult.TotalKilowattHoursIncoming}"),
                    string.Create(CultureInfo.InvariantCulture,
                        $"energy_kwh_out={simulationResult.TotalKilowattHoursOutgoing}"),
                    string.Create(CultureInfo.InvariantCulture,
                        $"generationMissed_kwh={simulationResult.TotalKilowattHoursForfeited}"),
                    $"seed={simulationResult.Seed}"));
            }

            var writeTask = Task.Run(async () =>
            {
                var stop = false;
                var resultsFileName = "results.txt";
                File.Delete(resultsFileName);
                using (var progress = new ConsoleProgressBar())
                //await using (var stream = File.OpenWrite(resultsFileName))
                await using (var stream = new FileStream(resultsFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                await using (var writer = new StreamWriter(stream))
                {
                    var previousTimes = new (int numberOfCombinations, TimeSpan time)[]
                    {
                        //( 7_577_010, TimeSpan.FromHours(3) + TimeSpan.FromMinutes(39) ),
                        //( 4_767_840, TimeSpan.FromHours(5) + TimeSpan.FromMinutes(16) ),
                        //( 3_350_160, TimeSpan.FromHours(5) + TimeSpan.FromMinutes(3) ), //small battery only
                        ( 2_138_400, TimeSpan.FromMinutes(51) + TimeSpan.FromSeconds(22) )
                    };
                    var prevTime = previousTimes[0];
                    var factor = (double) numberOfCombinations / prevTime.numberOfCombinations;
                    var estimatedTime = prevTime.time * factor;
                    progress.Setup(
                        numberOfCombinations,
                        $"Running the simulation ({numberOfCombinations} combinations, estimated to take {estimatedTime.Humanize(precision: 2)}, ETA {DateTime.Now.Add(estimatedTime):HH:mm:ss})");
                    while (true)
                    {
                        if (token.IsCancellationRequested)
                        {
                            stop = true;
                        }
                        while (results.TryTake(out var entry))
                        {
                            if (!string.IsNullOrEmpty(entry.StrategyName))
                            {
                                await WriteEntry(writer, entry);
                            }
                            progress.FinishOne();
                        }

                        if (stop)
                        {
                            return;
                        }

                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5), token);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    }
                }
            });

            var beforeSimulation = DateTime.Now;

            MeasureProfiler.StartCollectingData();
#if DEBUG
            Sequential.ForEach(
#else
            Parallel.ForEach(
#endif
                allCombinations,
                new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 12,
                },
                combination =>
                {
                    var (
                        dataSet,
                        battery,
                        strategy,
                        packetProbability,
                        random,
                        simulationStep,
                        packetSize
                        ) = combination;
                    var res = SimulateSingle(
                        simulationStep,
                        strategy,
                        packetSize,
                        dataSet,
                        packetProbability, 
                        random,
                        battery);
                    results.TryAdd(res);
                });

            cts.Cancel();
            MeasureProfiler.StopCollectingData();
            MeasureProfiler.SaveData();
            await writeTask;

            var elapsed = DateTime.Now - beforeSimulation;
            Console.WriteLine($"simulating took {elapsed.Humanize(precision: 2)} ({elapsed}) and finished at {DateTime.Now:HH:mm:ss}");
        }

        public delegate IEpDeviceController? CreateStrategy(
            Configuration configuration,
            bool withOscillationGuard = true);

        private static PowerFast GetNewChargePower(
            PowerFast initialReducedPower,
            PowerFast chargePower,
            PowerFast generatorPower)
        {
            if (initialReducedPower > generatorPower)
            {
                if (generatorPower >= chargePower)
                {
                    // reduced power was larger than the current generation due to the full battery buffer
                    // but there is some load, match that load with the generation exactly
                    // and since generator.CurrentGeneration >= chargePower by reducing by chargePower we are still within the rules
                    return PowerFast.Zero;
                }
                else
                {
                    // charge power is larger than the generation, try our luck without the generation
                    var withoutGeneration = chargePower - generatorPower;
                    return withoutGeneration;
                }
            }
            else if (initialReducedPower > chargePower)
            {
                // no need to discharge the battery, match the load with the generation exactly
                return PowerFast.Zero;
            }
            else
            {
                // the new charge power may be slightly too large due to number inaccuracy.
                // Decrease it slightly in favor of not being able to track at the absolute maximum of the battery anyways
                const double fullBatteryBuffer = 0.999;

                // reduce the power slightly more than necessary, but stay in business
                return (chargePower - initialReducedPower) * fullBatteryBuffer;
            }
        }

        private static void DoBatteryChargingWithPvMppTracking(
            BatteryElectricStorage2 battery,
            PowerFast generation,
            TimeSpan timeStep,
            PowerFast chargePower,
            ref EnergyFast forfeited)
        {
            // consider that PV generation has to be lowered to not exceed the battery limits
            var newSoC = battery.TrySimulate(timeStep, chargePower, PowerFast.Zero);
            if (newSoC >= battery.TotalCapacity)
            {
                var difference = newSoC - battery.TotalCapacity;
                var reducedPower = (difference / timeStep) / (battery.ChargingEfficiency);
                var newChargePower = GetNewChargePower(reducedPower, chargePower, generation);
                forfeited += (chargePower - newChargePower) * timeStep;
                battery.Simulate(timeStep, newChargePower, PowerFast.Zero);
            }
            else
            {
                battery.TrySetNewSoc(newSoC);
            }
        }

        private SimulationResult SimulateSingle(
            TimeSpan timeStep,
            CreateStrategy createStrategy,
            EnergyFast packetSize,
            DataSet dataSet,
            Ratio packetProbability,
            (int originalSeed, int strategySeed, Func<CachedDoublesRandomNumberGenerator> random) rng,
            BatteryConfiguration batteryConfig)
        {
            var dataTimeStep = TimeSpan.FromMinutes(1);
            var random = rng.random();
            // have random transfer accept decisions be independent from what strategies use the random values for.
            var strategyRandom = new SeededRandomNumberGenerator(rng.strategySeed);
            TransferResult lastDecision = TransferResult.NoTransferRequested.Instance;
            var battery = batteryConfig.CreateBattery();
            var strategyConfig = new Configuration()
            {
                Battery = battery,
                PacketSize = packetSize,
                Random = strategyRandom,
                DataSet = dataSet,
            };
            var strategy = createStrategy(strategyConfig);
            if (strategy == null)
            {
                return new SimulationResult();
            }
#if DEBUG
            //if (
            //    //strategy is DirectionAwareLinearProbabilisticFunctionControl dir
            //    //&& dir.Name.Contains("Estimation")
            //    //&& dataSet.Configuration == "Res1"
            //    strategy is AimForSpecificBatteryRange { Configuration: "[0.70, 1.00]" }
            //    && dataSet.Configuration == "Res1"
            //    && packetProbability.Equals(Ratio.FromPercent(15), .01, ComparisonType.Relative)
            //    && packetSize.Equals(Energy.FromKilowattHours(1), .01, ComparisonType.Relative)
            //    )
            //{
            //    int p = 5;
            //}
            //else
            //{
            //    return new SimulationResult();
            //}
#endif
            using (strategy as IDisposable)
            {
                var load = new UncontrollableLoad()
                {
                    MomentaryDemand = PowerFast.Zero,
                };

                var generator = new ControllableGeneration()
                {
                    MomentaryGeneration = PowerFast.Zero,
                };

                int step = 0;
                var result = new SimulationResult()
                {
                    PacketProbability = packetProbability,
                    PacketSize = packetSize,
                    TimeStep = timeStep,
                    StrategyName = strategy.Name,
                    StrategyConfiguration = strategy.Configuration,
                    StrategyPrettyConfiguration = strategy.PrettyConfiguration,
                    DataConfiguration = dataSet.Configuration,
                    GuardConfiguration = strategy is GuardedStrategyWrapper wrapper ? wrapper.GuardConfiguration : string.Empty,
                    BatteryConfiguration = $"{batteryConfig.Description}",
                    Seed = rng.originalSeed,
                };
                EnergyFast totalEnergyIn = EnergyFast.Zero;
                EnergyFast totalEnergyOut = EnergyFast.Zero;
                EnergyFast totalForfeitedEnergy = EnergyFast.Zero;
                var batteryMinSoC = battery.CurrentStateOfCharge;
                var batteryMaxSoC = battery.CurrentStateOfCharge;
                var batterySoCSum = battery.CurrentStateOfCharge;

                void SetResultValues()
                {
                    result.StepsSimulated = step;
                    result.TotalKilowattHoursIncoming = totalEnergyIn.KilowattHours;
                    result.TotalKilowattHoursOutgoing = totalEnergyOut.KilowattHours;
                    result.TotalKilowattHoursForfeited = totalForfeitedEnergy.KilowattHours;
                    result.BatteryMinSoC = batteryMinSoC;
                    result.BatteryMaxSoC = batteryMaxSoC;
                    result.BatteryAvgSoC = step != 0
                        ? batterySoCSum / step
                        : battery.CurrentStateOfCharge;
                    if (strategy is GuardedStrategy guarded)
                    {
                        result.GuardSummary = guarded.GuardSummary;
                    }

                    if (strategy is GuardedStrategyWrapper wrap)
                    {
                        result.GuardSummary = wrap.GuardSummary;
                    }
                }

                int dataPoint = 0;
                var packetProbabilityDecimalFractions = packetProbability.DecimalFractions;
                foreach (var batch in dataSet.Data)
                {
                    load.MomentaryDemand = dataSet.GetLoad(batch[0]);
                    generator.MomentaryGeneration = dataSet.GetGeneration(batch[0]);

                    ControlDecision decision;
                    try
                    {
                        decision = strategy.DoControl(dataPoint, timeStep, load, generator, lastDecision);
                    }
                    catch (Exception e)
                    {
                        SetResultValues();
                        result.Success = false;
                        result.FailReason = BatteryFailReason.Exception;
                        return result;
                    }
                    EnergyFast packet = EnergyFast.Zero;
                    // always do the same random calculation, no matter the decision of the algorithm
                    // this provides the same environmental conditions, e.g. any algorithm that requests
                    // an incoming transfer in step 325 will get the same result.
                    // The chance to get an incoming transfer and an outgoing transfer in the same time step should not be equal
                    var (allowIncoming, allowOutgoing) = (
                        random.NextDouble() <= packetProbabilityDecimalFractions,
                        random.NextDouble() <= packetProbabilityDecimalFractions);
                    if (decision is ControlDecision.RequestTransfer rt)
                    {
                        if (rt.RequestedDirection == PacketTransferDirection.Outgoing)
                        {
                            if (allowOutgoing)
                            {
                                packet = packetSize;
                                totalEnergyOut += packetSize;
                                lastDecision = TransferResult.Success.Outgoing;
                            }
                            else
                            {
                                lastDecision = TransferResult.Failure.Outgoing;
                            }
                        }
                        else if (rt.RequestedDirection == PacketTransferDirection.Incoming)
                        {
                            if (allowIncoming)
                            {
                                packet = -packetSize;
                                totalEnergyIn += packetSize;
                                lastDecision = TransferResult.Success.Incoming;
                            }
                            else
                            {
                                lastDecision = TransferResult.Failure.Incoming;
                            }
                        }
                    }
                    else
                    {
                        packet = EnergyFast.Zero;
                        lastDecision = TransferResult.NoTransferRequested.Instance;
                    }

                    var packetPower = packet / timeStep;
                    //var averageBatchLoad = batch.Average(dataSet.GetLoad);
                    //var averageBatchGeneration = batch.Average(dataSet.GetGeneration);
                    foreach (var entry in batch)
                    {
                        //var dischargePower = loads[0].CurrentDemand
                        //                     - generators[0].CurrentGeneration
                        //                     + packetPower;
                        //var generation = dataSet.GetGeneration(entry);
                        //var dischargePower = dataSet.GetLoad(entry)
                        //                     - generation
                        //                     + packetPower;
                        var generation = dataSet.GetGeneration(entry);
                        var dischargePower = dataSet.GetLoad(entry)
                                             - generation
                                             + packetPower;
                        if (dischargePower > PowerFast.Zero)
                        {
                            if (dischargePower > battery.MaximumDischargePower)
                            {
                                SetResultValues();
                                result.Success = false;
                                result.FailReason = BatteryFailReason.ExceedDischargePower;
                                return result;
                            }
                            battery.Simulate(dataTimeStep, PowerFast.Zero, dischargePower);
                        }
                        else
                        {
                            var chargePower = -dischargePower;
                            if (chargePower > battery.MaximumChargePower)
                            {
                                if (chargePower - generation > battery.MaximumChargePower)
                                {
                                    SetResultValues();
                                    result.Success = false;
                                    result.FailReason = BatteryFailReason.ExceedChargePower;
                                    return result;
                                }
                                else
                                {
                                    var reducedChargePower = battery.MaximumChargePower;
                                    var missed = chargePower - reducedChargePower;
                                    totalForfeitedEnergy += missed * dataTimeStep;
                                    chargePower = reducedChargePower;
                                    generation -= missed;
                                }
                            }

                            if (generation == PowerFast.Zero)
                            {
                                battery.Simulate(dataTimeStep, chargePower, PowerFast.Zero);
                            }
                            else
                            {
                                DoBatteryChargingWithPvMppTracking(battery, generation, dataTimeStep, chargePower, ref totalForfeitedEnergy);
                            }
                        }
                    }
                    step += 1;

                    batteryMinSoC = Units.Min(batteryMinSoC, battery.CurrentStateOfCharge);
                    batteryMaxSoC = Units.Max(batteryMaxSoC, battery.CurrentStateOfCharge);
                    batterySoCSum += battery.CurrentStateOfCharge;

                    var belowZero = battery.CurrentStateOfCharge <= EnergyFast.Zero;
                    var exceedCapacity = battery.CurrentStateOfCharge >= battery.TotalCapacity;
                    if (belowZero || exceedCapacity)
                    {
                        SetResultValues();
                        result.Success = false;
                        if (belowZero)
                        {
                            result.FailReason = BatteryFailReason.BelowZero;
                        }
                        else if (exceedCapacity)
                        {
                            result.FailReason = BatteryFailReason.ExceedCapacity;
                        }
                        return result;
                    }

                    dataPoint += 1;
                }

                SetResultValues();
                result.Success = true;
                return result;
            }
        }

        private static int GetNumberOfCombinations(params IEnumerable[] collections)
        {
            return collections.Aggregate(1, (num, collection) => num * (collection is ICollection c ? c.Count : collection.Count()));
        }
    }
}