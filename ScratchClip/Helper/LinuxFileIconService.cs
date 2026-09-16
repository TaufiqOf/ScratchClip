using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;

namespace ScratchClip.Helper;

public static class LinuxFileIconService
{
    private static Dictionary<string, string> _cachedMimeTypes = new();
    private const string Gio = "libgio-2.0.so.0";
    private const string GObject = "libgobject-2.0.so.0";
    private const string Glib = "libglib-2.0.so.0";

    // ============================================================
    // GIO
    // ============================================================

    [DllImport(Gio)]
    private static extern IntPtr g_file_new_for_path(
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        string path);

    [DllImport(Gio)]
    private static extern IntPtr g_file_query_info(
        IntPtr file,
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        string attributes,
        int flags,
        IntPtr cancellable,
        out IntPtr error);

    [DllImport(Gio)]
    private static extern IntPtr
        g_file_info_get_attribute_string(
            IntPtr info,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string attribute);

    [DllImport(Gio)]
    private static extern IntPtr
        g_content_type_get_icon(
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string contentType);

    [DllImport(Gio)]
    private static extern IntPtr
        g_themed_icon_get_names(
            IntPtr icon);

    // ============================================================
    // GObject
    // ============================================================

    [DllImport(GObject)]
    private static extern void g_object_unref(
        IntPtr obj);

    // ============================================================
    // GLib
    // ============================================================

    [DllImport(Glib)]
    private static extern void g_error_free(
        IntPtr error);


    // ============================================================
    // MIME type
    // ============================================================

    private static string? GetMimeType(
        string filePath)
    {
        if (Directory.Exists(filePath))
            return "inode/directory";

        IntPtr file = IntPtr.Zero;
        IntPtr info = IntPtr.Zero;
        IntPtr error = IntPtr.Zero;

        try
        {
            file = g_file_new_for_path(filePath);

            if (file == IntPtr.Zero)
                return null;

            info = g_file_query_info(
                file,
                "standard::content-type",
                0,
                IntPtr.Zero,
                out error);

            if (info == IntPtr.Zero)
                return null;

            var typePtr =
                g_file_info_get_attribute_string(
                    info,
                    "standard::content-type");

            if (typePtr == IntPtr.Zero)
                return null;

            return Marshal.PtrToStringUTF8(typePtr);
        }
        finally
        {
            if (error != IntPtr.Zero)
                g_error_free(error);

            if (info != IntPtr.Zero)
                g_object_unref(info);

            if (file != IntPtr.Zero)
                g_object_unref(file);
        }
    }

    // ============================================================
    // GIO themed icon names
    // ============================================================

    private static List<string> GetIconNames(
        string mimeType)
    {
        var result = new List<string>();

        IntPtr icon = IntPtr.Zero;

        try
        {
            icon =
                g_content_type_get_icon(mimeType);

            if (icon == IntPtr.Zero)
                return result;

            var names =
                g_themed_icon_get_names(icon);

            if (names == IntPtr.Zero)
                return result;

            return ReadStringArray(names);
        }
        finally
        {
            if (icon != IntPtr.Zero)
                g_object_unref(icon);
        }
    }

    private static List<string> ReadStringArray(
        IntPtr array)
    {
        var result =
            new List<string>();

        if (array == IntPtr.Zero)
            return result;

        var pointerSize = IntPtr.Size;

        for (var i = 0; i < 100; i++)
        {
            var ptr =
                Marshal.ReadIntPtr(
                    array,
                    i * pointerSize);

            if (ptr == IntPtr.Zero)
                break;

            var value =
                Marshal.PtrToStringUTF8(ptr);

            if (!string.IsNullOrWhiteSpace(value))
                result.Add(value);
        }

        return result;
    }

    // ============================================================
    // Find icon
    // ============================================================

    private static string? FindIcon(
        IEnumerable<string> iconNames,
        int size)
    {
        var home =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        var roots = new[]
        {
            Path.Combine(
                home,
                ".local",
                "share",
                "icons"),

            Path.Combine(
                home,
                ".icons"),

            "/usr/local/share/icons",
            "/usr/share/icons",

            "/usr/local/share/pixmaps",
            "/usr/share/pixmaps"
        };

        foreach (var iconName in iconNames)
        {
            if (string.IsNullOrWhiteSpace(iconName))
                continue;

            // Prefer normal icons over symbolic icons.
            if (iconName.EndsWith(
                    "-symbolic",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var root in roots)
            {
                var path =
                    FindIconInRoot(
                        root,
                        iconName,
                        size);

                if (path != null)
                    return path;
            }
        }

        return null;
    }

    private static string? FindIconInRoot(
        string root,
        string iconName,
        int requestedSize)
    {
        if (!Directory.Exists(root))
            return null;

        var extensions = new[]
        {
            ".png",
            ".svg",
            ".xpm"
        };

        var sizes = new[]
        {
            requestedSize,
            48,
            32,
            24,
            22,
            16,
            64,
            128,
            256
        };

        var categories = new[]
        {
            "mimetypes",
            "places",
            "actions",
            "categories",
            "devices",
            "status"
        };

        // Normal sized icons.
        foreach (var size in sizes.Distinct())
        {
            foreach (var category in categories)
            {
                foreach (var extension in extensions)
                {
                    var path = Path.Combine(
                        root,
                        $"{size}x{size}",
                        category,
                        iconName + extension);

                    if (File.Exists(path))
                        return path;
                }
            }
        }

        // @2x icons.
        foreach (var size in sizes.Distinct())
        {
            foreach (var category in categories)
            {
                foreach (var extension in extensions)
                {
                    var path = Path.Combine(
                        root,
                        $"{size}x{size}@2",
                        category,
                        iconName + extension);

                    if (File.Exists(path))
                        return path;
                }
            }
        }

        // SVG scalable icons.
        foreach (var category in categories)
        {
            foreach (var extension in extensions)
            {
                var path = Path.Combine(
                    root,
                    "scalable",
                    category,
                    iconName + extension);

                if (File.Exists(path))
                    return path;
            }
        }

        // Last resort.
        try
        {
            foreach (var extension in extensions)
            {
                var files =
                    Directory.EnumerateFiles(
                        root,
                        iconName + extension,
                        SearchOption.AllDirectories);

                var match =
                    files.FirstOrDefault();

                if (match != null)
                    return match;
            }
        }
        catch
        {
            // Ignore inaccessible directories.
        }

        return null;
    }


   
    public static string? GetIconPath(string filePath)
    {
        if (!OperatingSystem.IsLinux())
            return null;

        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        if (!File.Exists(filePath) &&
            !Directory.Exists(filePath))
            return null;

        try
        {
            var mimeType = GetMimeType(filePath);
            if(_cachedMimeTypes.TryGetValue(mimeType ?? string.Empty, out var cachedIconPath))
            {
                return cachedIconPath;
            }
            if (string.IsNullOrWhiteSpace(mimeType))
                return null;

            var iconNames = GetIconNames(mimeType);

            if (iconNames.Count == 0)
                return null;
            var iconPath = FindIcon(iconNames, 64);
            _cachedMimeTypes[mimeType] = iconPath!;
            return iconPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetIconPath failed: {ex}");
            return null;
        }
    }
}