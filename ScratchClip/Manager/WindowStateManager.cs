using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace ScratchClip.Manager;

public enum WindowType
{
    MainWindow,
    SettingsWindow,
    SnippetViewWindow
}
public static class WindowStateManager
{
    public static Dictionary<WindowType, Window> Windows { get; } = new Dictionary<WindowType, Window>();

    public static void Add(WindowType windowType, Window window)
    {
        Windows.TryAdd(windowType, window);
    }

    public static Window? Get(WindowType windowType)
    {
        Windows.TryGetValue(windowType, out var window);
        return window;
    }
    
    public static void ShowWindow(WindowType windowType)
    {
        var window = Get(windowType);
        if(window == null)
            return;
        if (window is { IsVisible: true, IsActive: true })
            HideToTray(window);
        else
            ShowFromTray(window);
    }
    
    public static void SetSize(WindowType windowType, double width, double height)
    {
        var window = Get(windowType);
        if(window == null)
            return;
        window.Width = width;
        window.Height = height;
    }
    
    public static void SetPosition(WindowType windowType, double left, double top)
    {
        var window = Get(windowType);
        if(window == null)
            return;
        window.Position = new PixelPoint((int)left, (int)top);
    }

    private static void PositionInBottomRight(WindowType windowType)
    {
        var window = Get(windowType);
        if(window == null)
            return;

        var screen = window.Screens.Primary;
        if (screen == null) return;

        var workArea = screen.WorkingArea;
        var windowWidthPixels = (int)(window.Width * screen.Scaling);
        var windowHeightPixels = (int)(window.Height * screen.Scaling);

        var x = workArea.X + workArea.Width - windowWidthPixels;
        var y = workArea.Y + workArea.Height - windowHeightPixels;

        SetPosition(windowType, x, y);
    }

    private static void ShowFromTray(Window window)
    {
        window.ShowInTaskbar = true;
        window.Show();
    }

    private static void HideToTray(Window window)
    {
        window.ShowInTaskbar = false;
        window.Hide();
    }
}