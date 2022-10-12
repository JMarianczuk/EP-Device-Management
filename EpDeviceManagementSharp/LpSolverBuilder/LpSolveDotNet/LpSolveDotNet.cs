using System;
using LpSolveDotNet;

namespace LpSolverBuilder.LpSolveDotNet;

public class LpSolveDotNet : ILpSolve
{
    public LpSolveDotNet(LpSolve solver)
    {
        UnderlyingSolver = solver;
        this.Columns = new ColumnContainer(this);
        this.Rows = new RowContainer(this);
    }

    public LpSolve UnderlyingSolver { get; }

#region Create / destroy model

    public ILpSolve Copy()
        => new LpSolveDotNet(this.UnderlyingSolver.copy_lp());

    public void Delete() => this.UnderlyingSolver.delete_lp();

    public void Dispose() => UnderlyingSolver?.Dispose();

#endregion

#region Build model

#region Column

    public bool AddColumn(double[] column)
        => this.UnderlyingSolver.add_column(column);

    public bool AddColumnEx(int count, double[] column, int[] rowNumbers)
        => this.UnderlyingSolver.add_columnex(count, column, rowNumbers);

    public bool AddColumnFromString(string columnString)
        => this.UnderlyingSolver.str_add_column(columnString);

    public bool RemoveColumn(int columnNumber)
        => this.UnderlyingSolver.del_column(columnNumber);

    public IColumnContainer Columns { get; }

#endregion

#region Constraint / Row

    public bool add_constraint(double[] row, lpsolve_constr_types constr_type, double rh)
        => this.UnderlyingSolver.add_constraint(row, constr_type, rh);

#if NET20
    private int[] columnNumbers = new int[0];
    private double[] row = new double[0];
#else
    private int[] columnNumbers = Array.Empty<int>();
    private double[] row = Array.Empty<double>();
#endif

    internal bool AddConstraint(LpSummand summand, lpsolve_constr_types constraintType, double rightHandSide)
    {
        this.EnsureLength(1);
        row[0] = summand.Factor;
        columnNumbers[0] = summand.VariableColumnNumber;
        return this.UnderlyingSolver.add_constraintex(1, row, columnNumbers, constraintType, rightHandSide);
    }

    internal bool AddConstraint(LpSum sum, lpsolve_constr_types constraintType, double rightHandSide)
    {
        var length = ExtractColumnNumbers(sum);
        return this.UnderlyingSolver.add_constraintex(length, row, columnNumbers, constraintType, rightHandSide);
    }

    private void EnsureLength(int length)
    {
        if (columnNumbers.Length < length)
        {
            columnNumbers = new int[length];
            row = new double[length];
        }
    }

    private int ExtractColumnNumbers(LpSum sum)
    {
        int length = 1;
#if NET20
        length = sum.SummandsCount;
#else
        length = sum.Summands.Count;
#endif
        this.EnsureLength(length);
        
#if NET20
        int i = 0;
        foreach (var summand in sum.Summands)
        {
            columnNumbers[i] = summand.VariableColumnNumber;
            row[i] = summand.Factor;
            i += 1;
        }
#else
        for (int i = 0; i < length; i += 1)
        {
            columnNumbers[i] = sum.Summands[i].VariableColumnNumber;
            row[i] = sum.Summands[i].Factor;
        }
#endif

        return length;
    }

    public bool AddConstraint(LpSummandConstraint constraint)
        => this.AddConstraint(constraint.Summand, constraint.ConstraintType, constraint.RightHandSide);

    public bool AddConstraint(LpSumConstraint constraint)
        => this.AddConstraint(constraint.Sum, constraint.ConstraintType, constraint.RightHandSide);

    public bool str_add_constraint(string row_string, lpsolve_constr_types constr_type, double rh)
        => this.UnderlyingSolver.str_add_constraint(row_string, constr_type, rh);

    public bool DeleteConstraint(int rowToDelete)
        => this.UnderlyingSolver.del_constraint(rowToDelete);

    public IRowContainer Rows { get; }

    public bool SetRow(int rowNumber, LpSummand singleSummand)
    {
        this.EnsureLength(1);
        row[0] = singleSummand.Factor;
        columnNumbers[0] = singleSummand.VariableColumnNumber;
        return this.UnderlyingSolver.set_rowex(rowNumber, 1, row, columnNumbers);
    }

