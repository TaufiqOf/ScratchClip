using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using XClip.Models;

namespace XClip.Services.ClipboardService;

internal class ImageClipboardService : AClipboardService
{
    public override async Task<AClipboardItem?> GetDataAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard == null)
            return null;

        var bitmap = await clipboard.TryGetBitmapAsync();
        if (bitmap == null)
            return null;

        return new ImageClipboardItem
        {
            Format = ClipboardDataFormat.Image,
            Timestamp = DateTime.Now,
            DisplayText = "Image",
            Image = bitmap
        };
    }

    public override Task CreateSignature(AClipboardItem item)
    {
        if (item is not ImageClipboardItem imageItem || imageItem.Image == null)
            return Task.CompletedTask;

        using var stream = new MemoryStream();
        imageItem.Image.Save(stream, 100);

        // Better to use a stable hash of the image bytes.
        item.Signature = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(stream.ToArray())
        );

        return Task.CompletedTask;
    }

    public override async Task CopyData(AClipboardItem value)
    {
        var clipboard = GetClipboard();

        if (clipboard == null || value is not ImageClipboardItem imageItem || imageItem.Image == null)
            return;

        await clipboard.SetBitmapAsync(imageItem.Image);
    }

    public override async Task<object?> GetClipboardData()
    {
        var clipboard = GetClipboard();
        if (clipboard == null)
            return null;

        return await clipboard.TryGetBitmapAsync();
    }
}