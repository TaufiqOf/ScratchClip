using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ScratchClip.Helper;

namespace ScratchClip.Views.Page;

public partial class PasswordPageControl : UserControl
{
    public Action? OnLogin { get; set; }

    public PasswordPageControl()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Dispatcher.UIThread.Post(
            () => LoginPasswordTextBox.Focus(),
            DispatcherPriority.Input);
    }

    private void LoginButtonOnClick(
        object? sender,
        RoutedEventArgs e)
    {
        TryLogin();
    }

    private void LoginPasswordTextBoxOnKeyUp(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryLogin();
        }
    }

    private void TryLogin()
    {
        var password = LoginPasswordTextBox.Text;

        if (string.IsNullOrEmpty(password))
            return;

        if (CheckPassword(password))
        {
            OnLogin?.Invoke();
        }
        else
        {
            // Show invalid password message here.
        }
    }

    private bool CheckPassword(string password)
    {
        return ApplicationKeyStore.VerifyPassword(password);
    }
}