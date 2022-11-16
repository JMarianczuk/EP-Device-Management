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

    public static Power Max(Power left, Power right)
    {
        if (left > right)
        {
            return left;
        }
        else
        {
            return right;
        }
    }

    public static Energy Max(Energy left, Energy right)
    {
        if (left > right)
        {
            return left;
        }
        else
        {
            return right;
        }
    }

    public static PowerFast Max(PowerFast left, PowerFast right)
    {
        if (left > right)
        {
            return left;
        }
        else
        {
            return right;
        }
    }

    public static EnergyFast Max(EnergyFast left, EnergyFast right)
    {
        if (left > right)
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

    public static Power Min(Power left, Power right)
    {
        if (left < right)
        {
            return left;
        }
        else
        {
            return right;
        }
    }

    public static Energy Min(Energy left, Energy right)
    {
        if (left < right)
        {
            return left;
        }
        else
        {
            return right;
        }
    }

    public static PowerFast Min(PowerFast left, PowerFast right)
    {
        if (left < right)
        {
            return left;
        }
        else
        {
            return right;
        }
    }

    public static EnergyFast Min(EnergyFast left, EnergyFast right)
    {
        if (left < right)
        {
            return left;
        }
        else
        {
            return right;
        }
    }
}