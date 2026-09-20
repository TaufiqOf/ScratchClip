using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ScratchClip.Helper;
using ScratchClip.Manager;

namespace ScratchClip.Views.Controls.SettingsControls;

public partial class SecuritySettingsControl : UserControl
{
    public SecuritySettingsControl()
    {
        InitializeComponent();
        UpdatePasswordControls();
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
        NotificationHelper.Success(
            "Password status",
            message);
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
            ApplicationKeyStore.SetSessionPassword(password);
            var clipboardHistory = ClipboardManager.GetClipboardHistorySnapshot();
            ClipboardHistoryManager.Save(clipboardHistory, ApplicationKeyStore.GetSessionPassword());
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

        var clipboardHistory = ClipboardManager.GetClipboardHistorySnapshot();
        ClipboardHistoryManager.Save(clipboardHistory, ApplicationKeyStore.GetSessionPassword());
        ShowPasswordStatus("Password removed.");
    }

}