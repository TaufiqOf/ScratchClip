using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace ScratchClip.Helper;

public static class NotificationHelper
{
    private static WindowNotificationManager? _manager;

    public static void Initialize(Window window)
    {
        _manager = new WindowNotificationManager(window)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3
        };
    }

    public static void ShowNotification(
        string title,
        string message,
        NotificationType type = NotificationType.Information,
        TimeSpan? expiration = null)
    {
        if (_manager == null)
            return;

        _manager.Show(new Notification(
            title,
            message,
            type,
            expiration ?? TimeSpan.FromSeconds(3)));
    }

    public static void Success(string title, string message)
    {
        ShowNotification(
            title,
            message,
            NotificationType.Success);
    }

    public static void Info(string title, string message)
    {
        ShowNotification(
            title,
            message);
    }

    public static void Warning(string title, string message)
    {
        ShowNotification(
            title,
            message,
            NotificationType.Warning);
    }

    public static void Error(string title, string message)
    {
        ShowNotification(
            title,
            message,
            NotificationType.Error,
            TimeSpan.FromSeconds(5));
    }
}