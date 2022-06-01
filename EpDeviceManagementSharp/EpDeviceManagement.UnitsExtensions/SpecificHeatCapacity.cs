using UnitsNet;

namespace EpDeviceManagement.UnitsExtensions;

public struct SpecificHeatCapacity : IQuantity
{
    public SpecificHeatCapacity(
        double value,
        SpecificHeatCapacityUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    private static SpecificHeatCapacity Zero =
        new SpecificHeatCapacity(0, SpecificHeatCapacityUnit.KiloJoulesPerKilogramCelsius);

    public QuantityType Type => QuantityType.Undefined;
    public BaseDimensions Dimensions { get; } = new BaseDimensions(2, 0, -2, 0, -1, 0, 0);

    public QuantityInfo QuantityInfo => new QuantityInfo(
        this.Type,
        new UnitInfo[]
        {
            new UnitInfo<SpecificHeatCapacityUnit>(SpecificHeatCapacityUnit.KiloJoulesPerKilogramCelsius, "", BaseUnits.Undefined),
        },
        SpecificHeatCapacityUnit.KiloJoulesPerKilogramCelsius,
        Zero,
        this.Dimensions);
    
    Enum IQuantity.Unit => this.Unit;
    public SpecificHeatCapacityUnit Unit { get; }
    public double Value { get; }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        throw new NotImplementedException();
    }

    double IQuantity.As(Enum unit)
    {
        if (unit is SpecificHeatCapacityUnit specificHeatCapacityUnit)
        {
            return this.As(specificHeatCapacityUnit);
        }

        throw new ArgumentException(nameof(unit));
    }

    public double As(SpecificHeatCapacityUnit unit)
    {
        if (this.Unit == unit)
        {
            return Convert.ToDouble(this.Value);
        }

        throw new NotImplementedException();
    }

    public double As(UnitSystem unitSystem)
    {
        throw new NotImplementedException();
    }

    public IQuantity ToUnit(Enum unit)
    {
        if (unit is SpecificHeatCapacityUnit specificHeatCapacityUnit)
        {
            return new SpecificHeatCapacity(As(unit), specificHeatCapacityUnit);
        }

        throw new ArgumentException(nameof(unit));
    }

    public IQuantity ToUnit(UnitSystem unitSystem)
    {
        throw new NotImplementedException();
    }

    public string ToString(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public string ToString(IFormatProvider? provider, int significantDigitsAfterRadix)
    {
        throw new NotImplementedException();
    }

    public string ToString(IFormatProvider? provider, string format, params object[] args)
    {
        throw new NotImplementedException();
    }
    
    
}

public enum SpecificHeatCapacityUnit
{
    Undefined,
    KiloJoulesPerKilogramCelsius,
}