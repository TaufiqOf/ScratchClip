using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using ScratchClip.Helper;
using YoutubeExplode;
using YoutubeExplode.Converter;

namespace ScratchClip.Models.TextType;

public partial class YoutubeLinkTextType : ATextType
{
    private static readonly YoutubeClient Youtube = new();

    private static readonly HttpClient MetadataClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public List<string> Metadata { get; set; }

    public YoutubeLinkTextType(
        string text,
        ObservableCollection<string> tags,
        List<string>? metadata)
        : base(text, tags)
    {
        Icon = Icon.Video;

        Metadata = metadata ?? new List<string>();

        VideoTitle = "YouTube Video";
        VideoDescription = string.Empty;
        VideoHost = "youtube.com";
    }

    public override string DisplayName => "YOUTUBE";

    public override string SuggestedExtension => ".txt";

    public Bitmap? VideoThumbnail
    {
        get;
        set
        {
            if (Equals(field, value))
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public string VideoTitle
    {
        get;
        set
        {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public string VideoDescription
    {
        get;
        set
        {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public string VideoHost
    {
        get;
        set
        {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

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

    public override bool IsMatch(string text)
    {
        return TryGetYoutubeUri(text, out _);
    }

    public override async Task PopulateMetadataAsync(string text)
    {
        Metadata.Clear();

        if (!TryGetYoutubeUri(text, out var uri))
        {
            ResetPreview();
            return;
        }

        VideoHost = uri.Host;

        Metadata.Add($"Host: {uri.Host}");
        Metadata.Add($"Path: {uri.AbsolutePath}");

        var videoId = ExtractVideoId(uri);

        if (string.IsNullOrWhiteSpace(videoId))
        {
            ResetPreview();
            return;
        }

        Metadata.Add($"VideoId: {videoId}");

        await PopulateYoutubeMetadataAsync(uri.ToString());
    }

    public override Task UpdateTagsAsync(string text)
    {
        if (!TryGetYoutubeUri(text, out _))
            return Task.CompletedTask;

        AddTagIfMissing("video");

        return Task.CompletedTask;
    }

    private async Task PopulateYoutubeMetadataAsync(string videoUrl)
    {
        try
        {
            var video = await Youtube.Videos.GetAsync(videoUrl);

            VideoTitle = video.Title;

            Metadata.Add($"Title: {video.Title}");
            Metadata.Add($"Channel: {video.Author.ChannelTitle}");

            if (video.Duration.HasValue)
            {
                Metadata.Add($"Duration: {video.Duration.Value}");

                VideoDescription =
                    $"{video.Author.ChannelTitle} • " +
                    $"{video.Duration.Value:hh\\:mm\\:ss}";
            }
            else
            {
                VideoDescription = video.Author.ChannelTitle;
            }

            if (video.Thumbnails.Any())
            {
                var thumbnail = video.Thumbnails
                    .OrderByDescending(x => x.Resolution.Area)
                    .First();

                await PopulateThumbnailAsync(thumbnail.Url);
            }
        }
        catch
        {
            // Keep URL-only preview if metadata cannot be retrieved.
        }
    }

    private async Task PopulateThumbnailAsync(string thumbnailUrl)
    {
        try
        {
            var bytes = await MetadataClient.GetByteArrayAsync(
                thumbnailUrl);

            if (bytes.Length == 0)
            {
                VideoThumbnail = null;
                return;
            }

            await using var stream = new MemoryStream(bytes);

            VideoThumbnail = new Bitmap(stream);
        }
        catch
        {
            VideoThumbnail = null;
        }
    }

    private void ResetPreview()
    {
        VideoTitle = "YouTube Video";
        VideoDescription = string.Empty;
        VideoHost = string.Empty;
        VideoThumbnail = null;
        DownloadProgress = 0;
        IsDownloading = false;
    }

    private void AddTagIfMissing(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        foreach (var existing in Tags)
        {
            if (string.Equals(
                    existing,
                    tag,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        Tags.Add(tag);
    }

    private static bool TryGetYoutubeUri(
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

        var host = parsed.Host.ToLowerInvariant();

        var isYoutubeHost =
            host == "youtu.be" ||
            host == "youtube.com" ||
            host.EndsWith(
                ".youtube.com",
                StringComparison.Ordinal);

        if (!isYoutubeHost)
            return false;

        uri = parsed;

        return true;
    }

    private static string ExtractVideoId(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();

        if (host == "youtu.be")
            return uri.AbsolutePath.Trim('/');

        if (uri.AbsolutePath.Equals(
                "/watch",
                StringComparison.OrdinalIgnoreCase))
        {
            var query = uri.Query
                .TrimStart('?')
                .Split(
                    '&',
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in query)
            {
                var kv = part.Split('=', 2);

                if (kv.Length == 2 &&
                    kv[0].Equals(
                        "v",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(kv[1]);
                }
            }
        }

        var segments = uri.AbsolutePath.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length >= 2 &&
            (
                segments[0].Equals(
                    "shorts",
                    StringComparison.OrdinalIgnoreCase) ||

                segments[0].Equals(
                    "embed",
                    StringComparison.OrdinalIgnoreCase) ||

                segments[0].Equals(
                    "live",
                    StringComparison.OrdinalIgnoreCase)
            ))
        {
            return segments[1];
        }

        return string.Empty;
    }

    [RelayCommand]
    private void OpenVideo()
    {
        if (!TryGetYoutubeUri(Text, out var uri))
            return;

        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = uri.ToString(),
                    UseShellExecute = true
                });
        }
        catch
        {
            // Ignore open failures.
        }
    }

    [RelayCommand]
    private async Task Download()
    {
        if (IsDownloading)
            return;

        if (!TryGetYoutubeUri(Text, out var uri))
            return;

        try
        {
            IsDownloading = true;
            DownloadProgress = 0;

            var video = await Youtube.Videos.GetAsync(
                uri.ToString());

            var downloadsFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "Downloads");

            Directory.CreateDirectory(downloadsFolder);

            var safeTitle = MakeSafeFileName(video.Title);

            var outputPath = GetUniqueFileName(
                Path.Combine(
                    downloadsFolder,
                    $"{safeTitle}.mp4"));

            var progress = new Progress<double>(value =>
            {
                DownloadProgress = Math.Clamp(
                    value * 100,
                    0,
                    100);
            });

            await Youtube.Videos.DownloadAsync(
                video.Id,
                new ConversionRequestBuilder(outputPath)
                    .SetPreset(ConversionPreset.UltraFast)
                    .Build(),
                progress);
            NotificationHelper.Success("Download Succeeded",$"Successfully downloaded video: {outputPath}");
            DownloadProgress = 0;
        }
        catch (Exception ex)
        {
            NotificationHelper.Error("Download Failed",$"Failed to download video: {ex.Message}");

            DownloadProgress = 0;
        }
        finally
        {
            IsDownloading = false;
        }
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
            name = "youtube-video";

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
}