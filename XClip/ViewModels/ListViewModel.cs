using System;
using System.Collections.ObjectModel;
using XClip.Models;

namespace XClip.ViewModels;

public class ListViewModel : ViewModelBase
{
    public Action<AClipboardItem>? OnItemSelected { get; set; }
    public required ObservableCollection<AClipboardItem> FilteredHistory { get; set; }
    public AClipboardItem? SelectedItem
    {
        get;
        set
        {
            OnItemSelected?.Invoke(value);
            SetProperty(ref field, value);
        }
    }
}