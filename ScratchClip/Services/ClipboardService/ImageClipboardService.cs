using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using ScratchClip.Models;
using ImageClipboardItem = ScratchClip.Models.ImageClipboardItem;

namespace ScratchClip.Services.ClipboardService;

internal class ImageClipboardService : AClipboardService
{
    public override async Task<AClipboardItem?> GetItemAsync(ClipboardDataFormat type)
    {
        var clipboard = GetClipboard();
        if (clipboard == null)
            return null;

        var bitmap = await clipboard.TryGetBitmapAsync();
        if (bitmap == null)
            return null;
        var clipboardData = await clipboard.TryGetDataAsync();
        return new ImageClipboardItem
        {
            Format = ClipboardDataFormat.Image,
            Timestamp = DateTime.Now,
            DisplayText = "Image",
            MataData = clipboardData?.Formats.Select(f => f.Identifier).ToList() ?? new List<string>(),
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
        if (data is not Bitmap imageData || existingItem is not ImageClipboardItem imageItem || imageItem.Image == null)
            return false;

        return await Task.Run(() => BitmapsAreEqual(imageItem.Image, imageData));
    }

    private static bool BitmapsAreEqual(Bitmap a, Bitmap b)
    {
        try
        {
            // 1. Quick structural checks
            if (a.PixelSize != b.PixelSize)
                return false;

            if (a.Dpi != b.Dpi)
                return false;

            if (a.Format != b.Format)
                return false;

            // 2. Calculate buffer dimensions
            int width = a.PixelSize.Width;
            int height = a.PixelSize.Height;
            int bytesPerPixel = 4; // Standard 32-bit pixel depth (BGRA/RGBA)
            int stride = width * bytesPerPixel;
            int bufferSize = stride * height;

            byte[] bytesA = new byte[bufferSize];
            byte[] bytesB = new byte[bufferSize];

            // 3. Pin arrays and copy pixels directly
            GCHandle handleA = GCHandle.Alloc(bytesA, GCHandleType.Pinned);
            GCHandle handleB = GCHandle.Alloc(bytesB, GCHandleType.Pinned);

            try
            {
                a.CopyPixels(default, handleA.AddrOfPinnedObject(), bufferSize, stride);
                b.CopyPixels(default, handleB.AddrOfPinnedObject(), bufferSize, stride);
            }
            finally
            {
                handleA.Free();
                handleB.Free();
            }

            // 4. Fast byte sequence equality check
            return bytesA.AsSpan().SequenceEqual(bytesB);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}