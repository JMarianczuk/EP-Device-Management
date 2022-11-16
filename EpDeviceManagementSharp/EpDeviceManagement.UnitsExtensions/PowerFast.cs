namespace EpDeviceManagement.UnitsExtensions;

public struct PowerFast
{
    private const double wattsInAKilowatt = 1000;
    private readonly double watts;

    public PowerFast(double watts)
    {
        this.watts = watts;
    }

    public static PowerFast Zero { get; } = new PowerFast(0);

    public static PowerFast FromWatts(double watts)
        => new(watts);

    public static PowerFast FromKilowatts(double kilowatts)
        => new(kilowatts * wattsInAKilowatt);

    public double Watts => this.watts;

    public double Kilowatts => this.watts / wattsInAKilowatt;

    public static PowerFast operator +(PowerFast left, PowerFast right)
        => new(left.watts + right.watts);

    public static PowerFast operator -(PowerFast left, PowerFast right)
        => new(left.watts - right.watts);

    public static PowerFast operator -(PowerFast power)
        => new(-power.watts);

    public static PowerFast operator *(PowerFast left, double right)
        => new(left.watts * right);

    public static PowerFast operator *(double left, PowerFast right)
        => new(right.watts * left);

    public static EnergyFast operator *(PowerFast left, TimeSpan right)
        => new(left.watts * right.TotalSeconds);

    public static EnergyFast operator *(TimeSpan left, PowerFast right)
        => new(right.watts * left.TotalSeconds);

    public static PowerFast operator /(PowerFast left, double right)
        => new(left.watts / right);

    public static bool operator ==(PowerFast left, PowerFast right)
        => left.watts == right.watts;

    public static bool operator !=(PowerFast left, PowerFast right)
        => left.watts != right.watts;

    public static bool operator >(PowerFast left, PowerFast right)
        => left.watts > right.watts;

    public static bool operator <(PowerFast left, PowerFast right)
        => left.watts < right.watts;

    public static bool operator >=(PowerFast left, PowerFast right)
        => left.watts >= right.watts;

    public static bool operator <=(PowerFast left, PowerFast right)
        => left.watts <= right.watts;
}