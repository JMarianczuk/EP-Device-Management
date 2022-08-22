using UnitsNet;

namespace EpDeviceManagement.Simulation;

public static class Units
{
    public static TQuantity Min<TQuantity>(TQuantity left, TQuantity right)
        where TQuantity : IQuantity
    {
        return left.Value < right.As(left.Unit)
            ? left
            : right;
    }

    public static TQuantity Max<TQuantity>(TQuantity left, TQuantity right)
        where TQuantity : IQuantity
    {
        return left.Value > right.As(left.Unit)
            ? left
            : right;
    }
}