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

using static MoreLinq.Extensions.CartesianExtension;

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

            var timeSteps = new[]
            {
                timeStep,
                //TimeSpan.FromMinutes(3),
            };

            var shortTimestep = timeSteps.Skip(1).Take(1)
                .Cartesian(packetSizes.Where(ps => ps <= Energy.FromKilowattHours(0.8)), ValueTuple.Create);
            var longTimestep = timeSteps.Take(1)
                .Cartesian(packetSizes, ValueTuple.Create);
            //var timestepCombinations = shortTimestep.Concat(longTimestep).ToList();
            var timestepCombinations = longTimestep.ToList();

            var seeds = new[]
            {
                //13254,
                148354,
            };

            IList<DataSet> dataSets;
            using (var progress = new ConsoleProgressBar())
            {
                dataSets = await TestData.GetDataSetsAsync(timeStep, progress);
            }

            var allCombinations =
                dataSets
                    .Cartesian(
                        batteries,
                        strategies,
                        packetProbabilities,
                        seeds,
                        ValueTuple.Create)
                    .Cartesian(
                        timestepCombinations,
                        ValueTupleExtensions.Combine);
            var numberOfCombinations = GetNumberOfCombinations(
                dataSets, batteries, strategies, packetProbabilities, seeds, timestepCombinations);
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
                    $"battery={simulationResult.BatteryConfiguration}",
                    $"batteryMin={simulationResult.BatteryMinSoC}",
                    $"batteryMax={simulationResult.BatteryMaxSoC}",
                    $"batteryAvg={simulationResult.BatteryAvgSoC}",
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
                        $"generationMissed_kwh={simulationResult.TotalKilowattHoursGenerationMissed}"),
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
                        ( 4_767_840, TimeSpan.FromHours(5) + TimeSpan.FromMinutes(16) ),
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
#if DEBUG
                            if (!string.IsNullOrEmpty(entry.StrategyName))
                            {
                                await WriteEntry(writer, entry);
                            }
#else
                            await WriteEntry(writer, entry);
