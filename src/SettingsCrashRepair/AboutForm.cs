using System.Diagnostics;
using System.Reflection;

namespace SettingsCrashRepair;

internal static class ProjectInfo
{
    public const string AuthorName = "Dawnnn816";
    public const string HomepageUrl = "https://github.com/Dawnnn816";
    public const string HomepageLabel = "GitHub 个人主页";
    public const string CreatedDate = "2026 年 8 月 16 日";
}

public sealed class AboutForm : Form
{
    private static readonly Color TextColor = Color.FromArgb(31, 35, 40);
    private static readonly Color MutedColor = Color.FromArgb(91, 99, 110);
    private static readonly Color AccentColor = Color.FromArgb(0, 103, 192);

    public AboutForm()
    {
        Text = "关于作者";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(700, 600);
        MinimumSize = new Size(620, 520);
        BackColor = Color.FromArgb(246, 247, 249);
        Font = new Font("Microsoft YaHei UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 22),
            ColumnCount = 1,
            RowCount = 6,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        root.Controls.Add(CreateLabel("Windows 设置闪退诊断与修复", 16, FontStyle.Bold), 0, 0);
        root.Controls.Add(CreateLabel($"作者：{ProjectInfo.AuthorName}", 10, FontStyle.Regular, MutedColor), 0, 1);
        root.Controls.Add(CreateLink(ProjectInfo.HomepageLabel, ProjectInfo.HomepageUrl), 0, 2);

        var disclaimer = CreateLabel(
            "本项目完全开源免费，所有功能无门槛使用。程序为离线版，数据仅在本机处理，不上传诊断内容。\n" +
            "项目不包含木马、后门或其他恶意程序；请从开源仓库获取发布文件并自行核对文件哈希。\n" +
            "如有任何问题，请通过开源社区反馈。工具不对第三方系统环境、数据丢失或使用结果作额外保证。",
            9.5F,
            FontStyle.Regular,
            TextColor);
        disclaimer.Dock = DockStyle.Fill;
        disclaimer.AutoSize = false;
        disclaimer.Padding = new Padding(14, 12, 14, 10);
        disclaimer.BorderStyle = BorderStyle.FixedSingle;
        root.Controls.Add(disclaimer, 0, 3);

        var created = CreateLabel($"程序创建日期：{ProjectInfo.CreatedDate}", 9, FontStyle.Regular, MutedColor);
        created.Dock = DockStyle.Fill;
        created.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(created, 0, 4);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var support = CreateButton("请作者喝奶茶", primary: true, width: 148, height: 36);
        var close = CreateButton("关闭", width: 90, height: 36);
        support.Click += (_, _) =>
        {
            using var dialog = new DonationForm();
            dialog.ShowDialog(this);
        };
        close.Click += (_, _) => Close();
        buttons.Controls.Add(close);
        buttons.Controls.Add(support);
        root.Controls.Add(buttons, 0, 5);
        Controls.Add(root);
    }

    private static LinkLabel CreateLink(string text, string url)
    {
        var link = new LinkLabel
        {
            Text = text,
            AutoSize = true,
            LinkColor = Color.FromArgb(0, 103, 192),
            ActiveLinkColor = Color.FromArgb(0, 78, 145),
            Font = new Font("Microsoft YaHei UI", 10F),
            Margin = new Padding(0, 4, 0, 0)
        };
        link.Links.Add(0, text.Length, url);
        link.LinkClicked += (_, e) => OpenUrl(e.Link?.LinkData as string ?? url);
        return link;
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private static Label CreateLabel(string text, float size, FontStyle style, Color? color = null)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", size, style),
            ForeColor = color ?? TextColor,
            BackColor = Color.Transparent,
            AutoSize = true,
            Margin = new Padding(0)
        };
    }

    private static Button CreateButton(string text, bool primary = false, int width = 120, int height = 36)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = height,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(0, 103, 192) : Color.FromArgb(242, 244, 247),
            ForeColor = primary ? Color.White : TextColor,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Font = new Font("Microsoft YaHei UI", 9F),
            Margin = new Padding(8, 0, 0, 0)
        };
    }
}

public sealed class DonationForm : Form
{
    public DonationForm()
    {
        Text = "支持项目";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(560, 650);
        Size = new Size(620, 760);
        BackColor = Color.White;
        Font = new Font("Microsoft YaHei UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 20),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.White
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.Controls.Add(new Label { Text = "请作者喝奶茶", Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
        root.Controls.Add(new Label
        {
            Text = "本项目完全开源免费，所有功能无门槛使用。\n如果项目对你有所帮助，欢迎自愿支持开发者喝杯奶茶。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft YaHei UI", 10F),
            ForeColor = Color.FromArgb(91, 99, 110)
        }, 0, 1);

        var picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White,
            AccessibleName = "项目支持收款码"
        };
        picture.Image = LoadDonationImage();
        root.Controls.Add(picture, 0, 2);
        var close = new Button { Text = "关闭", Width = 90, Height = 34, Anchor = AnchorStyles.None, FlatStyle = FlatStyle.Flat };
        close.Click += (_, _) => Close();
        root.Controls.Add(close, 0, 3);
        Controls.Add(root);
        FormClosed += (_, _) => picture.Image?.Dispose();
    }

    private static Image LoadDonationImage()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SettingsCrashRepair.DonationCode.jpg");
        if (stream is not null)
        {
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }

        return new Bitmap(1, 1);
    }
}
