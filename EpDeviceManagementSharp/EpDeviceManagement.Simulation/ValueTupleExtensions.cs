namespace EpDeviceManagement.Simulation;

public static class ValueTupleExtensions
{
    public static (T1, T2, T3) Combine<T1, T2, T3>((T1, T2) left, T3 right)
    {
        return (left.Item1, left.Item2, right);
    }

    public static (T1, T2, T3, T4, T5, T6, T7) Combine<T1, T2, T3, T4, T5, T6, T7>(
        (T1, T2, T3, T4, T5) left,
        (T6, T7) right)
    {
        return (left.Item1, left.Item2, left.Item3, left.Item4, left.Item5, right.Item1, right.Item2);
    }
}