#endif
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
                        dataSet,
                        battery,
                        strategy,
                        packetProbability,
                        seed,
                        simulationStep,
                        packetSize
                        ) = combination;
                    var res = SimulateSingle(
                        simulationStep,
                        strategy,
                        packetSize,
                        dataSet,
                        packetProbability, 
                        seed,
                        battery);
                    results.TryAdd(res);
                });

            cts.Cancel();
            await writeTask;

            var elapsed = DateTime.Now - beforeSimulation;
            Console.WriteLine($"simulating took {elapsed.Humanize(precision: 2)} ({elapsed}) and finished at {DateTime.Now:HH:mm:ss}");
        }

        public delegate IEpDeviceController CreateStrategy(
            Configuration configuration,
            bool withOscillationGuard = true);

        private static Power GetReducedPower(
            Power initialReducedPower,
            Power chargePower,
            Power generatorPower)
        {
            if (initialReducedPower > generatorPower)
            {
                if (generatorPower >= chargePower)
                {
                    // reduced power was larger than the current generation due to the full battery buffer
                    // but there is some load, match that load with the generation exactly
                    // and since generator.CurrentGeneration >= chargePower by reducing by chargePower we are still within the rules
                    return Power.Zero;
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
                return Power.Zero;
            }
            else
            {
                // reduced power may be slightly too little due to number inaccuracy.
                // Increase it slightly in favor of not being able to track at the absolute maximum of the battery anyways
                const decimal fullBatteryBuffer = 0.999m;

                // reduce the power slightly more than necessary, but stay in business
                return (chargePower - initialReducedPower) * fullBatteryBuffer;
            }
        }

        private static void DoBatteryChargingWithPvMppTracking(
            BatteryElectricStorage2 battery,
            ControllableGeneration generator,
            TimeSpan timeStep,
            Power chargePower,
            ref double generatedKwhMissed)
        {
            // consider that PV generation has to be lowered to not exceed the battery limits
            var newSoC = battery.TrySimulate(timeStep, chargePower, Power.Zero);
            if (newSoC >= battery.TotalCapacity)
            {
                var difference = newSoC - battery.TotalCapacity;
                var reducedPower = (difference / timeStep) / (battery.ChargingEfficiency);
                var newChargePower = GetReducedPower(reducedPower, chargePower, generator.CurrentGeneration);
                generatedKwhMissed += ((chargePower - newChargePower) * timeStep).KilowattHours;
                battery.Simulate(timeStep, newChargePower, Power.Zero);
            }
            else
            {
                battery.Simulate(timeStep, chargePower, Power.Zero);
            }
        }

        private SimulationResult SimulateSingle(
            TimeSpan timeStep,
            CreateStrategy createStrategy,
            Energy packetSize,
            DataSet dataSet,
            Ratio packetProbability,
            int seed,
            BatteryConfiguration batteryConfig)
        {
            var random = new SeededRandomNumberGenerator(seed);
            TransferResult lastDecision = TransferResult.NoTransferRequested.Instance;
            var battery = batteryConfig.CreateBattery();
            var strategyConfig = new Configuration()
            {
                Battery = battery,
                PacketSize = packetSize,
                Random = random,
                DataSet = dataSet,
            };
            var strategy = createStrategy(strategyConfig);
#if DEBUG
            if (
                //strategy is DirectionAwareLinearProbabilisticFunctionControl dir
                //&& dir.Name.Contains("Estimation")
                //&& dataSet.Configuration == "Res1"
                strategy is AimForSpecificBatteryRange { Configuration: "[0.70, 1.00]" }
                && dataSet.Configuration == "Res1"
                && packetProbability.Equals(Ratio.FromPercent(15), .01, ComparisonType.Relative)
                && packetSize.Equals(Energy.FromKilowattHours(1), .01, ComparisonType.Relative)
                )
            {
                int p = 5;
            }
            else
            {
                return new SimulationResult();
            }
#endif
            using (strategy as IDisposable)
            {
                var loads = new[]
                {
                    new UncontrollableLoad()
                    {
                        CurrentDemand = Power.Zero,
                    }
                };

                var generators = new[]
                {
                    new ControllableGeneration()
                    {
                        CurrentGeneration = Power.Zero,
                    },
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
                    BatteryConfiguration = $"{batteryConfig.Description}",
                    Seed = seed,
                };
                double totalKwhIn = 0;
                double totalKwhOut = 0;
                double generatedKwhMissed = 0;
                var batteryMinSoC = battery.CurrentStateOfCharge;
                var batteryMaxSoC = battery.CurrentStateOfCharge;
                var batterySoCSum = battery.CurrentStateOfCharge;

                void SetResultValues()
                {
                    result.StepsSimulated = step;
                    result.TotalKilowattHoursIncoming = totalKwhIn;
                    result.TotalKilowattHoursOutgoing = totalKwhOut;
                    result.TotalKilowattHoursGenerationMissed = generatedKwhMissed;
                    result.BatteryMinSoC = batteryMinSoC;
                    result.BatteryMaxSoC = batteryMaxSoC;
                    result.BatteryAvgSoC = step != 0
                        ? batterySoCSum / step
                        : battery.CurrentStateOfCharge;
                    if (strategy is GuardedStrategy guarded)
                    {
                        result.GuardSummary = guarded.GuardSummary;
                    }
                }

                int dataPoint = 0;
                foreach (var entry in dataSet.Data)
                {
                    loads[0].CurrentDemand = dataSet.GetLoadsTotalPower(entry);
                    loads[0].MomentaneousDemand = dataSet.GetMomentaneousLoadsPower(entry);
                    generators[0].CurrentGeneration = dataSet.GetGeneratorsTotalPower(entry);
                    generators[0].MomentaneousGeneration = dataSet.GetMomentaneousGeneratorsPower(entry);
                    generators[0].IsGenerating = true;

                    ControlDecision decision;
                    try
                    {
                        decision = strategy.DoControl(dataPoint, timeStep, loads, generators, lastDecision);
                    }
                    catch (Exception e)
                    {
                        SetResultValues();
                        result.Success = false;
                        result.FailReason = BatteryFailReason.Exception;
                        return result;
                    }
                    Energy packet;
                    if (decision is ControlDecision.RequestTransfer rt)
                    {
                        var success = random.NextDouble() <= packetProbability.DecimalFractions;
                        if (success)
                        {
                            if (rt.RequestedDirection == PacketTransferDirection.Outgoing)
                            {
                                packet = packetSize;
                                totalKwhOut += packetSize.KilowattHours;
                                lastDecision = TransferResult.Success.Outgoing;
                            }
                            else
                            {
                                packet = -packetSize;
                                totalKwhIn += packetSize.KilowattHours;
                                lastDecision = TransferResult.Success.Incoming;
                            }
                        }
                        else
                        {
                            packet = Energy.Zero;
                            lastDecision = TransferResult.Failure.For(rt.RequestedDirection);
                        }
                    }
                    else
                    {
                        packet = Energy.Zero;
                        lastDecision = TransferResult.NoTransferRequested.Instance;
                    }

                    var dischargePower = loads[0].CurrentDemand
                                         - generators[0].CurrentGeneration
                                         + packet / timeStep;
                    if (dischargePower > Power.Zero)
                    {
                        if (dischargePower > battery.MaximumDischargePower)
                        {
                            SetResultValues();
                            result.Success = false;
                            result.FailReason = BatteryFailReason.ExceedDischargePower;
                            return result;
                        }
                        battery.Simulate(timeStep, Power.Zero, dischargePower);
                    }
                    else
                    {
                        var chargePower = -dischargePower;
                        if (chargePower > battery.MaximumChargePower)
                        {
                            if (chargePower - generators[0].CurrentGeneration > battery.MaximumChargePower)
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
                                generatedKwhMissed += (missed * timeStep).KilowattHours;
                                chargePower = reducedChargePower;
                                generators[0].CurrentGeneration -= missed;
                            }
                        }

                        if (generators[0].CurrentGeneration == Power.Zero)
                        {
                            battery.Simulate(timeStep, chargePower, Power.Zero);
                        }
                        else
                        {
                            DoBatteryChargingWithPvMppTracking(battery, generators[0], timeStep, chargePower, ref generatedKwhMissed);
                        }
                    }
                    step += 1;

                    batteryMinSoC = Units.Min(batteryMinSoC, battery.CurrentStateOfCharge);
                    batteryMaxSoC = Units.Max(batteryMaxSoC, battery.CurrentStateOfCharge);
                    batterySoCSum += battery.CurrentStateOfCharge;

                    var belowZero = battery.CurrentStateOfCharge <= Energy.Zero;
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