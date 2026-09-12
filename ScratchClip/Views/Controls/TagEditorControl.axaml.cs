using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace ScratchClip.Views.Controls;

public partial class TagEditorControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<string>> TagsProperty =
        AvaloniaProperty.Register<TagEditorControl, ObservableCollection<string>>(
            nameof(Tags),
            new ObservableCollection<string>());

    public ObservableCollection<string> Tags
    {
        get => GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }


    public static readonly StyledProperty<ObservableCollection<string>> AvailableTagsProperty =
        AvaloniaProperty.Register<TagEditorControl, ObservableCollection<string>>(
            nameof(AvailableTags),
            new ObservableCollection<string>());

    public ObservableCollection<string> AvailableTags
    {
        get => GetValue(AvailableTagsProperty);
        set => SetValue(AvailableTagsProperty, value);
    }


    public static readonly StyledProperty<string?> TagTextProperty =
        AvaloniaProperty.Register<TagEditorControl, string?>(
            nameof(TagText));

    public string? TagText
    {
        get => GetValue(TagTextProperty);
        set => SetValue(TagTextProperty, value);
    }


    public static readonly StyledProperty<string?> SelectedTagProperty =
        AvaloniaProperty.Register<TagEditorControl, string?>(
            nameof(SelectedTag));

    public string? SelectedTag
    {
        get => GetValue(SelectedTagProperty);
        set => SetValue(SelectedTagProperty, value);
    }


    public RelayCommand AddTagCommand { get; }

    public RelayCommand<string> RemoveTagCommand { get; }


    public TagEditorControl()
    {
        AddTagCommand = new RelayCommand(AddTag);
        RemoveTagCommand = new RelayCommand<string>(RemoveTag);
        InitializeComponent();

    }


    private void AddTag()
    {
        var tag = TagText?.Trim();

        if (string.IsNullOrWhiteSpace(tag))
            return;

        // Prevent duplicate tags
        foreach (var existingTag in Tags)
        {
            if (string.Equals(existingTag, tag, StringComparison.OrdinalIgnoreCase))
            {
                TagText = string.Empty;
                SelectedTag = null;
                return;
            }
        }

        Tags.Add(tag);

        TagText = string.Empty;
        SelectedTag = null;
    }


    private void RemoveTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        // First tag cannot be deleted
        if (Tags.Count > 0 &&
            string.Equals(Tags[0], tag, StringComparison.Ordinal))
        {
            return;
        }

        Tags.Remove(tag);
    }
}