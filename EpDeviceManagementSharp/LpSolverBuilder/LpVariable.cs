namespace LpSolverBuilder
{
    public class LpVariable : LpTerm
    {
        private static int id_counter = 0;

        public int Id { get; }

        public int? LpIndex { get; internal set; } = null;

        public LpVariable()
        {
            this.Id = id_counter;
            id_counter += 1;
        }
    }

    public class LpSum : LpTerm
    {
        public LpTerm Left { get; }

        public LpTerm Right { get; }

        public LpSum(LpTerm left, LpTerm right)
        {
            this.Left = left;
            this.Right = right;
        }
    }

    public class LpDifference : LpTerm
    {
        public LpTerm Left { get; }

        public LpTerm Right { get; }

        public LpDifference(LpTerm left, LpTerm right)
        {
            this.Left = left;
            this.Right = right;
        }
    }

    public class LpTerm
    {
        public static LpSum operator +(LpTerm left, LpTerm right)
        {
            return new LpSum(left, right);
        }

        public static LpDifference operator -(LpTerm left, LpTerm right)
        {
            return new LpDifference(left, right);
        }
    }

    public enum LpConstraintType
    {
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }
}