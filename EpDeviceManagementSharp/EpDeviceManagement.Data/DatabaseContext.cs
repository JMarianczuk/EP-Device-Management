
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using EpDeviceManagement.Data.Generation;
using EpDeviceManagement.Data.Generation.Abstractions;

namespace EpDeviceManagement.Data;

public class ReadDataFromCsv
{
    public async Task<IEnumerable<EnergyDataSet>> Read()
    {
        using var reader = new StreamReader("household_data_15min_singleindex.csv");
        using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        var records = csvReader.GetRecordsAsync<EnergyDataSet>();

        IList<EnergyDataSet> result = new List<EnergyDataSet>();
        await foreach (var dataSet in records)
        {
            result.Add(dataSet);
        }

        return result;
    }
}

[GenerateFromCsv("household_data_15min_singleindex.csv", DefaultType = typeof(double))]
public partial class EnergyDataSet
{
    public DateTime utc_timestamp { get; set; }

    public DateTime cest_timestamp { get; set; }

    public string interpolated { get; set; }
}