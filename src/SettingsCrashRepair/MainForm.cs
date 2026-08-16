using System.Diagnostics;
using System.Text;
using SettingsCrashRepair.Models;
using SettingsCrashRepair.Services;

namespace SettingsCrashRepair;

public sealed class MainForm : Form
{
    private static readonly Color BackgroundColor = Color.FromArgb(246, 247, 249);
    private static readonly Color SurfaceColor = Color.White;
    private static readonly Color TextColor = Color.FromArgb(31, 35, 40);
    private static readonly Color MutedColor = Color.FromArgb(91, 99, 110);
    private static readonly Color AccentColor = Color.FromArgb(0, 103, 192);
    private static readonly Color GoodColor = Color.FromArgb(18, 128, 82);
    private static readonly Color WarningColor = Color.FromArgb(176, 104, 0);
    private static readonly Color ErrorColor = Color.FromArgb(196, 43, 28);

    private readonly BackupStore _backupStore = new();
    private readonly EventLogScanner _eventLogScanner = new();
    private readonly ClaudeStartupService _claudeService;
    private readonly SettingsAppxService _settingsAppxService = new();
    private readonly DiagnosticService _diagnosticService;

    private readonly Label _overallStatus = CreateLabel("尚未扫描", 12, FontStyle.Bold);
    private readonly Label _lastScanLabel = CreateLabel("点击“开始扫描”后才会打开设置进行动态验证。", 9, FontStyle.Regular, MutedColor);
    private readonly Label _progressText = CreateLabel("等待扫描", 9, FontStyle.Regular, MutedColor);
    private readonly ProgressBar _scanProgress = new() { Dock = DockStyle.Fill, Height = 10, Minimum = 0, Maximum = 100 };
    private readonly Label _claudeIndicator = CreateIndicator();
    private readonly Label _claudeSummary = CreateLabel("等待扫描", 10, FontStyle.Bold);
    private readonly Label _claudeDetail = CreateLabel("检查当前用户的 Claude 自启动命令。", 9, FontStyle.Regular, MutedColor);
    private readonly Label _settingsIndicator = CreateIndicator();
    private readonly Label _settingsSummary = CreateLabel("等待扫描", 10, FontStyle.Bold);
    private readonly Label _settingsDetail = CreateLabel("检查设置应用包及本次监控产生的新崩溃事件。", 9, FontStyle.Regular, MutedColor);
    private readonly Label _eventsIndicator = CreateIndicator();
    private readonly Label _eventsSummary = CreateLabel("等待扫描", 10, FontStyle.Bold);
    private readonly Label _eventsDetail = CreateLabel("这里只显示本次监控窗口内产生的新事件。", 9, FontStyle.Regular, MutedColor);
    private readonly Button _scanButton = CreateButton("开始扫描", primary: true, width: 188, height: 46);
    private readonly Button _oneClickRepairButton = CreateButton("一键修复已发现问题", primary: true, width: 178);
    private readonly Button _repairClaudeButton = CreateButton("修复 Claude 启动项", width: 178);
    private readonly Button _repairSettingsButton = CreateButton("重新注册设置应用", width: 178);
    private readonly Button _restoreButton = CreateButton("恢复 Claude 备份", width: 154);
    private readonly Button _exportButton = CreateButton("导出脱敏报告", width: 144);
    private readonly Button _exportLogButton = CreateButton("导出日志", width: 100, height: 30);
    private readonly Button _openStartupButton = CreateButton("打开启动页面", width: 178);
    private readonly Button _aboutButton = CreateButton("关于作者", width: 112, height: 34);
    private readonly RichTextBox _activityLog = new()
    {
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Color.FromArgb(250, 251, 252),
        ForeColor = TextColor,
        Font = new Font("Consolas", 8.8F),
        Dock = DockStyle.Fill,
        DetectUrls = false,
        TabStop = false,
        WordWrap = true
    };

    private DiagnosticSnapshot? _snapshot;
    private bool _busy;
    private bool _acceptProgressUpdates;
    private string? _lastProgressMessage;

