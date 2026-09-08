using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ScratchClip.Views.Controls.DetailItemControls;

public partial class StorageItemsControl : UserControl
{
    public StorageItemsControl()
    {
        InitializeComponent();
    }
    private async void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is MainWindow window)
            await window.ActivateSelectedItemAsync();

        e.Handled = true;
    }
}