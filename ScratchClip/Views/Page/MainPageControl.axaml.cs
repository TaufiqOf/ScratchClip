using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using ScratchClip.Helper;

namespace ScratchClip.Views.Page;

public partial class MainPageControl : UserControl
{
    private readonly WindowIcon? _icon;

    public MainPageControl(WindowIcon? icon)
    {
        _icon = icon;

        InitializeComponent();
        OnThemeChanged(ApplicationTheme.Theme);
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
}