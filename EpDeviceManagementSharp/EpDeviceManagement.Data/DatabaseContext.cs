
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using EpDeviceManagement.Data.Generation;
using EpDeviceManagement.Data.Generation.Abstractions;

namespace EpDeviceManagement.Data;

public class ReadDataFromCsv
{
    public (IAsyncEnumerable<EnergyDataSet>, IDisposable) ReadAsync()
    {
        var reader = new StreamReader("household_data_15min_singleindex.csv");
        var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
        });

        var records = csvReader.GetRecordsAsync<EnergyDataSet>();
        return (records, new DisposableCollection(new IDisposable[] {reader, csvReader}));

        //IList<EnergyDataSet> result = new List<EnergyDataSet>();
        //await foreach (var dataSet in records)
        //{
        //    result.Add(dataSet);
        //}

        //return result;
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