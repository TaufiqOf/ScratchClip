using System;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Media;

namespace ScratchClip.Views;

public partial class NotificationWindow : Window
{
    private readonly Timer _timer;

    public NotificationWindow(
        string title,
        string message,
        NotificationType messageType,
        int durationInSeconds = 3)
    {
        InitializeComponent();

        var (icon, color) = messageType switch
        {
            NotificationType.Error =>
                (FluentIcons.Common.Icon.ErrorCircle, Colors.Red),

            NotificationType.Warning =>
                (FluentIcons.Common.Icon.Warning, Colors.Orange),

            NotificationType.Information =>
                (FluentIcons.Common.Icon.Info, Colors.DodgerBlue),

            NotificationType.Success =>
                (FluentIcons.Common.Icon.CheckmarkCircle, Colors.LimeGreen),

            _ => throw new ArgumentOutOfRangeException(
                nameof(messageType),
                messageType,
                null)
        };

        Icon.Icon = icon;
        Icon.Foreground = new SolidColorBrush(color);

        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;

        _timer = new Timer(durationInSeconds * 1000);
        _timer.AutoReset = false;
        _timer.Elapsed += (_, _) =>
        {
            Dispatcher.Post(Close);
        };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _timer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();

        base.OnClosed(e);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Close();
    }
}