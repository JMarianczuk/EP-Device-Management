using LpSolveDotNet;
using UnitsNet;

namespace EpDeviceManagement.Windows;

public class TestProgramToSolve
{
    public void Solve()
    {
        LpSolve.Init();
        var appliances = new Appliance[]
        {
            new() // 1 - Kitchen light
            {
                RatedPower = Power.FromKilowatts(0.011),
                //Usages =
                //{
                //    new() // with the stove in the morning
                //    {
                //        Duration = TimeSpan.FromMinutes(30),
                //        Baseline = (31, 33),
                //    },
                //    new()
                //    {
                //        Duration = TimeSpan.FromMinutes(170),
                //        Baseline = (115, 131),
                //    }
                //}
            },
            new() // 2 - TV room light
            {
                RatedPower = Power.FromKilowatts(0.011),
                //Usages =
                //{
                //    new()
                //    {
                //        Duration = TimeSpan.FromMinutes(180),
                //        Baseline = (103, 120),
                //    },
                //},
            },
            new() // 3 - Laundry room light
            {
                RatedPower = Power.FromKilowatts(0.011),
                //Usages =
                //{
                //    new()
                //    {
                //        Duration = TimeSpan.FromMinutes(60),
                //        Baseline = (108, 113),
                //    },
                //    new()
                //    {
                //        Duration = TimeSpan.FromMinutes(30),
                //        Baseline = (115, 117),
                //    },
                //}
            },
            new() // 4 - Dishwasher
            {
                RatedPower = Power.FromKilowatts(1.8),
                Usages = 
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(150),
                        Baseline = (115, 129),
                    },
                },
            },
            new() // 5 - Breadmaker
            {
                RatedPower = Power.FromKilowatts(1.5),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(150),
                        Baseline = (117, 131),
                    },
                },
            },
            new() // 6 - Stove
            {
                RatedPower = Power.FromKilowatts(2),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(30),
                        Baseline = (31, 33),
                    },
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(50),
                        Baseline = (112, 116),
                    },
                },
            },
            new() // 7 - pump
            {
                RatedPower = Power.FromKilowatts(0.75),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(120),
                        Baseline = (103, 114),
                    },
                },
            },
            new() // 8 - Space heating
            {
                RatedPower = Power.FromKilowatts(2.4),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(120),
                        Baseline = (108, 119),
                    },
                },
            },
            new() // 9 - EWH
            {
                RatedPower = Power.FromKilowatts(3),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(120),
                        Baseline = (30, 41),
                    },
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(120),
                        Baseline = (103, 114),
                    },
                },
            },
            new() // 10 - Washing machine
            {
                RatedPower = Power.FromKilowatts(2),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(60),
                        Baseline = (108, 113),
                    },
                },
            },
            new() // 11 - Clothes dryer
            {
                RatedPower = Power.FromKilowatts(2),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(30),
                        Baseline = (115, 117),
                    },
                },
            },
            new() // 12 - Television
            {
                RatedPower = Power.FromKilowatts(0.133),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(180),
                        Baseline = (103, 120),
                    },
                },
            },
            new() // 13 - DVD player
            {
                RatedPower = Power.FromKilowatts(0.025),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(180),
                        Baseline = (103, 120),
                    },
                },
            },
            new() // 14 - Decoder
            {
                RatedPower = Power.FromKilowatts(0.07),
                Usages =
                {
                    new()
                    {
                        Duration = TimeSpan.FromMinutes(180),
                        Baseline = (103, 120),
                    },
                },
            },
        };
        var numberOfAppliances = appliances.Length;
        var timeStep = TimeSpan.FromMinutes(10);
        var numberOfSteps = (int) (TimeSpan.FromHours(24) / timeStep);
        TimeOnly GetTimeFromStep(int step)
        {
            var timePastMidnight = timeStep * step;
            return new TimeOnly(timePastMidnight.Hours, timePastMidnight.Minutes);
        }

        var tariffs = Enumerable
            .Range(0, numberOfSteps)
            .Select(x =>
            {
                var time = GetTimeFromStep(x);
                if (
                    (time >= new TimeOnly(7, 0) && time < new TimeOnly(10, 0))
                    || (time >= new TimeOnly(18, 0) && time < new TimeOnly(20, 0)))
                {
                    return 1.7487;
                }
                else
                {
                    return 0.5510;
                }
            })
            .ToList();
        double batteryChargeEfficiency = 0.75;
        double batteryDischargeEfficiency = 1.0;
        double maxPrize = 25;
        Energy initialBatterySoC = Energy.FromKilowattHours(5);
        Energy minimumBatterySoC = Energy.FromKilowattHours(5);
        Energy maximumBatterySoC = Energy.FromKilowattHours(10);
        Power batteryChargingPower = Power.FromKilowatts(5);
        Power batteryDischargingPower = Power.FromKilowatts(5);
        Power maximumGridPower = Power.FromKilowatts(5.2);

        var numberOfApplianceVariables = appliances.Select(x =>
        {
            var power = 1;
            var durations = 1;
            //var usages = x.Usages.Select(u =>
            //{
            //    var duration = 1;
            //    return duration;
            //}).Sum();
            return power + durations;
        }).Sum();
        var numberOfBaselineVariables = numberOfSteps * numberOfAppliances;
        var numberOfSwitchStatusVariables = numberOfSteps * numberOfAppliances;
        var numberOfBatterySoCVariables = numberOfSteps;
        var numberOfBatteryPowerVariables = numberOfSteps * 2;
        var numberOfGridPowerVariables = numberOfSteps;
        var numberOfTotalAppliancePowerVariables = numberOfSteps;
        var numberOfTotalGridPowerVariables = numberOfSteps;
        var numberOfStaticVariables = 7; //battery charge eff, discharge eff, max-cost, inital battery SoC, min battery cap, max battery cap, max grid power
        var numberOfVariables = numberOfApplianceVariables + numberOfBaselineVariables + numberOfSwitchStatusVariables
                                + numberOfBatterySoCVariables + numberOfBatteryPowerVariables
                                + numberOfGridPowerVariables
                                + numberOfTotalAppliancePowerVariables + numberOfTotalGridPowerVariables
                                + numberOfStaticVariables;
        using var solver = LpSolve.make_lp(0, numberOfVariables);
        for (int a = 0; a < numberOfAppliances; a += 1)
        {
            var ratedPowerConstraint = new double[numberOfVariables];
            ratedPowerConstraint[2 * a] = 1;
            solver.add_constraint(ratedPowerConstraint, lpsolve_constr_types.EQ, appliances[a].RatedPower.Kilowatts);
            var durationConstraint = new double[numberOfVariables];
            durationConstraint[2 * a + 1] = 1;
            solver.add_constraint(durationConstraint, lpsolve_constr_types.EQ, appliances[a].Usages[0].Duration.TotalMinutes);
            var (blStart, blStop) = appliances[a].Usages[0].Baseline;
            for (int i = 0; i < numberOfSteps; i += 1)
            {
                var baselineConstraint = new double[numberOfVariables];
                baselineConstraint[numberOfApplianceVariables + numberOfSteps * a + i] = 1;
                var isBaseline = blStart <= i && i < blStop;
                solver.add_constraint(baselineConstraint, lpsolve_constr_types.EQ, isBaseline ? 1 : 0);
            }
        }


        int index = 1;
        IReadOnlyList<LpVariable> CreateVariables(int count)
        {
            return Enumerable.Range(0, count).Select(_ => new LpVariable(index++)).ToList();
        }

        var powerVariables = CreateVariables(numberOfAppliances);
        var durationVariables = CreateVariables(numberOfAppliances);
        var additionalRuntimeVariables = CreateVariables(numberOfAppliances);
        var baselineVariables = new LpVariable[numberOfAppliances, numberOfSteps];
        var switchVariables = new LpVariable[numberOfAppliances, numberOfSteps];
        var batteryChargingEfficiencyVariable = new LpVariable(index++);
        var batteryDischargingEfficiencyVariable = new LpVariable(index++);
        var maxCostVariable = new LpVariable(index++);
        var initialBatterySoCVariable = new LpVariable(index++);
        var minBatteryCapVariable = new LpVariable(index++);
        var maxBatteryCapVariable = new LpVariable(index++);
        var maxGridPowerVariable = new LpVariable(index++);
        var gridPowerVariables = CreateVariables(numberOfSteps);
        var totalAppliancesPowerVariables = CreateVariables(numberOfSteps);
        var largestPowerPeakVariable = new LpVariable(index++);
        var electricityPriceVariables = CreateVariables(numberOfSteps);
        for (int i = 0; i < numberOfAppliances; i += 1)
        {
            for (int t = 0; t < numberOfSteps; t += 1)
            {
                baselineVariables[i, t] = new LpVariable(index++);
                switchVariables[i, t] = new LpVariable(index++);
            }
        }
        var builder = new LpSolveDotNet.LpSolveDotNet(LpSolve.make_lp(0, index));
        for (int i = 0; i < numberOfAppliances; i += 1)
        {
            builder.AddConstraint(powerVariables[i] >= 0);
            builder.AddConstraint(powerVariables[i] <= appliances[i].RatedPower.Kilowatts);
            //builder.AddConstraint(powerVariables[i], LpConstraintType.Equal, appliances[i].RatedPower.Kilowatts);
            //builder.AddConstraint(durationVariables[i], LpConstraintType.Equal,
            //    appliances[i].Usages[0].Duration.TotalMinutes);
            var (blStart, blStop) = appliances[i].Usages[0].Baseline;
            for (int t = 0; t < numberOfSteps; t += 1)
            {
                var isBaseline = blStart <= t && t < blStop;
                builder.AddConstraint(baselineVariables[i, t] == (isBaseline ? 1d : 0d));
            }
        }

        for (int t = 0; t < numberOfSteps; t += 1)
        {
            builder.AddConstraint(switchVariables[9, t] + switchVariables[10, t] <= 1);
        }
    }
}

public class Appliance
{
    public Power RatedPower { get; set; }

    public IList<ApplianceUsage> Usages { get; } = new List<ApplianceUsage>();

    public TimeSpan AdditionalRunTime { get; set; }
}

public class ApplianceUsage
{
    public TimeSpan Duration { get; set; }

    public (int, int) Baseline { get; set; }
}