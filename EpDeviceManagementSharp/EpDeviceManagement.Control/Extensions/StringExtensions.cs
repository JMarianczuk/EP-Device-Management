namespace EpDeviceManagement.Control.Extensions;

public static class StringExtensions
{
    public static string JoinIf(string separator, params string[] elements)
    {
        return string.Join(separator, elements.Where(s => !string.IsNullOrEmpty(s)));
    }
}