using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using XClip.Models;
using ImageClipboardItem = XClip.Models.TextType.ImageClipboardItem;

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

        return Task.Run(() =>
        {
            using var stream = new MemoryStream();
            imageItem.Image.Save(stream, 100);
            stream.Position = 0;

            // Run hashing off the UI thread to avoid frame stalls on large images.
            item.Signature = Convert.ToHexString(SHA256.HashData(stream));
        });
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