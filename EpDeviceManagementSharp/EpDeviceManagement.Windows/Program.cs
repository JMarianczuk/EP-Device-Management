// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using BenchmarkDotNet.Running;
using EpDeviceManagement.Control.Strategy;
using EpDeviceManagement.Data;
using EpDeviceManagement.Simulation;
using EpDeviceManagement.Simulation.Storage;
using EpDeviceManagement.Windows;
using LpSolveDotNet;
using Stateless.Graph;
using UnitsNet;

//Console.WriteLine("Hello, World!");

//EpDeviceManagementMpc.Solve();

//BenchmarkRunner.Run<LpSolveBenchmark>();

//var s = new LpSolveBenchmark();
//s.GlobalSetup();
//s.IterationSetup();
//s.SetupWithNewMethodWithStructs();
//s.IterationCleanup();

//s.IterationSetup();
//s.SetupWithNewMethod();
//s.IterationCleanup();

//var prob = new ProbabilisticModelingControl(
//    new BatteryElectricStorage(
//        Frequency.Zero, 
//        Ratio.FromPercent(100),
//        Ratio.FromPercent(100)),
//    Energy.Zero,
//    Ratio.FromPercent(10),
//    Ratio.FromPercent(90),
//    new SeededRandomNumberGenerator(123));
//var mach = prob.BuildSimplifiedMachine(ProbabilisticModelingControl.State.BatteryLow);
//var graph = UmlDotGraph.Format(mach.GetInfo());


CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var simulator = new Simulator();
//await simulator.AnalyzeAsync();
await simulator.SimulateAsync();