    public MainForm()
    {
        _claudeService = new ClaudeStartupService(_backupStore);
        _diagnosticService = new DiagnosticService(
            _claudeService,
            _eventLogScanner,
            _settingsAppxService,
            new StartupPageVerifier(_eventLogScanner));

        Text = "Windows 设置闪退诊断与修复";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1080, 780);
        Size = new Size(1180, 860);
        BackColor = BackgroundColor;
        ForeColor = TextColor;
        Font = new Font("Microsoft YaHei UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildLayout();
        WireEvents();
        UpdateActionAvailability();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _overallStatus.Text = "准备就绪，等待手动扫描";
        _overallStatus.ForeColor = AccentColor;
        AddLog("程序已启动。扫描不会自动开始，请点击“开始扫描”。");
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackgroundColor,
            Padding = new Padding(24, 20, 24, 18),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildOverallPanel(), 0, 1);
        root.Controls.Add(BuildBody(), 0, 2);
        root.Controls.Add(BuildActionBar(), 0, 3);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 204));
        var left = new Panel { Dock = DockStyle.Fill };
        var title = CreateLabel("Windows 设置闪退诊断与修复", 19, FontStyle.Bold);
        title.AutoSize = true;
        title.Location = new Point(0, 0);
        var subtitle = CreateLabel(
            "本地运行、无遥测。点击“开始扫描”后才会打开“设置 -> 应用 -> 启动”进行动态验证。",
            9.5F,
            FontStyle.Regular,
            MutedColor);
        subtitle.AutoSize = true;
        subtitle.MaximumSize = new Size(620, 0);
        subtitle.Location = new Point(2, 45);
        left.Controls.Add(title);
        left.Controls.Add(subtitle);
        _scanButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _scanButton.Margin = new Padding(0, 10, 0, 0);
        layout.Controls.Add(left, 0, 0);
        layout.Controls.Add(_scanButton, 1, 0);
        return layout;
    }

    private Control BuildOverallPanel()
    {
        var panel = CreateSurfacePanel(new Padding(18, 13, 18, 12));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _overallStatus.Dock = DockStyle.Fill;
        _lastScanLabel.Dock = DockStyle.Fill;
        _lastScanLabel.AutoEllipsis = true;
        _progressText.Dock = DockStyle.Fill;
        _progressText.TextAlign = ContentAlignment.MiddleRight;
        _scanProgress.Margin = new Padding(0, 7, 12, 2);
        layout.Controls.Add(_overallStatus, 0, 0);
        layout.SetColumnSpan(_overallStatus, 2);
        layout.Controls.Add(_lastScanLabel, 0, 1);
        layout.SetColumnSpan(_lastScanLabel, 2);
        layout.Controls.Add(_scanProgress, 0, 2);
        layout.Controls.Add(_progressText, 1, 2);
        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildBody()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 12, 0, 0) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        var results = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0, 0, 10, 0) };
        results.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        results.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        results.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        results.Controls.Add(BuildFindingPanel(_claudeIndicator, "Claude 启动项", _claudeSummary, _claudeDetail, _repairClaudeButton, _openStartupButton), 0, 0);
        results.Controls.Add(BuildFindingPanel(_settingsIndicator, "Windows 设置组件", _settingsSummary, _settingsDetail, _repairSettingsButton), 0, 1);
        results.Controls.Add(BuildFindingPanel(_eventsIndicator, "本次动态验证", _eventsSummary, _eventsDetail), 0, 2);
        layout.Controls.Add(results, 0, 0);
        layout.Controls.Add(BuildLogPanel(), 1, 0);
        return layout;
    }

    private static Control BuildFindingPanel(Label indicator, string titleText, Label summary, Label detail, params Button[] buttons)
    {
        var outer = CreateSurfacePanel(new Padding(18, 14, 16, 14));
        outer.Margin = new Padding(0, 0, 0, 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, BackColor = SurfaceColor };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, buttons.Length > 0 ? 186 : 0));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        indicator.Dock = DockStyle.Fill;
        layout.Controls.Add(indicator, 0, 0);
        layout.SetRowSpan(indicator, 3);
        var title = CreateLabel(titleText, 9, FontStyle.Regular, MutedColor);
        title.Dock = DockStyle.Fill;
        title.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(title, 1, 0);
        summary.Dock = DockStyle.Fill;
        summary.TextAlign = ContentAlignment.MiddleLeft;
        summary.AutoEllipsis = false;
        layout.Controls.Add(summary, 1, 1);
        detail.Dock = DockStyle.Fill;
        detail.TextAlign = ContentAlignment.TopLeft;
        detail.AutoEllipsis = false;
        layout.Controls.Add(detail, 1, 2);
        if (buttons.Length > 0)
        {
            var buttonFlow = new FlowLayoutPanel { AutoSize = false, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
            foreach (var button in buttons)
            {
                button.Margin = new Padding(0, 0, 0, 7);
                buttonFlow.Controls.Add(button);
            }
            layout.Controls.Add(buttonFlow, 2, 0);
            layout.SetRowSpan(buttonFlow, 3);
        }
        outer.Controls.Add(layout);
        return outer;
    }

    private Control BuildLogPanel()
    {
        var outer = CreateSurfacePanel(new Padding(14, 12, 14, 12));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = CreateLabel("操作日志", 10, FontStyle.Bold);
        title.Dock = DockStyle.Fill;
        title.TextAlign = ContentAlignment.MiddleLeft;
        _exportLogButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(_exportLogButton, 1, 0);
        layout.Controls.Add(_activityLog, 0, 1);
        layout.SetColumnSpan(_activityLog, 2);
        outer.Controls.Add(layout);
        return outer;
    }

    private Control BuildActionBar()
    {
        var bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 0) };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        left.Controls.Add(_oneClickRepairButton);
        left.Controls.Add(_restoreButton);
        left.Controls.Add(_exportButton);
        var right = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        _aboutButton.Margin = new Padding(0, 0, 0, 0);
        right.Controls.Add(_aboutButton);
        bar.Controls.Add(left, 0, 0);
        bar.Controls.Add(right, 1, 0);
        return bar;
    }

    private void WireEvents()
    {
        _scanButton.Click += async (_, _) => await ScanAsync();
        _oneClickRepairButton.Click += async (_, _) => await RepairAllAsync();
        _repairClaudeButton.Click += async (_, _) => await RepairClaudeAsync();
        _repairSettingsButton.Click += async (_, _) => await RepairSettingsAsync();
        _restoreButton.Click += async (_, _) => await RestoreBackupAsync();
        _exportButton.Click += async (_, _) => await ExportReportAsync();
        _exportLogButton.Click += async (_, _) => await ExportLogAsync();
        _openStartupButton.Click += (_, _) => OpenSettingsPage("ms-settings:startupapps");
        _aboutButton.Click += (_, _) =>
        {
            using var dialog = new AboutForm();
            dialog.ShowDialog(this);
        };
    }

    private async Task ScanAsync()
    {
        if (_busy) return;
        SetBusy(true, "正在扫描并准备动态验证...");
        _acceptProgressUpdates = true;
        _lastProgressMessage = null;
        SetProgress(0, "开始扫描");
        AddLog("开始扫描：静态检查不会修改系统；稍后将打开“设置 -> 应用 -> 启动”进行动态验证。");
        try
        {
            var progress = new Progress<ScanProgress>(UpdateProgress);
            _snapshot = await _diagnosticService.ScanAsync(progress);
            _acceptProgressUpdates = false;
            RenderSnapshot(_snapshot);
            SetProgress(100, "扫描完成");
            AddLog("扫描完成。");
        }
        catch (Exception ex)
        {
            _acceptProgressUpdates = false;
            _overallStatus.Text = "扫描未完成";
            _overallStatus.ForeColor = ErrorColor;
            SetProgress(0, "扫描失败");
            AddLog($"扫描失败：{OneLine(ex.Message)}");
        }
        finally
        {
            _acceptProgressUpdates = false;
            SetBusy(false);
        }
    }

    private void UpdateProgress(ScanProgress progress)
    {
        if (!_acceptProgressUpdates) return;
        if (progress.Percent >= _scanProgress.Value)
        {
            SetProgress(progress.Percent, progress.Message);
        }
        if (!string.Equals(_lastProgressMessage, progress.Message, StringComparison.Ordinal))
        {
            _lastProgressMessage = progress.Message;
            AddLog(progress.Message);
        }
    }

    private void SetProgress(int value, string message)
    {
        _scanProgress.Value = Math.Clamp(value, _scanProgress.Minimum, _scanProgress.Maximum);
        _progressText.Text = $"{_scanProgress.Value}%  {message}";
    }

    private void RenderSnapshot(DiagnosticSnapshot snapshot)
    {
        RenderClaude(snapshot.Claude);
        RenderSettings(snapshot.SettingsAppx, snapshot.Events.StorageSense);
        RenderEvents(snapshot.Events, snapshot.LiveVerification);
        var requiresClaudeRepair = snapshot.Claude.State == StartupValueState.KnownMalformed;
        var startupCrashConfirmed = snapshot.LiveVerification.State == LiveVerificationState.CrashConfirmed;
        var hasSettingsEvidence = snapshot.Events.StorageSense is not null;
        if (requiresClaudeRepair || startupCrashConfirmed)
        {
            _overallStatus.Text = "发现当前可复现或可确认修复的问题";
            _overallStatus.ForeColor = ErrorColor;
        }
        else if (hasSettingsEvidence)
        {
            _overallStatus.Text = "本次监控捕获到 Windows 设置组件的新崩溃事件";
            _overallStatus.ForeColor = WarningColor;
        }
        else
        {
            _overallStatus.Text = "本次启动页面动态验证未发现对应崩溃";
            _overallStatus.ForeColor = GoodColor;
        }
        _lastScanLabel.Text = $"扫描时间：{snapshot.ScannedAt:yyyy-MM-dd HH:mm:ss}    Windows：{snapshot.WindowsVersion}    工具：{snapshot.ToolVersion}";
        UpdateActionAvailability();
    }

    private void RenderClaude(StartupInspection inspection)
    {
        _claudeSummary.Text = inspection.Summary;
        (_claudeIndicator.ForeColor, _claudeSummary.ForeColor) = inspection.State switch
        {
            StartupValueState.KnownMalformed => (ErrorColor, ErrorColor),
            StartupValueState.Healthy => (GoodColor, TextColor),
            StartupValueState.Missing => (WarningColor, TextColor),
            StartupValueState.Unexpected => (WarningColor, WarningColor),
            _ => (ErrorColor, ErrorColor)
        };
        var version = _snapshot?.ClaudeVersion ?? "未读取到";
        _claudeDetail.Text = inspection.Error is not null ? $"读取错误：{OneLine(inspection.Error)}" : $"Claude 文件：{(inspection.ExecutableExists ? "存在" : "未找到")}；版本：{version}。" + (inspection.State == StartupValueState.KnownMalformed ? " 修复前会先创建本地备份。" : string.Empty);
        if (inspection.State == StartupValueState.KnownMalformed) AddLog("疑点：确认发现已知畸形 Claude 启动命令，可安全修复。");
    }

    private void RenderSettings(SettingsAppxInspection appx, CrashEvidence? storageEvidence)
    {
        if (appx.Error is not null)
        {
            _settingsIndicator.ForeColor = ErrorColor;
            _settingsSummary.ForeColor = ErrorColor;
            _settingsSummary.Text = "无法完整检查 Windows 设置应用包。";
            _settingsDetail.Text = $"{OneLine(appx.Error)}；清单文件：{(appx.ManifestExists ? "存在" : "未找到")}。";
            AddLog("疑点：无法完整读取 Windows 设置应用包状态。");
            return;
        }
        _settingsSummary.ForeColor = TextColor;
        if (storageEvidence is not null)
        {
            _settingsIndicator.ForeColor = WarningColor;
            _settingsSummary.Text = "本次监控检测到 Windows 设置组件的新崩溃事件。";
            AddLog($"疑点：本次监控检测到 Windows 设置组件的新崩溃事件（{storageEvidence.Count} 次）。");
        }
        else
        {
            _settingsIndicator.ForeColor = appx.PackageFound ? GoodColor : WarningColor;
            _settingsSummary.Text = appx.PackageFound ? "设置应用包可读取，本次扫描未发现对应的新崩溃。" : "未找到设置应用包。";
        }
        _settingsDetail.Text = $"包状态：{appx.Status}；版本：{appx.PackageVersion ?? "未知"}；系统清单：{(appx.ManifestExists ? "存在" : "未找到")}。";
    }

    private void RenderEvents(EventScanResult monitoredEvents, LiveVerificationResult live)
    {
        _eventsSummary.Text = live.Summary;
        _eventsDetail.Text = BuildEventDetail(monitoredEvents, live);
        (_eventsIndicator.ForeColor, _eventsSummary.ForeColor) = live.State switch
        {
            LiveVerificationState.Passed => (GoodColor, TextColor),
            LiveVerificationState.CrashConfirmed => (ErrorColor, ErrorColor),
            LiveVerificationState.Inconclusive => (WarningColor, WarningColor),
            LiveVerificationState.LaunchFailed => (ErrorColor, ErrorColor),
            _ => (MutedColor, TextColor)
        };
        if (live.State == LiveVerificationState.CrashConfirmed) AddLog("疑点：动态验证期间捕获到新的启动页面崩溃事件，当前故障已确认。");
        else if (live.State == LiveVerificationState.Passed) AddLog("动态验证通过：监控窗口内未捕获新的启动页面崩溃事件。");
        else AddLog($"动态验证结果未定：{live.Summary}");
    }

    private static string BuildEventDetail(EventScanResult monitoredEvents, LiveVerificationResult live)
    {
        if (monitoredEvents.Error is not null) return $"本次监控日志读取失败：{OneLine(monitoredEvents.Error)}";
        var parts = new List<string>();
        if (monitoredEvents.Startup is not null) parts.Add($"本次启动页 {monitoredEvents.Startup.Count} 次（最近 {FormatTime(monitoredEvents.Startup.LatestOccurrence)}）");
        if (monitoredEvents.StorageSense is not null) parts.Add($"本次设置组件 {monitoredEvents.StorageSense.Count} 次（最近 {FormatTime(monitoredEvents.StorageSense.LatestOccurrence)}）");
        if (live.NewEvents.Error is null && live.NewEvents.Evidence.Count > 0) parts.Add($"本次监控新增 {live.NewEvents.Evidence.Sum(x => x.Count)} 次事件");
        if (!live.SettingsProcessWasAlive && live.State == LiveVerificationState.Inconclusive) parts.Add("设置启动进程在监控期内退出，未据此判为崩溃");
        return parts.Count == 0 ? "本次监控窗口内没有产生两类已知崩溃事件。" : string.Join("；", parts) + "。";
    }

    private async Task RepairAllAsync()
    {
        if (_snapshot is null) return;
        var repairClaude = _snapshot.Claude.State == StartupValueState.KnownMalformed;
        var repairSettings = _snapshot.Events.StorageSense is not null && _snapshot.SettingsAppx.ManifestExists;
        if (!repairClaude && !repairSettings)
        {
            MessageBox.Show(this, "当前扫描结果没有可执行的一键修复项目。", "一键修复", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var targets = new List<string>();
        if (repairClaude) targets.Add("修复已确认的 Claude 启动项格式，并创建本地备份");
        if (repairSettings) targets.Add("重新注册 Windows 设置应用（会关闭已打开的设置窗口）");
        var answer = MessageBox.Show(this, "将执行以下独立修复：\n\n- " + string.Join("\n- ", targets) + "\n\n不会修改 StateRepository 数据库。完成后会自动重新扫描并动态验证启动页面。是否继续？", "确认一键修复", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        SetBusy(true, "正在执行一键修复...");
        var results = new List<OperationResult>();
        try
        {
            if (repairClaude) { SetProgress(25, "正在修复 Claude 启动项..."); results.Add(await _claudeService.RepairAsync()); }
            if (repairSettings) { SetProgress(65, "正在重新注册 Windows 设置应用..."); results.Add(await _settingsAppxService.RepairRegistrationAsync()); }
            foreach (var result in results) AddLog(result.Message);
        }
        finally { SetBusy(false); }
        MessageBox.Show(this, string.Join(Environment.NewLine, results.Select(x => x.Message)), "一键修复结果", MessageBoxButtons.OK, results.All(x => x.Success) ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        await ScanAsync();
    }

    private async Task RepairClaudeAsync()
    {
        if (_snapshot?.Claude.State != StartupValueState.KnownMalformed) return;
        var answer = MessageBox.Show(this, "工具只会把已确认的畸形 Claude 启动命令改为标准格式。\n\n修改前会创建本地备份。是否继续？", "确认修复 Claude 启动项", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        SetBusy(true, "正在备份并修复 Claude 启动项...");
        try { var result = await _claudeService.RepairAsync(); AddLog(result.Message); ShowOperationResult(result, "Claude 启动项"); }
        finally { SetBusy(false); }
        await ScanAsync();
    }

    private async Task RepairSettingsAsync()
    {
        if (_snapshot?.Events.StorageSense is null || !_snapshot.SettingsAppx.ManifestExists) return;
        var answer = MessageBox.Show(this, "这是与 Claude 启动项相互独立的修复。\n\n操作会关闭当前打开的“设置”窗口，并使用 Windows 自带清单重新注册设置应用；不会修改 StateRepository 数据库或删除个人设置。请仅在本次扫描捕获到设置组件崩溃时执行。\n\n是否继续？", "确认重新注册 Windows 设置应用", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        SetBusy(true, "正在重新注册 Windows 设置应用...");
        try { var result = await _settingsAppxService.RepairRegistrationAsync(); AddLog(result.Message); ShowOperationResult(result, "Windows 设置应用"); }
        finally { SetBusy(false); }
        await ScanAsync();
    }

    private async Task RestoreBackupAsync()
    {
        var answer = MessageBox.Show(this, "恢复会把 Claude 启动值改回最近一次修复前的内容。若备份中是已知畸形值，恢复后 Windows 设置的“启动”页面可能再次崩溃。\n\n工具会先确认当前值没有被其他程序改动。是否继续？", "确认恢复 Claude 备份", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        SetBusy(true, "正在检查并恢复最近的 Claude 备份...");
        try { var result = await _claudeService.RestoreLatestAsync(); AddLog(result.Message); ShowOperationResult(result, "恢复 Claude 备份"); }
        finally { SetBusy(false); }
        await ScanAsync();
    }

    private async Task ExportReportAsync()
    {
        if (_snapshot is null) return;
        using var dialog = new SaveFileDialog { Title = "导出脱敏诊断报告", Filter = "JSON 报告 (*.json)|*.json", FileName = $"SettingsCrashRepair-report-{DateTime.Now:yyyyMMdd-HHmmss}.json", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AddExtension = true, DefaultExt = "json", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        SetBusy(true, "正在导出脱敏报告...");
        try { await ReportExporter.ExportAsync(_snapshot, dialog.FileName); AddLog("脱敏诊断报告已导出。"); MessageBox.Show(this, "脱敏报告已导出。", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { AddLog($"报告导出失败：{OneLine(ex.Message)}"); MessageBox.Show(this, OneLine(ex.Message), "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
    }

    private async Task ExportLogAsync()
    {
        using var dialog = new SaveFileDialog { Title = "导出操作日志", Filter = "文本日志 (*.txt)|*.txt", FileName = $"SettingsCrashRepair-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AddExtension = true, DefaultExt = "txt", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var contents = $"Windows 设置闪退诊断与修复 - 操作日志{Environment.NewLine}导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{Environment.NewLine}" + _activityLog.Text;
            await File.WriteAllTextAsync(dialog.FileName, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            AddLog("操作日志已导出。");
        }
        catch (Exception ex) { AddLog($"日志导出失败：{OneLine(ex.Message)}"); MessageBox.Show(this, OneLine(ex.Message), "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void OpenSettingsPage(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); AddLog("已请求打开设置页面。"); }
        catch (Exception ex) { AddLog($"无法打开设置页面：{OneLine(ex.Message)}"); }
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _busy = busy;
        UseWaitCursor = busy;
        if (status is not null) { _overallStatus.Text = status; _overallStatus.ForeColor = AccentColor; }
        UpdateActionAvailability();
    }

    private void UpdateActionAvailability()
    {
        _scanButton.Enabled = !_busy;
        _repairClaudeButton.Enabled = !_busy && _snapshot?.Claude.State == StartupValueState.KnownMalformed;
        _repairSettingsButton.Enabled = !_busy && _snapshot?.Events.StorageSense is not null && _snapshot.SettingsAppx.ManifestExists;
        _oneClickRepairButton.Enabled = !_busy && _snapshot is not null && (_snapshot.Claude.State == StartupValueState.KnownMalformed || (_snapshot.Events.StorageSense is not null && _snapshot.SettingsAppx.ManifestExists));
        _restoreButton.Enabled = !_busy;
        _exportButton.Enabled = !_busy && _snapshot is not null;
        _exportLogButton.Enabled = !_busy && _activityLog.TextLength > 0;
        _openStartupButton.Enabled = !_busy;
        _aboutButton.Enabled = !_busy;
    }

    private void ShowOperationResult(OperationResult result, string title) => MessageBox.Show(this, result.Message, title, MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);

    private void AddLog(string message)
    {
        _activityLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {SanitizeForLog(OneLine(message))}{Environment.NewLine}");
        _activityLog.SelectionStart = _activityLog.TextLength;
        _activityLog.ScrollToCaret();
        UpdateActionAvailability();
    }

    private static string SanitizeForLog(string message)
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return message.Replace(profile, "<用户目录>", StringComparison.OrdinalIgnoreCase).Replace(Environment.UserName, "<用户名>", StringComparison.OrdinalIgnoreCase).Replace(Environment.MachineName, "<计算机名>", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatTime(DateTimeOffset? time) => time?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "未知时间";
    private static string OneLine(string text) => string.Join(" ", text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static Panel CreateSurfacePanel(Padding padding) => new() { Dock = DockStyle.Fill, BackColor = SurfaceColor, Padding = padding, Margin = new Padding(0), BorderStyle = BorderStyle.FixedSingle };
    private static Label CreateIndicator() => new() { Text = "●", Font = new Font("Segoe UI Symbol", 15F), ForeColor = MutedColor, TextAlign = ContentAlignment.TopCenter, Padding = new Padding(0, 3, 0, 0), AutoSize = false };
    private static Label CreateLabel(string text, float size, FontStyle style, Color? color = null) => new() { Text = text, Font = new Font("Microsoft YaHei UI", size, style), ForeColor = color ?? TextColor, BackColor = Color.Transparent, AutoSize = false };
    private static Button CreateButton(string text, bool primary = false, int width = 140, int height = 36) => new() { Text = text, Width = width, Height = height, Margin = new Padding(0, 0, 9, 7), FlatStyle = FlatStyle.Flat, BackColor = primary ? AccentColor : Color.FromArgb(242, 244, 247), ForeColor = primary ? Color.White : TextColor, UseVisualStyleBackColor = false, Cursor = Cursors.Hand, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular) };
}
