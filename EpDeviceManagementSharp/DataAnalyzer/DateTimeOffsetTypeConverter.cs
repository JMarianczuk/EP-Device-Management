using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DataAnalyzer;

public class DateTimeOffsetTypeConverter : DateTimeOffsetConverter
{
    public const string Iso8601Format = "yyyy-MM-dd'T'HH:mm:ssK";

    //public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    //{
    //    return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
    //}

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is DateTimeOffset dto)
        {
            return ConvertToString(dto);
        }

        return string.Empty;
    }

    public static string ConvertToString(DateTimeOffset dto)
    {
        return dto.ToString(Iso8601Format, CultureInfo.InvariantCulture);
    }
}