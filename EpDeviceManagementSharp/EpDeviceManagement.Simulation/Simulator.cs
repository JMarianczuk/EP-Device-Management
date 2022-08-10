using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control;
using EpDeviceManagement.Data;
using EpDeviceManagement.Simulation.Loads;
using EpDeviceManagement.Simulation.Storage;
using MoreLinq;
using Newtonsoft.Json;
using UnitsNet;
using UnitsNet.Serialization.JsonNet;
using UnitsNet.Serialization.SystemTextJson;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
            
            //var (data, handle) = new ReadDataFromCsv().ReadAsync();
            //var enhancedData = await EnhanceAsync(data, timeStep);
            //handle.Dispose();

            //stopwatch.Restart();
            //var jsonSettings = new JsonSerializerSettings
            //{
            //    Formatting = Formatting.None,
            //    Converters =
            //    {
            //        new UnitsNetIQuantityJsonConverter(),
            //    }
            //};
            //List<EnhancedEnergyDataSet> enhancedData2;
            //using (var stream = File.OpenText("enhanced.json"))
            //using (var reader = new JsonTextReader(stream))
            //{
            //    var serializer = Newtonsoft.Json.JsonSerializer.Create(jsonSettings);
            //    enhancedData2 =
            //        serializer.Deserialize<List<EnhancedEnergyDataSet>>(reader);
            //}

            //stopwatch.Stop();
            //Console.WriteLine(stopwatch.Elapsed);

            List<EnhancedEnergyDataSet> enhancedData;
            using (var stream = File.OpenRead("enhanced.json"))
            {
                var options = new JsonSerializerOptions()
                {
                    Converters =
                    {
                        new UnitsNetIQuantityJsonConverterFactory(),
                    }
                };
                enhancedData = await JsonSerializer.DeserializeAsync<List<EnhancedEnergyDataSet>>(stream);
            }

            var strategies = new List<Func<Configuration, IEpDeviceController>>()
            {
                config => new AlwaysRequestIncomingPackets(config.Battery, config.PacketSize),
                config => new AlwaysRequestOutgoingPackets(config.Battery, config.PacketSize),
                config => new NoExchangeWithTheCell(),
            };
            var upperLimits = MoreEnumerable
                .Generate(0.5d, x => x + 0.1d)
                .TakeWhile(x => x <= 1)
                .ToList();
            var lowerLimits = MoreEnumerable
                .Generate(0.5d, x => x - 0.1d)
                .TakeWhile(x => x >= 0)
                .ToList();
            foreach (var (upper, lower) in upperLimits.Cartesian(lowerLimits, Tuple.Create))
            {
                strategies.Add(config => new ProbabilisticModelingControl(
                    config.Battery,
                    config.Battery.TotalCapacity * upper,
                    config.Battery.TotalCapacity * lower,
                    config.Random));
            }
            foreach (var upper in upperLimits)
            {
                strategies.Add(config => new AimForSpecificBatteryLevel(
                    config.Battery,
                    config.Battery.TotalCapacity * upper));
            }

            var packetSizes = MoreEnumerable.Generate(0d, x => x + 0.5)
                .Skip(1)
                .Take(10)
                .Select(x => Energy.FromKilowattHours(x))
                .ToList();

            var packetProbabilities = MoreEnumerable.Sequence(5, 95, 5)
                .Select(x => Ratio.FromPercent(x))
                .ToList();

            var allCombinations = packetSizes.Cartesian(strategies, packetProbabilities, Tuple.Create);
            IProducerConsumerCollection<SimulationResult> results = new ConcurrentBag<SimulationResult>();

            using (var progress = new ConsoleProgressBar())
            {
                progress.Setup(packetSizes.Count * strategies.Count * packetProbabilities.Count, "Running the simulation");
                Parallel.ForEach(
                    allCombinations,
                    new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 16,
                    },
                    combination =>
                    {
                        var (packetSize, strategy, packetProbability) = combination;
                        var res = SimulateSingle(
                            timeStep,
                            strategy,
                            packetSize,
                            enhancedData,
                            packetProbability, 
                            13254);
                        progress.FinishOne();
                        results.TryAdd(res);
                    });
            }

            using (var stream = File.OpenWrite("results.txt"))
            using (var writer = new StreamWriter(stream))
            {
                foreach (var entry in results.OrderBy(x => x.PacketProbability.DecimalFractions).ThenBy(x => x.PacketSize.KilowattHours))
                {
                    await writer.WriteLineAsync(string.Join('\t', "RESULT",
                        $"strategy={entry.Strategy}",
                        $"success={entry.Success}",
                        $"steps={entry.StepsSimulated}",
                        $"battery={entry.BatteryStateOfCharge}",
                        $"packetSize={entry.PacketSize}",
                        $"probability={entry.PacketProbability}"));
                    //if (entry.Success)
                    //{
                    //    Console.WriteLine($"S - Strategy '{entry.Strategy}' managed to control the complete sequence. Battery: {entry.BatteryStateOfCharge}, Packet size: {entry.PacketSize}, Packet probabilty: {entry.PacketProbability}");
                    //}
                    //else
                    //{
                    //    //Console.WriteLine($"F - Strategy '{entry.Strategy.GetType().Name}' went out of bounds after {entry.StepsSimulated} simulation steps. Battery: {entry.BatteryStateOfCharge}, Packet size: {entry.PacketSize}, Packet probabilty: {entry.PacketProbability}");
                    //}
                }
            }
        }

        private SimulationResult SimulateSingle(
            TimeSpan timeStep,
            Func<Configuration, IEpDeviceController> createStrategy,
            Energy packetSize,
            IEnumerable<EnhancedEnergyDataSet> enhancedData,
            Ratio packetProbability,
            int seed)
        {
            var battery = new BatteryElectricStorage(
                Frequency.FromCyclesPerHour(0.02),
                Ratio.FromPercent(90),
                Ratio.FromPercent(110))
            {
                CurrentStateOfCharge = Energy.FromKilowattHours(5),
                TotalCapacity = Energy.FromKilowattHours(10),
                MaximumChargePower = Power.FromKilowatts(100),
                MaximumDischargePower = Power.FromKilowatts(100),
            };
            var random = new SeededRandomNumberGenerator(seed);
            TransferResult lastDecision = new TransferResult.NoTransferRequested();
            var strategy = createStrategy(
                new Configuration()
                {
                    Battery = battery,
                    PacketSize = packetSize,
                    Random = random,
                });

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
                Strategy = strategy,
            };
            foreach (var entry in enhancedData)
            {
                res1[0].CurrentDemand = entry.Residential1_Dishwasher;
                res1[1].CurrentDemand = entry.Residential1_Freezer;
                res1[2].CurrentDemand = entry.Residential1_HeatPump;
                res1[3].CurrentDemand = entry.Residential1_WashingMachine;
                res1gen[0].CurrentGeneration = entry.Residential1_PV;

                var decision = strategy.DoControl(timeStep, res1, lastDecision);
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
                        : new TransferResult.Failure();
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

                if (battery.CurrentStateOfCharge < Energy.FromKilowattHours(0.1)
                    || battery.CurrentStateOfCharge > Energy.FromKilowattHours(9.9))
                {
                    result.Success = false;
                    result.StepsSimulated = step;
                    result.BatteryStateOfCharge = battery.CurrentStateOfCharge;
                    return result;
                }

            }

            result.Success = true;
            result.StepsSimulated = step;
            result.BatteryStateOfCharge = battery.CurrentStateOfCharge;
            return result;
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
            }

            return result;
        }
    }

    public readonly struct Configuration
    {
        public IStorage Battery { get; init; }

        public Energy PacketSize { get; init; }

        public RandomNumberGenerator Random { get; init; }
    }

    public class SimulationResult
    {
        public Energy BatteryStateOfCharge { get; set; }

        public int StepsSimulated { get; set; }

        public Energy PacketSize { get; set; }

        public Ratio PacketProbability { get; set; }

        public bool Success { get; set; }

        public IEpDeviceController Strategy { get; set; }
    }
}