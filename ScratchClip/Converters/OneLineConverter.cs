using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ScratchClip.Converters;

public class OneLineConverter : IValueConverter
{
    public int MaxLength { get; set; } = 0;

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not string str)
            return value;

        // Remove whitespace (spaces, tabs, newlines, etc.) from the beginning.
        str = str.TrimStart();

        if (string.IsNullOrEmpty(str))
            return string.Empty;

        // Keep only the first line.
        int newlineIndex = str.IndexOfAny(['\r', '\n']);

        bool hasMoreLines = newlineIndex >= 0;

        string firstLine = hasMoreLines
            ? str[..newlineIndex]
            : str;

        // Remove whitespace from the beginning of the first line.
        firstLine = firstLine.TrimStart();

        // Truncate long first lines.
        if (MaxLength > 0 && firstLine.Length > MaxLength)
            firstLine = firstLine[..MaxLength] + "...";

        // Add line count if there were additional lines.
        if (hasMoreLines)
        {
            int lineCount = str.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None).Length;

            return $"({lineCount} Lines) {firstLine}";
        }

        return firstLine;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}