using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using ScratchClip.Models;
using ScratchClip.Services;
using ScratchClip.Services.ClipboardService;

namespace ScratchClip.Manager;

public static class ClipboardManager
{
    private static string? _lastSignature;
    public static Action<AClipboardItem>? OnClipboardItemAdded;
    public static Action<AClipboardItem>? OnSelectExistingClipboardItem;
    public static Action<AClipboardItem>? OnRemoveExistingClipboardItem;
    public static Action? OnClearExistingClipboardItem;

    private static readonly Dictionary<ClipboardDataFormat, AClipboardService> ClipboardServices;
    private static AClipboardItem? _selectedClipboardItem;

    public static int MaxItemsInHistory { get; set; } = 20;
    static ClipboardManager()
    {
        ClipboardServices = new Dictionary<ClipboardDataFormat, AClipboardService>();
        ClipboardServices[ClipboardDataFormat.Text] = new TextClipboardService();
        ClipboardServices[ClipboardDataFormat.Image] = new ImageClipboardService();
        ClipboardServices[ClipboardDataFormat.Storage] = new StorageClipboardService();
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
                _ = ClipboardServices[value.Format].CopyData(value);
                OnSelectExistingClipboardItem?.Invoke(value);
            }
        }
    }

    public static IReadOnlyList<AClipboardItem> GetClipboardHistorySnapshot()
    {
        return ClipboardHistory.ToList();
    }

    public static void LoadClipboardHistory(IEnumerable<AClipboardItem> items)
    {
        ClipboardHistory.Clear();
        _selectedClipboardItem = null;

        foreach (var item in items.OrderBy(q => q.Timestamp).Take(MaxItemsInHistory))
        {
            item.OnDelete += DeleteClipboardItem;
            ClipboardHistory.Add(item);
            OnClipboardItemAdded?.Invoke(item);
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
            if (_lastSignature != null)
                if (!await HasCheckDataChanged(type.Value, _lastSignature))
                    return;

            var item = await GetItemAsync(type.Value);
            if (item == null)
                return;
            _lastSignature = item.Signature;
            var existingItem = ClipboardHistory.FirstOrDefault(q => q.Signature == item.Signature);
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
                if(ClipboardHistory.Count >= MaxItemsInHistory)
                {
                    var lastItem = ClipboardHistory.Last();
                    ClipboardHistory.Remove(lastItem);
                    OnRemoveExistingClipboardItem?.Invoke(lastItem);
                }
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
        if (data.Formats.Any(q => q == DataFormat.File)) return ClipboardDataFormat.Storage;
        if (data.Formats.Any(q => q == DataFormat.Bitmap)) return ClipboardDataFormat.Image;
        if (data.Formats.Any(q => q == DataFormat.Text)) return ClipboardDataFormat.Text;
        return null;
    }

    private static async Task<AClipboardItem?> GetItemAsync(ClipboardDataFormat type)
    {
        AClipboardItem? item = null;
        if (ClipboardServices.TryGetValue(type, out var service))
        {
            item = await service.GetItemAsync(type);
            if (item == null) return item;
            await service.CreateSignature(item);
            item.OnDelete += DeleteClipboardItem;
        }

        return item;
    }

    private static async Task<bool> HasCheckDataChanged(
        ClipboardDataFormat type,
        string signature)
    {
        var existingItem = ClipboardHistory
            .FirstOrDefault(q => q.Signature == signature);

        if (existingItem == null)
            return true;

        if (existingItem.Format != type)
            return true;
        var data = await ClipboardServices[type].GetClipboardData();
        if(data == null)
            return false;
        return !await ClipboardServices[type].IsDataSame(existingItem, data);
    }
    
    
    public static async Task SetClipboardItemAsync(AClipboardItem targetItem)
    {
        _lastSignature = targetItem.Signature;
        var clipboard = GetClipboard();
        if (clipboard == null)
            return;

        await ClipboardServices[targetItem.Format].CopyData(targetItem);
    }

    public static void ClearClipboardHistory()
    {
        ClipboardHistory.Clear();
        OnClearExistingClipboardItem?.Invoke();
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