using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using ScratchClip.Helper;
using ScratchClip.ViewModels;

namespace ScratchClip.Views.Page;

public partial class MainPageControl : UserControl
{
    public MainPageControl()
    {
        InitializeComponent();
        OnThemeChanged(ApplicationTheme.Theme!);
        OnPasswordChanged();
        ApplicationKeyStore.OnPasswordChanged+= OnPasswordChanged;
        ApplicationTheme.OnThemeChanged += OnThemeChanged;
    }

    private void OnPasswordChanged()
    {
        var lockButtonIsVisible = ApplicationKeyStore.HasPassword();
        LockButton.IsVisible = lockButtonIsVisible;
    }

    private void OnThemeChanged(ThemeVariant obj)
    {
        var uri = new Uri(ApplicationTheme.GetIcon(obj,"png"));

        using var trayStream = AssetLoader.Open(uri);

        using var memoryStream = new MemoryStream();

        trayStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        LogoImage.Source = new Bitmap(memoryStream);
    }

    private void SearchTextBoxOnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if(DataContext is MainViewModel viewModel)
        {
            viewModel.IsSearchFocused = true;
        }
    }

    private void SearchTextBoxOnLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if(DataContext is MainViewModel viewModel)
        {
            viewModel.IsSearchFocused = false;
        }
    }
}