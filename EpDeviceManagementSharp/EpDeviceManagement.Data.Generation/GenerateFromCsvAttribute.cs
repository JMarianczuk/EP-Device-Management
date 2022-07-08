using System;

namespace EpDeviceManagement.Data.Generation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class GenerateFromCsvAttribute : Attribute
{
    public GenerateFromCsvAttribute(string filename)
    {
        Filename = filename;
    }

    public string Filename { get; }

    public Type DefaultType { get; set; }
}