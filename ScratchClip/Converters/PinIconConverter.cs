using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ScratchClip.Converters;

public class PinIconConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value is true ? "Pin" : "PinOff";
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