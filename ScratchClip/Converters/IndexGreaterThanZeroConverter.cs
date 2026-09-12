using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ScratchClip.Converters;

public class IndexGreaterThanZeroConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is int index)
            return index > 0;

        return false;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}