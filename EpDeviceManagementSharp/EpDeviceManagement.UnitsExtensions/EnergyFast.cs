using UnitsNet;

namespace EpDeviceManagement.UnitsExtensions;

public struct EnergyFast
{
    private const double wattsInAKilowatt = 1000;
    private const double secondsInAMinute = 60;
    private const double minutesInAnHour = 60;
    private const double secondsInAnHour = secondsInAMinute * minutesInAnHour;
    private const double wattSecondsInAKilowattHour = wattsInAKilowatt * secondsInAnHour;
    private readonly double joules;

    public EnergyFast(double joules)
    {
        this.joules = joules;
    }

    public static EnergyFast Zero { get; } = new EnergyFast(0);

    public static EnergyFast FromKilowattHours(double kilowattHours)
        => new(kilowattHours * wattSecondsInAKilowattHour);

    public double Joules => this.joules;

    public double KilowattHours => this.joules / wattSecondsInAKilowattHour;

    public override string ToString()
    {
        return $"{this.KilowattHours} kWh";
    }

    public static EnergyFast operator +(EnergyFast left, EnergyFast right)
        => new(left.joules + right.joules);

    public static EnergyFast operator -(EnergyFast left, EnergyFast right)
        => new(left.joules - right.joules);

    public static EnergyFast operator -(EnergyFast energy)
        => new(-energy.joules);

    public static EnergyFast operator *(EnergyFast left, double right)
        => new(left.joules * right);

    public static EnergyFast operator *(double left, EnergyFast right)
        => new(right.joules * left);

    public static EnergyFast operator /(EnergyFast left, double right)
        => new(left.joules / right);

    public static PowerFast operator /(EnergyFast left, TimeSpan right)
        => new(left.joules / right.TotalSeconds);

    public static double operator /(EnergyFast left, EnergyFast right)
        => left.joules / right.joules;

    public static PowerFast operator *(Frequency left, EnergyFast right)
        => new(right.joules * left.PerSecond);


    public static bool operator ==(EnergyFast left, EnergyFast right)
        => left.joules == right.joules;

    public static bool operator !=(EnergyFast left, EnergyFast right)
        => left.joules != right.joules;

    public static bool operator >(EnergyFast left, EnergyFast right)
        => left.joules > right.joules;

    public static bool operator <(EnergyFast left, EnergyFast right)
        => left.joules < right.joules;

    public static bool operator >=(EnergyFast left, EnergyFast right)
        => left.joules >= right.joules;

    public static bool operator <=(EnergyFast left, EnergyFast right)
        => left.joules <= right.joules;
}