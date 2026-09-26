using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using AvaloniaEdit.Utils;
using ScratchClip.Helper;
using ScratchClip.Models;
using ImageClipboardItem = ScratchClip.Models.ImageClipboardItem;

namespace ScratchClip.Manager;

public static class ClipboardHistoryManager
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScratchClip");

    public static async Task<IReadOnlyList<AClipboardItem>> Load(
        string? password)
    {
        try
        {
            var path = Path.Combine(
                FolderPath,
                "history.enc");

            if (!File.Exists(path))
                return Array.Empty<AClipboardItem>();

            var encrypted = await File.ReadAllBytesAsync(path);

            var plaintext = FileEncryption.Decrypt(
                encrypted,
                password);

            var json = Encoding.UTF8.GetString(plaintext);

            var records =
                JsonSerializer.Deserialize<List<ClipboardHistoryRecord>>(
                    json)
                ?? new List<ClipboardHistoryRecord>();

            var tasks = await Task.WhenAll(
                records.Select(ToClipboardItem));

            return tasks
                .Where(x => x != null)
                .Cast<AClipboardItem>()
                .ToList();
        }
        catch (CryptographicException)
        {
            // Wrong password or modified/corrupt file.
            return Array.Empty<AClipboardItem>();
        }
        catch (Exception ex)
        {
            NotificationHelper.Error("Error",$"Failed to load clipboard history: {ex.Message}");
            return Array.Empty<AClipboardItem>();
        }
    }

    public static void Save(
        IReadOnlyList<AClipboardItem> items,
        string? password)
    {
        try
        {
            Directory.CreateDirectory(FolderPath);

            var records = items
                .Select(ToRecord)
                .Where(record => record != null)
                .Cast<ClipboardHistoryRecord>()
                .ToList();

            var json = JsonSerializer.Serialize(
                records,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            var plaintext = Encoding.UTF8.GetBytes(json);
            var encrypted = FileEncryption.Encrypt(
                plaintext,
                password);

            var path = Path.Combine(
                FolderPath,
                "history.enc");

            File.WriteAllBytes(path, encrypted);
        }
        catch (Exception ex)
        {
            NotificationHelper.Error("Error",$"Failed to save clipboard history: {ex.Message}");
        }
    }

    private static ClipboardHistoryRecord? ToRecord(AClipboardItem item)
    {
        if (item is TextClipboardItem textItem)
            return new ClipboardHistoryRecord
            {
                Format = ClipboardDataFormat.Text,
                Text = textItem.Text,
                DisplayText = textItem.DisplayText,
                Signature = textItem.Signature,
                Timestamp = textItem.Timestamp,
                MataData = textItem.MataData!,
                Tags = textItem.Tags.ToList(),
                IsPinned = textItem.IsPinned,
                Note = textItem.Note
            };

        if (item is ImageClipboardItem imageItem && imageItem.Image != null)
        {
            using var stream = new MemoryStream();
            imageItem.Image.Save(
                stream,
                PngBitmapEncoderOptions.Default);
            return new ClipboardHistoryRecord
            {
                Format = ClipboardDataFormat.Image,
                DisplayText = imageItem.DisplayText,
                Text = imageItem.Text,
                Signature = imageItem.Signature,
                Timestamp = imageItem.Timestamp,
                ImageBase64 = Convert.ToBase64String(stream.ToArray()),
                MataData = imageItem.MataData!,
                Tags = imageItem.Tags.ToList(),
                IsPinned = imageItem.IsPinned,
                Note = imageItem.Note
            };
        }

        if (item is StorageClipboardItem storageItem)
            return new ClipboardHistoryRecord
            {
                Format = ClipboardDataFormat.Storage,
                DisplayText = storageItem.DisplayText,
                Signature = storageItem.Signature,
                Timestamp = storageItem.Timestamp,
                Paths = storageItem.Paths,
                MataData = storageItem.MataData!,
                Tags = storageItem.Tags.ToList(),
                IsPinned = storageItem.IsPinned,
                Note = storageItem.Note
            };

        return null;
    }

    private static async Task<AClipboardItem?> ToClipboardItem(ClipboardHistoryRecord record)
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
                    MataData = record.MataData!,
                    IsPinned = record.IsPinned,
                    Note = record.Note
                };
                item.Tags.Clear();
                item.Tags.AddRange(record.Tags ?? new List<string>());
                await item.UpdateByTags();
                return item;
            }
            case ClipboardDataFormat.Image:
            {
                if (string.IsNullOrWhiteSpace(record.ImageBase64))
                    return null;

                var bytes = Convert.FromBase64String(record.ImageBase64);
                using var stream = new MemoryStream(bytes);

                var imageClipboardItem = new ImageClipboardItem
                {
                    Format = ClipboardDataFormat.Image,
                    Text = record.Text ?? string.Empty,
                    DisplayText = string.IsNullOrWhiteSpace(record.DisplayText) ? "Image" : record.DisplayText,
                    Signature = record.Signature ?? string.Empty,
                    Timestamp = record.Timestamp == default ? DateTime.Now : record.Timestamp,
                    Image = new Bitmap(stream),
                    MataData = record.MataData!,
                    IsPinned = record.IsPinned,
                    Note = record.Note
                };
                imageClipboardItem.Tags.Clear();
                imageClipboardItem.Tags.AddRange(record.Tags ?? new List<string>());
                return imageClipboardItem;
            }
            case ClipboardDataFormat.Storage:
            {
                var storageClipboardItem = new StorageClipboardItem
                {
                    Format = ClipboardDataFormat.Storage,
                    Text = record.Text ?? string.Empty,
                    DisplayText = string.IsNullOrWhiteSpace(record.DisplayText) ? "Storage" : record.DisplayText,
                    Signature = record.Signature ?? string.Empty,
                    Timestamp = record.Timestamp == default ? DateTime.Now : record.Timestamp,
                    MataData = record.MataData!,
                    IsPinned = record.IsPinned,
                    Note = record.Note
                };
                storageClipboardItem.Paths = record.Paths ?? new List<string>();
                storageClipboardItem.Tags.Clear();
                storageClipboardItem.Tags.AddRange(record.Tags ?? new List<string>());

                return storageClipboardItem;
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

        public List<string>? Paths { get; set; }
        public List<string>? MataData { get; set; }
        public List<string>? Tags { get; set; }
        public bool IsPinned { get; set; }
        public string? Note { get; set; }
    }
}