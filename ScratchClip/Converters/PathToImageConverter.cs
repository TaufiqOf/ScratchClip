using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using ScratchClip.Helper;

namespace ScratchClip.Converters;

public class PathToImageConverter : IValueConverter
{
    public static readonly PathToImageConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string iconPath || string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
            return null;

        try
        {
            // Handle SVG icons using static SvgSource API
            if (iconPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                var svgSource = SvgSource.Load(iconPath);
                return new SvgImage { Source = svgSource };
            }

            // Handle PNG, JPG, XPM raster icons
            return new Bitmap(iconPath);
        }
        catch (Exception ex)
        {
            NotificationHelper.Error("Error",$"Failed to load image at '{iconPath}': {ex.Message}");
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}