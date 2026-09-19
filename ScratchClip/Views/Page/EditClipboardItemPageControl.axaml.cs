using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScratchClip.Helper;
using ScratchClip.Manager;
using ScratchClip.Models;

namespace ScratchClip.Views.Page;

public partial class EditClipboardItemPageControl : UserControl
{
    private readonly AClipboardItem? _item;
    private readonly AClipboardItem? _unsavedItem;

    public EditClipboardItemPageControl()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, InputElementOnKeyDown, RoutingStrategies.Tunnel);
    }

    public EditClipboardItemPageControl(AClipboardItem item)
    {
        _item = item;
        _unsavedItem = item switch
        {
            TextClipboardItem textItem => new TextClipboardItem
            {
                Text = textItem.Text,
                DisplayIndex = textItem.DisplayIndex,
                DisplayText = textItem.DisplayText,
                Format = textItem.Format,
                Signature = textItem.Signature,
                Timestamp = textItem.Timestamp,

                MataData = new List<string>(textItem.MataData ?? new List<string>()),

                Tags = new ObservableCollection<string>(textItem.Tags),
                Note = textItem.Note
            },
            ImageClipboardItem imageItem => new ImageClipboardItem
            {
                Image = imageItem.Image,
                DisplayIndex = imageItem.DisplayIndex,
                DisplayText = imageItem.DisplayText,
                Format = imageItem.Format,
                Signature = imageItem.Signature,
                Timestamp = imageItem.Timestamp,

                MataData = new List<string>(imageItem.MataData ?? new List<string>()),

                Tags = new ObservableCollection<string>(imageItem.Tags),
                Note = imageItem.Note
            },
            StorageClipboardItem storageItem => new StorageClipboardItem
            {
                Paths = new List<string>(storageItem.Paths),
                DisplayIndex = storageItem.DisplayIndex,
                DisplayText = storageItem.DisplayText,
                Format = storageItem.Format,
                Signature = storageItem.Signature,
                Timestamp = storageItem.Timestamp,
                MataData = new List<string>(storageItem.MataData ?? new List<string>()),
                Tags = new ObservableCollection<string>(storageItem.Tags),
                Note = storageItem.Note
            },

            _ => null
        };
        DataContext = _unsavedItem;
        InitializeComponent();
        AddHandler(KeyDownEvent, InputElementOnKeyDown, RoutingStrategies.Tunnel);

        TagEditor.AvailableTags =
            new ObservableCollection<string>("PLAINTEXT,MARKDOWN,CODE,PASSWORD,WEBSITE,JSON,XML".Split(','));
    }

    public Action<AClipboardItem?>? OnClose { get; set; }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (_item is TextClipboardItem textItem)
        {
            var item = _unsavedItem as TextClipboardItem;
            if (item == null) return;
            textItem.Text = item.Text;
            textItem.DisplayIndex = item.DisplayIndex;
            textItem.DisplayText = item.DisplayText;
            textItem.Format = item.Format;
            textItem.Signature = item.Signature;
            textItem.Timestamp = item.Timestamp;
            textItem.Note = item.Note;

            textItem.MataData?.Clear();
            foreach (var data in item.MataData!)
                textItem.MataData?.Add(data);

            textItem.Tags.Clear();
            foreach (var tag in item.Tags)
                textItem.Tags.Add(tag);
            await textItem.UpdateByTags();
            textItem.UpdateDisplayText();
        }

        if (_item is ImageClipboardItem imageItem)
        {
            var item = _unsavedItem as ImageClipboardItem;
            if (item is null)
                return;
            imageItem.Image = item.Image;
            imageItem.DisplayIndex = item.DisplayIndex;
            imageItem.DisplayText = item.DisplayText;
            imageItem.Format = item.Format;
            imageItem.Signature = item.Signature;
            imageItem.Timestamp = item.Timestamp;
            imageItem.Note = item.Note;

            imageItem.MataData?.Clear();
            foreach (var data in item.MataData!)
                imageItem.MataData?.Add(data);

            imageItem.Tags.Clear();
            foreach (var tag in item.Tags)
                imageItem.Tags.Add(tag);
        }

        if (_item is StorageClipboardItem storageItem)
        {
            var item = _unsavedItem as StorageClipboardItem;
            if (item is null)
                return;
            storageItem.Paths = new List<string>(item.Paths);
            storageItem.DisplayIndex = item.DisplayIndex;
            storageItem.DisplayText = item.DisplayText;
            storageItem.Format = item.Format;
            storageItem.Signature = item.Signature;
            storageItem.Timestamp = item.Timestamp;
            storageItem.Note = item.Note;
            storageItem.MataData?.Clear();
            foreach (var data in item.MataData!)
                storageItem.MataData?.Add(data);

            storageItem.Tags.Clear();
            foreach (var tag in item.Tags)
                storageItem.Tags.Add(tag);
        }

        ClipboardManager.UpdateSignature(_item);
        NotificationHelper.Success(
            "Item updated",
            "The item has been successfully updated.");
        OnClose?.Invoke(_item);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        // TODO:
        // Cancel / close page
        OnClose?.Invoke(null);
    }

    private void InputElementOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S) OnSaveClick(sender, new RoutedEventArgs());
    }
}