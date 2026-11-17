using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using PoeActGuide.App.Interop;
using PoeActGuide.Core.Progress;

namespace PoeActGuide.App;

/// <summary>What the tray menu can ask the app to do.</summary>
internal interface ITrayCommands
{
    bool IsOverlayVisible { get; }

    ViewMode ViewMode { get; }

    bool IsMoveMode { get; }

    void ToggleVisible();

    void SetViewMode(ViewMode mode);

    void ToggleMoveMode();

    void PreviousAct();

    void NextAct();

    void BackToDetectedAct();

    void NextStep();

    void PreviousStep();

    void ChooseLogFile();

    void OpenSettingsFile();

    void OpenDataFolder();

    void ToggleHelp();

    void ExitApp();
}

/// <summary>The notification-area icon; the overlay has no taskbar button, so this is the way in.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Icon _image;
    private readonly ToolStripMenuItem _visibleItem;
    private readonly ToolStripMenuItem _normalItem;
    private readonly ToolStripMenuItem _compactItem;
    private readonly ToolStripMenuItem _fullItem;
    private readonly ToolStripMenuItem _moveItem;

    public TrayIcon(ITrayCommands commands)
    {
        _image = DrawIcon();
        var menu = new ContextMenuStrip();
        _visibleItem = new ToolStripMenuItem("오버레이 보이기", null, (_, _) => commands.ToggleVisible());
        _normalItem = new ToolStripMenuItem("기본 보기", null, (_, _) => commands.SetViewMode(ViewMode.Normal));
        _compactItem = new ToolStripMenuItem("간단히 보기", null, (_, _) => commands.SetViewMode(ViewMode.Compact));
        _fullItem = new ToolStripMenuItem("액트 전체 보기", null, (_, _) => commands.SetViewMode(ViewMode.Full));
        _moveItem = new ToolStripMenuItem("위치 이동 모드", null, (_, _) => commands.ToggleMoveMode());
        menu.Items.Add(_visibleItem);
        menu.Items.Add(_moveItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_normalItem);
        menu.Items.Add(_compactItem);
        menu.Items.Add(_fullItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("다음 단계", null, (_, _) => commands.NextStep()));
        menu.Items.Add(new ToolStripMenuItem("이전 단계", null, (_, _) => commands.PreviousStep()));
        menu.Items.Add(new ToolStripMenuItem("이전 액트", null, (_, _) => commands.PreviousAct()));
        menu.Items.Add(new ToolStripMenuItem("다음 액트", null, (_, _) => commands.NextAct()));
        menu.Items.Add(new ToolStripMenuItem("자동 감지로 돌아가기", null, (_, _) => commands.BackToDetectedAct()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("로그 파일(Client.txt) 지정...", null, (_, _) => commands.ChooseLogFile()));
        menu.Items.Add(new ToolStripMenuItem("설정 파일 열기", null, (_, _) => commands.OpenSettingsFile()));
        menu.Items.Add(new ToolStripMenuItem("가이드 데이터 폴더 열기", null, (_, _) => commands.OpenDataFolder()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("도움말", null, (_, _) => commands.ToggleHelp()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("종료", null, (_, _) => commands.ExitApp()));
        menu.Opening += (_, _) =>
        {
            _visibleItem.Checked = commands.IsOverlayVisible;
            _normalItem.Checked = commands.ViewMode == ViewMode.Normal;
            _compactItem.Checked = commands.ViewMode == ViewMode.Compact;
            _fullItem.Checked = commands.ViewMode == ViewMode.Full;
            _moveItem.Checked = commands.IsMoveMode;
        };

        _icon = new NotifyIcon
        {
            Icon = _image,
            Text = $"PoE 액트 가이드 {AppVersion}",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => commands.ToggleVisible();
    }

    /// <summary>"v1.2.3" from the assembly version the release workflow stamps; "개발 빌드" otherwise.</summary>
    private static string AppVersion
    {
        get
        {
            var v = typeof(TrayIcon).Assembly.GetName().Version;
            return v is null || (v.Major == 0 && v.Minor == 0 && v.Build == 0) ? "(개발 빌드)" : $"v{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    public void ShowBalloon(string text) =>
        _icon.ShowBalloonTip(5000, "PoE 액트 가이드", text, ToolTipIcon.Warning);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        NativeMethods.DestroyIcon(_image.Handle);
        _image.Dispose();
    }

    private static Icon DrawIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);
            using var background = new SolidBrush(Color.FromArgb(0xE6, 0xC2, 0x7A));
            g.FillEllipse(background, 1, 1, 30, 30);
            using var font = new Font("Segoe UI", 15, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
            using var ink = new SolidBrush(Color.FromArgb(0x20, 0x18, 0x10));
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("A", font, ink, new RectangleF(0, 1, 32, 32), format);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }
}
