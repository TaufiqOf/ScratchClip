using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchClip.Helper;
using ScratchClip.Manager;
using ScratchClip.Models;
using ScratchClip.Services;
using SharpHook.Data;

namespace ScratchClip.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly GlobalHotkeyService _hotkeyService;

    [ObservableProperty]
    private string? _exportFilePath;

    [ObservableProperty]
    private string _hotkeyDisplay;

    [ObservableProperty]
    private string _hotkeyMenuDisplay;

    [ObservableProperty]
    private int _maximumItemsInHistory;



    [ObservableProperty]
    private AutoTag? _selectedAutoTag;
    // ========================================================
    // CODE LANGUAGE SETTINGS
    // ========================================================

    public ObservableCollection<CodeDetectionConfig.LanguageDefinition> LanguageDefinitions =>
        CodeDetectionConfig.Languages;

    [ObservableProperty]
    private CodeDetectionConfig.LanguageDefinition? _selectedLanguageDefinition;

    partial void OnSelectedLanguageDefinitionChanged(
        CodeDetectionConfig.LanguageDefinition? value)
    {
        OnPropertyChanged(nameof(LanguageKeywordsText));
        OnPropertyChanged(nameof(SelectedLanguageKeywords));
    }

    [ObservableProperty]
    private string _newLanguageKeyword = string.Empty;

    public ObservableCollection<string> SelectedLanguageKeywords =>
        SelectedLanguageDefinition?.Keywords ?? [];
    
    public ObservableCollection<AutoTag> AutoTags
    {
        get => AutoTagSettings.Tags;
        set => AutoTagSettings.Tags = value;
    }

    public string LanguageKeywordsText
    {
        get
        {
            if (SelectedLanguageDefinition == null)
                return string.Empty;

            return string.Join(
                ", ",
                SelectedLanguageDefinition.Keywords);
        }

        set
        {
            if (SelectedLanguageDefinition == null)
                return;

            var keywords = value
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            SelectedLanguageDefinition.Keywords.Clear();

            foreach (var keyword in keywords)
            {
                SelectedLanguageDefinition.Keywords.Add(keyword);
            }

            OnPropertyChanged();
        }
    }
    // ========================================================
    // CLIPBOARD SETTINGS
    // ========================================================

    private bool _willExportImageItems = true;
    private bool _willExportStorageItems = true;
    private bool _willExportTextItems = true;

    private bool _willCaptureImageItems;
    private bool _willCaptureTextItems;
    private bool _willCaptureStorageItems;


    // ========================================================
    // CONSTRUCTOR
    // ========================================================

    public SettingsViewModel(GlobalHotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;

        // Initialize with current settings
        var settings = SettingsManager.Load();

        PendingModifiers = _hotkeyService.TargetModifiers;
        PendingKey = _hotkeyService.TargetKey;

        PendingMenuModifiers = _hotkeyService.MenuTargetModifiers;
        PendingMenuKey = _hotkeyService.MenuTargetKey;

        HotkeyDisplay =
            $"{PendingModifiers} + {PendingKey}"
                .Replace("Left", "")
                .Replace("Right", "");

        HotkeyMenuDisplay =
            $"{PendingMenuModifiers} + {PendingMenuKey}"
                .Replace("Left", "")
                .Replace("Right", "");

        IsAutoStartEnabled = AutoStartManager.IsEnabled();

        IsSaveHistoryOnExitEnabled =
            settings.IsSaveHistoryOnExitEnabled;

        MaximumItemsInHistory =
            settings.MaxItemsInHistory;

        IsFastKeyEnabled =
            settings.IsFastKeyEnabled;

        IsReverseOrder =
            settings.IsReverseOrder;

        SelectedTheme =
            settings.Theme;

        WillCaptureImageItems =
            settings.WillCaptureImageItems;

        WillCaptureTextItems =
            settings.WillCaptureTextItems;

        WillCaptureStorageItems =
            settings.WillCaptureStorageItems;

        SelectedTheme = settings.Theme switch
        {
            "Light" => "Light",
            "Dark" => "Dark",
            _ => "System"
        };
        AutoTags = new ObservableCollection<AutoTag>(settings.AutoTags);
        LoadCodeDetectionLanguages(settings.CodeDetectionLanguages.ToArray());
        // Select the first language by default.
        SelectedLanguageDefinition =
            CodeDetectionConfig.Languages.FirstOrDefault();
    }
    private static void LoadCodeDetectionLanguages(
        CodeDetectionConfig.LanguageDefinition[]? languages)
    {
        if (languages == null || languages.Length == 0)
            return;

        CodeDetectionConfig.Languages.Clear();

        foreach (var source in languages)
        {
            var language =
                new CodeDetectionConfig.LanguageDefinition
                {
                    Id = source.Id,
                    Name = source.Name,
                    Extension = source.Extension,
                    NativeLibrary = source.NativeLibrary,
                    FunctionName = source.FunctionName,
                    Keywords = new ObservableCollection<string>(
                        source.Keywords ?? [])
                };

            CodeDetectionConfig.Languages.Add(language);
        }
    }

    // ========================================================
    // GENERAL SETTINGS
    // ========================================================

    public bool IsReverseOrder
    {
        get;
        set
        {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged();
        }
    }


    public bool IsAutoStartEnabled
    {
        get;
        set
        {
            AutoStartManager.SetEnabled(value);
            SetProperty(ref field, value);
        }
    }


    public bool IsSaveHistoryOnExitEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }


    // ========================================================
    // HOTKEY SETTINGS
    // ========================================================

    public EventMask PendingModifiers { get; private set; }

    public KeyCode PendingKey { get; private set; }

    public EventMask PendingMenuModifiers { get; set; }

    public KeyCode PendingMenuKey { get; set; }


    public string SelectedTheme
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
            }
        }
    }


    public bool IsFastKeyEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }


    // ========================================================
    // EXPORT SETTINGS
    // ========================================================

    public bool WillExportTextItems
    {
        get => _willExportTextItems;

        set
        {
            if (value == _willExportTextItems)
                return;

            _willExportTextItems = value;
            ExportFilePath = string.Empty;

            OnPropertyChanged();
        }
    }


    public bool WillExportImageItems
    {
        get => _willExportImageItems;

        set
        {
            if (value == _willExportImageItems)
                return;

            _willExportImageItems = value;
            ExportFilePath = string.Empty;

            OnPropertyChanged();
        }
    }


    public bool WillExportStorageItems
    {
        get => _willExportStorageItems;

        set
        {
            if (value == _willExportStorageItems)
                return;

            _willExportStorageItems = value;
            ExportFilePath = string.Empty;

            OnPropertyChanged();
        }
    }


    // ========================================================
    // CAPTURE SETTINGS
    // ========================================================

    public bool WillCaptureTextItems
    {
        get => _willCaptureTextItems;

        set
        {
            SetProperty(
                ref _willCaptureTextItems,
                value);
        }
    }


    public bool WillCaptureImageItems
    {
        get => _willCaptureImageItems;

        set
        {
            SetProperty(
                ref _willCaptureImageItems,
                value);
        }
    }


    public bool WillCaptureStorageItems
    {
        get => _willCaptureStorageItems;

        set
        {
            SetProperty(
                ref _willCaptureStorageItems,
                value);
        }
    }


    // ========================================================
    // LANGUAGE KEYWORDS
    // ========================================================

    [RelayCommand]
    private void AddLanguageKeyword()
    {
        if (SelectedLanguageDefinition == null)
            return;

        var keyword = NewLanguageKeyword.Trim();

        if (string.IsNullOrWhiteSpace(keyword))
            return;

        // Don't add duplicates.
        if (SelectedLanguageDefinition.Keywords.Any(
                x => string.Equals(
                    x,
                    keyword,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedLanguageDefinition.Keywords.Add(keyword);

        NewLanguageKeyword = string.Empty;

        OnPropertyChanged(
            nameof(SelectedLanguageKeywords));
    }


    [RelayCommand]
    private void RemoveLanguageKeyword(
        string? keyword)
    {
        if (SelectedLanguageDefinition == null ||
            string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        SelectedLanguageDefinition.Keywords.Remove(
            keyword);

        OnPropertyChanged(
            nameof(SelectedLanguageKeywords));
    }


    // ========================================================
    // ADD LANGUAGE
    // ========================================================

    [RelayCommand]
    private void AddLanguage()
    {
        var language =
            new CodeDetectionConfig.LanguageDefinition
            {
                Id = Guid.NewGuid().ToString(),

                Name = "NEW LANGUAGE",

                Extension = ".txt",

                NativeLibrary = string.Empty,

                FunctionName = string.Empty,

                Keywords = []
            };

        CodeDetectionConfig.Languages.Add(
            language);

        SelectedLanguageDefinition =
            language;

        OnPropertyChanged(
            nameof(LanguageDefinitions));

        OnPropertyChanged(
            nameof(SelectedLanguageKeywords));
    }


    // ========================================================
    // REMOVE LANGUAGE
    // ========================================================

    [RelayCommand]
    private void RemoveLanguage()
    {
        if (SelectedLanguageDefinition == null)
            return;

        if (!CodeDetectionConfig.Languages.Contains(
                SelectedLanguageDefinition))
        {
            return;
        }

        CodeDetectionConfig.Languages.Remove(
            SelectedLanguageDefinition);

        SelectedLanguageDefinition =
            CodeDetectionConfig.Languages.FirstOrDefault();

        OnPropertyChanged(
            nameof(LanguageDefinitions));

        OnPropertyChanged(
            nameof(SelectedLanguageKeywords));
    }


    // ========================================================
    // RESET LANGUAGE
    // ========================================================

    [RelayCommand]
    private void ResetSelectedLanguage()
    {
        if (SelectedLanguageDefinition == null)
            return;

        var selectedLanguage =
            SelectedLanguageDefinition;

        // Find the original built-in definition
        // using its stable ID.
        var defaultLanguage =
            CodeDetectionConfig.DefaultLanguages
                .FirstOrDefault(
                    x => string.Equals(
                        x.Id,
                        selectedLanguage.Id,
                        StringComparison.OrdinalIgnoreCase));

        // Custom languages cannot be reset because
        // they don't exist in DefaultLanguages.
        if (defaultLanguage == null)
            return;

        // Restore editable properties.
        selectedLanguage.Name =
            defaultLanguage.Name;

        selectedLanguage.Extension =
            defaultLanguage.Extension;

        selectedLanguage.NativeLibrary =
            defaultLanguage.NativeLibrary;

        selectedLanguage.FunctionName =
            defaultLanguage.FunctionName;

        // Restore keywords.
        selectedLanguage.Keywords.Clear();

        foreach (var keyword in defaultLanguage.Keywords)
        {
            selectedLanguage.Keywords.Add(
                keyword);
        }

        OnPropertyChanged(
            nameof(SelectedLanguageDefinition));

        OnPropertyChanged(
            nameof(SelectedLanguageKeywords));
    }
    [RelayCommand]
    private void AddAutoTag()
    {
        var tag = new AutoTag
        {
            TagName = "NEW TAG",
            Regex = string.Empty
        };

        AutoTagSettings.Tags.Add(tag);

        SelectedAutoTag = tag;

        OnPropertyChanged(nameof(AutoTags));
    }

    [RelayCommand]
    private void RemoveAutoTag()
    {
        if (SelectedAutoTag == null)
            return;

        var index =
            AutoTagSettings.Tags.IndexOf(SelectedAutoTag);

        AutoTagSettings.Tags.Remove(SelectedAutoTag);

        if (AutoTagSettings.Tags.Count == 0)
        {
            SelectedAutoTag = null;
        }
        else
        {
            var newIndex = Math.Min(
                index,
                AutoTagSettings.Tags.Count - 1);

            SelectedAutoTag =
                AutoTagSettings.Tags[newIndex];
        }

        OnPropertyChanged(nameof(AutoTags));
    }

    [RelayCommand]
    private void ResetAutoTags()
    {
        AutoTagSettings.Tags.Clear();

        foreach (var tag in AutoTagSettings.DefaultTags)
        {
            AutoTagSettings.Tags.Add(
                new AutoTag
                {
                    Enabled = tag.Enabled,
                    TagName = tag.TagName,
                    Regex = tag.Regex
                });
        }

        SelectedAutoTag =
            AutoTagSettings.Tags.FirstOrDefault();

        OnPropertyChanged(nameof(AutoTags));
    }

    // ========================================================
    // EXPORT
    // ========================================================

    [RelayCommand]
    public void Export()
    {
        try
        {
            if (string.IsNullOrEmpty(ExportFilePath))
            {
                NotificationHelper.Error(
                    "Export Error",
                    "Please select a valid export file path.");

                return;
            }

            var clipboardHistory =
                ClipboardManager
                    .GetClipboardHistorySnapshot()
                    .Where(q =>
                        (WillExportTextItems &&
                         q.ClipboardType ==
                         ClipboardType.Text)

                        ||

                        (WillExportImageItems &&
                         q.ClipboardType ==
                         ClipboardType.Image)

                        ||

                        (WillExportStorageItems &&
                         q.ClipboardType ==
                         ClipboardType.Storage))
                    .ToList();

            var finalePath =
                Path.GetDirectoryName(
                    ExportFilePath);

            if (finalePath == null)
                return;

            if (WillExportImageItems &&
                WillExportStorageItems)
            {
                finalePath =
                    Path.Combine(
                        finalePath,
                        Path.GetFileNameWithoutExtension(
                            ExportFilePath));
            }

            if (!Directory.Exists(finalePath))
            {
                Directory.CreateDirectory(
                    finalePath);
            }


            // ------------------------------------------------
            // Text
            // ------------------------------------------------

            if (WillExportTextItems)
            {
                var textItems =
                    clipboardHistory
                        .Where(q =>
                            q.ClipboardType ==
                            ClipboardType.Text)
                        .Cast<TextClipboardItem>()
                        .ToList();

                if (textItems.Any())
                {
                    var i = 1;

                    var stringBuilder =
                        new StringBuilder();

                    foreach (var item in textItems)
                    {
                        stringBuilder.AppendLine(
                            $"Item {i}:");

                        stringBuilder.AppendLine(
                            item.Text);

                        stringBuilder.AppendLine(
                            "------------------------------");

                        i++;
                    }

                    var textFilePath =
                        Path.Combine(
                            finalePath,
                            "TextItems.txt");

                    File.WriteAllText(
                        textFilePath,
                        stringBuilder.ToString());
                }
            }


            // ------------------------------------------------
            // Images
            // ------------------------------------------------

            if (WillExportImageItems)
            {
                var imageItems =
                    clipboardHistory
                        .Where(q =>
                            q.ClipboardType ==
                            ClipboardType.Image)
                        .Cast<ImageClipboardItem>()
                        .ToList();

                if (imageItems.Any())
                {
                    var i = 1;

                    foreach (
                        var imageClipboardItem
                        in imageItems)
                    {
                        var imageFilePath =
                            Path.Combine(
                                finalePath,
                                $"ImageItem_{i}.png");

                        imageClipboardItem.Image?.Save(
                            imageFilePath,
                            new PngBitmapEncoderOptions());

                        i++;
                    }
                }
            }


            // ------------------------------------------------
            // Storage
            // ------------------------------------------------

            if (WillExportStorageItems)
            {
                var storageItems =
                    clipboardHistory
                        .Where(q =>
                            q.ClipboardType ==
                            ClipboardType.Storage)
                        .Cast<StorageClipboardItem>()
                        .ToList();

                if (storageItems.Any())
                {
                    var i = 1;

                    foreach (
                        var storageClipboardItem
                        in storageItems)
                    {
                        foreach (
                            var file
                            in storageClipboardItem.Files)
                        {
                            var storageFilePath =
                                Path.Combine(
                                    finalePath,
                                    $"StorageItem_{i}",
                                    Path.GetFileName(file));

                            var directory =
                                Path.GetDirectoryName(
                                    storageFilePath);

                            if (!string.IsNullOrEmpty(
                                    directory) &&
                                !Directory.Exists(
                                    directory))
                            {
                                Directory.CreateDirectory(
                                    directory);
                            }

                            if (File.Exists(file))
                            {
                                try
                                {
                                    File.Copy(
                                        file,
                                        storageFilePath,
                                        true);
                                }
                                catch (Exception e)
                                {
                                    NotificationHelper.Error(
                                        "Error",
                                        $"Failed to copy file '{file}' to '{storageFilePath}': {e.Message}");
                                }
                            }
                        }


                        foreach (
                            var folder
                            in storageClipboardItem.Folders)
                        {
                            var folderName =
                                new DirectoryInfo(folder).Name;

                            var storageFolderPath =
                                Path.Combine(
                                    finalePath,
                                    $"StorageItem_{i}",
                                    folderName);

                            if (Directory.Exists(folder))
                            {
                                if (!Directory.Exists(
                                        storageFolderPath))
                                {
                                    Directory.CreateDirectory(
                                        storageFolderPath);
                                }

                                CopyDirectory(
                                    folder,
                                    storageFolderPath);
                            }
                        }

                        i++;
                    }
                }
            }


            // ------------------------------------------------
            // ZIP
            // ------------------------------------------------

            if (File.Exists(ExportFilePath))
            {
                File.Delete(
                    ExportFilePath);
            }

            if (WillExportImageItems ||
                WillExportStorageItems)
            {
                ZipFile.CreateFromDirectory(
                    finalePath,
                    ExportFilePath,
                    CompressionLevel.Optimal,
                    false);

                Directory.Delete(
                    finalePath,
                    true);
            }


            NotificationHelper.Success(
                "Export Success",
                "Export completed successfully.");
        }
        catch (Exception e)
        {
            NotificationHelper.Error(
                "Export Failed",
                e.Message);

            Console.WriteLine(e);
        }
    }


    // ========================================================
    // COPY DIRECTORY
    // ========================================================

    public static void CopyDirectory(
        string sourceDir,
        string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
            return;

        Directory.CreateDirectory(
            destinationDir);


        foreach (var file in Directory.GetFiles(
                     sourceDir))
        {
            var destinationFile =
                Path.Combine(
                    destinationDir,
                    Path.GetFileName(file));

            try
            {
                if (!File.Exists(file))
                    continue;

                File.Copy(
                    file,
                    destinationFile,
                    true);
            }
            catch (Exception e)
            {
                NotificationHelper.Error(
                    "Error",
                    $"Failed to copy file '{file}' to '{destinationFile}': {e.Message}");
            }
        }


        foreach (
            var directory
            in Directory.GetDirectories(sourceDir))
        {
            var destinationSubdirectory =
                Path.Combine(
                    destinationDir,
                    Path.GetFileName(directory));

            CopyDirectory(
                directory,
                destinationSubdirectory);
        }
    }


    // ========================================================
    // SELECT EXPORT FILE
    // ========================================================

    [RelayCommand]
    public async Task SelectFile()
    {
        var storageProvider =
            ApplicationReference
                .MainWindow?
                .StorageProvider;

        if (storageProvider == null)
            return;


        var filePickerTypes =
            new List<FilePickerFileType>();


        if (WillExportImageItems ||
            WillExportStorageItems)
        {
            filePickerTypes.Clear();

            filePickerTypes.Add(
                new FilePickerFileType(
                    "Zip Files")
                {
                    Patterns =
                        new List<string>
                        {
                            "*.zip"
                        }
                });

            filePickerTypes.Add(
                new FilePickerFileType(
                    "All Files")
                {
                    Patterns =
                        new List<string>
                        {
                            "*.*"
                        }
                });
        }
        else
        {
            filePickerTypes.Clear();

            filePickerTypes.Add(
                new FilePickerFileType(
                    "Text Files")
                {
                    Patterns =
                        new List<string>
                        {
                            "*.txt"
                        }
                });

            filePickerTypes.Add(
                new FilePickerFileType(
                    "All Files")
                {
                    Patterns =
                        new List<string>
                        {
                            "*.*"
                        }
                });
        }


        var startPath =
            ApplicationReference.LastPath != null &&
            Directory.Exists(
                ApplicationReference.LastPath)

                ? ApplicationReference.LastPath

                : Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop);


        var files =
            await storageProvider
                .SaveFilePickerWithResultAsync(
                    new FilePickerSaveOptions
                    {
                        Title = "Select a file",

                        FileTypeChoices =
                            filePickerTypes,

                        SuggestedStartLocation =
                            await storageProvider
                                .TryGetFolderFromPathAsync(
                                    startPath)
                    });


        if (files.File?.Path != null)
        {
            ApplicationReference.LastPath =
                Path.GetDirectoryName(
                    files.File.Path.LocalPath)
                ?? startPath;

            var file =
                files.File.Path;

            if (file != null)
            {
                ExportFilePath =
                    file.AbsolutePath;
            }
        }
    }


    // ========================================================
    // HOTKEY RESET
    // ========================================================

    [RelayCommand]
    private void ClearHotkey()
    {
        PendingModifiers =
            EventMask.LeftAlt |
            EventMask.LeftShift;

        PendingKey =
            KeyCode.VcK;

        HotkeyDisplay =
            "Alt + Shift + K";
    }


    [RelayCommand]
    private void ClearHotkeyMenu()
    {
        PendingMenuModifiers =
            EventMask.LeftAlt |
            EventMask.LeftShift;

        PendingMenuKey =
            KeyCode.VcL;

        HotkeyMenuDisplay =
            "Alt + Shift + L";
    }


    // ========================================================
    // SAVE
    // ========================================================

    public bool Save()
    {
        if (!WillCaptureImageItems &&
            !WillCaptureTextItems &&
            !WillCaptureStorageItems)
        {
            NotificationHelper.Error(
                "Settings Error",
                "At least one clipboard type must be enabled for capture.");

            return false;
        }


        // ----------------------------------------------------
        // Update active runtime hotkey configuration
        // ----------------------------------------------------

        _hotkeyService.UpdateHotkey(
            PendingModifiers,
            PendingKey);

        _hotkeyService.UpdateMenuHotkey(
            PendingMenuModifiers,
            PendingMenuKey);


        Application.Current?.RequestedThemeVariant =
            SelectedTheme?.ToLowerInvariant() switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };


        // ----------------------------------------------------
        // Persist settings
        // ----------------------------------------------------

        var settings =
            SettingsManager.Load();

        settings.IsSaveHistoryOnExitEnabled =
            IsSaveHistoryOnExitEnabled;

        settings.Modifiers =
            PendingModifiers;

        settings.Key =
            PendingKey;

        settings.MenuModifiers =
            PendingMenuModifiers;

        settings.MenuKey =
            PendingMenuKey;

        settings.IsAutoStartEnabled =
            AutoStartManager.IsEnabled();

        settings.MaxItemsInHistory =
            MaximumItemsInHistory;

        settings.IsReverseOrder =
            IsReverseOrder;

        settings.IsFastKeyEnabled =
            IsFastKeyEnabled;

        settings.WillCaptureImageItems =
            WillCaptureImageItems;

        settings.WillCaptureTextItems =
            WillCaptureTextItems;

        settings.WillCaptureStorageItems =
            WillCaptureStorageItems;

        settings.Theme =
            SelectedTheme;
        
        settings.AutoTags =
            AutoTags.ToList();

        settings.CodeDetectionLanguages =
            CodeDetectionConfig.Languages
                .Select(language =>
                    new CodeDetectionConfig.LanguageDefinition
                    {
                        Id = language.Id,
                        Name = language.Name,
                        Extension = language.Extension,
                        NativeLibrary = language.NativeLibrary,
                        FunctionName = language.FunctionName,
                        Keywords =
                            new ObservableCollection<string>(
                                language.Keywords)
                    })
                .ToArray();
        
        ClipboardManager.MaxItemsInHistory =
            settings.MaxItemsInHistory;

        SettingsManager.Save(
            settings);

        return true;
    }


    // ========================================================
    // HOTKEY HELPERS
    // ========================================================

    public void SetMenuHotkey(
        EventMask modifiers,
        KeyCode key,
        string display)
    {
        PendingMenuModifiers =
            modifiers;

        PendingMenuKey =
            key;

        HotkeyMenuDisplay =
            display;
    }


    public void SetHotkey(
        EventMask modifiers,
        KeyCode key,
        string display)
    {
        PendingModifiers =
            modifiers;

        PendingKey =
            key;

        HotkeyDisplay =
            display;
    }
}