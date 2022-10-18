using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using LpSolveDotNet;
using UnitsNet;

namespace EpDeviceManagement.Windows;

[MemoryDiagnoser]
public class LpSolveBenchmark
{
    private readonly TimeSpan TimeStep = TimeSpan.FromMinutes(5);
    private readonly TimeSpan TotalTime = TimeSpan.FromHours(1);
    private readonly int Steps;
    private readonly Temperature InitialTemperature = Temperature.FromDegreesCelsius(18);
    private readonly Temperature DesiredTemperature = Temperature.FromDegreesCelsius(21);
    private readonly Temperature OutsideTemperature = Temperature.FromDegreesCelsius(5);
    private readonly Power[] Solar;
    private const double BasePricePerKwhHeat = 0.31d;
    private const double PremiumPriceForTheFirstHalfHour = 0.1d;
    private readonly double[] PricePerKwhHeat;
    private readonly Power MaxHeatPower = Power.FromKilowatts(10);
    private readonly Frequency StandingLoss = Frequency.FromCyclesPerHour(1);
    private readonly Volume BuildingVolume = Volume.FromCubicMeters(1000);
    private readonly Density AirDensity = Density.FromKilogramsPerCubicMeter(1.25);
    private readonly Mass BuildingAirWeight;
    private readonly SpecificEntropy AirSpecificEntropy = SpecificEntropy.FromKilojoulesPerKilogramKelvin(1);
    private readonly Entropy AirEntropy;
    private const int WattsPerKilowatt = 1000;

    private Random Random;
    private LpSolve? solver;

    private readonly int[] x_indices;
    private readonly int[] d_indices;
    private readonly int[] u_indices;
    private readonly int[] s_indices;
    private readonly int[] r_indices;

    private int index_scheme;

    public LpSolveBenchmark()
    {
        Random = new Random(69);
        Steps = (int)(TotalTime / TimeStep);
        Solar = Enumerable.Range(0, Steps).Select(_ => Power.FromKilowatts(Random.Next(0, 3))).ToArray();
        PricePerKwhHeat = Enumerable.Repeat(BasePricePerKwhHeat, Steps).Select((x, i) =>
        {
            if (i <= Steps / 2)
            {
                return x + PremiumPriceForTheFirstHalfHour;
            }
            else
            {
                return x;
            }
        }).ToArray();
        BuildingAirWeight = BuildingVolume * AirDensity;
        AirEntropy = AirSpecificEntropy * BuildingAirWeight;

        x_indices = new int[Steps + 1];
        d_indices = new int[Steps];
        u_indices = new int[Steps];
        s_indices = new int[Steps];
        r_indices = new int[Steps];
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        LpSolve.Init();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        Random = new Random(42);
    }

    [Benchmark]
    public void SetupWithOldMethod()
    {
        this.index_scheme = 0;
        int[] columnNumbers = new int[2 * Steps];
        double[] row = new double[2 * Steps];
        int totalVariables = 5 * Steps + 1;
        this.solver = LpSolve.make_lp(0, totalVariables);

        int x = 0;
        int d = 1;
        int u = 2;
        int s = 3;
        int r = 4;
        int numberOfVariables = 5;
        int ColumnNumber(int variable, int index) => index * numberOfVariables + variable + 1;
        for (int i = 0; i < Steps; i += 1)
        {
            columnNumbers[2 * i] = ColumnNumber(u, i);
            row[2 * i] = PricePerKwhHeat[i] * TimeStep.TotalHours;
            columnNumbers[2 * i + 1] = ColumnNumber(s, i);
            row[2 * i + 1] = 0.04d;
        }

        solver.set_obj_fnex(2 * Steps, row, columnNumbers);
        solver.set_minim();
        solver.resize_lp(1 + Steps * 6, totalVariables);
        solver.set_add_rowmode(true);

        columnNumbers[0] = ColumnNumber(x, 0);
        row[0] = 1;
        if (!solver.add_constraintex(1, row, columnNumbers, lpsolve_constr_types.EQ, InitialTemperature.DegreesCelsius))
        {
            Debugger.Break();
        }

        for (var i = 0; i < Steps; i += 1)
        {
            columnNumbers[0] = ColumnNumber(r, i);
            row[0] = 1;
            if (!solver.add_constraintex(1, row, columnNumbers, lpsolve_constr_types.EQ,
                    DesiredTemperature.DegreesCelsius))
            {
                Debugger.Break();
            }

            columnNumbers[0] = ColumnNumber(d, i);
            row[0] = 1;
            if (!solver.add_constraintex(1, row, columnNumbers, lpsolve_constr_types.EQ, Solar[i].Kilowatts))
            {
                Debugger.Break();
            }

            //columnNumbers[0] = ColumnNumber(u, i);
            //row[0] = 1;
            //if (!solver.add_constraintex(1, row, columnNumbers, lpsolve_constr_types.GE, 0))
            //{
            //    Debugger.Break();
            //}

            //columnNumbers[0] = ColumnNumber(u, i);
            //row[0] = 1;
            //if (!solver.add_constraintex(1, row, columnNumbers, lpsolve_constr_types.LE, MaxHeatPower.Kilowatts))
            //{
            //    Debugger.Break();
            //}
            solver.set_upbo(ColumnNumber(u, i), MaxHeatPower.Kilowatts);

            columnNumbers[0] = ColumnNumber(s, i);
            row[0] = -1;
            columnNumbers[1] = ColumnNumber(r, i);
            row[1] = 1;
            columnNumbers[2] = ColumnNumber(x, i);
            row[2] = -1;
            if (!solver.add_constraintex(3, row, columnNumbers, lpsolve_constr_types.EQ, 0))
            {
                Debugger.Break();
            }

            columnNumbers[0] = ColumnNumber(x, i + 1);
            row[0] = -1;
            var powerFactor = WattsPerKilowatt * TimeStep.TotalSeconds / AirEntropy.JoulesPerDegreeCelsius;
            columnNumbers[1] = ColumnNumber(u, i);
            row[1] = powerFactor;
            columnNumbers[2] = ColumnNumber(d, i);
            row[2] = powerFactor;
            columnNumbers[3] = ColumnNumber(x, i);
            row[3] = 1 - StandingLoss.PerSecond * TimeStep.TotalSeconds;
            if (!solver.add_constraintex(4, row, columnNumbers, lpsolve_constr_types.EQ,
                    -OutsideTemperature.DegreesCelsius * TimeStep.TotalSeconds * StandingLoss.PerSecond))
            {
                Debugger.Break();
            }
        }

        solver.set_add_rowmode(false);
    }

