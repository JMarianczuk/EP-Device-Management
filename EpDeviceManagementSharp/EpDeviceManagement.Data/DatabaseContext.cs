
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using EpDeviceManagement.Data.Extensions;
using EpDeviceManagement.Data.Generation;
using EpDeviceManagement.Data.Generation.Abstractions;

namespace EpDeviceManagement.Data;

public class ReadDataFromCsv
{
    public const string FileName_01 = "household_data_1min_singleindex.csv";
    public const string FileName_15 = "household_data_15min_singleindex.csv";
    public const string FileName_60 = "household_data_60min_singleindex.csv";
    
    public const string FileName_Power_01 = "household_data_1min_power.csv";
    public const string FileName_Power_15 = "household_data_15min_power.csv";
    public const string FileName_Power_60 = "household_data_60min_power.csv";

    public (IAsyncEnumerable<EnergyDataSet>, IDisposable) ReadAsync(string fileName = FileName_15)
    {
        var reader = new StreamReader(fileName);
        var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
        });

        var records = csvReader.GetRecordsAsync<EnergyDataSet>();
        return (records, new DisposableCollection(new IDisposable[] {reader, csvReader}));
    }

    public (IAsyncEnumerable<PowerDataSet>, IDisposable) ReadAsync2(string fileName = FileName_Power_15)
    {
        var reader = new StreamReader(fileName);
        var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        var records = csvReader.GetRecordsAsync<PowerDataSet>();
        return (records, new DisposableCollection(new IDisposable[] { csvReader, reader }));
    }

    private class DisposableCollection : IDisposable
    {
        private readonly IEnumerable<IDisposable> disposables;
        private bool disposed;

        public DisposableCollection(IEnumerable<IDisposable> disposables)
        {
            this.disposables = disposables;
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
                foreach (var d in this.disposables)
                {
                    d?.Dispose();
                }
            }
        }
    }
}

//[GenerateFromCsv("household_data_15min_singleindex.csv", DefaultType = typeof(double))]
//public partial class EnergyDataSet
//{
//    public DateTime utc_timestamp { get; set; }

//    public DateTime cest_timestamp { get; set; }

//    public string interpolated { get; set; }
//}