    public bool SetRow(int rowNumber, LpSum term)
    {
        var length = this.ExtractColumnNumbers(term);
        return this.UnderlyingSolver.set_rowex(rowNumber, length, row, columnNumbers);
    }

    public void set_rh_vec(double[] rh)
        => this.UnderlyingSolver.set_rh_vec(rh);

    public bool str_set_rh_vec(string rh_string)
        => this.UnderlyingSolver.str_set_rh_vec(rh_string);

#endregion

#region Objective

    public double ObjectiveBound
    {
        get => this.UnderlyingSolver.get_obj_bound();
        set => this.UnderlyingSolver.set_obj_bound(value);
    }

    public bool set_obj(int column, double value)
        => this.UnderlyingSolver.set_obj(column, value);

    public bool set_obj_fn(double[] row)
        => this.UnderlyingSolver.set_obj_fn(row);

    public bool SetObjectiveFunction(LpSummand singleSummand)
    {
        this.EnsureLength(1);
        row[0] = singleSummand.Factor;
        columnNumbers[0] = singleSummand.VariableColumnNumber;
        return this.UnderlyingSolver.set_obj_fnex(1, row, columnNumbers);
    }

    public bool SetObjectiveFunction(LpSum term)
    {
        var length = this.ExtractColumnNumbers(term);
        return this.UnderlyingSolver.set_obj_fnex(length, row, columnNumbers);
    }

    public bool str_set_obj_fn(string row_string)
        => this.UnderlyingSolver.str_set_obj_fn(row_string);

    public bool IsMaximize
    {
        get => this.UnderlyingSolver.is_maxim();
        set
        {
            if (value)
            {
                this.UnderlyingSolver.set_maxim();
            }
            else
            {
                this.UnderlyingSolver.set_minim();
            }
        }
    }

    public void SetSense(bool maximize)
        => this.UnderlyingSolver.set_sense(maximize);

#endregion

    public string LpName
    {
        get => this.UnderlyingSolver.get_lp_name();
        set => this.UnderlyingSolver.set_lp_name(value); //TODO bool return value
    }

    public bool ResizeLp(int rows, int columns)
        => this.UnderlyingSolver.resize_lp(rows, columns);

    public bool IsAddRowmode
    {
        get => this.UnderlyingSolver.is_add_rowmode();
        set => this.UnderlyingSolver.set_add_rowmode(value); //TODO bool return value
    }

    public int GetNameIndex(string name, bool isRow)
        => this.UnderlyingSolver.get_nameindex(name, isRow);

    public double Infinity
    {
        get => this.UnderlyingSolver.get_infinite();
        set => this.UnderlyingSolver.set_infinite(value);
    }

    public bool IsInfinity(double value)
        => this.UnderlyingSolver.is_infinite(value);

    public double this[int row, int column]
    {
        get => this.UnderlyingSolver.get_mat(row, column);
        set => this.UnderlyingSolver.set_mat(row, column, value); // TODO bool return value
    }

    public bool BoundsTighter
    {
        get => this.UnderlyingSolver.get_bounds_tighter();
        set => this.UnderlyingSolver.set_bounds_tighter(value);
    }

    public bool set_var_weights(double[] weights)
        => this.UnderlyingSolver.set_var_weights(weights);

    public int add_SOS(string name, int sostype, int priority, int count, int[] sosvars, double[] weights)
        => this.UnderlyingSolver.add_SOS(name, sostype, priority, count, sosvars, weights);

#endregion

#region SolverSettings

#region Epsilon / Tolerance

    public double RightHandSideTolerance
    {
        get => this.UnderlyingSolver.get_epsb();
        set => this.UnderlyingSolver.set_epsb(value);
    }

    public double ReducedCostsTolerance
    {
        get => this.UnderlyingSolver.get_epsd();
        set => this.UnderlyingSolver.set_epsd(value);
    }

    public double RoundingValuesTolerance
    {
        get => this.UnderlyingSolver.get_epsel();
        set => this.UnderlyingSolver.set_epsel(value);
    }

    public double IntegerTolerance
    {
        get => this.UnderlyingSolver.get_epsint();
        set => this.UnderlyingSolver.set_epsint(value);
    }

    public double PerturbationScalar
    {
        get => this.UnderlyingSolver.get_epsperturb();
        set => this.UnderlyingSolver.set_epsperturb(value);
    }

