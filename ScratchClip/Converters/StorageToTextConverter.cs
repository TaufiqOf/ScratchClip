using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ScratchClip.Converters;

public class StorageToTextConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is ICollection<string> values)
        {
            int count = values.Count;
            string type = parameter?.ToString() ?? "Items";

            return $"({count} {type})";
        }

        return parameter?.ToString() switch
        {
            "Files" => "(0 Files)",
            "Folders" => "(0 Folders)",
            _ => "(0 Items)"
        };
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}