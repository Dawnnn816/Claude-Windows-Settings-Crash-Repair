# Claude-related Windows Settings Crash Diagnostic and Repair Tool

[简体中文](README.md) | **English**

A fully local Windows utility for diagnosing and repairing a crash on the Windows **Settings > Apps > Startup** page triggered by a malformed Claude Desktop startup entry. It also retains an independent Windows Settings app re-registration repair for a second, confirmed Settings component registration issue.

This project grew out of a real troubleshooting experience. On August 14, 2026, Windows Settings consistently froze and closed on two different pages. Event logs, isolation tests, and post-repair regression testing showed that these were two independent crash paths.

## The Story

It started with something simple: I opened Windows Settings, went to **Apps > Startup**, and the Settings app suddenly froze and closed.

I tried security scans, `sfc /scannow`, and the built-in DISM repair, but none of them fixed it. After a lot of AI-assisted analysis, log inspection, and repeated testing, I discovered that one of the culprits was, surprisingly, a Claude Desktop startup entry.

The startup command written by Claude to the Windows Registry contained an extra pair of quotation marks. Such a tiny formatting error prevented Windows Settings from enumerating startup apps correctly, eventually causing the entire Settings process to freeze and crash.

During the investigation, I found another independent crash path. Under **System > System components**, opening **Advanced options** for a component could also fail to load and crash Settings. This issue was related to the registration state of the Windows Settings app. It could not be attributed to Claude, but the tool retains a separate inspection and repair option for it.

I have reported the Claude startup-entry issue to the Claude team, and I also found community reports from users who experienced similar behavior with matching crash modules and error signatures. Although later Claude release notes mentioned a related fix, real-world feedback suggests that the issue can still occur in later versions and on some systems.

That is why I turned the entire investigation and repair process into this small utility. It checks whether the Claude startup entry matches the known malformed format, dynamically verifies whether Windows Settings still crashes, and creates a backup before applying a confirmed repair.

Honestly, I still find it surprising that this tool was built almost entirely through step-by-step conversations with AI. AI tools are genuinely useful, but their conclusions are not guaranteed to be 100% correct. What ultimately confirms a diagnosis is evidence: logs, repeatable tests, and the result after a repair.

This bug usually does not affect the core operation of Windows, but the tool can at least help establish that the available evidence does not point to malware. It may simply be a small and deeply hidden startup-command error that is still enough to make Windows Settings crash repeatedly.

If you have experienced something similar, you can download this open-source tool and use it to inspect and repair the issue. If you find anything incomplete or incorrect, you are welcome to discuss it through GitHub Issues.

| Crash path | Direct trigger | Faulting module | Verified repair |
| --- | --- | --- | --- |
| Apps > Startup | An extra leading quotation mark in Claude's `HKCU\\...\\Run\\Claude` command | `SettingsHandlers_Startup.dll` | Correct the startup command |
| System > System components > Advanced options | Windows Settings AppX registration/state association issue | `SettingsHandlers_StorageSense.dll` | Re-register the Settings app manifest |

> Claude was the source of the malformed data in the first crash path, but Windows Settings should not crash with an access violation because of a malformed startup command. There is no evidence that Claude or another specific third-party application caused the second crash path.

## Download

Download `SettingsCrashRepair-1.3.0-win-x64.zip` from the [Releases](../../releases/latest) page. Extract it and run `SettingsCrashRepair.exe`. The executable is self-contained and does not require a separate .NET runtime installation.

- Current version: `1.3.0`
- Supported systems: 64-bit Windows 10 and Windows 11
- Network requirement: none
- Administrator privileges: usually not required for scanning or repairing the Claude startup entry; Windows may request elevated privileges when re-registering the Settings app

## Features

- Scanning starts only when requested and does not run automatically at launch.
- Checks whether the Claude startup entry exactly matches the known malformed format.
- Opens the Windows Startup Apps page and monitors only new crash events created during the current scan.
- Closes the Settings window opened for verification when the check is complete.
- Enables Claude startup-entry repair only for the known bad value, with automatic backup and rollback support.
- Keeps Windows Settings app re-registration as a separate repair from the Claude startup-entry repair.
- Offers one-click repair for issues explicitly confirmed by the current scan, followed by an automatic rescan.
- Exports sanitized diagnostic reports and operation logs.
- No telemetry and no network requests; all diagnostic data stays on the local computer.

## How to Use

1. Run `SettingsCrashRepair.exe`.
2. Select **Start Scan**. The tool briefly opens **Settings > Apps > Startup** and monitors for new crash events.
3. Use an individual repair or **Repair All Detected Issues** according to the scan result.
4. Review the automatic rescan after the repair completes.
5. When reporting a problem, use the tool's sanitized diagnostic report and log exports. Do not upload a complete Registry export or full event log.

For more details, see the [Chinese user guide](使用说明.txt).

## Incident Review

- [Full sanitized bug report (Chinese)](设置程序崩溃_Bug报告_2026-08-14.md)
- [Public evidence summary (Chinese)](docs/公开证据摘要.md)

The original evidence bundle contains local user paths, unrelated startup applications, raw Registry values, event Report IDs, and a Settings cache file inventory. It is therefore not included in the public repository. The public documents retain the reproduction conditions, exception codes, faulting modules, offsets, tests, and before-and-after conclusions.

## Safety Boundaries

- Scanning does not modify the Registry or the Windows Settings app registration state.
- Historical crashes are not used to decide whether the problem is currently present. Only new events created after the current scan begins are used for live verification.
- The Claude repair handles only an exact match for the known bad value. Unknown formats are not modified automatically.
- Claude startup-entry repair and Windows Settings re-registration are separate operations.
- The tool does not modify the StateRepository database directly.
- Exported reports exclude user names, SIDs, computer names, full paths, raw Registry values, unrelated startup entries, and complete event records.
- The current release is not signed with a commercial code-signing certificate, so Windows may display an "Unknown publisher" warning. Download it from this repository and verify its SHA-256 hash.

## Build from Source

Windows and the .NET 10 SDK are required:

```powershell
dotnet build SettingsCrashRepair.slnx -c Release
dotnet run --project tests/SettingsCrashRepair.Tests -c Release
dotnet run --project tests/SettingsCrashRepair.Tests -c Release -- --integration
```

Publish a self-contained, single-file Windows x64 build:

```powershell
dotnet publish src/SettingsCrashRepair/SettingsCrashRepair.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## Project Structure

```text
src/SettingsCrashRepair/        WinForms application
tests/SettingsCrashRepair.Tests Startup-command classification and sanitization integration tests
docs/                           Public evidence summary
```

## Feedback and Contributions

Please read the [contribution guide](CONTRIBUTING.md) before opening a GitHub Issue. Do not publicly upload a complete `HKCU\\Run` export, full event logs, or Settings cache files from a user profile.

## Author and License

- Author: [`Dawnnn816`](https://github.com/Dawnnn816)
- Created: August 16, 2026
- License: [MIT](LICENSE)

This project is not affiliated with or officially endorsed by Microsoft or Anthropic. The software is provided "as is." Review the confirmation prompts and keep backups of important data before use.
