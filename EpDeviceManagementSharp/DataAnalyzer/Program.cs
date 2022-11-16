
using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DataAnalyzer;
using EpDeviceManagement.Data;
using UnitsNet;

//await Analyzer.AnalyzeDifferenceAsync();
//await Analyzer.AnalyzePowerDifferenceAsync();

//await PowerCalculator.CalculateAsync();
//await Analyzer.WritePowerValuesToDatabase();

//await Analyzer.AnalyzeGridImport();

//await Forecasting.WriteDataAsync();

await Analyzer.CalculateStatsAsync();