    [Benchmark]
    public void SetupWithNewMethod()
    {
        this.index_scheme = 1;
        int index = 1;
        LpVariable[] Create(int count, string name)
        {
            return Enumerable.Range(0, count).Select(x =>
            {
                var variable = new LpVariable(index);
                index += 1;
                return variable;
            }).ToArray();
        }

        var x = Create(Steps + 1, "x");
        var d = Create(Steps, "d");
        var u = Create(Steps, "u");
        var s = Create(Steps, "s");
        var r = Create(Steps, "r");

        var lp = new LpSolveDotNet.LpSolveDotNet(LpSolve.make_lp(0, index));

        LpSum objectiveFunction = new LpSum();
        for (int i = 0; i < Steps; i += 1)
        {
            objectiveFunction += u[i] * PricePerKwhHeat[i] * TimeStep.TotalHours
                                 + s[i] * 0.04d;
        }

        lp.SetObjectiveFunction(objectiveFunction);
        lp.IsAddRowmode = true;

        lp.AddConstraint(x[0] == InitialTemperature.DegreesCelsius);
        for (var i = 0; i < Steps; i += 1)
        {
            lp.AddConstraint(r[i] == DesiredTemperature.DegreesCelsius);
            lp.AddConstraint(d[i] == Solar[i].Kilowatts);
            //builder.AddConstraint(u[i] >= 0);
            //builder.AddConstraint(u[i] <= MaxHeatPower.Kilowatts);
            // s[i] = r[i] - x[i]
            lp.AddConstraint(-s[i] + r[i] - x[i] == 0);
            // x[i+1] = x[i] + u[i] * timeStep + d[i] * timeStep - (x[i] - outside) * standingLoss * timeStep
            lp.AddConstraint(-x[i + 1]
                      + (u[i] + d[i]) * (WattsPerKilowatt * TimeStep.TotalSeconds / AirEntropy.JoulesPerDegreeCelsius)
                      + (1 - StandingLoss.PerSecond * TimeStep.TotalSeconds) * x[i]
                      == -OutsideTemperature.DegreesCelsius * TimeStep.TotalSeconds * StandingLoss.PerSecond);
        }
        
        for (int i = 0; i < Steps; i += 1)
        {
            var column = lp.Columns[u[i].ColumnNumber];
            column.UpperBound = MaxHeatPower.Kilowatts;
        }
    }

    //[Benchmark]
    //public void SetupWithNewMethodWithStructs()
    //{
    //    this.index_scheme = 1;
    //    int index = 1;

    //    LpVariableStruct[] Create(int count, string name)
    //    {
    //        return Enumerable.Range(0, count).Select(x =>
    //        {
    //            var variable = new LpVariableStruct(index);
    //            index += 1;
    //            return variable;
    //        }).ToArray();
    //    }

