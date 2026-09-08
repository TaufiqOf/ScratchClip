using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using ScratchClip.Models;

namespace ScratchClip.Services;

internal abstract class AClipboardService
{
    protected static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow?.Clipboard;

        return null;
    }

    public abstract Task<AClipboardItem?> GetDataAsync();
    public abstract Task CreateSignature(AClipboardItem item);
    public abstract Task CopyData(AClipboardItem value);
    public abstract Task<object?> GetClipboardData();
    public abstract Task<bool> IsDataSame(AClipboardItem existingItem, object data);
}