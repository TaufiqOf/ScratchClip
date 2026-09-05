using System;
using XClip.ViewModels;

namespace XClip.Models;

public abstract class AClipboardItem : ViewModelBase
{
    public Action<AClipboardItem>? OnDelete { get; set; }

    public string Text
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    } = string.Empty;

    public int DisplayIndex
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public string DisplayText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public ClipboardDataFormat Format
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = ClipboardDataFormat.Text;

    public string Signature
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public DateTime Timestamp
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = DateTime.Now;
}