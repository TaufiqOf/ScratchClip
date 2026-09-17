using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Threading;
using SharpHook;
using SharpHook.Data;
using SharpHook.Providers;
using SharpHook.Simulation;

namespace ScratchClip.Services;

public class GlobalHotkeyService : IDisposable
{
    private readonly EventLoopGlobalHook? _hook;
    private readonly bool _isWaylandSession;
    private readonly Action _onHotKeyPressed;
    private readonly Action _onMenuHotKeyPressed;
    private readonly EventSimulator? _simulator;

    public GlobalHotkeyService(
        Action onHotKeyPressed,
        Action onMenuHotKeyPressed)
    {
        _onHotKeyPressed = onHotKeyPressed;
        _onMenuHotKeyPressed = onMenuHotKeyPressed;

        _isWaylandSession = IsWaylandSession();

        if (!_isWaylandSession)
        {
            _simulator = EventSimulator.Create(
                "ScratchClip",
                UioHookProvider.Instance);

            _hook = new EventLoopGlobalHook(
                UioHookProvider.Instance);

            _hook.KeyPressed += OnKeyPressed;
        }
    }

    // Main window hotkey
    public EventMask TargetModifiers { get; set; } =
        EventMask.LeftAlt | EventMask.LeftShift;

    public KeyCode TargetKey { get; set; } =
        KeyCode.VcK;


    // Menu window hotkey
    public EventMask MenuTargetModifiers { get; set; } =
        EventMask.LeftAlt | EventMask.LeftShift;

    public KeyCode MenuTargetKey { get; set; } =
        KeyCode.VcM;


    public bool IsSupported => true;


    public void Dispose()
    {
        if (_hook != null)
        {
            _hook.KeyPressed -= OnKeyPressed;
            _hook.Dispose();
        }
    }


    public void Start()
    {
        if (!_isWaylandSession)
            _hook?.RunAsync();
    }


    private void OnKeyPressed(
        object? sender,
        KeyboardHookEventArgs e)
    {
        var currentMask = e.RawEvent.Mask;
        var keyCode = e.Data.KeyCode;

        // Main hotkey
        var mainModifiersMatch =
            (currentMask & TargetModifiers) == TargetModifiers;

        var mainKeyMatches =
            keyCode == TargetKey;

        if (mainModifiersMatch && mainKeyMatches)
        {
            Dispatcher.UIThread.Post(_onHotKeyPressed);
            return;
        }


        // Menu hotkey
        var menuModifiersMatch =
            (currentMask & MenuTargetModifiers) == MenuTargetModifiers;

        var menuKeyMatches =
            keyCode == MenuTargetKey;

        if (menuModifiersMatch && menuKeyMatches)
        {
            Dispatcher.UIThread.Post(_onMenuHotKeyPressed);
        }
    }


    public void UpdateHotkey(
        EventMask modifiers,
        KeyCode key)
    {
        TargetModifiers = modifiers;
        TargetKey = key;
    }


    public void UpdateMenuHotkey(
        EventMask modifiers,
        KeyCode key)
    {
        MenuTargetModifiers = modifiers;
        MenuTargetKey = key;
    }


    public void TriggerHotkeyFromSystem()
    {
        Dispatcher.UIThread.Post(_onHotKeyPressed);
    }


    public void TriggerMenuHotkeyFromSystem()
    {
        Dispatcher.UIThread.Post(_onMenuHotKeyPressed);
    }


    public async Task SimulatePasteAsync()
    {
        await Task.Delay(100);

        if (_isWaylandSession)
        {
            await SimulateWaylandPasteAsync();
            return;
        }

        if (_simulator == null)
            return;

        var pid = GetFocusedProcessId();
        var processName = string.Empty;

        if (pid.HasValue)
        {
            try
            {
                var process = Process.GetProcessById(pid.Value);
                processName = process.ProcessName;

                Console.WriteLine(
                    $"Focused process: {processName}");
            }
            catch
            {
                return;
            }
        }

        var isMac =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        var isTerminal =
            processName.Contains(
                "terminal",
                StringComparison.OrdinalIgnoreCase) ||
            processName.Contains(
                "konsole",
                StringComparison.OrdinalIgnoreCase) ||
            processName.Contains(
                "kitty",
                StringComparison.OrdinalIgnoreCase) ||
            processName.Contains(
                "alacritty",
                StringComparison.OrdinalIgnoreCase);

        if (isTerminal)
        {
            _simulator.SimulateKeyPress(
                KeyCode.VcLeftControl);

            _simulator.SimulateKeyPress(
                KeyCode.VcLeftShift);

            await Task.Delay(30);

            _simulator.SimulateKeyPress(KeyCode.VcV);
            _simulator.SimulateKeyRelease(KeyCode.VcV);

            _simulator.SimulateKeyRelease(
                KeyCode.VcLeftShift);

            _simulator.SimulateKeyRelease(
                KeyCode.VcLeftControl);
        }
        else
        {
            var modifierKey = isMac
                ? KeyCode.VcLeftMeta
                : KeyCode.VcLeftControl;

            _simulator.SimulateKeyPress(modifierKey);
            _simulator.SimulateKeyPress(KeyCode.VcV);
            _simulator.SimulateKeyRelease(KeyCode.VcV);
            _simulator.SimulateKeyRelease(modifierKey);
        }
    }


    private static int? GetFocusedProcessId()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "xprop",
            Arguments = "-root _NET_ACTIVE_WINDOW",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);

        if (process == null)
            return null;

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var match = Regex.Match(
            output,
            @"window id # (0x[0-9a-fA-F]+)");

        if (!match.Success)
            return null;

        var windowId = match.Groups[1].Value;

        psi.Arguments =
            $"-id {windowId} _NET_WM_PID";

        using var pidProcess = Process.Start(psi);

        if (pidProcess == null)
            return null;

        var pidOutput =
            pidProcess.StandardOutput.ReadToEnd();

        pidProcess.WaitForExit();

        var pidMatch =
            Regex.Match(pidOutput, @"= (\d+)");

        return pidMatch.Success
            ? int.Parse(pidMatch.Groups[1].Value)
            : null;
    }


    private static async Task SimulateWaylandPasteAsync()
    {
        if (IsToolInstalled("wtype"))
        {
            RunProcess(
                "wtype",
                "-M ctrl -k v -m ctrl");

            return;
        }

        if (IsToolInstalled("ydotool"))
        {
            RunProcess(
                "ydotool",
                "key 29:1 47:1 47:0 29:0");

            return;
        }

        await Task.CompletedTask;
    }


    private static bool IsWaylandSession()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return false;

        var sessionType =
            Environment.GetEnvironmentVariable(
                "XDG_SESSION_TYPE");

        if (string.Equals(
                sessionType,
                "wayland",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(
                "WAYLAND_DISPLAY"));
    }


    private static bool IsToolInstalled(string toolName)
    {
        try
        {
            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = toolName,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

            process?.WaitForExit();

            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }


    private static void RunProcess(
        string fileName,
        string args)
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[ScratchClip] Failed to run {fileName}: {ex.Message}");
        }
    }
}