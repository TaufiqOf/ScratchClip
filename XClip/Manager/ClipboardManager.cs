using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using XClip.Models;
using XClip.Services;
using XClip.Services.ClipboardService;

namespace XClip.Manager;

public static class ClipboardManager
{
    public static Action<AClipboardItem>? OnClipboardItemAdded;
    public static Action<AClipboardItem>? OnSelectExistingClipboardItem;
    public static Action<AClipboardItem>? OnRemoveExistingClipboardItem;

    private static readonly Dictionary<ClipboardDataFormat, AClipboardService> _clipboardServices;
    private static AClipboardItem? _selectedClipboardItem;

    static ClipboardManager()
    {
        _clipboardServices = new Dictionary<ClipboardDataFormat, AClipboardService>();
        _clipboardServices[ClipboardDataFormat.Text] = new TextClipboardService();
        _clipboardServices[ClipboardDataFormat.Image] = new ImageClipboardService();
    }

    private static List<AClipboardItem> ClipboardHistory { get; } = new();

    public static AClipboardItem? SelectedClipboardItem
    {
        get => _selectedClipboardItem;
        set
        {
            if (value != null && ClipboardHistory.Contains(value))
            {
                _selectedClipboardItem = value;
                _clipboardServices[value.Format].CopyData(value);
                OnSelectExistingClipboardItem?.Invoke(value);
            }
        }
    }

    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow?.Clipboard;

        return null;
    }


    public static async Task CheckClipboard()
    {
        try
        {
            var clipboard = GetClipboard();
            if (clipboard == null)
                return;
            var type = await GetDataTypeAsync(clipboard);
            if (type == null)
                return;
            AClipboardItem? item = null;
            item = await GetItemAsync(type.Value);
            if (item == null)
                return;
            var existingItem = ClipboardHistory.FirstOrDefault(q => q.Signature == item?.Signature);
            if (existingItem != null)
            {
                if (SelectedClipboardItem != existingItem)
                {
                    _selectedClipboardItem = existingItem;
                    OnSelectExistingClipboardItem?.Invoke(existingItem);
                }
            }
            else
            {
                ClipboardHistory.Insert(0, item);
                OnClipboardItemAdded?.Invoke(item);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static async Task<ClipboardDataFormat?> GetDataTypeAsync(IClipboard clipboard)
    {
        var data = await clipboard.TryGetDataAsync();
        if (data == null) return null;
        if (data.Formats.Any(q => q == DataFormat.Text)) return ClipboardDataFormat.Text;
        if (data.Formats.Any(q => q == DataFormat.Bitmap)) return ClipboardDataFormat.Image;
        if (data.Formats.Any(q => q == DataFormat.File)) return ClipboardDataFormat.StorageItems;
        return null;
    }

    private static async Task<AClipboardItem?> GetItemAsync(ClipboardDataFormat type)
    {
        AClipboardItem? item = null;
        if (_clipboardServices.TryGetValue(type, out var service))
        {
            item = await service.GetDataAsync();
            if (item == null) return item;
            await service.CreateSignature(item);
            item.OnDelete += DeleteClipboardItem;
        }

        return item;
    }


    public static async Task SetClipboardItemAsync(AClipboardItem targetItem)
    {
        await _clipboardServices[targetItem.Format].CopyData(targetItem);
    }

    public static void ClearClipboardHistory()
    {
        ClipboardHistory.Clear();
    }

    public static async Task ClearClipboardData()
    {
        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            await clipboard.ClearAsync();
            await clipboard.SetTextAsync("");
        }
    }

    public static async void DeleteClipboardItem(AClipboardItem item)
    {
        if (SelectedClipboardItem == item)
            SelectedClipboardItem = null;
        var clipboard = GetClipboard();
        if (clipboard != null) await clipboard.SetTextAsync("");

        ClipboardHistory.Remove(item);
        OnRemoveExistingClipboardItem?.Invoke(item);
    }
}