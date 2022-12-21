
using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DataAnalyzer;
using EpDeviceManagement.Data;
using EpDeviceManagement.Simulation;
using UnitsNet;
//await Analyzer.AnalyzeDifferenceAsync();
//await Analyzer.AnalyzePowerDifferenceAsync();
await Analyzer.AnalyzePowerDifferenceAsync(TimeSpan.FromMinutes(6));

//await PowerCalculator.CalculateAsync();
//await Analyzer.WritePowerValuesToDatabase();

//await Forecasting.WriteDataAsync();

//await Analyzer.CalculateStatsAsync();