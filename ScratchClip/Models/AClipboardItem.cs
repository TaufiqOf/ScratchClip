using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ScratchClip.ViewModels;

namespace ScratchClip.Models;

public abstract partial class AClipboardItem : ViewModelBase
{
    public Action<AClipboardItem>? OnEdit { get; set; }
    public Action<AClipboardItem>? OnDelete { get; set; }

    public ObservableCollection<string> Tags { get; set; } = new ObservableCollection<string>();

    public AClipboardItem()
    {

    }

    public bool IsPinned
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = false;

    public abstract Task<object?> GetData();

    public abstract string SuggestedFile { get; }


    public string Text
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    } = string.Empty;

    public int DisplayIndex
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public string DisplayText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public ClipboardDataFormat Format
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = ClipboardDataFormat.Text;

    public string Signature
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public DateTime Timestamp
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = DateTime.Now;

    public List<string>? MataData { get; set; } = new List<string>();

    [RelayCommand]
    public void Pin()
    {
        IsPinned = !IsPinned;
    }

    [RelayCommand]
    public abstract void Delete();

    [RelayCommand]
    public void Edit()
    {
        OnEdit?.Invoke(this);
    }

    [RelayCommand]
    public async Task SaveAs()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var window = desktop.MainWindow;

            if (window == null)
                return;

            var file = await window.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save Clipboard Item",
                    SuggestedFileName = SuggestedFile
                });

            if (file == null)
                return;
            var data = await GetData();
            switch (data)
            {
                case string text:
                {
                    await using var destination = await file.OpenWriteAsync();
                    await using var writer = new StreamWriter(destination);

                    await writer.WriteAsync(text);
                    break;
                }

                case FileStream source:
                {
                    await using (source)
                    {
                        await using var destination = await file.OpenWriteAsync();

                        source.Position = 0;
                        await source.CopyToAsync(destination);
                    }

                    break;
                }

                case MemoryStream source:
                {
                    await using (source)
                    {
                        await using var destination = await file.OpenWriteAsync();

                        source.Position = 0;
                        await source.CopyToAsync(destination);
                    }

                    break;
                }

                case byte[] bytes:
                {
                    await using var destination = await file.OpenWriteAsync();

                    await destination.WriteAsync(bytes);
                    break;
                }

                case Bitmap bitmap:
                {
                    await using var destination = await file.OpenWriteAsync();

                    bitmap.Save(destination, new PngBitmapEncoderOptions());
                    break;
                }

                default:
                    throw new NotSupportedException(
                        $"Cannot save data of type {data?.GetType().Name ?? "null"}.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}