using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace ScratchClip.Models;

public class ImageClipboardItem : AClipboardItem
{
    public ImageClipboardItem()
    {
        ClipboardType = ClipboardType.Image;
        Tags.Add("IMAGE");
    }

    public Bitmap? Image
    {
        get;
        set
        {
            if (Equals(value, field)) return;
            field = value;
            OnPropertyChanged();
        }
    }


    public override Task<object?> GetData()
    {
        return Task.FromResult<object?>(Image);
    }

    public override string SuggestedFile { get; } = "image.png";

    public override void Delete()
    {
        OnDelete?.Invoke(this);
    }

    public override Task OpenItem()
    {
        //get temporary file path
        var tempFilePath = Path.Combine(Path.GetTempPath(), "ScratchClip",
            Guid.NewGuid().ToString(), ".png");
        //open the file with the default application
        Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath) ?? string.Empty);
        Image?.Save(tempFilePath, new PngBitmapEncoderOptions());
        Process.Start(new ProcessStartInfo(tempFilePath)
            { UseShellExecute = true });
        return Task.CompletedTask;
    }
}