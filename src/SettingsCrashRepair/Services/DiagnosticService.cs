using System.Diagnostics;
using System.Reflection;
using SettingsCrashRepair.Models;

namespace SettingsCrashRepair.Services;

public sealed class DiagnosticService(
    ClaudeStartupService claudeService,
    EventLogScanner eventLogScanner,
    SettingsAppxService settingsAppxService,
    StartupPageVerifier startupPageVerifier)
{
    private readonly ClaudeStartupService _claudeService = claudeService;
    private readonly EventLogScanner _eventLogScanner = eventLogScanner;
    private readonly SettingsAppxService _settingsAppxService = settingsAppxService;
    private readonly StartupPageVerifier _startupPageVerifier = startupPageVerifier;

    public async Task<DiagnosticSnapshot> ScanAsync(
        IProgress<ScanProgress>? progress = null,
        bool performLiveVerification = true,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new ScanProgress(4, performLiveVerification
            ? "已开始监控，并准备打开启动页面..."
            : "正在进行静态检查（未运行动态验证）..."));
        var liveTask = performLiveVerification
            ? _startupPageVerifier.VerifyAsync(
                message => progress?.Report(new ScanProgress(64, message)),
                cancellationToken)
            : Task.FromResult(new LiveVerificationResult(
                LiveVerificationState.NotRun,
                "本次为静态检查，未运行启动页面动态验证。",
                new EventScanResult([]),
                false));

        progress?.Report(new ScanProgress(16, "正在检查 Claude 启动项格式..."));
        var claude = _claudeService.Inspect();
        progress?.Report(new ScanProgress(30, "正在检查 Windows 设置应用包..."));
        var appx = await _settingsAppxService.InspectAsync(cancellationToken);
        progress?.Report(new ScanProgress(56, performLiveVerification
            ? "静态信息已读取，等待动态监控完成..."
            : "正在汇总静态检查结果..."));
        var liveVerification = await liveTask;
        progress?.Report(new ScanProgress(94, "正在汇总本次监控结果..."));

        return new DiagnosticSnapshot(
            DateTimeOffset.Now,
            claude,
            liveVerification.NewEvents,
            appx,
            Environment.OSVersion.VersionString,
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0",
            ReadFileVersion(claude.ExpectedExecutablePath),
            ReadFileVersion(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "ImmersiveControlPanel",
                "SystemSettings.exe")),
            liveVerification);
    }

    private static string? ReadFileVersion(string path)
    {
        try
        {
            return File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).FileVersion : null;
        }
        catch
        {
            return null;
        }
    }
}
