## Claude导致的Windows 设置闪退诊断与修复工具

**简体中文** | [English](README_EN.md)

一个完全本地运行的 Windows 工具，用于诊断并修复 Claude Desktop 异常启动项触发的 Windows“设置 -> 应用 -> 启动”页面崩溃，同时保留 Windows 设置应用重新注册功能，用于处理另一类已确认的设置组件注册异常。

本项目源自一次真实故障排查。2026 年 8 月 14 日，Windows 设置应用在两个页面稳定卡死并退出。事件日志、隔离测试和修复后回归验证表明，这是两条相互独立的故障路径：
### 简介

事情最开始其实很简单：我在使用电脑时打开 Windows 设置，进入“应用 -> 启动”管理页面，设置程序却突然卡死，然后直接闪退。

我试过安全软件扫描，也执行过 Windows 自带的 `sfc /scannow` 和 DISM 修复，但都没有解决。后来在大量 AI 辅助分析、日志检查和反复测试之后，我才发现，其中一个罪魁祸首竟然是 Claude Desktop 的启动项。

Claude 写入 Windows 注册表的开机启动命令中，多出了一层不该出现的引号。就是这么一个看起来很小的符号错误，导致 Windows 设置无法正确列举启动项目，最后使整个设置程序卡死并闪退。

排查过程中，我还发现了另一条独立的崩溃路径：进入“系统 -> 系统组件”，点击任意组件的“高级选项”时，设置程序也可能无法正常加载并闪退。这一项与 Windows 设置应用的注册状态有关，并不能确认是 Claude 导致的，但工具也保留了对应的检查和修复能力。

目前我已经将 Claude 启动项问题反馈给 Claude 官方，并且在社区里发现也有用户遇到了类似情况，其崩溃模块和错误特征与本次问题相同。Claude 后续的更新说明中曾提到相关修复，但从实际反馈来看，在后续版本和部分电脑环境中，这个问题仍然可能复现。

因此，我把整个排查和修复过程做成了这个小工具。它可以检查 Claude 启动项是否属于已知的错误格式，动态验证 Windows 设置是否仍会崩溃，并在确认问题后进行备份和修复。

说实话，我自己也很难想象，这个工具几乎是通过 AI 和自然语言交流一步一步开发出来的。AI 工具确实很好用，但我们也应该明白，它的判断不一定百分之百正确。最终真正能确认问题的，还是日志、重复测试，以及修复之后的实际结果。

这个 Bug 通常不会影响 Windows 的核心运行，但至少这个工具可以让你知道：现有证据并不指向电脑中毒。它更可能只是一个很小、很隐蔽，却又足以让 Windows 设置反复闪退的启动命令错误。

如果你也有类似经历，可以尝试下载这个开源工具进行检查和修复。后续如果发现任何不足，欢迎来 GitHub 一起讨论。

| 故障路径 | 直接触发因素 | 崩溃模块 | 已验证修复 |
| --- | --- | --- | --- |
| 应用 -> 启动 | Claude 的 `HKCU\\...\\Run\\Claude` 命令存在多余前置引号 | `SettingsHandlers_Startup.dll` | 校正启动命令 |
| 系统 -> 系统组件 -> 高级选项 | Windows 设置 AppX 注册/状态关联异常 | `SettingsHandlers_StorageSense.dll` | 重新注册设置应用清单 |

> Claude 是第一条路径的异常数据来源，但 Windows 设置不应因一条畸形启动命令发生访问冲突。第二条路径没有证据表明由 Claude 或其他特定第三方软件造成。

## 下载

请从仓库的 [Releases](../../releases/latest) 页面下载 `SettingsCrashRepair-1.3.0-win-x64.zip`。解压后运行 `SettingsCrashRepair.exe`，无需安装 .NET 运行时。

- 当前版本：`1.3.0`
- 适用系统：Windows 10 / Windows 11，64 位
- 网络要求：不需要联网
- 管理员权限：日常扫描和 Claude 启动项修复通常不需要；设置应用重新注册可能触发系统权限要求

## 功能

- 手动开始扫描，启动时不会自动操作系统。
- 检查 Claude 启动项是否精确匹配已知畸形格式。
- 打开 Windows 启动项页面并仅监控本次扫描期间产生的新崩溃事件。
- 验证完成后关闭本次扫描打开的设置窗口。
- 仅对已知坏值开放 Claude 启动项修复，修复前自动备份并支持回滚。
- 保留 Windows 设置应用重新注册功能，作为与 Claude 修复相互独立的操作。
- 一键修复本次扫描明确确认的问题，然后自动重新扫描。
- 导出经过脱敏的诊断报告与操作日志。
- 无遥测、无网络请求，所有数据仅在本机处理。

## 使用方法

1. 运行 `SettingsCrashRepair.exe`。
2. 点击“开始扫描”。程序会短暂打开“设置 -> 应用 -> 启动”并监控新崩溃事件。
3. 根据扫描结果使用单项修复或“一键修复已发现问题”。
4. 修复完成后查看自动复扫结果。
5. 如需反馈问题，使用程序内的“导出诊断报告”和“导出日志”，不要上传完整注册表或完整事件日志。

更详细的操作说明见 [使用说明.txt](使用说明.txt)。

## 事件复盘

- [完整脱敏 Bug 报告](设置程序崩溃_Bug报告_2026-08-14.md)
- [公开证据摘要](docs/公开证据摘要.md)

原始证据包包含本机用户名路径、其他启动软件、注册表原始值、事件 Report ID 和设置缓存文件清单，因此不会提交到公开仓库。公开文档保留了复现条件、异常码、故障模块、偏移、测试过程和修复前后结论。

## 安全边界

- 扫描不会修改注册表或设置应用注册状态。
- 程序不使用历史崩溃记录判定当前故障；只有扫描开始后的新事件才用于动态验证。
- Claude 修复只处理精确匹配的已知坏值，未知格式不会自动修改。
- Claude 修复与 Windows 设置重新注册是两个独立操作。
- 不直接修改 StateRepository 数据库。
- 导出内容不包含用户名、SID、计算机名、完整路径、原始注册表值、无关启动项或完整事件记录。
- 当前发布文件没有商业代码签名证书，Windows 可能显示“未知发布者”。请从本仓库 Release 下载并核对 SHA-256。

## 从源码构建

需要 Windows 和 .NET 10 SDK：

```powershell
dotnet build SettingsCrashRepair.slnx -c Release
dotnet run --project tests/SettingsCrashRepair.Tests -c Release
dotnet run --project tests/SettingsCrashRepair.Tests -c Release -- --integration
```

生成 64 位自包含单文件：

```powershell
dotnet publish src/SettingsCrashRepair/SettingsCrashRepair.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## 项目结构

```text
src/SettingsCrashRepair/        WinForms 主程序
tests/SettingsCrashRepair.Tests 启动命令分类与脱敏集成测试
docs/                           公开证据摘要
```

## 反馈与贡献

遇到问题请先阅读 [贡献指南](CONTRIBUTING.md)，然后在 GitHub Issues 提交脱敏信息。请勿公开上传完整 `HKCU\\Run` 导出、完整事件日志或用户目录下的设置缓存。

## 作者与许可

- 作者：[`Dawnnn816`](https://github.com/Dawnnn816)
- 创建日期：2026 年 8 月 16 日
- 许可证：[MIT](LICENSE)

本项目与 Microsoft、Anthropic 没有隶属或官方合作关系。软件按“原样”提供，使用前请阅读界面确认信息并保留重要数据备份。
