using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using ScratchClip.Views;

namespace ScratchClip.Helper;

public static class NotificationHelper
{
    private static readonly List<NotificationWindow> _notifications = new();

    private static Window? _mainWindow;

    private const int Margin = 20;
    private const int Spacing = 10;

    public static void Initialize(Window window)
    {
        _mainWindow = window;

        // Reposition when the application moves between monitors.
        window.PositionChanged += (_, _) =>
        {
            RepositionNotifications();
        };
    }

    public static void ShowNotification(
        string title,
        string message,
        NotificationType type = NotificationType.Information,
        int? expiration = null)
    {
        if (_mainWindow == null)
            throw new InvalidOperationException(
                "NotificationHelper.Initialize() must be called first.");

        var notification = new NotificationWindow(
            title,
            message,
            type,
            expiration ?? 3);

        _notifications.Add(notification);

        notification.Closed += (_, _) =>
        {
            _notifications.Remove(notification);
            RepositionNotifications();
        };

        notification.Opened += (_, _) =>
        {
            RepositionNotifications();
        };

        notification.Show();

        RepositionNotifications();
    }


    private static void RepositionNotifications()
    {
        if (_mainWindow == null)
            return;

        double bottom = Margin;

        // Newest notification at the bottom.
        foreach (var notification in _notifications.AsEnumerable().Reverse())
        {
            if (!notification.IsVisible)
                continue;

            notification.UpdateLayout();

            var screen = notification.Screens.ScreenFromWindow(_mainWindow);

            if (screen == null)
                continue;

            var workingArea = screen.WorkingArea;

            // Right edge
            var x = workingArea.Right
                    - notification.Bounds.Width
                    - Margin;

            // Bottom edge
            var y = workingArea.BottomRight.Y
                    - notification.Bounds.Height
                    - bottom;

            notification.Position = new PixelPoint(
                (int)x,
                (int)y);

            bottom += notification.Bounds.Height + Spacing;
        }
    }

    public static void Success(string title, string message)
    {
        ShowNotification(title, message, NotificationType.Success);
    }

    public static void Info(string title, string message)
    {
        ShowNotification(title, message);
    }

    public static void Warning(string title, string message)
    {
        ShowNotification(title, message, NotificationType.Warning);
    }

    public static void Error(string title, string message)
    {
        ShowNotification(title, message, NotificationType.Error, 5);
    }
}