using System.Text.Json;
using SettingsCrashRepair.Models;

namespace SettingsCrashRepair.Services;

public sealed class SettingsAppxService
{
    public string ManifestPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        "ImmersiveControlPanel",
        "AppxManifest.xml");

    public async Task<SettingsAppxInspection> InspectAsync(CancellationToken cancellationToken = default)
    {
        const string command = "$p=Get-AppxPackage -Name windows.immersivecontrolpanel -ErrorAction SilentlyContinue; " +
            "if($null -eq $p){'null'}else{[pscustomobject]@{Name=$p.Name;Version=$p.Version.ToString();" +
            "Status=$p.Status.ToString()} | ConvertTo-Json -Compress}";

        try
        {
            var result = await RunPowerShellAsync(command, cancellationToken);
            if (result.ExitCode != 0)
            {
                return new SettingsAppxInspection(
                    File.Exists(ManifestPath), false, "读取失败", null, FirstLine(result.StandardError));
            }

            var json = result.StandardOutput.Trim();
            if (json.Length == 0 || json.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return new SettingsAppxInspection(File.Exists(ManifestPath), false, "未找到", null);
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var status = root.TryGetProperty("Status", out var statusElement)
                ? statusElement.ToString()
                : "未知";
            var version = root.TryGetProperty("Version", out var versionElement)
                ? versionElement.ToString()
                : null;
            return new SettingsAppxInspection(File.Exists(ManifestPath), true, status, version);
        }
        catch (Exception ex)
        {
            return new SettingsAppxInspection(
                File.Exists(ManifestPath), false, "读取失败", null, FirstLine(ex.Message));
        }
    }

    public async Task<OperationResult> RepairRegistrationAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ManifestPath))
        {
            return new OperationResult(false, "找不到 Windows 设置应用清单，未执行修复。");
        }

        var escapedManifest = ManifestPath.Replace("'", "''", StringComparison.Ordinal);
        var command = $"Add-AppxPackage -DisableDevelopmentMode -Register '{escapedManifest}' " +
            "-ForceApplicationShutdown -ErrorAction Stop; 'REPAIR_OK'";

        try
        {
            var result = await RunPowerShellAsync(command, cancellationToken);
            if (result.ExitCode != 0 || !result.StandardOutput.Contains("REPAIR_OK", StringComparison.Ordinal))
            {
                return new OperationResult(false, $"设置应用重新注册失败：{FirstLine(result.StandardError)}");
            }

            var inspection = await InspectAsync(cancellationToken);
            if (!inspection.PackageFound)
            {
                return new OperationResult(false, "命令已执行，但重新检查时未找到设置应用包。");
            }

            return new OperationResult(true, $"Windows 设置应用已重新注册，当前包状态：{inspection.Status}。");
        }
        catch (Exception ex)
        {
            return new OperationResult(false, $"设置应用重新注册失败：{FirstLine(ex.Message)}");
        }
    }

    private static Task<ProcessResult> RunPowerShellAsync(
        string command,
        CancellationToken cancellationToken) =>
        ProcessRunner.RunAsync(
            "powershell.exe",
            ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", command],
            cancellationToken);

    private static string FirstLine(string text) =>
        text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
        ?? "未知错误";
}
