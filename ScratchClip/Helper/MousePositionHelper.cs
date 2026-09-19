using System;
using System.Runtime.InteropServices;

namespace ScratchClip.Helper;

public static class MousePositionHelper
{
    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XDefaultRootWindow(
        IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern bool XQueryPointer(
        IntPtr display,
        IntPtr window,
        out IntPtr rootReturn,
        out IntPtr childReturn,
        out int rootX,
        out int rootY,
        out int winX,
        out int winY,
        out uint maskReturn);

    public static (int X, int Y)? GetPosition()
    {
        if (!OperatingSystem.IsLinux())
            return null;

        var display = XOpenDisplay(IntPtr.Zero);

        if (display == IntPtr.Zero)
            return null;

        try
        {
            var root = XDefaultRootWindow(display);

            if (XQueryPointer(
                    display,
                    root,
                    out _,
                    out _,
                    out var rootX,
                    out var rootY,
                    out _,
                    out _,
                    out _))
                return (rootX, rootY);

            return null;
        }
        finally
        {
            XCloseDisplay(display);
        }
    }
}