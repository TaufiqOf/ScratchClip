using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ScratchClip.Models;

namespace ScratchClip.Services;

internal abstract class AClipboardService
{
    protected static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is { } window)
            return TopLevel.GetTopLevel(window)?.Clipboard;

        return null;
    }

    protected static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is { } window)
            return TopLevel.GetTopLevel(window)?.StorageProvider;

        return null;
    }

    public abstract Task<AClipboardItem?> GetItemAsync(ClipboardDataFormat type);
    public abstract Task CreateSignature(AClipboardItem item);
    public abstract Task CopyData(AClipboardItem value);
    public abstract Task<object?> GetClipboardData();
    public abstract Task<bool> IsDataSame(AClipboardItem existingItem, object data);
}