using System.Diagnostics;
using System.Globalization;
using BenchmarkDotNet.Running;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.Data;
using EpDeviceManagement.Simulation;
using EpDeviceManagement.Simulation.Loads;
using EpDeviceManagement.Simulation.Storage;
using EpDeviceManagement.Windows;
using LpSolveDotNet;
using Stateless.Graph;
using UnitsNet;

namespace EpDeviceManagement.Windows;

internal class Program
{
    public static async Task Main(string[] args)
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        //var start = new DateTimeOffset(2015, 12, 11, 10, 00, 00, TimeSpan.FromHours(1));
        //var timeStep = TimeSpan.FromMinutes(5);
        //var steps = 90352;
        //var fail_time = start + steps * timeStep;
        //Console.WriteLine(fail_time);
        //return;

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
    }
}