using System.Globalization;
using System.Runtime.CompilerServices;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Control.Extensions;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.Simulation.Storage;
using EpDeviceManagement.UnitsExtensions;
using Humanizer;
using LpSolveDotNet;
using LpSolverBuilder.LpSolveDotNet;
using UnitsNet;
using LpSolveWrap = LpSolverBuilder.LpSolveDotNet.LpSolveDotNet;

namespace EpDeviceManagement.Control.Strategy;

public class ModelPredictiveController2 : GuardedStrategy, IEpDeviceController, IDisposable
{
    private bool disposed = false;
    private readonly IStorage battery;
    private readonly Ratio targetBatteryState;
    private readonly IValuePredictor<Power> loadsPredictor;
    private readonly IValuePredictor<Power> generationPredictor;
    private readonly TimeSpan predictionHorizon;
    private readonly LpVariable[] loads;
    private readonly LpVariable[] generation;
    private readonly LpVariable current_battery;
    private readonly LpVariable packet_incoming;
    private readonly LpVariable packet_outgoing;
    private readonly double[] variables_buffer;

    static ModelPredictiveController2()
    {
        LpSolve.Init();
    }

    private void SetupPositiveNegativeSplit(
        LpVariable baseVariable,
        LpVariable positivePart,
        LpVariable negativePart,
        ILpSolve lp)
    {
        lp.AddConstraint(-baseVariable + positivePart - negativePart == 0);
        //var baseCol = lp.Columns[baseVariable];
        //var posCol = lp.Columns[positivePart];
        //var negCol = lp.Columns[negativePart];
        //posCol.SetBounds(0, baseCol.UpperBound);
        //posCol.SetBounds(0, -baseCol.LowerBound);
    }

