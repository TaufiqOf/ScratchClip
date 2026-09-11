using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;

namespace ScratchClip.Models.TextType;

public partial class PlainTextType : ATextType
{
    private readonly string _text;
    private int _maxLength = 600;
    public PlainTextType(string text, ObservableCollection<string> tags, List<string?> mataData) : base(text, tags)
    {
        ShowMore = false;
        _text = text;
        Icon = Icon.Note;
        if (_text.Length < _maxLength)
        {
            ShowMore = false;
        }
        else
        {
            ShowMore = true;
        }
        Text = text.Substring(0, Math.Min(text.Length, _maxLength));  
        UpdateShowMore();
    }

    [ObservableProperty]
    private bool _showMore; 
    private void UpdateShowMore()
    {
        if(_text.Length > _maxLength)
        {
            ShowMoreText = "more";
        }
        else
        {
            ShowMoreText = "hide";
        }
    }

    public string? ShowMoreText
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsTruncated => Text.Length < _maxLength;
    
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
            _maxLength = 600;
        }
        else
        {
            _maxLength = _text.Length;
        }
        Text = _text.Substring(0, Math.Min(_text.Length, _maxLength));
        UpdateShowMore();
    }
}