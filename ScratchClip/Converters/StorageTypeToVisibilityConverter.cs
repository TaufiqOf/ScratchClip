using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ScratchClip.Models;

namespace ScratchClip.Converters;

public class StorageTypeToVisibilityConverter : IValueConverter
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
                return true;
            if (parameter is StorageType storageType)
                return storageType == storageItem;
        }

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