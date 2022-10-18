using System.Globalization;
using EpDeviceManagement.Simulation;
using Microsoft.Data.Sqlite;
using UnitsNet;

namespace DataAnalyzer;

public class Analyzer
{
    public static async Task AnalyzeDifferenceAsync()
    {
        //var enhanced = TestData.GetDataSets
    }

    public static async Task CalculateStatsAsync()
    {
        var timeStep = TimeSpan.FromMinutes(15);
        var data = await TestData.GetDataSetsAsync(timeStep, new NoProgress());
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
            foreach (var batteryCapacityKwh in new[] {10d, 15d})
            {
                foreach (var ds in data)
                {
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
                            @$"INSERT INTO data_stat VALUES (""{ds.Configuration}"", {batteryCapacityKwh}, {totalIncomingKwh}, {totalOutgoingKwh})"),
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