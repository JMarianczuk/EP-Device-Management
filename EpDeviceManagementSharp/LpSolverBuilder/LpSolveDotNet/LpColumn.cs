namespace LpSolveDotNet;

public struct LpColumn
{
    public LpColumn(int columnNumber, ILpSolve iSolver)
    {
        ColumnNumber = columnNumber;
        ISolver = iSolver;
    }

    public int ColumnNumber { get; }

    public ILpSolve ISolver { get; }

    public LpSolve UnderlyingSolver => this.ISolver.UnderlyingSolver;

    public LpVariable SymbolicVariable => new LpVariable(this.ColumnNumber);

    #region Build model

#region Column

    public bool SetValues(double[] column)
        => this.UnderlyingSolver.set_column(this.ColumnNumber, column);

    public bool SetValuesEx(int count, double[] column, int[] rowno)
        => this.UnderlyingSolver.set_columnex(this.ColumnNumber, count, column, rowno);

    public bool GetValues(double[] column)
        => this.UnderlyingSolver.get_column(this.ColumnNumber, column);

    public int GetValuesEx(double[] column, int[] nzrow)
        => this.UnderlyingSolver.get_columnex(this.ColumnNumber, column, nzrow);

    public string Name
    {
        get => this.UnderlyingSolver.get_col_name(this.ColumnNumber);
        set => this.UnderlyingSolver.set_col_name(this.ColumnNumber, value); //TODO bool return value
    }

    public string OriginalName => this.UnderlyingSolver.get_origcol_name(this.ColumnNumber);

    public bool IsNegative => this.UnderlyingSolver.is_negative(this.ColumnNumber);

    public bool IsInteger
    {
        get => this.UnderlyingSolver.is_int(this.ColumnNumber);
        set => this.UnderlyingSolver.set_int(this.ColumnNumber, value); //TODO bool return value
    }

    public bool IsBinary
    {
        get => this.UnderlyingSolver.is_binary(this.ColumnNumber);
        set => this.UnderlyingSolver.set_binary(this.ColumnNumber, value); //TODO bool return value
    }

    public bool IsSemicontinuous
    {
        get => this.UnderlyingSolver.is_semicont(this.ColumnNumber);
        set => this.UnderlyingSolver.set_semicont(this.ColumnNumber, value); //TODO bool return value
    }

    public bool SetBounds(double lowerBound, double upperBound) => this.UnderlyingSolver.set_bounds(this.ColumnNumber, lowerBound, upperBound);

    public bool SetUnbounded() => this.UnderlyingSolver.set_unbounded(this.ColumnNumber);

    public bool IsUnbounded
    {
        get => this.UnderlyingSolver.is_unbounded(this.ColumnNumber);
    }

    public double LowerBound
    {
        get => this.UnderlyingSolver.get_upbo(this.ColumnNumber);
        set => this.UnderlyingSolver.set_upbo(this.ColumnNumber, value); //TODO bool return value
    }

    public double UpperBound
    {
        get => this.UnderlyingSolver.get_lowbo(this.ColumnNumber);
        set => this.UnderlyingSolver.set_lowbo(this.ColumnNumber, value); //TODO bool return value
    }

#endregion

    public int Priority => this.UnderlyingSolver.get_var_priority(this.ColumnNumber);

    public bool IsSpecialOrderedSet => this.UnderlyingSolver.is_SOS_var(this.ColumnNumber);

#endregion

#region Branching

    public lpsolve_branch Branch
    {
        get => this.UnderlyingSolver.get_var_branch(this.ColumnNumber);
        set => this.UnderlyingSolver.set_var_branch(this.ColumnNumber, value);
    }

#endregion

#region Solution

    public double ReducedCost => this.UnderlyingSolver.get_var_dualresult(this.ColumnNumber);
    
#endregion
}