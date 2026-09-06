using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;

namespace XClip.Models.TextType;

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


    [RelayCommand]
    private void Delete()
    {
        OnDelete?.Invoke(this);
    }
}