using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ScratchClip.Models;

namespace ScratchClip.Views.Page;

public partial class EditClipboardItemPageControl : UserControl
{
    public EditClipboardItemPageControl()
    {
        InitializeComponent();
    }
    public EditClipboardItemPageControl(AClipboardItem item)
    {
        DataContext = item;
        InitializeComponent();
    }

    public Action OnClose { get; set; }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TextClipboardItem item)
            return;

        // TODO:
        // Save changes / close page
        OnClose?.Invoke();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        // TODO:
        // Cancel / close page
        OnClose?.Invoke();
    }
}