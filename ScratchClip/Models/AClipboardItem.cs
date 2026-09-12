using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ScratchClip.ViewModels;

namespace ScratchClip.Models;

public abstract partial class AClipboardItem : ViewModelBase
{
    public Action<AClipboardItem>? OnEdit { get; set; }
    public Action<AClipboardItem>? OnDelete { get; set; }

    public ObservableCollection<string> Tags { get; set; } = new ObservableCollection<string>();

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

    public List<string?> MataData { get; set; } = new List<string?>();
    
    [RelayCommand]
    public abstract void Delete();

    [RelayCommand]
    public void Edit()
    {
        OnEdit?.Invoke(this);
    }

    
}