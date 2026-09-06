using SharpHook.Data;

namespace XClip.Helper;

public class AppSettings
{
    public EventMask Modifiers { get; set; } = EventMask.LeftAlt | EventMask.LeftShift;
    public KeyCode Key { get; set; } = KeyCode.VcK;
    public bool IsAutoStartEnabled { get; set; }
    public double WindowWidth { get; set; } = 450;
    public double WindowHeight { get; set; } = 600;
    public bool IsSaveHistoryOnExitEnabled { get; set; } = false;
}