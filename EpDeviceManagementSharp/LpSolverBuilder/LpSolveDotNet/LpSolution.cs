namespace LpSolveDotNet;

public struct LpSolution
{
    public LpSolution(ILpSolve iSolver, lpsolve_return result)
    {
        ISolver = iSolver;
        Result = result;
    }

    public ILpSolve ISolver { get; }

    public LpSolve UnderlyingSolver => this.ISolver.UnderlyingSolver;

    public lpsolve_return Result { get; }


#region Solution

    public double get_constr_value(int row, int count, double[] primsolution, int[] nzindex)
        => this.UnderlyingSolver.get_constr_value(row, count, primsolution, nzindex);

    public bool get_constraints(double[] constr)
        => this.UnderlyingSolver.get_constraints(constr);

    public bool get_dual_solution(double[] rc)
        => this.UnderlyingSolver.get_dual_solution(rc);

    public int MaxBranchAndBoundLevel => this.UnderlyingSolver.get_max_level();

    public double ObjectiveFunctionValue => this.UnderlyingSolver.get_objective();

    public bool get_primal_solution(double[] pv)
        => this.UnderlyingSolver.get_primal_solution(pv);

    public bool get_sensitivity_obj(double[] objfrom, double[] objtill)
        => this.UnderlyingSolver.get_sensitivity_obj(objfrom, objtill);

    public bool get_sensitivity_objex(double[] objfrom, double[] objtill, double[] objfromvalue, double[] objtillvalue)
        => this.UnderlyingSolver.get_sensitivity_objex(objfrom, objtill, objfromvalue, objtillvalue);

    public bool get_sensitivity_rhs(double[] duals, double[] dualsfrom, double[] dualstill)
        => this.UnderlyingSolver.get_sensitivity_rhs(duals, dualsfrom, dualstill);

    public int SolutionCount => this.UnderlyingSolver.get_solutioncount();

    public long TotalIterations => this.UnderlyingSolver.get_total_iter();

    public long TotalNodes => this.UnderlyingSolver.get_total_nodes();

    public double get_var_primalresult(int index)
        => this.UnderlyingSolver.get_var_primalresult(index);

    public bool GetVariables(double[] variables)
        => this.UnderlyingSolver.get_variables(variables);

    public double WorkingObjective => this.UnderlyingSolver.get_working_objective();

    public bool IsFeasible(double[] values, double threshold)
        => this.UnderlyingSolver.is_feasible(values, threshold);

#endregion
}