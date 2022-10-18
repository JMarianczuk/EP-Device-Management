using EpDeviceManagement.Simulation;
using System.Globalization;

namespace DataAnalyzer;

public class Forecasting
{
    public static async Task WriteDataAsync()
    {
        var data = await TestData.GetDataSetsAsync(TimeSpan.FromMinutes(15), new NoProgress());
        var ds = data[0];
        var loadsTimeSeries = ds.Data.Select(ds.GetLoadsTotalPower).Select(x => x.Kilowatts).ToList();
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var asString = string.Join(',', loadsTimeSeries.Take(20000).Select(x => x.ToString("0.0#", CultureInfo.InvariantCulture)));
        var prediction = string.Join(',', loadsTimeSeries.Skip(20000).Take(100).Select(x => x.ToString("0.0#", CultureInfo.InvariantCulture)));
        int p = 5;
    }
}