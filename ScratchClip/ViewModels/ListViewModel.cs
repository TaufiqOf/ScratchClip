using System;
using System.Collections.ObjectModel;
using ScratchClip.Models;

namespace ScratchClip.ViewModels;

public class ListViewModel : ViewModelBase
{
    public Action<AClipboardItem>? OnItemSelected { get; set; }
    public required ObservableCollection<AClipboardItem> FilteredHistory { get; set; }
    public AClipboardItem? SelectedItem
    {
        get;
        set
        {
            
            if(SetProperty(ref field, value))
            {
                if (value != null)
                {
                    OnItemSelected?.Invoke(value);
                }
            }
        }
    }
}