    //    var x = Create(Steps + 1, "x");
    //    var d = Create(Steps, "d");
    //    var u = Create(Steps, "u");
    //    var s = Create(Steps, "s");
    //    var r = Create(Steps, "r");
    //    var builder = new BuilderStruct();
    //    builder.AddConstraint(x[0] == InitialTemperature.DegreesCelsius);
    //    for (var i = 0; i < Steps; i += 1)
    //    {
    //        builder.AddConstraint(r[i] == DesiredTemperature.DegreesCelsius);
    //        builder.AddConstraint(d[i] == Solar[i].Kilowatts);
    //        //builder.AddConstraint(u[i] >= 0);
    //        //builder.AddConstraint(u[i] <= MaxHeatPower.Kilowatts);
    //        // s[i] = r[i] - x[i]
    //        builder.AddConstraint(-s[i] + r[i] - x[i] == 0);
    //        // x[i+1] = x[i] + u[i] * timeStep + d[i] * timeStep - (x[i] - outside) * standingLoss * timeStep
    //        builder.AddConstraint(-x[i + 1]
    //                              + (u[i] + d[i]) * (WattsPerKilowatt * TimeStep.TotalSeconds / AirEntropy.JoulesPerDegreeCelsius)
    //                              + (1 - StandingLoss.PerSecond * TimeStep.TotalSeconds) * x[i]
    //                              == -OutsideTemperature.DegreesCelsius * TimeStep.TotalSeconds * StandingLoss.PerSecond);
    //    }

    //    LpSumStruct objectiveFunction = new LpSumStruct(Enumerable.Empty<LpSummandStruct>());
    //    for (int i = 0; i < Steps; i += 1)
    //    {
    //        objectiveFunction += u[i] * PricePerKwhHeat[i] * TimeStep.TotalHours
    //                             + s[i] * 0.04d;
    //    }

    //    this.solver = builder.CreateSolver(objectiveFunction, false);
    //    for (int i = 0; i < Steps; i += 1)
    //    {
    //        this.solver.set_upbo(u[i].ColumnNumber, MaxHeatPower.Kilowatts);
    //    }
    //}

    [IterationCleanup]
    public void IterationCleanup()
    {
        if (this.solver == null)
        {
            return;
        }
        ApplyIndexScheme();
        solver.set_verbose(lpsolve_verbosity.IMPORTANT);
        var result = solver.solve();

        if (result == lpsolve_return.OPTIMAL)
        {
            var values = new double[5 * Steps + 1];
            string Get(int v) => values[v - 1].ToString("00.000", CultureInfo.InvariantCulture);
            solver.get_variables(values);
            var x = x_indices;
            //var u = u_indices;
            //var s = s_indices;
            //var d = d_indices;
            //var r = r_indices;
            //for (int i = 0; i < Steps; i += 1)
            //{
            //    void Write(int[] var, [CallerArgumentExpression("var")] string varExpr = null) => Console.Write($"{varExpr}[{i:00}] = {Get(var[i])}, ");
            //    Write(x);
            //    Write(u);
            //    Write(s);
            //    Write(d);
            //    Write(r);
            //    Console.WriteLine();
            //}
            Console.WriteLine($"x[N] = {Get(x[Steps])}");
        }
        this.solver.Dispose();
        this.solver = null;
    }

    private void ApplyIndexScheme()
    {
        if (index_scheme == 0)
        {
            int x = 0;
            int d = 1;
            int u = 2;
            int s = 3;
            int r = 4;
            int numberOfVariables = 5;
            int ColumnNumber(int variable, int index) => index * numberOfVariables + variable + 1;
            for (int i = 0; i < Steps; i += 1)
            {
                x_indices[i] = ColumnNumber(x, i);
                d_indices[i] = ColumnNumber(d, i);
                u_indices[i] = ColumnNumber(u, i);
                s_indices[i] = ColumnNumber(s, i);
                r_indices[i] = ColumnNumber(r, i);
            }

            x_indices[Steps] = ColumnNumber(x, Steps);
        }

        if (index_scheme == 1)
        {
            int index = 1;
            LpVariable[] Create(int count, string name)
            {
                return Enumerable.Range(0, count).Select(x =>
                {
                    var variable = new LpVariable(index);
                    index += 1;
                    return variable;
                }).ToArray();
            }
            var x = Create(Steps + 1, "x");
            var d = Create(Steps, "d");
            var u = Create(Steps, "u");
            var s = Create(Steps, "s");
            var r = Create(Steps, "r");
            for (int i = 0; i < Steps; i += 1)
            {
                x_indices[i] = x[i].ColumnNumber;
                d_indices[i] = d[i].ColumnNumber;
                u_indices[i] = u[i].ColumnNumber;
                s_indices[i] = s[i].ColumnNumber;
                r_indices[i] = r[i].ColumnNumber;
            }

            x_indices[Steps] = x[Steps].ColumnNumber;
        }
    }
}