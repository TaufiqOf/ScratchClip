using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ScratchClip.Models;

namespace ScratchClip.Services.ClipboardService;

internal class StorageClipboardService : AClipboardService
{
    private readonly IStorageProvider? _storageProvider;

    public StorageClipboardService(IStorageProvider? storageProvider = null)
    {
        _storageProvider = storageProvider;
    }

    public override async Task<AClipboardItem?> GetItemAsync(ClipboardDataFormat type)
    {
        var clipboard = GetClipboard();
        if (clipboard == null)
            return null;

        var items = await GetStorageItemsAsync(clipboard);
        if (items == null || items.Count == 0)
            return null;

        var paths = items
            .Select(x => x.Path.IsAbsoluteUri ? x.Path.LocalPath : x.Path.ToString())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (paths.Count == 0)
            return null;

        var displayText = paths.Count == 1
            ? Path.GetFileName(paths[0])
            : $"{paths.Count} items";
        var clipboardData = await clipboard.TryGetDataAsync();
        return new StorageClipboardItem
        {
            Format = ClipboardDataFormat.Storage,
            Timestamp = DateTime.Now,
            DisplayText = displayText,
            Paths = paths,
            MataData= clipboardData?.Formats.Select(f => f.Identifier).ToList(),
        };
    }

    public override Task CreateSignature(AClipboardItem item)
    {
        if (item is not StorageClipboardItem storageItem || storageItem.Paths.Count == 0)
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            var sb = new StringBuilder();
            foreach (var path in storageItem.Paths.OrderBy(p => p, StringComparer.Ordinal))
            {
                sb.Append(path);
                if (File.Exists(path))
                {
                    sb.Append(':').Append(File.GetLastWriteTimeUtc(path).Ticks);
                }
                else if (Directory.Exists(path))
                {
                    sb.Append(':').Append(Directory.GetLastWriteTimeUtc(path).Ticks);
                }

                sb.Append(';');
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            item.Signature = Convert.ToHexString(SHA256.HashData(bytes));
        });
    }

    public override async Task CopyData(AClipboardItem value)
    {
        var clipboard = GetClipboard();
        if (clipboard == null || value is not StorageClipboardItem storageItem ||
            storageItem.Paths.Count == 0)
            return;

        var storageItems = new List<IStorageItem>();

        if (_storageProvider != null)
        {
            foreach (var path in storageItem.Paths)
            {
                if (File.Exists(path))
                {
                    var file = await _storageProvider.TryGetFileFromPathAsync(path);
                    if (file != null) storageItems.Add(file);
                }
                else if (Directory.Exists(path))
                {
                    var folder = await _storageProvider.TryGetFolderFromPathAsync(path);
                    if (folder != null) storageItems.Add(folder);
                }
            }
        }

        if (storageItems.Count > 0)
        {
            // 1. Create the item payload containing the file data
            var item = new DataTransferItem();
            foreach (var item1 in storageItems)
            {
                item.Set(DataFormat.File, item1);
            }

            // 2. Wrap it inside a DataTransfer container (which implements IAsyncDataTransfer)
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(item);

            // 3. Pass the DataTransfer object to the clipboard
            await clipboard.SetDataAsync(dataTransfer);
        }
    }

    public override async Task<object?> GetClipboardData()
    {
        var clipboard = GetClipboard();
        if (clipboard == null)
            return null;

        return await GetStorageItemsAsync(clipboard);
    }

    public override Task<bool> IsDataSame(AClipboardItem existingItem, object data)
    {
        if (existingItem is not StorageClipboardItem storageItem)
            return Task.FromResult(false);

        var currentPaths = ExtractPaths(data);
        if (currentPaths == null)
            return Task.FromResult(false);

        if (storageItem.Paths.Count != currentPaths.Count)
            return Task.FromResult(false);

        return Task.FromResult(storageItem.Paths.SequenceEqual(currentPaths));
    }

    private static async Task<IReadOnlyList<IStorageItem>?> GetStorageItemsAsync(IClipboard clipboard)
    {
        var rawData = await clipboard.TryGetFilesAsync();

        if (rawData is IEnumerable<IStorageItem> items)
        {
            return items.ToList();
        }

        return null;
    }

    private static List<string>? ExtractPaths(object? data)
    {
        if (data is IEnumerable<IStorageItem> storageItems)
        {
            return storageItems
                .Select(x => x.Path.IsAbsoluteUri ? x.Path.LocalPath : x.Path.ToString())
                .ToList();
        }

        if (data is IEnumerable<string> pathStrings)
        {
            return pathStrings.ToList();
        }

        return null;
    }
}