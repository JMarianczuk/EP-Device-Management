
using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DataAnalyzer;
using EpDeviceManagement.Data;
using UnitsNet;

//await Analyzer.AnalyzeDifferenceAsync();
//await Analyzer.AnalyzeDifferenceBetween1And15MinutesPower();
//await Forecasting.WriteDataAsync();
//await PowerCalculator.CalculateAsync();
await Analyzer.CalculateStatsAsync();