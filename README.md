# Windows 设置闪退诊断与修复工具

这是一个本地运行的 Windows 小工具，用于检查 Claude 启动项并验证 Windows“设置”启动页面是否闪退：

1. Claude Desktop 写入的特定畸形启动命令导致“设置 -> 应用 -> 启动”崩溃。
2. 扫描期间产生的两类已知 Windows 设置崩溃事件。

程序启动后不会自动扫描，用户点击“开始扫描”后才会打开设置进行动态验证。

## 作者

- GitHub：[`Dawnnn816`](https://github.com/Dawnnn816)
- 创建日期：2026 年 8 月 16 日

## 安全边界

- 启动时会打开“设置 -> 应用 -> 启动”，在 8 秒监控窗口内检查是否产生新的对应崩溃事件，并在验证后自动关闭该设置窗口。
- 扫描不读取历史崩溃状态；当前故障仅由扫描开始后监控窗口内的新事件确认。若 Windows Shell 没有暴露可观察的设置窗口，结果会显示为“不作判定”，不会据此判为崩溃。
- Claude 修复仅在注册表值完全匹配已知畸形格式时开放。
- 修复前在当前用户的本地应用数据目录创建备份，可一键回滚。
- Claude 启动项修复与 Windows 设置重新注册是两个独立操作。
- 扫描不会自动打开或操作“系统组件”的更多选项；Windows 设置应用重新注册功能仍作为独立修复保留。
- “一键修复”会列出本次已确认的修复项目，获得确认后依次执行并自动重新扫描。
- 不直接修改 StateRepository 数据库。
- 导出的报告不包含用户名、SID、计算机名、完整路径、其他启动项或完整事件日志；界面日志可以单独导出，已对本机用户名和用户目录脱敏。
- 无遥测，不依赖网络。

## 开发

```powershell
dotnet build SettingsCrashRepair.slnx
dotnet run --project tests/SettingsCrashRepair.Tests
dotnet run --project src/SettingsCrashRepair
```

## 已知崩溃特征

| 页面 | 故障模块 | 异常代码 | 偏移 |
| --- | --- | --- | --- |
| 应用 -> 启动 | `SettingsHandlers_Startup.dll` | `0xc0000005` | `0x267b3` |
| 系统组件 -> 高级选项 | `SettingsHandlers_StorageSense.dll` | `0xc0000005` | `0xD69E3` |