    public ModelPredictiveController2(
        BatteryElectricStorage battery,
        Energy packetSize,
        Ratio targetBatteryState,
        IValuePredictor<Power> loadsPredictor,
        IValuePredictor<Power> generationPredictor,
        TimeSpan predictionHorizon)
        : base(
            new BatteryCapacityGuard(battery, packetSize),
            new BatteryPowerGuard(battery, packetSize),
            new OscillationGuard())
    {
        this.battery = battery;
        this.targetBatteryState = targetBatteryState;
        this.loadsPredictor = loadsPredictor;
        this.generationPredictor = generationPredictor;
        this.predictionHorizon = predictionHorizon;
        var timeStep = TimeSpan.FromMinutes(15);
        var steps = (int) (predictionHorizon / timeStep);
        var packetPower = packetSize / timeStep;
        int numberOfColumns = 0;
#if DEBUG
        Console.WriteLine($"MPC: {predictionHorizon:hh\\:mm}, {targetBatteryState}");
#endif
        LpVariable[] Create(int count)
        {
            var result = new LpVariable[count];
            for (int i = 0; i < count; i += 1)
            {
                numberOfColumns += 1;
                result[i] = new LpVariable(numberOfColumns);
            }

            return result;
        }
        LpVariable CreateSingle()
        {
            numberOfColumns += 1;
            var result = new LpVariable(numberOfColumns);
            return result;
        }
        
        var a = Create(steps);
        var a_plus = Create(steps);
        var a_minus = Create(steps);
        var x_b = Create(steps + 1);
        var x_abs = Create(steps);
        this.current_battery = x_b[0];
        this.loads = Create(steps);
        this.generation = Create(steps);
        var packet_in = Create(steps);
        this.packet_incoming = packet_in[0];
        var packet_out = Create(steps);
        this.packet_outgoing = packet_out[0];
        //var x_plus = CreateSingle();
        //var x_minus = CreateSingle();

        var lp = new LpSolveWrap(LpSolve.make_lp(0, numberOfColumns));
        this.variables_buffer = new double[numberOfColumns];
        lp.IsAddRowmode = true;

        for (int i = 0; i < steps; i += 1)
        {
            var load_col = lp.Columns[this.loads[i]];
            load_col.Name = $"loads[{i + 1}]";
            var gen_col = lp.Columns[this.generation[i]];
            gen_col.Name = $"generation[{i + 1}]";

            lp.AddConstraint(-a[i] + this.loads[i] - this.generation[i] + packetPower.Kilowatts * (packet_out[i] - packet_in[i]) == 0);
            var in_column = lp.Columns[packet_in[i]];
            in_column.IsBinary = true;
            in_column.Name = $"packet_in[{i + 1}]";
            var out_column = lp.Columns[packet_out[i]];
            out_column.IsBinary = true;
            out_column.Name = $"packet_out[{i + 1}]";

            lp.AddConstraint(packet_in[i] + packet_out[i] <= 1);
            var battery_column = lp.Columns[x_b[i]];
            battery_column.SetBounds(0, battery.TotalCapacity.KilowattHours);
            battery_column.Name = $"x_b[{i + 1}]";
            var x_abs_col = lp.Columns[x_abs[i]];
            x_abs_col.Name = $"x_abs[{i + 1}]";

            var power_column = lp.Columns[a[i]];
            power_column.SetBounds(-battery.MaximumChargePower.Kilowatts, battery.MaximumDischargePower.Kilowatts);
            power_column.Name = $"a[{i + 1}]";
            SetupPositiveNegativeSplit(a[i], a_plus[i], a_minus[i], lp);
            var pos_col = lp.Columns[a_plus[i]];
            pos_col.Name = $"a_plus[{i + 1}]";
            var neg_col = lp.Columns[a_minus[i]];
            neg_col.Name = $"a_minus[{i + 1}]";

            lp.AddConstraint(-x_b[i + 1]
                             + (1 - timeStep.TotalSeconds * battery.StandingLosses.PerSecond) * x_b[i]
                             //- timeStep.TotalHours * a[i]
                             // remove efficiencies because the plus/minus split did not work
                             + timeStep.TotalHours * (
                                 (double)battery.ChargingEfficiency * a_minus[i]
                                 - (double)battery.DischargingEfficiency * a_plus[i])
                             == 0);

            // diff = target - x_b
            // diff <= x_abs <=> target <= x_abs + x_b
            // -diff <= x_abs <=> -target <= x_abs - x_b
            var batteryTarget = battery.TotalCapacity.KilowattHours * targetBatteryState.DecimalFractions;
            lp.AddConstraint(x_abs[i] + x_b[i + 1] >= batteryTarget);
            lp.AddConstraint(x_abs[i] - x_b[i + 1] >= -batteryTarget);
        }
        // split distance from target into positive and negative
        // dist = x_b[steps] - target = x+ - x-;
        var last_battery_column = lp.Columns[x_b[steps]];
        last_battery_column.SetBounds(0, battery.TotalCapacity.KilowattHours);
        last_battery_column.Name = $"x_b[{steps + 1}]";
        //lp.AddConstraint(-x_b[steps] + x_plus - x_minus ==
        //                 -battery.TotalCapacity.KilowattHours * targetBatteryState.DecimalFractions);
        //var plus_col = lp.Columns[x_plus];
        //plus_col.Name = $"x_plus";
        //var minus_col = lp.Columns[x_minus];
        //minus_col.Name = $"x_minus";

        lp.IsAddRowmode = false;
        LpSum objectiveFunction = LpSum.Empty();
        for (int i = 0; i < steps; i += 1)
        {
            const double deltaFactor = 1;
            const double packet_in_factor = 0.1;
            const double packet_out_factor = 0.1;
            const double powerSplitFactor = 0.1;
            objectiveFunction +=
                deltaFactor * x_abs[i]
                //+ packet_in_factor * packet_in[i]
                //+ packet_out_factor * packet_out[i]
                + powerSplitFactor * (a_plus[i] + a_minus[i]);
        }
        lp.SetObjectiveFunction(objectiveFunction);
        lp.IsMaximize = false;
#if DEBUG
        lp.Verbosity = lpsolve_verbosity.FULL;
        lp.SetOutputFile("output.lp");
        //lp.SetOutputFile("");
#else
        lp.SetOutputFile("");
#endif

        this.LinearProgram = lp;
    }

