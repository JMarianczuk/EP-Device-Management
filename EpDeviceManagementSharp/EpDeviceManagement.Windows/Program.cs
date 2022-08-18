// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using BenchmarkDotNet.Running;
using EpDeviceManagement.Data;
using EpDeviceManagement.Simulation;
using EpDeviceManagement.Windows;
using LpSolveDotNet;

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

CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var simulator = new Simulator();
//await simulator.AnalyzeAsync();
await simulator.SimulateAsync();