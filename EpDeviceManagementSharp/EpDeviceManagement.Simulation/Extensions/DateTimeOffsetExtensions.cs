namespace EpDeviceManagement.Simulation.Extensions;

public static class DateTimeOffsetExtensions
{
    public static bool IsDivisibleBy(this DateTimeOffset dateTimeOffset, TimeSpan timeSpan)
    {
        var time = dateTimeOffset.TimeOfDay;
        var fraction = time / timeSpan;
        var result = fraction == (int)fraction;
        return result;
    }
}