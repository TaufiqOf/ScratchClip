using ScratchClip.Models;
using SharpHook.Data;

namespace ScratchClip.Helper;

public class AppSettings
{
    public EventMask Modifiers { get; set; } = EventMask.LeftAlt | EventMask.LeftShift;
    public KeyCode Key { get; set; } = KeyCode.VcK;
    public EventMask MenuModifiers { get; set; }    = EventMask.LeftAlt | EventMask.LeftShift;
    public KeyCode MenuKey { get; set; } = KeyCode.VcL;
    public bool IsAutoStartEnabled { get; set; }
    public double WindowWidth { get; set; } = 450;
    public double WindowHeight { get; set; } = 600;
    public bool IsSaveHistoryOnExitEnabled { get; set; } = true;
    public ViewMode ViewMode { get; set; }
 
    public int MaxItemsInHistory { get; set; } = 200;
    public bool IsPinned { get; set; } = false;
    public bool IsReverseOrder { get; set; }

}