    public double PivotElementTolerance
    {
        get => this.UnderlyingSolver.get_epspivot();
        set => this.UnderlyingSolver.set_epspivot(value);
    }

    public void SetMipGap(bool absolute, double mipGap)
        => this.UnderlyingSolver.set_mip_gap(absolute, mipGap);

    public double GetMipGap(bool absolute)
        => this.UnderlyingSolver.get_mip_gap(absolute);

    public bool set_epslevel(lpsolve_epsilon_level level)
        => this.UnderlyingSolver.set_epslevel(level);

#endregion

#region Basis

    public void ResetBasis() => this.UnderlyingSolver.reset_basis();

    public void DefaultBasis() => this.UnderlyingSolver.default_basis();

    public bool read_basis(string filename, string info)
        => this.UnderlyingSolver.read_basis(filename, info);

    public bool write_basis(string filename)
        => this.UnderlyingSolver.write_basis(filename);

    public bool set_basis(int[] bascolumn, bool nonbasic)
        => this.UnderlyingSolver.set_basis(bascolumn, nonbasic);

    public bool get_basis(int[] bascolumn, bool nonbasic)
        => this.UnderlyingSolver.get_basis(bascolumn, nonbasic);

    public bool guess_basis(double[] guessvector, int[] basisvector)
        => this.UnderlyingSolver.guess_basis(guessvector, basisvector);
    
    public lpsolve_basiscrash BasisCrash
    {
        get => this.UnderlyingSolver.get_basiscrash();
        set => this.UnderlyingSolver.set_basiscrash(value);
    }

    public bool HasBasisFactorizationPackage => this.UnderlyingSolver.has_BFP();

    public bool IsNativeBasisFactorizationPackage => this.UnderlyingSolver.is_nativeBFP();

    public bool SetBasisFactorizationPackage(string fileName)
        => this.UnderlyingSolver.set_BFP(fileName);

#endregion

#region Pivoting

    public int MaximumNumberOfPivots
    {
        get => this.UnderlyingSolver.get_maxpivot();
        set => this.UnderlyingSolver.set_maxpivot(value);
    }

    public PivotRuleAndModes Pivoting
    {
        get => this.UnderlyingSolver.get_pivoting();
        set => this.UnderlyingSolver.set_pivoting(value.Rule, value.Modes);
    }

    public bool IsPivotingRuleActive(lpsolve_pivot_rule rule)
        => this.UnderlyingSolver.is_piv_rule(rule);

    public bool IsPivotingModeSpecified(lpsolve_pivot_modes testMask)
        => this.UnderlyingSolver.is_piv_mode(testMask);

#endregion

#region Scaling

    public double ScaleLimit
    {
        get => this.UnderlyingSolver.get_scalelimit();
        set => this.UnderlyingSolver.set_scalelimit(value);
    }

    public ScalingAlgorithmAndParameters Scaling
    {
        get => this.UnderlyingSolver.get_scaling();
        set => this.UnderlyingSolver.set_scaling(value.Algorithm, value.Parameters);
    }

    public bool is_scalemode(
        lpsolve_scale_algorithm algorithmMask = lpsolve_scale_algorithm.SCALE_NONE,
        lpsolve_scale_parameters parameterMask = lpsolve_scale_parameters.SCALE_NONE)
        => this.UnderlyingSolver.is_scalemode(algorithmMask, parameterMask);

    public bool is_scaletype(lpsolve_scale_algorithm algorithm)
        => this.UnderlyingSolver.is_scalemode(algorithm);

    public bool IsIntegerScaling => this.UnderlyingSolver.is_integerscaling();

    public void Unscale() => this.UnderlyingSolver.unscale();

    #endregion

#region Branching

    public bool BreakAtFirst
    {
        get => this.UnderlyingSolver.is_break_at_first();
        set => this.UnderlyingSolver.set_break_at_first(value);
    }

    public double BreakAtValue
    {
        get => this.UnderlyingSolver.get_break_at_value();
        set => this.UnderlyingSolver.set_break_at_value(value);
    }

    public lpsolve_BBstrategies BranchAndBoundRule
    {
        get => this.UnderlyingSolver.get_bb_rule();
        set => this.UnderlyingSolver.set_bb_rule(value);
    }

    public int BranchAndBoundDepthLimit
    {
        get => this.UnderlyingSolver.get_bb_depthlimit();
        set => this.UnderlyingSolver.set_bb_depthlimit(value);
    }

