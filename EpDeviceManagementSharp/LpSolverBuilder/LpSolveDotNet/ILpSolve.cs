using System;
using System.Runtime.CompilerServices;

namespace LpSolveDotNet;

public interface ILpSolve : IDisposable
{
    LpSolve UnderlyingSolver { get; }

    ILpSolve Copy();

    void Delete();

#region Build model

#region Column

    IColumnContainer Columns { get; }

#endregion

#region Constraint / Row

    bool add_constraint(double[] row, lpsolve_constr_types constr_type, double rh);
    bool AddConstraint(LpSummandConstraint constraint);
    bool AddConstraint(LpSumConstraint constraint);
    bool str_add_constraint(string row_string, lpsolve_constr_types constr_type, double rh);
    bool DeleteConstraint(int rowToDelete);
    IRowContainer Rows { get; }
    bool SetRow(int rowNumber, LpSummand singleSummand);
    bool SetRow(int rowNumber, LpSum term);
    void set_rh_vec(double[] rh);
    bool str_set_rh_vec(string rh_string);

#endregion

#region Objective

    double ObjectiveBound { get; set; }
    bool set_obj(int column, double value);
    bool set_obj_fn(double[] row);
    bool SetObjectiveFunction(LpSummand singleSummand);
    bool SetObjectiveFunction(LpSum term);
    bool str_set_obj_fn(string row_string);
    bool IsMaximize { get; set; }
    void SetSense(bool maximize);

#endregion

    string LpName { get; set; }
    bool ResizeLp(int rows, int columns);
    bool IsAddRowmode { get; set; }
    int GetNameIndex(string name, bool isRow);
    double Infinity { get; set; }
    bool IsInfinity(double value);
    double this[int row, int column] { get; set; }
    bool BoundsTighter { get; set; }
    bool set_var_weights(double[] weights);

    int add_SOS(
        string name,
        int sostype,
        int priority,
        int count,
        int[] sosvars,
        double[] weights);

#endregion

#region SolverSettings

#region Epsilon / Tolerance

    double RightHandSideTolerance { get; set; }
    double ReducedCostsTolerance { get; set; }
    double RoundingValuesTolerance { get; set; }
    double IntegerTolerance { get; set; }
    double PerturbationScalar { get; set; }
    double PivotElementTolerance { get; set; }
    void SetMipGap(bool absolute, double mipGap);
    double GetMipGap(bool absolute);
    bool set_epslevel(lpsolve_epsilon_level level);

#endregion

#region Basis

    void ResetBasis();
    void DefaultBasis();
    bool read_basis(string filename, string info);
    bool write_basis(string filename);
    bool set_basis(int[] bascolumn, bool nonbasic);
    bool get_basis(int[] bascolumn, bool nonbasic);
    bool guess_basis(double[] guessvector, int[] basisvector);
    lpsolve_basiscrash BasisCrash { get; set; }
    bool HasBasisFactorizationPackage { get; }
    bool IsNativeBasisFactorizationPackage { get; }
    bool SetBasisFactorizationPackage(string fileName);

#endregion

#region Pivoting

    int MaximumNumberOfPivots { get; set; }
    PivotRuleAndModes Pivoting { get; set; }
    bool IsPivotingRuleActive(lpsolve_pivot_rule rule);
    bool IsPivotingModeSpecified(lpsolve_pivot_modes testMask);

#endregion

#region Scaling

    double ScaleLimit { get; set; }
    ScalingAlgorithmAndParameters Scaling { get; set; }
    bool is_scalemode(
        lpsolve_scale_algorithm algorithmMask = lpsolve_scale_algorithm.SCALE_NONE,
        lpsolve_scale_parameters parameterMask = lpsolve_scale_parameters.SCALE_NONE);
    bool is_scaletype(lpsolve_scale_algorithm algorithm);
    bool IsIntegerScaling { get; }
    void Unscale();

#endregion

#region Branching

    bool BreakAtFirst { get; set; }
    double BreakAtValue { get; set; }
    lpsolve_BBstrategies BranchAndBoundRule { get; set; }
    int BranchAndBoundDepthLimit { get; set; }
    lpsolve_branch BranchAndBoundFloorFirst { get; set; }

    delegate int BranchAndBoundNodeSelector();
    void SetBranchAndBoundNodeFunction(BranchAndBoundNodeSelector nodeSelector);

    delegate BranchSelectorResult BranchAndBoundBranchSelector(LpColumn column);
    void SetBranchAndBoundBranchFunction(BranchAndBoundBranchSelector branchSelector);

#endregion

    lpsolve_improves IterativeImprovementLevel { get; set; }
    double NegativeRange { get; set; }
    lpsolve_anti_degen AntiDegeneracyRule { get; set; }
    bool IsAntiDegeneracy(lpsolve_anti_degen testMask);
    void ResetParameters();
    bool read_params(string filename, string options);
    bool write_params(string filename, string options);
    lpsolve_simplextypes SimplexType { get; set; }
    bool PreferDual { set; }
    int SolutionLimit { get; set; }
    TimeSpan Timeout { get; set; }
    bool IsUseNames(bool isRow);
    void SetUseNames(bool isRow, bool useNames);
    bool IsPresolve(lpsolve_presolve testMask);
    lpsolve_presolve Presolve { get; }
    int PresolveLoops { get; }
    void SetPresolve(lpsolve_presolve doPresolve, int maxLoops);

#endregion

#region Callback methods

    delegate bool ControlFunction();
    void SetAbortFunction(ControlFunction controlFunction);

    delegate void LogFunction(string text);
    void SetLogFunction(LogFunction logFunction);

    delegate void MessageFunction();

    void SetMessageFunction(MessageFunction messageFunction);

#endregion

#region Solve

    LpSolution Solve();

#endregion

#region Debug/print settings

    bool SetOutputFile(string fileName);
    lpsolve_print_sol_option PrintSolutions { get; set; }
    lpsolve_verbosity Verbosity { get; set; }
    bool IsDebug { get; set; }
    bool Trace { get; set; }

#endregion

#region Debug/print

    void PrintConstraints(int columns);
    bool PrintDebugDump(string fileName);
    void PrintDuals();
    void PrintLp();
    void PrintObjective();
    void PrintScales();
    void PrintSolution(int columns);
    void PrintString(string text);
    void PrintTableau();

#endregion

#region Write model to file

    bool WriteLp(string fileName);
    bool WriteFreeMps(string fileName);
    bool WriteMps(string fileName);
    bool IsNativeExternalLanguageInterface { get; }
    bool HasExternalLanguageInterface { get; }
    bool SetExternalLanguageInterface(string fileName);
    bool WriteToExternalLanguageInterface(string fileName, string options, bool results);

#endregion

#region Miscellaneous methods

    int ColumnInLp(double[] column);
    bool DualizeLp();
    int NonZeroEntries { get; }
    int NumberOfOriginalColumns { get; }
    int NumberOfOriginalRows { get; }
    int MostRecentStatus { get; }
    string MostRecentStatusText { get; }
    TimeSpan TimeElapsed { get; }
    int GetLpIndex(int originalIndex);
    int GetOriginalIndex(int lpIndex);

#endregion
}

public interface IRowContainer
{
    int Count { get; }

    LpRow this[int rowNumber] { get; }
}

public interface IColumnContainer
{
    int Count { get; }

    LpColumn this[int columnNumber] { get; }

    void Add(double[] column);

    void AddEx(int count, double[] column, int[] rowNumbers);

    void AddFromString(string columnString);

    void Remove(int columnNumber);
}