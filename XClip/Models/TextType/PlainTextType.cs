using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;

namespace XClip.Models.TextType;

public partial class PlainTextType : ATextType
{
    public PlainTextType(string text, ObservableCollection<string> tags) : base(text, tags)
    {
        Icon = Icon.Note;
        Text = text.Substring(0, Math.Min(text.Length, 600));   
    }
    
    public override string DisplayName => "Plain Text";

    public override bool IsMatch(string text)
    {
        return true;
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
}