    public lpsolve_branch BranchAndBoundFloorFirst
    {
        get => this.UnderlyingSolver.get_bb_floorfirst();
        set => this.UnderlyingSolver.set_bb_floorfirst(value);
    }

    public void SetBranchAndBoundNodeFunction(ILpSolve.BranchAndBoundNodeSelector nodeSelector)
        => this.UnderlyingSolver.put_bb_nodefunc(_ => nodeSelector());

    public void SetBranchAndBoundBranchFunction(ILpSolve.BranchAndBoundBranchSelector branchSelector)
        => this.UnderlyingSolver.put_bb_branchfunc((_, columnNumber) => branchSelector(new LpColumn(columnNumber, this)));

#endregion

    public lpsolve_improves IterativeImprovementLevel
    {
        get => this.UnderlyingSolver.get_improve();
        set => this.UnderlyingSolver.set_improve(value);
    }

    public double NegativeRange
    {
        get => this.UnderlyingSolver.get_negrange();
        set => this.UnderlyingSolver.set_negrange(value);
    }

    public lpsolve_anti_degen AntiDegeneracyRule
    {
        get => this.UnderlyingSolver.get_anti_degen();
        set => this.UnderlyingSolver.set_anti_degen(value);
    }

    public bool IsAntiDegeneracy(lpsolve_anti_degen testMask)
        => this.UnderlyingSolver.is_anti_degen(testMask);

    public void ResetParameters() => this.UnderlyingSolver.reset_params();

    public bool read_params(string filename, string options)
        => this.UnderlyingSolver.read_params(filename, options);

    public bool write_params(string filename, string options)
        => this.UnderlyingSolver.write_params(filename, options);

    public lpsolve_simplextypes SimplexType
    {
        get => this.UnderlyingSolver.get_simplextype();
        set => this.UnderlyingSolver.set_simplextype(value);
    }

    public bool PreferDual
    {
        set => this.UnderlyingSolver.set_preferdual(value);
    }

    public int SolutionLimit
    {
        get => this.UnderlyingSolver.get_solutionlimit();
        set => this.UnderlyingSolver.set_solutionlimit(value);
    }

    public TimeSpan Timeout
    {
        get => TimeSpan.FromSeconds(this.UnderlyingSolver.get_timeout());
        set => this.UnderlyingSolver.set_timeout((int)value.TotalSeconds);
    }

    public bool IsUseNames(bool isRow) => this.UnderlyingSolver.is_use_names(isRow);

    public void SetUseNames(bool isRow, bool useNames)
        => this.UnderlyingSolver.set_use_names(isRow, useNames);

    public bool IsPresolve(lpsolve_presolve testMask)
        => this.UnderlyingSolver.is_presolve(testMask);

    public lpsolve_presolve Presolve => this.UnderlyingSolver.get_presolve();

    public int PresolveLoops => this.UnderlyingSolver.get_presolveloops();

    public void SetPresolve(lpsolve_presolve doPresolve, int maxLoops)
        => this.UnderlyingSolver.set_presolve(doPresolve, maxLoops);

#endregion

#region Callback methods

    public void SetAbortFunction(ILpSolve.ControlFunction controlFunction)
        => this.UnderlyingSolver.put_abortfunc((_, __) => controlFunction(), IntPtr.Zero);

    public void SetLogFunction(ILpSolve.LogFunction logFunction)
        => this.UnderlyingSolver.put_logfunc((_, __, text) => logFunction(text), IntPtr.Zero);

    public void SetMessageFunction(ILpSolve.MessageFunction messageFunction)
        => this.UnderlyingSolver.put_msgfunc((_, __, ___) => messageFunction(), IntPtr.Zero, lpsolve_msgmask.MSG_NONE);

#endregion

#region Solve

    public LpSolution Solve() => new LpSolution(this, this.UnderlyingSolver.solve());

#endregion

#region Debug/print settings

    public bool SetOutputFile(string fileName)
        => this.UnderlyingSolver.set_outputfile(fileName);

    public lpsolve_print_sol_option PrintSolutions
    {
        get => this.UnderlyingSolver.get_print_sol();
        set => this.UnderlyingSolver.set_print_sol(value);
    }

    public lpsolve_verbosity Verbosity
    {
        get => this.UnderlyingSolver.get_verbose();
        set => this.UnderlyingSolver.set_verbose(value);
    }

