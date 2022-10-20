using UnitsNet;

namespace EpDeviceManagement.UnitsExtensions;

public static class Units
{
    public static TUnit Max<TUnit>(TUnit left, TUnit right)
        where TUnit : IQuantity, IComparable<TUnit>
    {
        if (left.CompareTo(right) > 0)
        {
            return left;
        }
        else
        {
            return right;
        }
    }

    public static TUnit Min<TUnit>(TUnit left, TUnit right)
        where TUnit : IQuantity, IComparable<TUnit>
    {
        if (left.CompareTo(right) < 0)
        {
            return left;
        }
        else
        {
            return right;
        }
    }
}