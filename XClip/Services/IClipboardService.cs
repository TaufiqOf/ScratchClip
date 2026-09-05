using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using XClip.Models;

namespace XClip.Services;

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
    public abstract object GetClipboardData();
}