using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ScratchClip.Models;

public class VisibilityByTypeConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is ClipboardType currentType &&
            parameter is ClipboardType expectedType)
            return currentType == expectedType;

        return false;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}