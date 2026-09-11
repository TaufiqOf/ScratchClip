using System;
using Avalonia.Styling;

namespace ScratchClip.Helper;

public static class ApplicationTheme
{
    public static Action<ThemeVariant>? OnThemeChanged;
    public static ThemeVariant Theme { get; set; }
    public static string GetIcon(ThemeVariant theme, string type)
    {
        var assetUri = theme == ThemeVariant.Dark
            ? $"avares://ScratchClip/Assets/icon-dark.{type}"
            : $"avares://ScratchClip/Assets/icon-light.{type}";
        return assetUri;
    }
}