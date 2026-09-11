using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScratchClip.Helper;
using ScratchClip.ViewModels;
using SharpHook.Data;

namespace ScratchClip.Views.Page;

public partial class SettingPageControl : UserControl
{
    public event EventHandler? CloseRequested;

    public SettingPageControl()
    {
        InitializeComponent();
        UpdatePasswordControls();
    }

    private void HotkeyTextBox_OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not SettingsViewModel vm)
            return;

        // Ignore modifier-only presses.
        if (e.Key is
            Key.LeftCtrl or
            Key.RightCtrl or
            Key.LeftAlt or
            Key.RightAlt or
            Key.LeftShift or
            Key.RightShift or
            Key.LWin or
            Key.RWin)
        {
            return;
        }

        var mask = EventMask.None;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            mask |= EventMask.LeftCtrl;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            mask |= EventMask.LeftAlt;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            mask |= EventMask.LeftShift;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Meta))
            mask |= EventMask.LeftMeta;

        if (!TryMapKeyToSharpHook(e.Key, out var keyCode))
            return;

        var display = BuildDisplayString(
            e.KeyModifiers,
            e.Key);

        vm.SetHotkey(
            mask,
            keyCode,
            display);
    }

    private static bool TryMapKeyToSharpHook(
        Key key,
        out KeyCode keyCode)
    {
        // Most keys have a matching VcXXX enum value.
        if (Enum.TryParse(
                $"Vc{key}",
                true,
                out keyCode))
        {
            return true;
        }

        // Avalonia D0-D9 -> SharpHook Vc0-Vc9.
        if (key >= Key.D0 && key <= Key.D9)
        {
            keyCode = (KeyCode)(
                (int)KeyCode.Vc0 +
                (key - Key.D0));

            return true;
        }

        keyCode = KeyCode.VcUndefined;
        return false;
    }

    private static string BuildDisplayString(
        KeyModifiers modifiers,
        Key key)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(KeyModifiers.Control))
            parts.Add("Ctrl");

        if (modifiers.HasFlag(KeyModifiers.Alt))
            parts.Add("Alt");

        if (modifiers.HasFlag(KeyModifiers.Shift))
            parts.Add("Shift");

        if (modifiers.HasFlag(KeyModifiers.Meta))
            parts.Add("Super");

        parts.Add(key.ToString());

        return string.Join(" + ", parts);
    }

    private void OnSaveClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SaveCommand.Execute(null);
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

    private void UpdatePasswordControls()
    {
        var hasPassword = ApplicationKeyStore.HasPassword();

        SetPasswordButton.Content =
            hasPassword
                ? "Change password"
                : "Set password";

        DeletePasswordButton.IsVisible = hasPassword;

        PasswordTextBox.PlaceholderText =
            hasPassword
                ? "New password"
                : "Password";

        ConfirmPasswordTextBox.PlaceholderText =
            hasPassword
                ? "Confirm new password"
                : "Confirm password";

        PasswordStatusText.IsVisible = false;
    }
    
    private void ShowPasswordStatus(string message)
    {
        PasswordStatusText.Text = message;
        PasswordStatusText.IsVisible = true;
    }
    private void SetPasswordButtonOnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var password = PasswordTextBox.Text;
        var confirmation = ConfirmPasswordTextBox.Text;

        PasswordStatusText.IsVisible = false;

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowPasswordStatus("Please enter a password.");
            return;
        }

        if (password.Length < 8)
        {
            ShowPasswordStatus(
                "Password must be at least 8 characters.");
            return;
        }

        if (password != confirmation)
        {
            ShowPasswordStatus(
                "The passwords do not match.");
            return;
        }

        try
        {
            ApplicationKeyStore.SetPassword(password);

            PasswordTextBox.Clear();
            ConfirmPasswordTextBox.Clear();

            UpdatePasswordControls();

            ShowPasswordStatus(
                "Password updated successfully.");
        }
        catch (Exception)
        {
            ShowPasswordStatus(
                "Unable to save the password.");
        }
    }
    private void DeletePasswordButtonOnClick(
        object? sender,
        RoutedEventArgs e)
    {
        ApplicationKeyStore.DeletePassword();

        PasswordTextBox.Clear();
        ConfirmPasswordTextBox.Clear();

        UpdatePasswordControls();

        ShowPasswordStatus(
            "Password removed.");
    }
}