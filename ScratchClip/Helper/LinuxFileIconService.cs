using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ScratchClip.Helper;

public static class LinuxFileIconService
{
    private static readonly ConcurrentDictionary<string, string?> CachedIcons = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] IconSearchPaths;

    static LinuxFileIconService()
    {
        var paths = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (!string.IsNullOrEmpty(home))
        {
            paths.Add(Path.Combine(home, ".local", "share", "icons"));
            paths.Add(Path.Combine(home, ".icons"));
        }

        paths.Add("/usr/share/icons");
        paths.Add("/usr/share/pixmaps");

        IconSearchPaths = paths.ToArray();
    }

    public static string? GetIconPath(string rawFilePath)
    {
        if (!OperatingSystem.IsLinux() || string.IsNullOrWhiteSpace(rawFilePath))
            return null;

        var filePath = SanitizeFilePath(rawFilePath);

        if (!File.Exists(filePath) && !Directory.Exists(filePath))
            return null;

        bool isDir = Directory.Exists(filePath);
        var cacheKey = isDir ? "inode/directory" : Path.GetExtension(filePath).ToLowerInvariant();

        if (CachedIcons.TryGetValue(cacheKey, out var cachedPath))
            return cachedPath;

        var resolvedPath = ResolveIconPathInternal(filePath, isDir);
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

    private static string? ResolveIconPathInternal(string filePath, bool isDirectory)
    {
        if (isDirectory)
            return FindIconCandidates(new[] { "folder", "inode-directory", "folder-open" });

        var mimeType = GetMimeType(filePath);
        var candidates = new List<string>();

        if (!string.IsNullOrEmpty(mimeType))
        {
            // Exact MIME icon candidate (e.g. "application-pdf")
            var mimeFormatted = mimeType.Replace('/', '-');
            candidates.Add(mimeFormatted);
            candidates.Add($"gnome-mime-{mimeFormatted}");

            // Category candidate (e.g. "image-x-generic", "application-x-generic")
            var parts = mimeType.Split('/');
            if (parts.Length == 2)
            {
                candidates.Add($"{parts[0]}-{parts[1]}");
                candidates.Add($"{parts[0]}-x-generic");
            }
        }

        // Extension fallback candidates
        var ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        if (!string.IsNullOrEmpty(ext))
        {
            candidates.Add($"file-type-{ext}");
            candidates.Add(ext);
        }

        candidates.Add("text-x-generic");
        candidates.Add("unknown");

        return FindIconCandidates(candidates);
    }

    private static string? GetMimeType(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "xdg-mime",
                Arguments = $"query filetype \"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(300);

            return output.Contains('/') ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindIconCandidates(IEnumerable<string> candidates)
    {
        var extensions = new[] { ".svg", ".png", ".xpm" };

        foreach (var candidate in candidates)
        {
            foreach (var basePath in IconSearchPaths)
            {
                if (!Directory.Exists(basePath)) continue;

                try
                {
                    // Recursively scan icon theme paths for matching icon names
                    foreach (var file in Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                        
                        if (string.Equals(fileNameWithoutExt, candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            var ext = Path.GetExtension(file).ToLowerInvariant();
                            if (Array.Exists(extensions, e => e == ext))
                                return file;
                        }
                    }
                }
                catch
                {
                    // Ignore directory permission issues
                }
            }
        }

        return null;
    }
}