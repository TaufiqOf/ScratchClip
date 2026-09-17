using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ScratchClip.Helper;

public static class LinuxFileIconService
{
    private static readonly Dictionary<string, string?> _cachedMimeTypes = new();

    private const string Gio = "libgio-2.0.so.0";
    private const string GObject = "libgobject-2.0.so.0";
    private const string Glib = "libglib-2.0.so.0";
    private const string Gtk = "libgtk-3.so.0";

    // ============================================================
    // Native Methods
    // ============================================================

    [DllImport(Gio)]
    private static extern IntPtr g_file_new_for_path([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [DllImport(Gio)]
    private static extern IntPtr g_file_query_info(IntPtr file, [MarshalAs(UnmanagedType.LPUTF8Str)] string attributes, int flags, IntPtr cancellable, out IntPtr error);

    [DllImport(Gio)]
    private static extern IntPtr g_file_info_get_attribute_string(IntPtr info, [MarshalAs(UnmanagedType.LPUTF8Str)] string attribute);

    [DllImport(Gio)]
    private static extern IntPtr g_content_type_get_icon([MarshalAs(UnmanagedType.LPUTF8Str)] string contentType);

    [DllImport(Gio)]
    private static extern IntPtr g_themed_icon_get_names(IntPtr icon);

    [DllImport(GObject)]
    private static extern void g_object_unref(IntPtr obj);

    [DllImport(Glib)]
    private static extern void g_error_free(IntPtr error);

    // GTK Native Calls for Theme Lookup
    [DllImport(Gtk)]
    private static extern bool gtk_init_check(IntPtr argc, IntPtr argv);

    [DllImport(Gtk)]
    private static extern IntPtr gtk_icon_theme_get_default();

    [DllImport(Gtk)]
    private static extern IntPtr gtk_icon_theme_choose_icon(IntPtr theme, IntPtr[] iconNames, int size, int flags);

    [DllImport(Gtk)]
    private static extern IntPtr gtk_icon_info_get_filename(IntPtr iconInfo);

    static LinuxFileIconService()
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                // Ensure GTK is initialized for GTK theme queries
                gtk_init_check(IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                // Ignore if running headlessly
            }
        }
    }

    public static string? GetIconPath(string filePath)
    {
        if (!OperatingSystem.IsLinux() || string.IsNullOrWhiteSpace(filePath))
            return null;

        if (!File.Exists(filePath) && !Directory.Exists(filePath))
            return null;

        try
        {
            var mimeType = GetMimeType(filePath);
            if (string.IsNullOrWhiteSpace(mimeType))
                return null;

            if (_cachedMimeTypes.TryGetValue(mimeType, out var cachedPath))
            {
                return cachedPath;
            }

            var iconNames = GetIconNames(mimeType);
            if (iconNames.Count == 0)
                return null;

            // First attempt: Resolve using GTK's native engine (Handles Xfce/Mint themes)
            var iconPath = ResolveWithGtkTheme(iconNames, 64);

            // Fallback attempt: Manual file search
            iconPath ??= FindIconFallback(iconNames, 64);

            _cachedMimeTypes[mimeType] = iconPath;
            return iconPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetIconPath failed: {ex}");
            return null;
        }
    }

    private static string? ResolveWithGtkTheme(List<string> iconNames, int size)
    {
        try
        {
            var iconTheme = gtk_icon_theme_get_default();
            if (iconTheme == IntPtr.Zero)
                return null;

            // Convert string list to null-terminated UTF-8 pointer array
            var ptrArray = new IntPtr[iconNames.Count + 1];
            for (int i = 0; i < iconNames.Count; i++)
            {
                ptrArray[i] = Marshal.StringToHGlobalAnsi(iconNames[i]);
            }
            ptrArray[iconNames.Count] = IntPtr.Zero;

            IntPtr iconInfo = gtk_icon_theme_choose_icon(iconTheme, ptrArray, size, 0);

            // Free allocated string pointers
            foreach (var ptr in ptrArray)
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }

            if (iconInfo == IntPtr.Zero)
                return null;

            var filenamePtr = gtk_icon_info_get_filename(iconInfo);
            string? filename = Marshal.PtrToStringUTF8(filenamePtr);

            g_object_unref(iconInfo);
            return filename;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetMimeType(string filePath)
    {
        if (Directory.Exists(filePath))
            return "inode/directory";

        IntPtr file = IntPtr.Zero;
        IntPtr info = IntPtr.Zero;
        IntPtr error = IntPtr.Zero;

        try
        {
            file = g_file_new_for_path(filePath);
            if (file == IntPtr.Zero) return null;

            info = g_file_query_info(file, "standard::content-type", 0, IntPtr.Zero, out error);
            if (info == IntPtr.Zero) return null;

            var typePtr = g_file_info_get_attribute_string(info, "standard::content-type");
            if (typePtr == IntPtr.Zero) return null;

            return Marshal.PtrToStringUTF8(typePtr);
        }
        finally
        {
            if (error != IntPtr.Zero) g_error_free(error);
            if (info != IntPtr.Zero) g_object_unref(info);
            if (file != IntPtr.Zero) g_object_unref(file);
        }
    }

    private static List<string> GetIconNames(string mimeType)
    {
        var result = new List<string>();
        IntPtr icon = IntPtr.Zero;

        try
        {
            icon = g_content_type_get_icon(mimeType);
            if (icon == IntPtr.Zero) return result;

            var names = g_themed_icon_get_names(icon);
            if (names == IntPtr.Zero) return result;

            return ReadStringArray(names);
        }
        finally
        {
            if (icon != IntPtr.Zero) g_object_unref(icon);
        }
    }

    private static List<string> ReadStringArray(IntPtr array)
    {
        var result = new List<string>();
        if (array == IntPtr.Zero) return result;

        var pointerSize = IntPtr.Size;
        for (var i = 0; i < 100; i++)
        {
            var ptr = Marshal.ReadIntPtr(array, i * pointerSize);
            if (ptr == IntPtr.Zero) break;

            var value = Marshal.PtrToStringUTF8(ptr);
            if (!string.IsNullOrWhiteSpace(value))
                result.Add(value);
        }

        return result;
    }

    private static string? FindIconFallback(IEnumerable<string> iconNames, int size)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roots = new[]
        {
            Path.Combine(home, ".local", "share", "icons"),
            Path.Combine(home, ".icons"),
            "/usr/share/icons",
            "/usr/local/share/icons",
            "/usr/share/pixmaps"
        };

        var extensions = new[] { ".png", ".svg", ".xpm" };

        foreach (var iconName in iconNames)
        {
            if (string.IsNullOrWhiteSpace(iconName) || iconName.EndsWith("-symbolic"))
                continue;

            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;

                foreach (var ext in extensions)
                {
                    try
                    {
                        var files = Directory.EnumerateFiles(root, iconName + ext, SearchOption.AllDirectories);
                        var match = files.FirstOrDefault();
                        if (match != null) return match;
                    }
                    catch
                    {
                        // Ignore restricted access folders
                    }
                }
            }
        }

        return null;
    }
}