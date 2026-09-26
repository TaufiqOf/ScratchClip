using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using ScratchClip.Helper;

namespace ScratchClip.Models.TextType;

public partial class DownloadLinkTextType : ATextType
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    private static readonly HashSet<string> KnownDownloadExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz",
            ".exe", ".msi", ".deb", ".rpm", ".apk", ".dmg", ".pkg", ".appimage",
            ".pdf", ".epub", ".mobi",
            ".mp3", ".wav", ".flac", ".ogg", ".m4a",
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm",
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg",
            ".iso", ".img",
            ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".torrent"
        };

    public List<string> Metadata { get; set; }

    public DownloadLinkTextType(
        string text,
        ObservableCollection<string> tags,
        List<string>? metadata)
        : base(text, tags)
    {
        Icon = Icon.ArrowDownload;
        Metadata = metadata ?? new List<string>();
    }

    public override string DisplayName => "DOWNLOAD LINK";

    public override string SuggestedExtension => ".txt";

    public bool IsDownloading
    {
        get;
        private set
        {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public double DownloadProgress
    {
        get;
        private set
        {
            if (Math.Abs(value - field) < 0.01)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public string FileName
    {
        get;
        private set
        {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string FileSize
    {
        get;
        private set
        {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public override bool IsMatch(string text)
    {
        if (!TryGetHttpUri(text, out var uri))
            return false;

        if (LooksLikeDownloadByPath(uri))
            return true;

        if (LooksLikeDownloadByQuery(uri))
            return true;

        return false;
    }

    public override Task PopulateMetadataAsync(string text)
    {
        Metadata.Clear();

        if (!TryGetHttpUri(text, out var uri))
            return Task.CompletedTask;

        Metadata.Add($"Host: {uri.Host}");
        Metadata.Add($"Path: {uri.AbsolutePath}");

        var fileName = GetFileNameFromUri(uri);

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName;
            Metadata.Add($"File: {fileName}");
        }
        else
        {
            FileName = string.Empty;
        }
        using var response = HttpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead).Result;
        var totalBytes = response.Content.Headers.ContentLength;

        if (totalBytes.HasValue)
        {
            FileSize = FormatFileSize(totalBytes.Value);
        }
        var ext = Path.GetExtension(uri.AbsolutePath);

        if (!string.IsNullOrWhiteSpace(ext))
        {
            Metadata.Add($"Extension: {ext.ToLowerInvariant()}");
        }

        return Task.CompletedTask;
    }

    public override Task UpdateTagsAsync(string text)
    {
        if (!TryGetHttpUri(text, out var uri))
            return Task.CompletedTask;


        var ext = Path.GetExtension(uri.AbsolutePath);

        return Task.CompletedTask;
    }



    private static bool TryGetHttpUri(
        string? text,
        out Uri uri)
    {
        uri = null!;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (!Uri.TryCreate(
                text.Trim(),
                UriKind.Absolute,
                out var parsed))
        {
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttp &&
            parsed.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        uri = parsed;

        return true;
    }

    private static bool LooksLikeDownloadByPath(Uri uri)
    {
        var ext = Path.GetExtension(uri.AbsolutePath);

        return !string.IsNullOrWhiteSpace(ext) &&
               KnownDownloadExtensions.Contains(ext);
    }

    private static bool LooksLikeDownloadByQuery(Uri uri)
    {
        var q = uri.Query;

        if (string.IsNullOrWhiteSpace(q))
            return false;

        return q.Contains(
                   "download=",
                   StringComparison.OrdinalIgnoreCase) ||
               q.Contains(
                   "attachment=",
                   StringComparison.OrdinalIgnoreCase) ||
               q.Contains(
                   "filename=",
                   StringComparison.OrdinalIgnoreCase) ||
               q.Contains(
                   "file=",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFileNameFromUri(Uri uri)
    {
        var path = uri.AbsolutePath.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var file = Path.GetFileName(path);

        return file ?? string.Empty;
    }

    [RelayCommand]
    private async Task Download()
    {
        if (IsDownloading)
            return;

        if (!TryGetHttpUri(Text, out var uri))
            return;

        try
        {
            IsDownloading = true;
            DownloadProgress = 0;
            FileSize = string.Empty;

            using var response = await HttpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var fileName = GetFileNameFromResponse(response, uri);

            fileName = MakeSafeFileName(fileName);

            var downloadsFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "Downloads");

            Directory.CreateDirectory(downloadsFolder);

            var outputPath = GetUniqueFileName(
                Path.Combine(
                    downloadsFolder,
                    fileName));

            var totalBytes = response.Content.Headers.ContentLength;

            if (totalBytes.HasValue)
            {
                FileSize = FormatFileSize(totalBytes.Value);
            }

            await using var input = await response.Content.ReadAsStreamAsync();

            await using var output = new FileStream(
                outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            var buffer = new byte[81920];

            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await input.ReadAsync(buffer)) > 0)
            {
                await output.WriteAsync(
                    buffer.AsMemory(0, bytesRead));

                totalRead += bytesRead;

                if (totalBytes.HasValue &&
                    totalBytes.Value > 0)
                {
                    DownloadProgress = Math.Clamp(
                        totalRead * 100d / totalBytes.Value,
                        0,
                        100);
                }
            }

            DownloadProgress = 100;

            NotificationHelper.Success(
                "Download Succeeded",
                $"Successfully downloaded: {outputPath}");

            // Keep the completed state visible briefly through
            // the notification, then reset the progress indicator.
            DownloadProgress = 0;
        }
        catch (Exception ex)
        {
            NotificationHelper.Error(
                "Download Failed",
                $"Failed to download file: {ex.Message}");

            DownloadProgress = 0;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private static string GetFileNameFromResponse(
        HttpResponseMessage response,
        Uri uri)
    {
        var contentDisposition =
            response.Content.Headers.ContentDisposition;

        var fileName =
            contentDisposition?.FileNameStar ??
            contentDisposition?.FileName;

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            fileName = fileName.Trim('"');

            return fileName;
        }

        fileName = GetFileNameFromUri(uri);

        if (!string.IsNullOrWhiteSpace(fileName))
            return fileName;

        return "download";
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(
                invalidChar,
                '_');
        }

        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            name = "download";

        return name;
    }

    private static string GetUniqueFileName(string path)
    {
        if (!File.Exists(path))
            return path;

        var directory = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        var counter = 1;
        string candidate;

        do
        {
            candidate = Path.Combine(
                directory,
                $"{name} ({counter}){extension}");

            counter++;
        }
        while (File.Exists(candidate));

        return candidate;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024d:0.0} KB";

        if (bytes < 1024L * 1024 * 1024)
            return $"{bytes / (1024d * 1024):0.0} MB";

        return $"{bytes / (1024d * 1024 * 1024):0.0} GB";
    }
}
