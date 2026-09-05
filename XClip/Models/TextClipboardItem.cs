using System.Collections.Generic;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XClip.Models;

public partial class TextClipboardItem : AClipboardItem
{
    [RelayCommand]
    private void Delete()
    {
        OnDelete?.Invoke(this);
    }
}

public partial class ImageClipboardItem : AClipboardItem
{
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