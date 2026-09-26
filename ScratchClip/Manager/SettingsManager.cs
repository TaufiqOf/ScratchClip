using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using ScratchClip.Helper;

namespace ScratchClip.Manager;

public static class SettingsManager
{
    private static AppSettings? _settings;
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScratchClip");

    private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");

    static SettingsManager()
    {
        if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
    }

    public static Action<AppSettings>? OnSettingsUpdated { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if(_settings != null) return _settings;
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                return _settings;
            }
        }
        catch
        {
            // Fall back to defaults on read errors
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            _settings = settings;
            Directory.CreateDirectory(FolderPath);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            OnSettingsUpdated?.Invoke(settings);
        }
        catch (Exception ex)
        {
            NotificationHelper.Error("Error",$"Failed to save settings: {ex.Message}");
        }
    }
}