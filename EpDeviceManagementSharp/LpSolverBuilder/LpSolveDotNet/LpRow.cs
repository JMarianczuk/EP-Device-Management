namespace LpSolveDotNet;

public struct LpRow
{
    public LpRow(int rowNumber, ILpSolve newInterface)
    {
        RowNumber = rowNumber;
        ISolver = newInterface;
    }

    public int RowNumber { get; }

    public ILpSolve ISolver { get; }

    public LpSolve UnderlyingSolver => ISolver.UnderlyingSolver;

    public bool GetValues(double[] row)
        => this.UnderlyingSolver.get_row(this.RowNumber, row);

    public int GetValuesEx(double[] row, int[] columnNumbers)
        => this.UnderlyingSolver.get_rowex(this.RowNumber, row, columnNumbers);

    public bool SetValues(double[] row)
        => this.UnderlyingSolver.set_row(this.RowNumber, row);

    // Not yet sure about this one.
    // On the one hand, Id like to be able to use the functionality from LpTerm instead of having SetValuesEx here.
    // But this raises the question where the double[] row and the int[] columnNumbers come from.
    // That is why this calles ISolver.SetRow, because ISolver holds these arrays
    // But that means that ISolver must have the SetRow method, which means that functionality now exists twice and does not have a single location where it belongs
    public bool SetValues(LpSummand singleSummand)
        => this.ISolver.SetRow(this.RowNumber, singleSummand);

    public bool SetValues(LpSum term)
        => this.ISolver.SetRow(this.RowNumber, term);

    public string Name
    {
        get => this.UnderlyingSolver.get_row_name(this.RowNumber);
        set => this.UnderlyingSolver.set_row_name(this.RowNumber, value); //TODO bool return value
    }

    public string OriginalName => this.UnderlyingSolver.get_origrow_name(this.RowNumber);

    public bool IsConstraintType(lpsolve_constr_types mask)
        => this.UnderlyingSolver.is_constr_type(this.RowNumber, mask);

    public lpsolve_constr_types ConstraintType
    {
        get => this.UnderlyingSolver.get_constr_type(this.RowNumber);
        set => this.UnderlyingSolver.set_constr_type(this.RowNumber, value); //TODO bool return value
    }

    public double RightHandSide
    {
        get => this.UnderlyingSolver.get_rh(this.RowNumber);
        set => this.UnderlyingSolver.set_rh(this.RowNumber, value); //TODO bool return value
    }

    public double RightHandSideRange
    {
        get => this.UnderlyingSolver.get_rh_range(this.RowNumber);
        set => this.UnderlyingSolver.set_rh_range(this.RowNumber, value); //TODO bool return value
    }
}