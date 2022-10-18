
using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DataAnalyzer;
using EpDeviceManagement.Data;
using UnitsNet;

//await Analyzer.AnalyzeDifferenceAsync();
//await Forecasting.WriteDataAsync();
await Analyzer.CalculateStatsAsync();