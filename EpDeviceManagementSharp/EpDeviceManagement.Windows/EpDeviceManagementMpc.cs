using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Simulation.Heating;
using EpDeviceManagement.Simulation.Storage;
using LpSolveDotNet;
using Microsoft.CodeAnalysis.Operations;
using UnitsNet;

namespace EpDeviceManagement.Windows;

public class EpDeviceManagementMpc
{
    private static readonly SpecificEntropy waterSpecificEntropy = SpecificEntropy.FromKilojoulesPerKilogramKelvin(4.186);
    private static readonly Density waterDensity = Density.FromKilogramsPerLiter(0.99);
    private static readonly Temperature ambientTemperature = Temperature.FromDegreesCelsius(20);
    private static readonly Temperature inletTemperature = Temperature.FromDegreesCelsius(15);

    private static readonly Ratio minimumStorageCapacity = Ratio.FromPercent(20);
    private static readonly Ratio maximumStorageCapacity = Ratio.FromPercent(80);
    private static readonly Ratio usableStorageCapacity = minimumStorageCapacity - maximumStorageCapacity;

    public static void Solve()
    {
        LpSolve.Init();

        var random = new Random(96);

        T[] GetRandom<T>(
            int count,
            int lower,
            int upper,
            Func<int, T> selector)
        {
            return Enumerable
                .Range(0, count)
                .Select(_ => selector(random.Next(lower, upper)))
                .ToArray();
        }

        var timeStep = TimeSpan.FromMinutes(5);
        var totalTime = TimeSpan.FromHours(1);
        var steps = (int)(totalTime / timeStep);
        var solar = GetRandom(steps, 0, 3, x => Power.FromKilowatts(x));
        var uncontrollableLoad = GetRandom(steps, 2, 5, x => Power.FromKilowatts(x));
        var hotWaterWithdrawal = GetRandom(steps, 0, 40, x => VolumeFlow.FromLitersPerMinute(x));

        // Electric Water Heater
        var ewh = new ElectricWaterHeater(
            Ratio.FromPercent(98),
            TimeSpan.FromHours(1),
            waterSpecificEntropy,
            waterDensity)
        {
            MinimumWaterTemperature = Temperature.FromDegreesCelsius(60),
            MaximumWaterTemperature = Temperature.FromDegreesCelsius(90),
            CurrentTemperature = Temperature.FromDegreesCelsius(70),
            MaximumChargePower = Power.FromKilowatts(3),
            TotalWaterCapacity = Volume.FromLiters(800),
        };
        var ewhTargetTemperature = Temperature.FromDegreesCelsius(80);
        var ewhTargetSoC = ewh.ToEnergy(ewhTargetTemperature);

        var ewhLosses = hotWaterWithdrawal
            .Select(x => ewh.EquivalentLoss(timeStep, ambientTemperature, x, inletTemperature))
            .ToArray();

        // EV

        // Battery Storage
        IStorage battery = new BatteryElectricStorage(
            Frequency.FromCyclesPerHour(0.1),
            Ratio.FromPercent(90),
            Ratio.FromPercent(110))
        {
            CurrentStateOfCharge = Energy.FromKilowattHours(3),
            TotalCapacity = Energy.FromKilowattHours(13.5 * usableStorageCapacity.DecimalFractions),
            MaximumChargePower = Power.FromKilowatts(4.7),
            MaximumDischargePower = Power.FromKilowatts(4.7),
        };
        var batteryTargetSoC = battery.TotalCapacity;
        var packetEnergy = Energy.FromKilowattHours(1);

        int index = 0;

        LpVariable GetSingle()
        {
            var variable = new LpVariable(index);
            index += 1;
            return variable;
        }
        LpVariable[] Get(int count)
        {
            return Enumerable
                .Range(0, count)
                .Select(_ => GetSingle())
                .ToArray();
        }

        var x_ewh = Get(steps + 1);
        var x_battery = Get(steps + 1);
        var u_ewh = Get(steps);
        var u_packet_in = Get(steps);
        var u_packet_out = Get(steps);
        var d_solar = Get(steps);
        var d_loads = Get(steps);

        var s_ewh = Get(steps);
        var s_battery = Get(steps);
        var r_ewh = GetSingle();
        var r_battery = GetSingle();

        using var lp = new LpSolveDotNet.LpSolveDotNet(LpSolve.make_lp(0, index));

        LpSum objectiveFunction = LpSum.Empty();
        for (int i = 0; i < steps; i += 1)
        {
            objectiveFunction += (u_packet_in[i] + u_packet_out[i]) * 0.1
                                 + s_ewh[i] * 0.5
                                 + s_battery[i] * 0.4;
        }
        lp.SetObjectiveFunction(objectiveFunction);
        //lp.ResizeLp(7 * steps + 2, index);

        lp.IsAddRowmode = true;

        lp.AddConstraint(r_ewh == ewhTargetSoC.KilowattHours);
        lp.AddConstraint(r_battery == batteryTargetSoC.KilowattHours);

        for (int i = 0; i < steps; i += 1)
        {
            lp.AddConstraint(d_solar[i] == solar[i].Kilowatts);
            lp.AddConstraint(d_loads[i] == uncontrollableLoad[i].Kilowatts);

            {
                var column = lp.Columns[u_ewh[i].ColumnNumber];
                column.IsBinary = true;
            }
            {
                var column = lp.Columns[x_ewh[i].ColumnNumber];
                column.UpperBound = ewh.TotalCapacity.KilowattHours;
                column.LowerBound = ewh.MinimumStateOfCharge.KilowattHours;
            }
            lp.AddConstraint(
                -x_ewh[i + 1] 
                + x_ewh[i]
                - u_ewh[i] * (ewh.MaximumChargePower * timeStep).KilowattHours
                == ewhLosses[i].Kilowatts);

            {
                var column = lp.Columns[x_battery[i].ColumnNumber];
                column.UpperBound = battery.TotalCapacity.KilowattHours;
            }
            {
                var column = lp.Columns[u_packet_in[i].ColumnNumber];
                column.IsBinary = true;
            }
            {
                var column = lp.Columns[u_packet_out[i].ColumnNumber];
                column.IsBinary = true;
            }
            lp.AddConstraint(u_packet_in[i] + u_packet_out[i] <= 1);
            lp.AddConstraint(
                -x_battery[i + 1]
                + x_battery[i]
                + (
                    d_solar[i]
                    - d_loads[i]
                    - u_ewh[i] * ewh.MaximumChargePower.Kilowatts
                ) * timeStep.TotalHours
                + (u_packet_in[i] - u_packet_out[i]) * packetEnergy.KilowattHours
                == 0);

            lp.AddConstraint(
                -s_ewh[i]
                + r_ewh
                - x_ewh[i]
                == 0);
            lp.AddConstraint(
                -s_battery[i]
                + r_battery
                - x_battery[i]
                == 0);
        }

        lp.Verbosity = lpsolve_verbosity.FULL;
        lp.SetLogFunction(text => Debug.WriteLine($"LOG: {text}"));
        var solution = lp.Solve();
        if (solution.Result == lpsolve_return.OPTIMAL)
        {
            var values = new double[index];
            string GetString(LpVariable v) => values[v.ColumnNumber - 1].ToString("00.000", CultureInfo.InvariantCulture);
            solution.GetVariables(values);
            for (int i = 0; i < steps; i += 1)
            {
                void Write(LpVariable[] var, [CallerArgumentExpression("var")] string varExpr = null) => Console.Write($"{varExpr}[{i:00}] = {GetString(var[i])}, ");
                Write(x_ewh);
                Write(u_ewh);
                Write(x_battery);
                Write(u_packet_in);
                Write(u_packet_out);
                Write(d_solar);
                Write(d_loads);
                Write(s_ewh);
                Write(s_battery);
                Console.WriteLine();
            }
        }
    }
}