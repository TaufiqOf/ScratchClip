using System;
using System.Diagnostics;
using System.IO;
using ScratchClip.Models;

namespace ScratchClip.Services;

public class LinuxClipboardSourceProvider : IClipboardSourceProvider
{
    public string? GetApplicationName()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xdotool",
                    Arguments = "getactivewindow getwindowpid",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                return null;

            if (!int.TryParse(output.Trim(), out var pid))
                return null;

            var commPath = $"/proc/{pid}/comm";

            if (!File.Exists(commPath))
                return null;

            return File.ReadAllText(commPath).Trim();
        }
        catch
        {
            return null;
        }
    }
}