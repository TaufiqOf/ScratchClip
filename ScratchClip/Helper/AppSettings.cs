using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ScratchClip.Models;
using SharpHook.Data;

namespace ScratchClip.Helper;

public class AppSettings
{
    public EventMask Modifiers { get; set; } = EventMask.LeftAlt | EventMask.LeftShift;
    public KeyCode Key { get; set; } = KeyCode.VcK;
    public EventMask MenuModifiers { get; set; } = EventMask.LeftAlt | EventMask.LeftShift;
    public KeyCode MenuKey { get; set; } = KeyCode.VcL;
    public bool IsAutoStartEnabled { get; set; }
    public double WindowWidth { get; set; } = 450;
    public double WindowHeight { get; set; } = 600;
    public bool IsSaveHistoryOnExitEnabled { get; set; } = true;
    public ViewMode ViewMode { get; set; }

    public int MaxItemsInHistory { get; set; } = 200;
    public bool IsPinned { get; set; } = false;
    public bool IsReverseOrder { get; set; }
    public string Theme { get; set; }
    public bool IsFastKeyEnabled { get; set; }
    public bool WillCaptureImageItems { get; set; } = true;
    public bool WillCaptureTextItems { get; set; } = true;
    public bool WillCaptureStorageItems { get; set; } = true;

    public CodeDetectionConfig.LanguageDefinition[] CodeDetectionLanguages { get; set; } =
        CodeDetectionConfig.Languages.ToArray();

    public List<AutoTag> AutoTags { get; set; } = AutoTagSettings.Tags.ToList();
}