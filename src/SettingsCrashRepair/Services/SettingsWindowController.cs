using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SettingsCrashRepair.Services;

public sealed record SettingsWindowTarget(nint Handle, int ProcessId, string ProcessName, string Title);

public static class SettingsWindowController
{
    private const uint WmClose = 0x0010;

    public static IReadOnlyList<SettingsWindowTarget> FindWindows()
    {
        var windows = new List<SettingsWindowTarget>();
        EnumWindows((handle, parameter) =>
        {
            if (!IsWindowVisible(handle))
            {
                return true;
            }

            GetWindowThreadProcessId(handle, out var processId);
            if (processId == 0)
            {
                return true;
            }

            var title = GetWindowTitle(handle);
            var processName = GetProcessName((int)processId);
            if (IsSettingsWindow(processName, title))
            {
                windows.Add(new SettingsWindowTarget(handle, (int)processId, processName, title));
            }

            return true;
        }, nint.Zero);
        return windows;
    }

    public static void RequestClose(IEnumerable<SettingsWindowTarget> windows)
    {
        foreach (var window in windows.DistinctBy(window => window.Handle))
        {
            _ = PostMessage(window.Handle, WmClose, nint.Zero, nint.Zero);
        }
    }

    private static bool IsSettingsWindow(string processName, string title)
    {
        if (processName.Equals("SystemSettings", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return processName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) &&
            (title.Contains("设置", StringComparison.OrdinalIgnoreCase) ||
             title.Contains("Settings", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetWindowTitle(nint handle)
    {
        var length = GetWindowTextLength(handle);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private delegate bool EnumWindowsProc(nint handle, nint parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint handle);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint handle, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint handle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint handle, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint handle, uint message, nint wParam, nint lParam);
}
