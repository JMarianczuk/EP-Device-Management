using System.Globalization;
using EpDeviceManagement.Simulation;
using Microsoft.CodeAnalysis;
using Microsoft.Data.Sqlite;
using UnitsNet;

namespace DataAnalyzer;

public class Analyzer
{
    public static async Task AnalyzeDifferenceAsync()
    {
        //var enhanced = TestData.GetDataSets
    }

    public static async Task AnalyzeDifferenceBetween1And15MinutesPower()
    {
        var timeStep = TimeSpan.FromMinutes(5);
        IList<DataSet> data;
        using (var progress = new ConsoleProgressBar())
        {
            data = await TestData.GetDataSetsAsync(timeStep, progress, extended: true);
        }

        foreach (var ds in data)
        {
            double maxLoadDiffKw = -1d;
            double maxGenDiffKw = -1d;
            DateTimeOffset loadTime = default;
            DateTimeOffset genTime = default;

            foreach (var entry in ds.Data)
            {
                var loadDiff = Math.Abs(
                    (ds.GetLoadsTotalPower(entry) - ds.GetMomentaneousLoadsPower(entry)).Kilowatts);
                var genDiff = Math.Abs(
                    (ds.GetGeneratorsTotalPower(entry) - ds.GetMomentaneousGeneratorsPower(entry)).Kilowatts);
                if (loadDiff > maxLoadDiffKw)
                {
                    maxLoadDiffKw = loadDiff;
                    loadTime = entry.Timestamp;
                }

                if (genDiff > maxGenDiffKw)
                {
                    maxGenDiffKw = genDiff;
                    genTime = entry.Timestamp;
                }
            }

            Console.WriteLine($"Data set {ds.Configuration} differs by {maxLoadDiffKw} kW for loads at {loadTime} and {maxGenDiffKw} kW at {genTime}");
        }
    }

    public static async Task CalculateStatsAsync()
    {
        var timeStep = TimeSpan.FromMinutes(5);
        IList<DataSet> data;
        using (var progress = new ConsoleProgressBar())
        {
            data = await TestData.GetDataSetsAsync(timeStep, progress, extended: true);
        }
        var resultDir = GetResultDir();
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
;
                ",
                conn);
            await tableCommand.ExecuteNonQueryAsync();
            foreach (var batteryConfiguration in TestData.GetBatteries())
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
                        var load = ds.GetLoadsTotalPower(entry);
                        var generation = ds.GetGeneratorsTotalPower(entry);
                        var net = load - generation;
                        if (net > Power.Zero)
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