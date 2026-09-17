using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ScratchClip.Helper;

public static class LinuxFileIconService
{
    private static readonly ConcurrentDictionary<string, string?> CachedIcons = new(StringComparer.OrdinalIgnoreCase);

    private const string Gio = "libgio-2.0.so.0";
    private const string GObject = "libgobject-2.0.so.0";
    private const string Glib = "libglib-2.0.so.0";
    private const string Gtk = "libgtk-3.so.0";

    // ============================================================
    // Native GIO / GObject / GTK3 Imports
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

    [DllImport(Gtk)]
    private static extern bool gtk_init_check(ref int argc, ref IntPtr argv);

    [DllImport(Gtk)]
    private static extern IntPtr gtk_icon_theme_get_default();

    [DllImport(Gtk)]
    private static extern IntPtr gtk_icon_theme_choose_icon(IntPtr theme, IntPtr[] iconNames, int size, int flags);

    [DllImport(Gtk)]
    private static extern IntPtr gtk_icon_info_get_filename(IntPtr iconInfo);

    [DllImport(GObject)]
    private static extern void g_object_unref(IntPtr obj);

    [DllImport(Glib)]
    private static extern void g_error_free(IntPtr error);

    private static readonly bool IsGtkInitialized;

    static LinuxFileIconService()
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                int argc = 0;
                IntPtr argv = IntPtr.Zero;
                IsGtkInitialized = gtk_init_check(ref argc, ref argv);
            }
            catch
            {
                IsGtkInitialized = false;
            }
        }
    }

    public static string? GetIconPath(string rawFilePath)
    {
        if (!OperatingSystem.IsLinux() || string.IsNullOrWhiteSpace(rawFilePath))
            return null;

        // 1. Sanitize input path (Strip 'file://' scheme)
        var filePath = SanitizeFilePath(rawFilePath);

        if (!File.Exists(filePath) && !Directory.Exists(filePath))
            return null;

        var cacheKey = Directory.Exists(filePath) ? "inode/directory" : Path.GetExtension(filePath).ToLowerInvariant();

        if (CachedIcons.TryGetValue(cacheKey, out var cachedPath))
            return cachedPath;

        var resolvedPath = ResolveIconPathInternal(filePath);
        CachedIcons[cacheKey] = resolvedPath;

        Console.WriteLine($"[LinuxFileIconService] Input: '{rawFilePath}' -> Sanitized: '{filePath}' | Resolved Icon: '{resolvedPath}'");

        return resolvedPath;
    }

    private static string SanitizeFilePath(string path)
    {
        if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return Uri.UnescapeDataString(new Uri(path).AbsolutePath);
            }
            catch
            {
                return path.Replace("file://", string.Empty);
            }
        }
        return path;
    }

    private static string? ResolveIconPathInternal(string filePath)
    {
        if (!IsGtkInitialized)
            return null;

        var mimeType = GetMimeType(filePath);
        if (string.IsNullOrWhiteSpace(mimeType))
            return null;

        var iconNames = GetIconNamesFromMime(mimeType);
        if (iconNames.Count == 0)
            return null;

        return ResolveGtkIconFromNames(iconNames, 48);
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

    private static List<string> GetIconNamesFromMime(string mimeType)
    {
        var result = new List<string>();
        IntPtr icon = IntPtr.Zero;

        try
        {
            icon = g_content_type_get_icon(mimeType);
            if (icon == IntPtr.Zero) return result;

            var namesPtr = g_themed_icon_get_names(icon);
            if (namesPtr == IntPtr.Zero) return result;

            var pointerSize = IntPtr.Size;
            for (var i = 0; i < 100; i++)
            {
                var ptr = Marshal.ReadIntPtr(namesPtr, i * pointerSize);
                if (ptr == IntPtr.Zero) break;

                var value = Marshal.PtrToStringUTF8(ptr);
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(value);
            }
        }
        finally
        {
            if (icon != IntPtr.Zero) g_object_unref(icon);
        }

        return result;
    }

    private static string? ResolveGtkIconFromNames(List<string> iconNames, int size)
    {
        IntPtr iconInfo = IntPtr.Zero;
        try
        {
            var iconTheme = gtk_icon_theme_get_default();
            if (iconTheme == IntPtr.Zero) return null;

            var ptrArray = new IntPtr[iconNames.Count + 1];
            for (int i = 0; i < iconNames.Count; i++)
            {
                ptrArray[i] = Marshal.StringToHGlobalAnsi(iconNames[i]);
            }
            ptrArray[iconNames.Count] = IntPtr.Zero;

            iconInfo = gtk_icon_theme_choose_icon(iconTheme, ptrArray, size, 0);

            for (int i = 0; i < iconNames.Count; i++)
            {
                if (ptrArray[i] != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrArray[i]);
            }

            if (iconInfo == IntPtr.Zero) return null;

            var filenamePtr = gtk_icon_info_get_filename(iconInfo);
            if (filenamePtr == IntPtr.Zero) return null;

            return Marshal.PtrToStringUTF8(filenamePtr);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (iconInfo != IntPtr.Zero) g_object_unref(iconInfo);
        }
    }
}