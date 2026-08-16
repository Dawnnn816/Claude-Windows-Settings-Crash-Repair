using Microsoft.Win32;
using SettingsCrashRepair.Models;

namespace SettingsCrashRepair.Services;

public sealed class ClaudeStartupService(BackupStore backupStore)
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "Claude";

    private readonly BackupStore _backupStore = backupStore;

    public StartupInspection Inspect()
    {
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AnthropicClaude",
            "claude.exe");

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (key is null || !key.GetValueNames().Contains(ValueName, StringComparer.OrdinalIgnoreCase))
            {
                var missingState = StartupCommandClassifier.Classify(null, expectedPath, out var missingCorrect);
                return new StartupInspection(
                    missingState,
                    "未发现 Claude 自启动项。",
                    null,
                    expectedPath,
                    missingCorrect,
                    File.Exists(expectedPath),
                    null);
            }

            var raw = key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
            var kind = key.GetValueKind(ValueName);
            var state = StartupCommandClassifier.Classify(raw, expectedPath, out var correctValue);
            var summary = state switch
            {
                StartupValueState.Healthy => "Claude 启动命令格式正常。",
                StartupValueState.KnownMalformed => "确认发现会触发 Windows 设置崩溃的 Claude 启动命令。",
                StartupValueState.Unexpected => "Claude 启动命令不是已知格式，工具不会自动修改。",
                _ => "未发现 Claude 自启动项。"
            };

            return new StartupInspection(
                state,
                summary,
                raw,
                expectedPath,
                correctValue,
                File.Exists(expectedPath),
                kind);
        }
        catch (Exception ex)
        {
            return new StartupInspection(
                StartupValueState.ReadError,
                "无法读取 Claude 启动项。",
                null,
                expectedPath,
                StartupCommandClassifier.BuildCorrectValue(expectedPath),
                File.Exists(expectedPath),
                null,
                ex.Message);
        }
    }

    public async Task<OperationResult> RepairAsync(CancellationToken cancellationToken = default)
    {
        var inspection = Inspect();
        if (inspection.State != StartupValueState.KnownMalformed ||
            inspection.RawValue is null ||
            inspection.ValueKind is null)
        {
            return new OperationResult(false, "当前值未精确匹配已知故障格式，因此没有进行修改。");
        }

        if (inspection.ValueKind is not (RegistryValueKind.String or RegistryValueKind.ExpandString))
        {
            return new OperationResult(false, "启动项的数据类型不受支持，因此没有进行修改。");
        }

        var backup = new ClaudeBackup(
            DateTimeOffset.Now,
            $@"HKEY_CURRENT_USER\{RunKeyPath}",
            ValueName,
            inspection.RawValue,
            inspection.CorrectValue,
            inspection.ValueKind.Value);

        string backupPath;
        try
        {
            backupPath = await _backupStore.SaveAsync(backup, cancellationToken);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, $"无法创建备份，未修改注册表：{ex.Message}");
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("无法打开当前用户启动项注册表键。");
            key.SetValue(ValueName, inspection.CorrectValue, inspection.ValueKind.Value);

            var verified = Inspect();
            if (verified.State != StartupValueState.Healthy)
            {
                return new OperationResult(false, "写入后验证未通过。备份已保留，请使用恢复功能。");
            }

            return new OperationResult(true, $"Claude 启动项已修复。备份：{backupPath}");
        }
        catch (Exception ex)
        {
            return new OperationResult(false, $"修复失败，备份已保留：{ex.Message}");
        }
    }

    public async Task<OperationResult> RestoreLatestAsync(CancellationToken cancellationToken = default)
    {
        ClaudeBackup? backup;
        string? path;
        try
        {
            (backup, path) = await _backupStore.LoadLatestAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, $"读取备份失败：{ex.Message}");
        }

        if (backup is null)
        {
            return new OperationResult(false, "没有找到可恢复的 Claude 启动项备份。");
        }

        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AnthropicClaude",
            "claude.exe");
        var expectedMalformed = StartupCommandClassifier.BuildKnownMalformedValue(expectedPath);
        var expectedRepaired = StartupCommandClassifier.BuildCorrectValue(expectedPath);
        if (!backup.OriginalValue.Equals(expectedMalformed, StringComparison.OrdinalIgnoreCase) ||
            !backup.RepairedValue.Equals(expectedRepaired, StringComparison.OrdinalIgnoreCase) ||
            backup.ValueKind is not (RegistryValueKind.String or RegistryValueKind.ExpandString))
        {
            return new OperationResult(false, "最近的备份未通过格式校验，因此没有恢复。");
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("无法打开当前用户启动项注册表键。");
            var current = key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;

            if (!string.Equals(current, backup.RepairedValue, StringComparison.OrdinalIgnoreCase))
            {
                return new OperationResult(
                    false,
                    "当前启动项已在备份后发生变化。为避免覆盖新设置，工具拒绝自动恢复。");
            }

            key.SetValue(ValueName, backup.OriginalValue, backup.ValueKind);
            return new OperationResult(true, $"已恢复备份：{path}");
        }
        catch (Exception ex)
        {
            return new OperationResult(false, $"恢复失败：{ex.Message}");
        }
    }
}
