using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;

namespace ScratchClip.Models;

public partial class ImageClipboardItem : AClipboardItem
{
    public ImageClipboardItem()
    {
        Tags.Add("Image");
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


    public override void Delete()
    {
        OnDelete?.Invoke(this);
    }

 
}