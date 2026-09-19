using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ScratchClip.Models;

namespace ScratchClip.Converters;

public class StorageTypeToBrushConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        
        if (value is StorageType storageItem)
        {
            if(storageItem == StorageType.Invalid)
                return new SolidColorBrush(Colors.LightPink);
            if (parameter is Color color)
                return new SolidColorBrush(color);
        }

        return new SolidColorBrush(Colors.LightPink);
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