using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using ScratchClip.Models;
using ImageClipboardItem = ScratchClip.Models.ImageClipboardItem;

namespace ScratchClip.Services.ClipboardService;

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

            imageItem.Image.Save(
                stream,
                PngBitmapEncoderOptions.Default);
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

    public override async Task<bool> IsDataSame(AClipboardItem existingItem, object data)
    {
        var imageData = data as Bitmap;
        if (imageData == null || existingItem is not ImageClipboardItem imageItem || imageItem.Image == null)
            return false;
        return await BitmapsAreEqual(imageItem.Image, imageData);
    }
    private static async Task<bool> BitmapsAreEqual(Bitmap a, Bitmap b)
    {
        if (a.PixelSize != b.PixelSize)
            return false;

        if (a.Dpi != b.Dpi)
            return false;

        var bytesA = await Task.Run(() =>
        {
            using var stream = new MemoryStream();
            a.Save(
                stream,
                PngBitmapEncoderOptions.Default);
            return stream.ToArray();
        });

        var bytesB = await Task.Run(() =>
        {
            using var stream = new MemoryStream();
            b.Save(
                stream,
                PngBitmapEncoderOptions.Default);
            return stream.ToArray();
        });

        return bytesA.AsSpan().SequenceEqual(bytesB);
    }
}