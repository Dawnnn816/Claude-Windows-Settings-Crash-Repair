using System.Diagnostics;
using SettingsCrashRepair.Models;

namespace SettingsCrashRepair.Services;

public sealed class StartupPageVerifier(EventLogScanner eventLogScanner)
{
    private readonly EventLogScanner _eventLogScanner = eventLogScanner;

    public async Task<LiveVerificationResult> VerifyAsync(
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var existingWindows = SettingsWindowController.FindWindows();
            progress?.Invoke("已开始实时监控，正在打开“设置 -> 应用 -> 启动”。");
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:startupapps",
                UseShellExecute = true
            });

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            var observedWindows = SelectVerificationWindows(existingWindows, SettingsWindowController.FindWindows());
            progress?.Invoke(observedWindows.Count > 0
                ? "已检测到设置窗口，继续监控 7 秒。"
                : "未检测到可跟踪的设置窗口，继续监控 7 秒。"
            );
            await Task.Delay(TimeSpan.FromSeconds(7), cancellationToken);

            if (observedWindows.Count > 0)
            {
                SettingsWindowController.RequestClose(observedWindows);
                await Task.Delay(TimeSpan.FromMilliseconds(600), cancellationToken);
                progress?.Invoke($"已请求关闭本次动态验证的设置窗口（{observedWindows.Count} 个）。");
            }
            else
            {
                progress?.Invoke("未找到可关闭的设置窗口。");
            }

            progress?.Invoke("正在检查监控开始后是否产生新的设置崩溃事件。");
            var newEvents = await _eventLogScanner.ScanAsync(startedAt.AddSeconds(-1), cancellationToken);
            if (newEvents.Error is not null)
            {
                return new LiveVerificationResult(
                    LiveVerificationState.Inconclusive,
                    "启动页面已尝试打开，但无法读取本次事件监控结果。",
                    newEvents,
                    observedWindows.Count > 0,
                    newEvents.Error);
            }

            if (newEvents.Startup is not null)
            {
                return new LiveVerificationResult(
                    LiveVerificationState.CrashConfirmed,
                    "本次动态验证确认：打开“启动”页面后产生了新的设置崩溃事件。",
                    newEvents,
                    observedWindows.Count > 0);
            }

            if (observedWindows.Count > 0)
            {
                return new LiveVerificationResult(
                    LiveVerificationState.Passed,
                    "本次动态验证通过：启动页面已观察并关闭，未监测到新的对应崩溃。",
                    newEvents,
                    true);
            }

            return new LiveVerificationResult(
                LiveVerificationState.Inconclusive,
                "本次监控未捕捉新崩溃，但未检测到可跟踪的设置窗口。",
                newEvents,
                false);
        }
        catch (Exception ex)
        {
            return new LiveVerificationResult(
                LiveVerificationState.LaunchFailed,
                "启动页面动态验证未完成。",
                new EventScanResult([]),
                false,
                ex.Message);
        }
    }

    private static List<SettingsWindowTarget> SelectVerificationWindows(
        IReadOnlyList<SettingsWindowTarget> existingWindows,
        IReadOnlyList<SettingsWindowTarget> currentWindows)
    {
        var existingHandles = existingWindows.Select(window => window.Handle).ToHashSet();
        var newlyOpened = currentWindows
            .Where(window => !existingHandles.Contains(window.Handle))
            .ToList();

        // The Settings protocol can reuse a single existing window. That window received this scan's navigation.
        return newlyOpened.Count > 0 ? newlyOpened : currentWindows.ToList();
    }
}
