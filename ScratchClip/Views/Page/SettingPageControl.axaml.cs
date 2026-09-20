using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScratchClip.Helper;
using ScratchClip.Manager;
using ScratchClip.ViewModels;
using SharpHook.Data;

namespace ScratchClip.Views.Page;

public partial class SettingPageControl : UserControl
{
    public SettingPageControl()
    {
        InitializeComponent();

        AddHandler(KeyDownEvent, InputElementOnKeyDown, RoutingStrategies.Tunnel);
    }

    public event EventHandler? CloseRequested;

    private void InputElementOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control)) OnSaveClick(sender, new RoutedEventArgs());
    }

    private void OnSaveClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            if (!vm.Save())
            {
                return;
            }
            NotificationHelper.Success(
                "Preferences saved",
                "Your preferences have been successfully saved.");
        }

        CloseRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    private void OnCancelClick(
        object? sender,
        RoutedEventArgs e)
    {
        CloseRequested?.Invoke(
            this,
            EventArgs.Empty);
    }


    private async void SettingsNavButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Control target })
            return;

        // Get the target's position relative to the ScrollViewer
        var point = target.TranslatePoint(
            new Avalonia.Point(0, 0),
            SettingsScrollViewer);

        if (point is null)
            return;

        var start = SettingsScrollViewer.Offset.Y;
        var end = Math.Max(0, start + point.Value.Y);

        await AnimateScrollAsync(start, end, TimeSpan.FromMilliseconds(350));
        await BlinkSection(target);
    }

    private async Task AnimateScrollAsync(
        double start,
        double end,
        TimeSpan duration)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            var progress =
                stopwatch.Elapsed.TotalMilliseconds /
                duration.TotalMilliseconds;

            // Ease-out
            progress = 1 - Math.Pow(1 - progress, 3);

            var value = start + (end - start) * progress;

            SettingsScrollViewer.Offset =
                new Avalonia.Vector(
                    SettingsScrollViewer.Offset.X,
                    value);

            await Task.Delay(3000 / 60); // ~60 FPS
        }

        SettingsScrollViewer.Offset =
            new Avalonia.Vector(
                SettingsScrollViewer.Offset.X,
                end);
    }
    private async Task BlinkSection(Control section)
    {
        // Make it visible/normal first
        section.Opacity = 1;

        // Blink 3 times
        for (int i = 0; i < 1; i++)
        {
            section.Opacity = 0.6;
            await Task.Delay(250);

            section.Opacity = 1;
            await Task.Delay(250);
        }
    }
}