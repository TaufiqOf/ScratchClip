using System;
using Avalonia.Styling;

namespace ScratchClip.Helper;

public static class ApplicationTheme
{
    public static Action<ThemeVariant>? OnThemeChanged;
    public static ThemeVariant? Theme { get; set; }

    public static string GetIcon(ThemeVariant theme, string type, string? size = null)
    {
        var assetUri = theme == ThemeVariant.Dark
            ? $"avares://ScratchClip/Assets/icon-dark.{type}"
            : $"avares://ScratchClip/Assets/icon-light.{type}";
        if (size is not null)
        {
            assetUri = theme == ThemeVariant.Dark
                ? $"avares://ScratchClip/Assets/icon-dark-icons/icon-dark-{size}.{type}"
                : $"avares://ScratchClip/Assets/icon-light-icons/icon-light-{size}.{type}";
        }

        return assetUri;
    }
}