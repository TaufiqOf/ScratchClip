using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;

namespace ScratchClip.Models;

public partial class ImageClipboardItem : AClipboardItem
{
    public ImageClipboardItem()
    {
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
}