using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;

namespace XClip.Models.TextType;

public partial class PlainTextType : ATextType
{
    private readonly string _text;
    private int maxLength = 600;
    public PlainTextType(string text, ObservableCollection<string> tags) : base(text, tags)
    {
        _text = text;
        Icon = Icon.Note;
        if (_text.Length < maxLength)
        {
            ShowMore = false;
        }
        else
        {
            ShowMore = true;
        }
        Text = text.Substring(0, Math.Min(text.Length, maxLength));  
        UpdateShowMore();
    }

    [ObservableProperty]
    private bool _showMore; 
    private void UpdateShowMore()
    {
        if(_text.Length > maxLength)
        {
            ShowMoreText = "more";
        }
        else
        {
            ShowMoreText = "hide";
        }
    }

    public string ShowMoreText
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsTruncated => Text.Length < maxLength;
    
    public override string DisplayName => "Plain Text";

    public override bool IsMatch(string text)
    {
        return true;
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
    
    [RelayCommand]
    private void More()
    {
        if(ShowMoreText == "hide")
        {
            maxLength = 600;
        }
        else
        {
            maxLength = _text.Length;
        }
        Text = _text.Substring(0, Math.Min(_text.Length, maxLength));
        UpdateShowMore();
    }
}