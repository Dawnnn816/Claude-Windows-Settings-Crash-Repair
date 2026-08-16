# 参与贡献

感谢帮助改进这个项目。提交问题或代码前，请注意本项目会接触 Windows 事件日志和当前用户启动项，隐私保护优先于证据完整度。

## 报告问题

请使用 GitHub Issue 模板并提供：

- Windows 显示版本与完整 OS Build。
- Claude Desktop 版本。
- 工具版本。
- 具体复现路径。
- 程序导出的脱敏诊断报告和脱敏日志。

不要公开上传：

- 完整 `HKCU\Run` 或 `StartupApproved` 注册表导出。
- 完整 Windows 事件日志。
- 用户名、SID、计算机名、用户目录绝对路径。
- Windows 设置应用的 LocalState、WebView 缓存或账户数据。

## 提交代码

1. Fork 仓库并从 `main` 创建分支。
2. 保持修复范围明确，不扩大自动修改注册表的匹配规则。
3. 新增诊断字段时同步检查脱敏导出逻辑。
4. 执行构建和测试：

```powershell
dotnet build SettingsCrashRepair.slnx -c Release
dotnet run --project tests/SettingsCrashRepair.Tests -c Release -- --integration
```

5. 在 Pull Request 中说明行为变化、风险和验证方式。

## 设计原则

- 默认只扫描，不自动修复。
- 只修复精确匹配的已知坏值。
- 修改前备份，能回滚时提供回滚。
- 当前故障由当前扫描窗口的新证据判定，不复用历史崩溃作结论。
- 不上传诊断数据，不直接修改 StateRepository 数据库。
