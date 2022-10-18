using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using LpSolveDotNet;
using LpSolverBuilder.LpSolveDotNet;
using UnitsNet;

namespace EpDeviceManagement.Windows;

public class SimpleMpc
{
    public static void Solve()
    {
        LpSolve.Init();

        var timeStep = TimeSpan.FromMinutes(5);
        var totalTime = TimeSpan.FromHours(1);
        var steps = (int) (totalTime / timeStep);
        var initialTemperature = Temperature.FromDegreesCelsius(18);
        var desiredTemperature = Temperature.FromDegreesCelsius(21);
        var outsideTemperature = Temperature.FromDegreesCelsius(5);
        var random = new Random(42);
        var solar = Enumerable.Range(0, steps).Select(_ => Power.FromKilowatts(random.Next(0, 3))).ToArray();
        var basePricePerKwhHeat = 0.31d;
        var premiumForFourthStep = 1.2;
        var pricePerKwhHeat = Enumerable.Repeat(basePricePerKwhHeat, steps).Select((x, i) =>
        {
            if (i == 3)
            {
                return x + premiumForFourthStep;
            }
            else
            {
                return x;
            }
        }).ToArray();

        var maxHeatPower = Power.FromKilowatts(10);
        var standingLoss = Frequency.FromCyclesPerHour(0.5d);

        var buildingVolume = Volume.FromCubicMeters(1000);
        var airDensity = Density.FromKilogramsPerCubicMeter(1.25);
        var buildingAirWeight = buildingVolume * airDensity;
        var airSpecificEntropy = SpecificEntropy.FromKilojoulesPerKilogramKelvin(1);
        var airEntropy = airSpecificEntropy * buildingAirWeight;

        var wattsPerKilowatt = 1000;

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

        var x = Create(steps + 1, "x");
        var d = Create(steps, "d");
        var u = Create(steps, "u");
        //var y = Create(steps);
        var s = Create(steps, "s");
        var r = Create(steps, "r");

        using var lp = new LpSolverBuilder.LpSolveDotNet.LpSolveDotNet(LpSolve.make_lp(0, index));
        
        LpSum objectiveFunction = new LpSum();
        for (int i = 0; i < steps; i += 1)
        {
            objectiveFunction += u[i] * pricePerKwhHeat[i] * timeStep.TotalHours
                                 + s[i] * 0.04d;
        }

        lp.SetObjectiveFunction(objectiveFunction);
        lp.IsMaximize = false;

        lp.IsAddRowmode = true;
        lp.AddConstraint(x[0] == initialTemperature.DegreesCelsius);
        for (var i = 0; i < steps; i += 1)
        {
            lp.AddConstraint(r[i] == desiredTemperature.DegreesCelsius);
            lp.AddConstraint(d[i] == solar[i].Kilowatts);
            //builder.AddConstraint(u[i] >= 0); // not needed, lower bound is always 0
            //builder.AddConstraint(u[i] <= maxHeatPower.Kilowatts); // set later as upper bound
            // s[i] = r[i] - x[i]
            lp.AddConstraint(-s[i] + r[i] - x[i] == 0);
            // x[i+1] = x[i] + u[i] * timeStep + d[i] * timeStep - (x[i] - outside) * standingLoss * timeStep
            //if (false)
            //{
            //    var x_i_plus_1 = new Temperature();
            //    var x_i = new Temperature();
            //    var u_i = new Power();
            //    var timeStep_ = new TimeSpan();
            //    var d_i = new Power();
            //    var outside_ = new Temperature();
            //    var sl = new Frequency();
            //    x_i_plus_1 = x_i + ((u_i + d_i) * timeStep_).DivideBy(airEntropy) 
            //                 - (x_i - outside_) * standingLoss.Multiply(timeStep);
            //}
            lp.AddConstraint(-x[i + 1]
                      + (u[i] + d[i]) * (wattsPerKilowatt * timeStep.TotalSeconds / airEntropy.JoulesPerDegreeCelsius)
                      + (1 - standingLoss.PerSecond * timeStep.TotalSeconds) * x[i]
                      == -outsideTemperature.DegreesCelsius * timeStep.TotalSeconds * standingLoss.PerSecond);
        }

        for (int i = 0; i < steps; i += 1)
        {
            var column = lp.Columns[u[i].ColumnNumber];
            column.UpperBound = maxHeatPower.Kilowatts;
        }

        lp.Verbosity = lpsolve_verbosity.IMPORTANT;
        var solution = lp.Solve();
        if (solution.Result == lpsolve_return.OPTIMAL)
        {
            var values = new double[index];
            string Get(LpVariable v) => values[v.ColumnNumber - 1].ToString("00.000", CultureInfo.InvariantCulture);
            solution.GetVariables(values);
            for (int i = 0; i < steps; i += 1)
            {
                void Write(LpVariable[] var, [CallerArgumentExpression("var")] string varExpr = null) => Console.Write($"{varExpr}[{i:00}] = {Get(var[i])}, ");
                Write(x);
                Write(u);
                Write(s);
                Write(d);
                Write(r);
                Console.WriteLine();
            }
            Console.WriteLine($"x[N] = {Get(x[steps])}");
        }
    }
}