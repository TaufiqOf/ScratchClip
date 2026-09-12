using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Newtonsoft.Json;
using ScratchClip.Models;
using ScratchClip.Models.TextType;

namespace ScratchClip.Views.Page;

public partial class EditClipboardItemPageControl : UserControl
{
    private readonly AClipboardItem _item;
    private readonly AClipboardItem? _unsavedItem;

    public EditClipboardItemPageControl()
    {
        InitializeComponent();
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

                MataData = new List<string?>(textItem.MataData),

                Tags = new ObservableCollection<string>(textItem.Tags)
            },
            ImageClipboardItem imageItem => new ImageClipboardItem
            {
                Image = imageItem.Image,
                DisplayIndex = imageItem.DisplayIndex,
                DisplayText = imageItem.DisplayText,
                Format = imageItem.Format,
                Signature = imageItem.Signature,
                Timestamp = imageItem.Timestamp,

                MataData = new List<string?>(imageItem.MataData),

                Tags = new ObservableCollection<string>(imageItem.Tags)
            },
            StorageClipboardItem storageItem => new StorageClipboardItem
            {
                Paths = new List<string>(storageItem.Paths),
                DisplayIndex = storageItem.DisplayIndex,
                DisplayText = storageItem.DisplayText,
                Format = storageItem.Format,
                Signature = storageItem.Signature,
                Timestamp = storageItem.Timestamp,
                MataData = new List<string?>(storageItem.MataData),
                Tags = new ObservableCollection<string>(storageItem.Tags)
            },

            _ => null
        };
        DataContext = _unsavedItem;
        InitializeComponent();
        TagEditor.AvailableTags = new ObservableCollection<string>("PLAINTEXT,MARKDOWN,CODE,PASSWORD,WEBSITE,JSON,XML".Split(','));
    }

    public Action<AClipboardItem?>? OnClose { get; set; }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (_item is TextClipboardItem textItem)
        {
            var item = _unsavedItem as TextClipboardItem;
            textItem.Text = item.Text;
            textItem.DisplayIndex = item.DisplayIndex;
            textItem.DisplayText = item.DisplayText;
            textItem.Format = item.Format;
            textItem.Signature = item.Signature;
            textItem.Timestamp = item.Timestamp;

            textItem.MataData.Clear();
            foreach (var data in item.MataData)
                textItem.MataData.Add(data);

            textItem.Tags.Clear();
            foreach (var tag in item.Tags)
                textItem.Tags.Add(tag);
            var list = textItem.Tags.ToList();
            textItem.UpdateByTags();
        }

        if (_item is ImageClipboardItem imageItem)
        {
            var item = _unsavedItem as ImageClipboardItem;
            imageItem.Image = item.Image;
            imageItem.DisplayIndex = item.DisplayIndex;
            imageItem.DisplayText = item.DisplayText;
            imageItem.Format = item.Format;
            imageItem.Signature = item.Signature;
            imageItem.Timestamp = item.Timestamp;

            imageItem.MataData.Clear();
            foreach (var data in item.MataData)
                imageItem.MataData.Add(data);

            imageItem.Tags.Clear();
            foreach (var tag in item.Tags)
                imageItem.Tags.Add(tag);
        }
        if( _item is StorageClipboardItem storageItem)
        {
            var item = _unsavedItem as StorageClipboardItem;
            storageItem.Paths = new List<string>(item.Paths);
            storageItem.DisplayIndex = item.DisplayIndex;
            storageItem.DisplayText = item.DisplayText;
            storageItem.Format = item.Format;
            storageItem.Signature = item.Signature;
            storageItem.Timestamp = item.Timestamp;

            storageItem.MataData.Clear();
            foreach (var data in item.MataData)
                storageItem.MataData.Add(data);

            storageItem.Tags.Clear();
            foreach (var tag in item.Tags)
                storageItem.Tags.Add(tag);
        }

        OnClose?.Invoke(_item);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        // TODO:
        // Cancel / close page
        OnClose?.Invoke(null);
    }
}