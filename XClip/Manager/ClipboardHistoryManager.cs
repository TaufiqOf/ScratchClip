using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia.Media.Imaging;
using XClip.Models;

namespace XClip.Manager;

public static class ClipboardHistoryManager
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XClip");

    private static readonly string FilePath = Path.Combine(FolderPath, "history.json");

    public static IReadOnlyList<AClipboardItem> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return Array.Empty<AClipboardItem>();

            var json = File.ReadAllText(FilePath);
            var records = JsonSerializer.Deserialize<List<ClipboardHistoryRecord>>(json) ?? new List<ClipboardHistoryRecord>();

            return records
                .Select(ToClipboardItem)
                .Where(item => item != null)
                .Cast<AClipboardItem>()
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load clipboard history: {ex.Message}");
            return Array.Empty<AClipboardItem>();
        }
    }

    public static void Save(IReadOnlyList<AClipboardItem> items)
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            var records = items
                .Select(ToRecord)
                .Where(record => record != null)
                .Cast<ClipboardHistoryRecord>()
                .ToList();

            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save clipboard history: {ex.Message}");
        }
    }

    private static ClipboardHistoryRecord? ToRecord(AClipboardItem item)
    {
        if (item is TextClipboardItem textItem)
        {
            return new ClipboardHistoryRecord
            {
                Format = ClipboardDataFormat.Text,
                Text = textItem.Text,
                DisplayText = textItem.DisplayText,
                Signature = textItem.Signature,
                Timestamp = textItem.Timestamp,
                IsWebSite = textItem.IsWebSite,
                WebsiteTitle = textItem.WebsiteTitle,
                WebsiteDescription = textItem.WebsiteDescription,
                WebsiteHost = textItem.WebsiteHost
            };
        }

        if (item is ImageClipboardItem imageItem && imageItem.Image != null)
        {
            using var stream = new MemoryStream();
            imageItem.Image.Save(stream, 100);

            return new ClipboardHistoryRecord
            {
                Format = ClipboardDataFormat.Image,
                DisplayText = imageItem.DisplayText,
                Signature = imageItem.Signature,
                Timestamp = imageItem.Timestamp,
                ImageBase64 = Convert.ToBase64String(stream.ToArray())
            };
        }

        return null;
    }

    private static AClipboardItem? ToClipboardItem(ClipboardHistoryRecord record)
    {
        switch (record.Format)
        {
            case ClipboardDataFormat.Text:
            {
                var text = record.Text ?? string.Empty;
                var item = new TextClipboardItem
                {
                    Format = ClipboardDataFormat.Text,
                    Text = text,
                    DisplayText = string.IsNullOrWhiteSpace(record.DisplayText) ? text : record.DisplayText,
                    Signature = string.IsNullOrWhiteSpace(record.Signature) ? HashText(text) : record.Signature,
                    Timestamp = record.Timestamp == default ? DateTime.Now : record.Timestamp,
                    WebsiteTitle = record.WebsiteTitle ?? string.Empty,
                    WebsiteDescription = record.WebsiteDescription ?? string.Empty,
                    WebsiteHost = record.WebsiteHost ?? string.Empty
                };

                _ = item.PopulateWebsiteMetadataAsync();

                return item;
            }
            case ClipboardDataFormat.Image:
            {
                if (string.IsNullOrWhiteSpace(record.ImageBase64))
                    return null;

                var bytes = Convert.FromBase64String(record.ImageBase64);
                using var stream = new MemoryStream(bytes);

                return new ImageClipboardItem
                {
                    Format = ClipboardDataFormat.Image,
                    DisplayText = string.IsNullOrWhiteSpace(record.DisplayText) ? "Image" : record.DisplayText,
                    Signature = record.Signature ?? string.Empty,
                    Timestamp = record.Timestamp == default ? DateTime.Now : record.Timestamp,
                    Image = new Bitmap(stream)
                };
            }
            default:
                return null;
        }
    }

    private static string HashText(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private sealed class ClipboardHistoryRecord
    {
        public ClipboardDataFormat Format { get; set; }
        public string? Text { get; set; }
        public string? DisplayText { get; set; }
        public string? Signature { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ImageBase64 { get; set; }
        public bool IsWebSite { get; set; }
        public string? WebsiteTitle { get; set; }
        public string? WebsiteDescription { get; set; }
        public string? WebsiteHost { get; set; }
    }
}
