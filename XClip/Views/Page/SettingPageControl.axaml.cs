using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SharpHook.Data;
using XClip.ViewModels;

namespace XClip.Views.Page;

public partial class SettingPageControl : UserControl
{
    public SettingPageControl()
    {
        InitializeComponent();
    }

    public event EventHandler? CloseRequested;

    private void HotkeyTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not SettingsViewModel vm) return;

        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin)
            return;

        var mask = EventMask.None;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) mask |= EventMask.LeftAlt;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) mask |= EventMask.LeftCtrl;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) mask |= EventMask.LeftShift;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Meta)) mask |= EventMask.LeftMeta;

        if (TryMapKeyToSharpHook(e.Key, out var keyCode))
            vm.SetHotkey(mask, keyCode, BuildDisplayString(e.KeyModifiers, e.Key));
    }

    private static bool TryMapKeyToSharpHook(Key key, out KeyCode keyCode)
    {
        if (Enum.TryParse($"Vc{key}", true, out keyCode)) return true;

        if (key >= Key.D0 && key <= Key.D9)
        {
            keyCode = (KeyCode)((int)KeyCode.Vc0 + (key - Key.D0));
            return true;
        }

        keyCode = KeyCode.VcUndefined;
        return false;
    }

    private static string BuildDisplayString(KeyModifiers modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Cmd");
        parts.Add(key.ToString());
        return string.Join(" + ", parts);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.SaveCommand.Execute(null);

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}