using System.Globalization;
using System.Text;
using EpDeviceManagement.Simulation;
using EpDeviceManagement.Simulation.Storage;
using EpDeviceManagement.UnitsExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.Data.Sqlite;
using UnitsNet;
using static MoreLinq.Extensions.BatchExtension;
using static EpDeviceManagement.Simulation.Extensions.DataSetExtensions;

namespace DataAnalyzer;

public class Analyzer
{
    public static async Task AnalyzeDifferenceAsync()
    {
        //var enhanced = TestData.GetDataSets
    }

    private class ExtremeValueDef
    {
        public double MinValue { get; set; }

        public DateTimeOffset MinTime { get; set; }

        public double MaxValue { get; set; }

        public DateTimeOffset MaxTime { get; set; }

        public override string ToString()
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"{MinValue} at {MinTime}, {MaxValue} at {MaxTime}");
        }
    }

    public static async Task AnalyzePowerDifferenceAsync(TimeSpan? controlStep)
    {
        var timeStep = controlStep ?? TimeSpan.FromMinutes(5);
        IList<DataSet> data;
        using (var progress = new ConsoleProgressBar())
        {
            data = await TestData.GetDataSetsAsync(timeStep, progress, extended: true);
        }

        foreach (var ds in data)
        {
            var diffMomentaryAvgLoad = new ExtremeValueDef();
            var diffMomentaryAvgGen = new ExtremeValueDef();
            var diffMomentaryAvgEff = new ExtremeValueDef();

            var diffMomentaryStepLoad = new ExtremeValueDef();
            var diffMomentaryStepGen = new ExtremeValueDef();
            var diffMomentaryStepEff = new ExtremeValueDef();

            int signChangeMomentaryAvgEffToPositive = 0;
            int signChangeMomentaryAvgEffToNegative = 0;

            double Load(EnhancedPowerDataSet entry) => ds.GetLoad(entry).Kilowatts;
            double Gen(EnhancedPowerDataSet entry) => ds.GetGeneration(entry).Kilowatts;

            void MinMax(double value, DateTimeOffset timeStep, ExtremeValueDef def)
            {
                if (value < def.MinValue)
                {
                    def.MinValue = value;
                    def.MinTime = timeStep;
                }

                if (value > def.MaxValue)
                {
                    def.MaxValue = value;
                    def.MaxTime = timeStep;
                }
            }

            foreach (var entry in ds.Data)
            {
                var t = entry[0].Timestamp;
                var momentaryLoad = Load(entry[0]);
                var momentaryGen = Gen(entry[0]);
                var momentaryEff = momentaryLoad - momentaryGen;

                var avgLoad = entry.Select(Load).Average();
                var avgGen = entry.Select(Gen).Average();
                var avgEff = entry.Select(x => Load(x) - Gen(x)).Average();

                MinMax(momentaryLoad - avgLoad, t, diffMomentaryAvgLoad);
                MinMax(momentaryGen - avgGen, t, diffMomentaryAvgGen);
                MinMax(momentaryEff - avgEff, t, diffMomentaryAvgEff);
                if (momentaryEff < 0 && avgEff > 0)
                {
                    signChangeMomentaryAvgEffToPositive += 1;
                }

                if (momentaryEff > 0 && avgEff < 0)
                {
                    signChangeMomentaryAvgEffToNegative += 1;
                }

                for (var i = 1; i < entry.Count; i += 1)
                {
                    var stepLoad = Load(entry[i]);
                    var stepGen = Gen(entry[i]);
                    var stepEff = stepLoad - stepGen;
                    MinMax(momentaryLoad - stepLoad, t, diffMomentaryStepLoad);
                    MinMax(momentaryGen - stepGen, t, diffMomentaryStepGen);
                    MinMax(momentaryEff - stepEff, t, diffMomentaryStepEff);
                }
            }

            string ValueAndPercent(int signChanges) => $"{signChanges} ({100d * signChanges / ds.Data.Count:F2}%)";
            Console.WriteLine(string.Join(Environment.NewLine,
                $"Data Set {ds.Configuration}",
                $"Difference between momentary and average",
                $"Load: {diffMomentaryAvgLoad}",
                $"Generation: {diffMomentaryAvgGen}",
                $"Effective: {diffMomentaryAvgEff}",
                $"Difference between momentary and step",
                $"Load: {diffMomentaryStepLoad}",
                $"Generation: {diffMomentaryStepGen}",
                $"Effective: {diffMomentaryStepEff}",
                $"Sign changed between momentary and average",
                $"ToPositive: {ValueAndPercent(signChangeMomentaryAvgEffToPositive)}, ToNegative: {ValueAndPercent(signChangeMomentaryAvgEffToNegative)}, Total: {ValueAndPercent(signChangeMomentaryAvgEffToPositive + signChangeMomentaryAvgEffToNegative)}",
                ""));
            //Console.WriteLine(string.Join(Environment.NewLine,
            //        $"Data set {ds.Configuration}",
            //        $"differs on avg by {maxLoadDiffKwAvg} kW for loads at {loadAvgTime} and {maxGenDiffKwAvg} kW for generation at {genAvgTime}",
            //        $"and for the step by {maxLoadDiffKwStep} kW for loads at {loadStepTime} and {maxGenDiffKwStep} kW for generation at {genStepTime}",
            //        $"and has the largest power with {maxEffectivePower} kW for loads at {maxEffectiveTime} and {-minEffectivePower} kW for generation at {minEffectiveTime}",
            //        $"the effective power changed sign {effectivePowerSignChangeCount} out of {ds.Data.Count} control steps ({100d * effectivePowerSignChangeCount / ds.Data.Count}%)"));
            //Console.WriteLine("");
        }
    }

    public static async Task WritePowerValuesToDatabase()
    {
        foreach (var ts in new[]
                 {
                     1,
                     5,
                     15,
                     60,
                     240,
                     360,
                     1440,
                 })
        {
            Console.WriteLine($"Writing for time step of {ts} minutes");
            var timeStep = TimeSpan.FromMinutes(ts);
            IList<DataSet> data;
            using (var progress = new ConsoleProgressBar())
            {
                data = await TestData.GetDataSetsAsync(timeStep, progress, extended: true);
            }

            var resultDir = GetResultDir();
            using (var progress = new ConsoleProgressBar())
            await using (var conn = new SqliteConnection($"Data Source={Path.Combine(resultDir.FullName, "data.sqlite")}"))
            {
                var dataSets = data.Count;
                var dataSetLength = data[0].Data.Count;
                const int batchSize = 500;
                progress.Setup((dataSets * dataSetLength) / batchSize, "Writing to the database");
                await conn.OpenAsync();

                foreach (var ds in data)
                {
                    var dbFriendlyName = ds.Configuration.Replace(" ", "_");
                    var tableName = $"data_{dbFriendlyName}_{ts}min";
                    var tableCommand = new SqliteCommand($@"
DROP TABLE IF EXISTS {tableName};

CREATE TABLE
    {tableName}
    (
        cet_cest_timestamp TEXT,
        load_kw REAL,
        generation_kw REAL,
        momentaryLoad_kw REAL,
        momentaryGeneration_kw REAL
    )
;                   ",
                        conn);
                    await tableCommand.ExecuteNonQueryAsync();
                    var sb = new StringBuilder();
                    foreach (var entries in ds.Data.Batch(batchSize))
                    {
                        sb.Clear();
                        sb.Append($"INSERT INTO {tableName} VALUES ");
                        bool first = true;
                        foreach (var entry in entries)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                sb.Append(",");
                            }
                            sb.Append("(");
                            sb.Append("'");
                            sb.Append(DateTimeOffsetTypeConverter.ConvertToString(entry[0].Timestamp));
                            sb.Append("'");
                            sb.Append(",");
                            sb.Append(Convert(entry.Average(ds.GetLoad)));
                            sb.Append(",");
                            sb.Append(Convert(entry.Average(ds.GetGeneration)));
                            sb.Append(",");
                            sb.Append(Convert(ds.GetLoad(entry[0])));
                            sb.Append(",");
                            sb.Append(Convert(ds.GetGeneration(entry[0])));
                            sb.Append(")");
                        }

                        var insertCommand = new SqliteCommand(sb.ToString(), conn);
                        await insertCommand.ExecuteNonQueryAsync();
                        progress.FinishOne();
                    }
                }
            }
        }
    }

    private static string Convert(PowerFast p)
    {
        return p.Kilowatts.ToString(CultureInfo.InvariantCulture);
    }

    private static BatteryConfiguration NoBatteryConfiguration { get; } = new BatteryConfiguration()
    {
        CreateBattery = () =>
        {
            var battery = new BatteryElectricStorage2(
                PowerFast.Zero,
                Ratio.FromPercent(100),
                Ratio.FromPercent(100))
            {
                CurrentStateOfCharge = EnergyFast.Zero,
                TotalCapacity = EnergyFast.Zero,
                MaximumChargePower = PowerFast.Zero,
                MaximumDischargePower = PowerFast.Zero,
            };
            return battery;
        },
        Description = "No Battery",
    };

    public static async Task CalculateStatsAsync()
    {
        var timeStep = TimeSpan.FromMinutes(5);
        IList<DataSet> data;
        using (var progress = new ConsoleProgressBar())
        {
            data = await TestData.GetDataSetsAsync(timeStep, progress, extended: true);
        }
        var resultDir = GetResultDir();
        using (var progress = new ConsoleProgressBar())
        await using (var conn = new SqliteConnection($"Data Source={Path.Combine(resultDir.FullName, "results.sqlite")}"))
        {
            await conn.OpenAsync();
            var tableCommand = new SqliteCommand(@"
DROP TABLE IF EXISTS data_stat;

CREATE TABLE
    data_stat
    (
        data TEXT,
        battery REAL,
        totalIncoming_kwh REAL,
        totalOutgoing_kwh REAL
    )
;               ",
                conn);
            await tableCommand.ExecuteNonQueryAsync();
            var batteries = TestData.GetBatteries(extended: true).Append(NoBatteryConfiguration).ToList();
            progress.Setup(batteries.Count * data.Count, "Calculating Stats");
            var dataTimeStep = TimeSpan.FromMinutes(1);
            foreach (var batteryConfiguration in batteries)
            {
                foreach (var ds in data)
                {
                    var battery = batteryConfiguration.CreateBattery();
                    var batteryCapacityKwh = battery.TotalCapacity.KilowattHours;

                    var totalIncomingKwh = 0d;
                    var totalOutgoingKwh = 0d;
                    var batteryKwh = batteryCapacityKwh / 2;
                    foreach (var entry in ds.Data.SelectMany(x => x))
                    {
                        var load = ds.GetLoad(entry);
                        var generation = ds.GetGeneration(entry);
                        var effective = load - generation;
                        if (effective > PowerFast.Zero)
                        {
                            // load > generation
                            batteryKwh -= (effective * dataTimeStep).KilowattHours;
                            if (batteryKwh < 0)
                            {
                                // battery empty, acquire exactly the deficit of energy without losses from the grid
                                totalIncomingKwh += Math.Abs(batteryKwh);
                                batteryKwh = 0;
                            }
                        }
                        else
                        {
                            // load <= generation
                            // effective <= 0

                            var chargePower = -effective;
                            chargePower = Units.Min(chargePower, battery.MaximumChargePower);
                            batteryKwh += (chargePower * dataTimeStep).KilowattHours;
                            //batteryKwh += Math.Abs((effective * dataTimeStep).KilowattHours);
                            if (batteryKwh > batteryCapacityKwh)
                            {
                                // battery full, send exactly the excess of energy without losses to the grid
                                var diff = batteryKwh - batteryCapacityKwh;
                                totalOutgoingKwh += diff;
                                batteryKwh = batteryCapacityKwh;
                            }
                        }
                    }

                    var insertCommand = new SqliteCommand(
                        string.Create(CultureInfo.InvariantCulture,
                            @$"INSERT INTO data_stat VALUES (""{ds.Configuration}"", ""{batteryConfiguration.Description}"", {totalIncomingKwh}, {totalOutgoingKwh})"),
                        conn);
                    await insertCommand.ExecuteNonQueryAsync();
                    progress.FinishOne();
                }
            }
        }
    }

    public static DirectoryInfo GetResultDir()
    {
        var here = new DirectoryInfo(".");
        while (here.Name != "epdevicemanagement-code")
        {
            here = here.Parent;
            if (here is null)
            {
                throw new DriveNotFoundException();
            }
        }

        var dirs = here.GetDirectories();
        return dirs.First(
#if EXPERIMENTAL
            d => d.Name == "result-experimental"
#else
            d => d.Name == "result"
#endif
            );
    }
}