    public bool IsDebug
    {
        get => this.UnderlyingSolver.is_debug();
        set => this.UnderlyingSolver.set_debug(value);
    }

    public bool Trace
    {
        get => this.UnderlyingSolver.is_trace();
        set => this.UnderlyingSolver.set_trace(value);
    }

#endregion

#region Debug/print

    public void PrintConstraints(int columns)
        => this.UnderlyingSolver.print_constraints(columns);

    public bool PrintDebugDump(string fileName)
        => this.UnderlyingSolver.print_debugdump(fileName);

    public void PrintDuals() => this.UnderlyingSolver.print_duals();

    public void PrintLp() => this.UnderlyingSolver.print_lp();

    public void PrintObjective() => this.UnderlyingSolver.print_objective();

    public void PrintScales() => this.UnderlyingSolver.print_scales();

    public void PrintSolution(int columns)
        => this.UnderlyingSolver.print_solution(columns);

    public void PrintString(string text)
        => this.UnderlyingSolver.print_str(text);

    public void PrintTableau() => this.UnderlyingSolver.print_tableau();

#endregion

#region Write model to file

    public bool WriteLp(string fileName)
        => this.UnderlyingSolver.write_lp(fileName);

    public bool WriteFreeMps(string fileName)
        => this.UnderlyingSolver.write_freemps(fileName);

    public bool WriteMps(string fileName)
        => this.UnderlyingSolver.write_mps(fileName);

    public bool IsNativeExternalLanguageInterface => this.UnderlyingSolver.is_nativeXLI();

    public bool HasExternalLanguageInterface => this.UnderlyingSolver.has_XLI();

    public bool SetExternalLanguageInterface(string fileName)
        => this.UnderlyingSolver.set_XLI(fileName); 

    public bool WriteToExternalLanguageInterface(string fileName, string options, bool results)
        => this.UnderlyingSolver.write_XLI(fileName, options, results);

#endregion

#region Miscellaneous methods

    public int ColumnInLp(double[] column)
        => this.UnderlyingSolver.column_in_lp(column);

    public bool DualizeLp()
        => this.UnderlyingSolver.dualize_lp();

    public int NonZeroEntries => this.UnderlyingSolver.get_nonzeros();

    public int NumberOfColumns => this.UnderlyingSolver.get_Ncolumns();

    public int NumberOfRows => this.UnderlyingSolver.get_Nrows();

    public int NumberOfOriginalColumns => this.UnderlyingSolver.get_Norig_columns();

    public int NumberOfOriginalRows => this.UnderlyingSolver.get_Norig_rows();

    public int MostRecentStatus => this.UnderlyingSolver.get_status();

    public string MostRecentStatusText => this.UnderlyingSolver.get_statustext(this.MostRecentStatus);

    public TimeSpan TimeElapsed => TimeSpan.FromSeconds(this.UnderlyingSolver.time_elapsed());

    public int GetLpIndex(int originalIndex) => this.UnderlyingSolver.get_lp_index(originalIndex);

    public int GetOriginalIndex(int lpIndex) => this.UnderlyingSolver.get_orig_index(lpIndex);

#endregion
}

public sealed class RowContainer : IRowContainer
{
    private readonly LpSolveDotNet solver;

    public RowContainer(LpSolveDotNet solver)
    {
        this.solver = solver;
    }

    public int Count => this.solver.NumberOfRows;

    public LpRow this[int rowNumber]
        => new LpRow(rowNumber, this.solver);
}

public sealed class ColumnContainer : IColumnContainer
{
    private readonly LpSolveDotNet solver;

    public ColumnContainer(LpSolveDotNet solver)
    {
        this.solver = solver;
    }

    public int Count => this.solver.NumberOfColumns;

    public LpColumn this[int columnNumber]
        => new LpColumn(columnNumber, this.solver);

    public LpColumn this[LpVariable variable]
        => this[variable.ColumnNumber];

    public void Add(double[] column)
        => this.solver.AddColumn(column);

    public void AddEx(int count, double[] column, int[] rowNumbers) =>
        this.solver.AddColumnEx(count, column, rowNumbers);

    public void AddFromString(string columnString)
        => this.solver.AddColumnFromString(columnString);

    public void Remove(int columnNumber)
        => this.solver.RemoveColumn(columnNumber);
}