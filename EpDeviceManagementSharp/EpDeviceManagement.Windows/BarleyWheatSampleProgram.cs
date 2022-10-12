using LpSolveDotNet;
using LpSolverBuilder.LpSolveDotNet;

namespace EpDeviceManagement.Windows;

public class BarleyWheatSampleProgram
{
    public static void Solve()
    {
        const int NumberOfColumns = 2;

        LpSolve.Init();
        using var solver = LpSolve.make_lp(0, NumberOfColumns);

        solver.set_col_name(1, "x");
        solver.set_col_name(2, "y");
        solver.set_add_rowmode(true);

        var columnNumber = new int[NumberOfColumns];
        var row = new double[NumberOfColumns];

        columnNumber[0] = 1;
        columnNumber[1] = 2;

        row[0] = 120;
        row[1] = 210;
        solver.add_constraintex(2, row, columnNumber, lpsolve_constr_types.LE, 15000);

        row[0] = 110;
        row[1] = 30;
        solver.add_constraintex(2, row, columnNumber, lpsolve_constr_types.LE, 4000);

        row[0] = 1;
        row[1] = 1;
        solver.add_constraintex(2, row, columnNumber, lpsolve_constr_types.LE, 75);
        solver.set_add_rowmode(false);

        row[0] = 143;
        row[1] = 60;
        solver.set_obj_fnex(2, row, columnNumber);
        solver.set_maxim();

        solver.write_lp("created_model.lp");
        solver.set_verbose(lpsolve_verbosity.IMPORTANT);

        var result = solver.solve();
        if (result == lpsolve_return.OPTIMAL)
        {
            solver.get_variables(row);
            for (int variable = 0; variable < NumberOfColumns; variable += 1)
            {
                Console.WriteLine(solver.get_col_name(variable + 1) + ": " + row[variable]);
            }
        }

        //var wheat = new LpVariable(1);
        //var barley = new LpVariable(2);
        //var builder = new LpSolveDotNet.LpSolveDotNet(LpSolve.make_lp(0, 2));
        //builder.AddConstraint(120 * wheat + 210 * barley, lpsolve_constr_types.LE, 15000);
        //builder.AddConstraint(110 * wheat + 30 * barley, lpsolve_constr_types.LE, 4000);
        //builder.AddConstraint(wheat + barley, lpsolve_constr_types.LE, 75);
        //builder.AddConstraint(wheat, lpsolve_constr_types.GE, 0);
        //builder.AddConstraint(barley, lpsolve_constr_types.GE, 0);
        //var builtSolver = builder.CreateSolver(143 * wheat + 60 * barley, true);
        //builtSolver.set_verbose(lpsolve_verbosity.IMPORTANT);
        //var builtResult = builtSolver.solve();
        //if (builtResult == lpsolve_return.OPTIMAL)
        //{
        //    var builtValues = new double[2];
        //    solver.get_variables(builtValues);
        //    if (builtValues[0] != row[0] || builtValues[1] != row[1])
        //    {
        //        Console.WriteLine("something went wrong");
        //    }
        //    else
        //    {
        //        Console.WriteLine("it worked!");
        //    }
        //}

        var wheat = new LpVariable(1);
        var barley = new LpVariable(2);
        var lp = new LpSolverBuilder.LpSolveDotNet.LpSolveDotNet(LpSolve.make_lp(0, 2));
        lp.SetObjectiveFunction(143 * wheat + 60 * barley);
        lp.AddConstraint(120 * wheat + 210 * barley <= 15000);
        lp.AddConstraint(110 * wheat + 30 * barley <= 4000);
        lp.AddConstraint(wheat + barley <= 75);
        lp.AddConstraint(wheat >= 0);
        lp.AddConstraint(barley >= 0);
        lp.Verbosity = lpsolve_verbosity.IMPORTANT;
        var solution = lp.Solve();
        if (solution.Result == lpsolve_return.OPTIMAL)
        {
            var builtValues = new double[2];
            solver.get_variables(builtValues);
            if (builtValues[0] != row[0] || builtValues[1] != row[1])
            {
                Console.WriteLine("something went wrong");
            }
            else
            {
                Console.WriteLine("it worked!");
            }
        }
    }
}