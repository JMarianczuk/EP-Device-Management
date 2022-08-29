using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control;
using EpDeviceManagement.Simulation.Loads;
using UnitsNet;
using Humanizer;

using static MoreLinq.Extensions.CartesianExtension;

namespace EpDeviceManagement.Simulation
{
    public class Simulator
    {
        public async Task SimulateAsync()
        {
            var timeStep = TimeSpan.FromMinutes(15);

            var batteries = TestData.GetBatteries();
            var strategies = TestData.GetStrategies();
            var packetSizes = TestData.GetPacketSizes();
            var packetProbabilities = TestData.GetPacketProbabilities();

            var timeSteps = new[]
            {
                timeStep,
                TimeSpan.FromMinutes(3),
            };

            var shortTimestep = timeSteps.Skip(1).Take(1).Cartesian(packetSizes.Take(8), ValueTuple.Create);
            var longTimestep = timeSteps.Take(1).Cartesian(packetSizes.Skip(4), ValueTuple.Create);
            var timestepCombinations = shortTimestep.Concat(longTimestep).ToList();

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
                batteries
                    .Cartesian(
                        strategies,
                        packetProbabilities,
                        seeds,
                        dataSets,
                        ValueTuple.Create)
                    .Cartesian(
                        timestepCombinations,
                        ValueTupleExtensions.Combine);
            var numberOfCombinations = GetNumberOfCombinations(
                batteries, strategies, packetProbabilities, seeds, dataSets, timestepCombinations);
            IProducerConsumerCollection<SimulationResult> results = new ConcurrentQueue<SimulationResult>();
            var cts = new CancellationTokenSource();
            var token = cts.Token;
#if DEBUG
            var writeTask = Task.CompletedTask;
#else
            var writeTask = Task.Run(async () =>
            {
                var stop = false;
                var resultsFileName = "results.txt";
                File.Delete(resultsFileName);
                using (var progress = new ConsoleProgressBar())
                await using (var stream = File.OpenWrite(resultsFileName))
                await using (var writer = new StreamWriter(stream))
                {
                    progress.Setup(
                        numberOfCombinations,
                        "Running the simulation");
                    while (true)
                    {
                        if (token.IsCancellationRequested)
                        {
                            stop = true;
                        }
                        while (results.TryTake(out var entry))
                        {
                            await writer.WriteLineAsync(string.Join('\t',
                                "RESULT",
                                $"strategy={entry.StrategyName}",
                                $"configuration={entry.StrategyConfiguration}",
                                $"prettyConfiguration={entry.StrategyPrettyConfiguration}",
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
                                $"packets={entry.TotalPacketsTransferred}",
                                $"seed={entry.Seed}"));
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
                        catch (TaskCanceledException e)
                        {
                        }
                    }
                }
            });
#endif

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
                        battery,
                        strategy,
                        packetProbability,
                        seed,
                        dataSet,
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
            Console.WriteLine($"simulating took {elapsed.Humanize()} ({elapsed})");
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
//                && pmc.Configuration == "[6.44 kWh, 12.88 kWh]"
//                && dataSet.Configuration == "Res1 noSolar"
//                && packetSize.Equals(Energy.FromKilowattHours(0.2), 0.01, ComparisonType.Relative)
//                && packetProbability.Equals(Ratio.FromPercent(60), 0.01, ComparisonType.Relative)
//                && batteryConfig.Description == "Fenecon Home"
//                && timeStep == TimeSpan.FromMinutes(3)
//                )
//            {
//                int p = 5;
//            }
//            else
//            {
//                return new SimulationResult();
//            }
//#endif

            UncontrollableLoad[] loads = new UncontrollableLoad[]
            {
                new UncontrollableLoad()
                {
                    CurrentDemand = Power.Zero,
                }
            };

            UncontrollableGeneration[] generators = new UncontrollableGeneration[]
            {
                new UncontrollableGeneration()
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
                BatteryConfiguration = $"{batteryConfig.Description} [{battery.TotalCapacity}]",
                Seed = seed,
            };
            int totalPackets = 0;
            var batteryMinSoC = battery.CurrentStateOfCharge;
            var batteryMaxSoC = battery.CurrentStateOfCharge;
            var batterySoCSum = battery.CurrentStateOfCharge;
            var lastTimestamp = dataSet.Data.First().Timestamp - timeStep;

            void SetResultValues()
            {
                result.StepsSimulated = step;
                result.TotalPacketsTransferred = totalPackets;
                result.BatteryMinSoC = batteryMinSoC;
                result.BatteryMaxSoC = batteryMaxSoC;
                result.BatteryAvgSoC = batterySoCSum / step;
            }
            foreach (var entry in dataSet.Data)
            {
                var timePassed = entry.Timestamp - lastTimestamp;
                lastTimestamp = entry.Timestamp;
                var numberOfSimSteps = timePassed / timeStep;
                loads[0].CurrentDemand = dataSet.GetLoadsTotalPower(entry);
                generators[0].CurrentGeneration = dataSet.GetGeneratorsTotalPower(entry);

//#if DEBUG
//                if (step >= 368856)
//                {
//                    int p = 6;
//                }
//#endif

                for (int i = 0; i < (int)Math.Round(numberOfSimSteps); i += 1)
                {
                    var decision = strategy.DoControl(timeStep, loads, generators, lastDecision);
                    Energy packet;
                    if (decision is ControlDecision.RequestTransfer rt)
                    {
                        var success = random.NextDouble() <= packetProbability.DecimalFractions;
                        if (success)
                        {
                            packet = rt.RequestedDirection == PacketTransferDirection.Outgoing
                                ? packetSize
                                : -packetSize;
                            lastDecision = new TransferResult.Success()
                            {
                                PerformedDirection = rt.RequestedDirection,
                            };
                            totalPackets += 1;
                        }
                        else
                        {
                            packet = Energy.Zero;
                            lastDecision = new TransferResult.Failure()
                            {
                                RequestedDirection = rt.RequestedDirection,
                            };
                        }
                    }
                    else
                    {
                        packet = Energy.Zero;
                        lastDecision = new TransferResult.NoTransferRequested();
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
                            SetResultValues();
                            result.Success = false;
                            result.FailReason = BatteryFailReason.ExceedChargePower;
                            return result;
                        }
                        battery.Simulate(timeStep, chargePower, Power.Zero);
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
                }
            }

            SetResultValues();
            result.Success = true;
            return result;
        }

        private static int GetNumberOfCombinations(params IEnumerable[] collections)
        {
            return collections.Aggregate(1, (num, collection) => num * (collection is ICollection c ? c.Count : collection.Count()));
        }
    }
}