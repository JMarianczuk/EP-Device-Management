using System.Globalization;
using System.Text;
using EpDeviceManagement.Data;
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

    public static async Task AnalyzePowerDifferenceAsync()
    {
        var timeStep = TimeSpan.FromMinutes(5);
        IList<DataSet> data;
        using (var progress = new ConsoleProgressBar())
        {
            data = await TestData.GetDataSetsAsync(timeStep, progress, extended: true);
        }

        foreach (var ds in data)
        {
            double maxNetLoad = -1d;
            double maxNetGen = -1d;

            double maxLoadDiffKwAvg = -1d;
            double maxGenDiffKwAvg = -1d;

            double maxLoadDiffKwStep = -1d;
            double maxGenDiffKwStep = -1d;
            DateTimeOffset loadNetTime = default;
            DateTimeOffset loadAvgTime = default;
            DateTimeOffset loadStepTime = default;
            DateTimeOffset genNetTime = default;
            DateTimeOffset genAvgTime = default;
            DateTimeOffset genStepTime = default;

            foreach (var entry in ds.Data)
            {
                var momentaryLoad = ds.GetLoad(entry[0]);
                var totalLoad = entry.Average(ds.GetLoad);
                var maxLoad = entry.Skip(1).Max(ds.GetLoad);

                var avgLoadDiff = Math.Abs((momentaryLoad - totalLoad).Kilowatts);
                var stepLoadDiff = Math.Abs((momentaryLoad - maxLoad).Kilowatts);

                var momentaryGeneration = ds.GetGeneration(entry[0]);
                var totalGeneration = entry.Average(ds.GetGeneration);
                var maxGeneration = entry.Skip(1).Max(ds.GetGeneration);

                var avgGenDiff = Math.Abs((momentaryGeneration - totalGeneration).Kilowatts);
                var stepGenDiff = Math.Abs((momentaryGeneration - maxGeneration).Kilowatts);

                if (avgLoadDiff > maxLoadDiffKwAvg)
                {
                    maxLoadDiffKwAvg = avgLoadDiff;
                    loadAvgTime = entry[0].Timestamp;
                }

                if (stepLoadDiff > maxLoadDiffKwStep)
                {
                    maxLoadDiffKwStep = stepLoadDiff;
                    loadStepTime = entry[0].Timestamp;
                }

                if (avgGenDiff > maxGenDiffKwAvg)
                {
                    maxGenDiffKwAvg = avgGenDiff;
                    genAvgTime = entry[0].Timestamp;
                }

                if (stepGenDiff > maxGenDiffKwStep)
                {
                    maxGenDiffKwStep = stepGenDiff;
                    genStepTime = entry[0].Timestamp;
                }

                foreach (var dataSet in entry)
                {
                    var netLoad = ds.GetLoad(dataSet) - ds.GetGeneration(dataSet);
                    if (netLoad > PowerFast.Zero)
                    {
                        if (netLoad.Kilowatts > maxNetLoad)
                        {
                            maxNetLoad = netLoad.Kilowatts;
                            loadNetTime = dataSet.Timestamp;
                        }
                    }
                    else
                    {
                        var netGeneration = -netLoad;
                        if (netGeneration.Kilowatts > maxNetGen)
                        {
                            maxNetGen = netGeneration.Kilowatts;
                            genNetTime = dataSet.Timestamp;
                        }
                    }
                }
            }

            Console.WriteLine(string.Join(Environment.NewLine,
                    $"Data set {ds.Configuration}",
                    $"differs on avg by {maxLoadDiffKwAvg} kW for loads at {loadAvgTime} and {maxGenDiffKwAvg} kW for generation at {genAvgTime}",
                    $"and for the step by {maxLoadDiffKwStep} kW for loads at {loadStepTime} and {maxGenDiffKwStep} kW for generation at {genStepTime}",
                    $"and has the largest power with {maxNetLoad} kW for loads at {loadNetTime} and {maxNetGen} kW for generation at {genNetTime}"));
        }
    }

    public static async Task AnalyzeGridImport()
    {
        var filename = TestData.GetFileName(TimeSpan.FromMinutes(5));

        var (data, handle) = new ReadDataFromCsv().ReadAsync2(filename);
        var min = Power.FromKilowatts(1000000);
        var max = Power.FromKilowatts(-1000000);

        var nullCount = 0;
        var nonNullCount = 0;

        using (var progress = new ConsoleProgressBar())
        {
            var oneYearOfSteps = TimeSpan.FromDays(365) / TimeSpan.FromMinutes(5);
            progress.Setup((int) (5 * oneYearOfSteps), "Analying");
            await foreach (var entry in data)
            {
                var l1 = PowerFast.FromKilowatts(entry.DE_KN_residential5_grid_import);
                var ex = PowerFast.FromKilowatts(entry.DE_KN_residential6_grid_export);
                var g1 = PowerFast.FromKilowatts(entry.DE_KN_residential6_pv);
                var actual = TestData.GetLoad(l1, ex, g1);

                progress.FinishOne();
            }
        }

        Console.WriteLine($"Null: {nullCount}, NonNull: {nonNullCount}");
    }

    public static async Task WritePowerValuesToDatabase()
    {
        foreach (var ts in new[]
                 {
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
            foreach (var batteryConfiguration in batteries)
            {
                foreach (var ds in data)
                {
                    var battery = batteryConfiguration.CreateBattery();
                    var batteryCapacityKwh = battery.TotalCapacity.KilowattHours;

                    var totalIncomingKwh = 0d;
                    var totalOutgoingKwh = 0d;
                    var batteryKwh = batteryCapacityKwh / 2;
                    foreach (var entry in ds.Data)
                    {
                        var load = entry.Average(ds.GetLoad);
                        var generation = entry.Average(ds.GetGeneration);
                        var net = load - generation;
                        if (net > PowerFast.Zero)
                        {
                            // load > generation
                            batteryKwh -= (net * timeStep).KilowattHours;
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
                            batteryKwh += Math.Abs((net * timeStep).KilowattHours);
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
        return dirs.First(d => d.Name == "result");
    }
}