    private ILpSolve LinearProgram { get; }

    public override string Name => nameof(ModelPredictiveController2);

    public override string Configuration => string.Create(CultureInfo.InvariantCulture, 
        $"{this.predictionHorizon:hh\\:mm} [{this.targetBatteryState.DecimalFractions:0.#}]");

    public override string PrettyConfiguration => $"{this.predictionHorizon:hh\\:mm} {this.targetBatteryState}";

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
        TransferResult lastTransferResult)
    {
        var predictedSteps = (int)(this.predictionHorizon / timeStep) - 1;
        var currentLoad = loads.Sum();
        var loadPredictions = this.loadsPredictor
            .Predict(predictedSteps, dataPoint)
            .Prepend(currentLoad)
            .ToList();
        var currentGeneration = generators.Sum();
        var generationPredictions = this.generationPredictor
            .Predict(predictedSteps, dataPoint)
            .Prepend(currentGeneration)
            .ToList();
        for (var i = 0; i < loadPredictions.Count; i++)
        {
            var loadPred = loadPredictions[i];
            var column = this.LinearProgram.Columns[this.loads[i]];
            column.SetBounds(loadPred.Kilowatts, loadPred.Kilowatts);
        }

        for (var i = 0; i < generationPredictions.Count; i++)
        {
            var genPred = generationPredictions[i];
            var column = this.LinearProgram.Columns[this.generation[i]];
            column.SetBounds(genPred.Kilowatts, genPred.Kilowatts);
        }

        var battery_column = this.LinearProgram.Columns[this.current_battery];
        battery_column.SetBounds(this.battery.CurrentStateOfCharge.KilowattHours,
            this.battery.CurrentStateOfCharge.KilowattHours);
#if DEBUG
        this.LinearProgram.UnderlyingSolver.write_lp("model.lp");
#endif
        var solution = this.LinearProgram.Solve();
        if (solution.Result == lpsolve_return.INFEASIBLE)
        {
            var targetBatterySoC = this.battery.TotalCapacity * this.targetBatteryState.DecimalFractions;
            if (this.battery.CurrentStateOfCharge > targetBatterySoC)
            {
                return ControlDecision.RequestTransfer.Outgoing;
            }
            else if (this.battery.CurrentStateOfCharge < targetBatterySoC)
            {
                return ControlDecision.RequestTransfer.Incoming;
            }
            else
            {
                return ControlDecision.NoAction.Instance;
            }
        }
        solution.GetVariables(this.variables_buffer);
        var packet_in = this.variables_buffer[this.packet_incoming.ColumnNumber - 1];
        var packet_out = this.variables_buffer[this.packet_outgoing.ColumnNumber - 1];
#if DEBUG
        string[] names = new string[this.variables_buffer.Length];
        for (int i = 0; i < names.Length; i += 1)
        {
            var col = this.LinearProgram.Columns[i + 1];
            names[i] = col.Name;
        }

        var zip = names.Zip(this.variables_buffer).ToList();
        if (dataPoint == 172)
        {
            int p = 5;
        }
        if (dataPoint == 4)
        {
            int p = 5;
        }

        var steps = (int)(this.predictionHorizon / timeStep);
        var in_start = this.packet_incoming.ColumnNumber - 1;
        var out_start = this.packet_outgoing.ColumnNumber - 1;
        var in_decisions = this.variables_buffer[in_start..(in_start + steps)];
        var out_decisions = this.variables_buffer[out_start..(out_start + steps)];
        Console.WriteLine($"d {dataPoint}, l {currentLoad}, g {currentGeneration}, b {this.battery}, in {string.Join(',', in_decisions)}, out {string.Join(',', out_decisions)}");
#endif
        if (packet_in != 0)
        {
            return ControlDecision.RequestTransfer.Incoming;
        }

        if (packet_out != 0)
        {
            return ControlDecision.RequestTransfer.Outgoing;
        }

        return ControlDecision.NoAction.Instance;
    }

    public void Dispose()
    {
        if (!this.disposed)
        {
            this.disposed = true;
            this.LinearProgram?.Dispose();
        }
    }
}