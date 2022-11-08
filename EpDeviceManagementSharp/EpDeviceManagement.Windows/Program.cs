using System.Diagnostics;
using System.Globalization;
using BenchmarkDotNet.Running;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.Data;
using EpDeviceManagement.Simulation;
using EpDeviceManagement.Simulation.Loads;
using EpDeviceManagement.Simulation.Storage;
using EpDeviceManagement.Windows;
using LpSolveDotNet;
using Stateless.Graph;
using UnitsNet;

CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var simulator = new Simulator();
//await simulator.AnalyzeAsync();
try
{
    await simulator.SimulateAsync();
}
catch (Exception e)
{
    if (Debugger.IsAttached)
    {
        Debugger.Break();
    }
    else
    {
        Console.WriteLine(e);
        Console.WriteLine(e.StackTrace);
    }
}