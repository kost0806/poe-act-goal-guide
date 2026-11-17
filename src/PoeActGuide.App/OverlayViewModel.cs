using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using PoeActGuide.Core.Guide;
using PoeActGuide.Core.Help;
using PoeActGuide.Core.Progress;
using PoeActGuide.Core.Settings;
using Brushes = System.Windows.Media.Brushes;

namespace PoeActGuide.App;

/// <summary>Binding source for <see cref="OverlayWindow"/>.</summary>
internal sealed class OverlayViewModel : INotifyPropertyChanged
{
    private static readonly Brush CurrentTextBrush = Frozen(Color.FromRgb(0xFF, 0xF1, 0x8A));
    private static readonly Brush CurrentRowBrush = Frozen(Color.FromArgb(0x40, 0xFF, 0xD8, 0x6B));
    private static readonly Brush HereBrush = Frozen(Color.FromRgb(0xF5, 0xE3, 0xB0));
    private static readonly Brush KeyBrush = Frozen(Color.FromRgb(0xF2, 0xE6, 0xC9));
    private static readonly Brush DoneBrush = Frozen(Color.FromRgb(0x6E, 0x6A, 0x62));
    private static readonly Brush TextBrush = Frozen(Color.FromRgb(0xE8, 0xE4, 0xDA));
    private static readonly Brush NoteBrush = Frozen(Color.FromRgb(0x9C, 0x97, 0x8C));
    private static readonly Brush MoveBorderBrush = Frozen(Color.FromRgb(0xFF, 0xC3, 0x5C));
    private static readonly Brush IdleBorderBrush = Frozen(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));

    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private bool _isManual;
    private string? _status;
    private IReadOnlyList<StepItem> _steps = Array.Empty<StepItem>();
    private string _progress = string.Empty;
    private double _fontSize = 13;
    private Brush _background = Brushes.Black;
    private bool _isMoveMode;
    private bool _isHelpVisible;
    private IReadOnlyList<HelpSectionItem> _helpSections = Array.Empty<HelpSectionItem>();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get => _title; private set => Set(ref _title, value); }

    public string Subtitle { get => _subtitle; private set => Set(ref _subtitle, value); }

    public bool IsManual { get => _isManual; private set => Set(ref _isManual, value); }

    /// <summary>A problem to show above the goals (log not found, hotkey taken, ...); null when fine.</summary>
    public string? Status
    {
        get => _status;
        set
        {
            if (Set(ref _status, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_status);

    public IReadOnlyList<StepItem> Steps { get => _steps; private set => Set(ref _steps, value); }

    public string Progress { get => _progress; private set => Set(ref _progress, value); }

    public double FontSize
    {
        get => _fontSize;
        private set
        {
            if (Set(ref _fontSize, value))
            {
                OnPropertyChanged(nameof(TitleFontSize));
                OnPropertyChanged(nameof(SmallFontSize));
            }
        }
    }

    public double TitleFontSize => _fontSize + 3;

    public double SmallFontSize => Math.Max(9, _fontSize - 2);

    public Brush Background { get => _background; private set => Set(ref _background, value); }

    public bool IsMoveMode
    {
        get => _isMoveMode;
        set
        {
            if (Set(ref _isMoveMode, value))
            {
                OnPropertyChanged(nameof(BorderBrush));
            }
        }
    }

    public Brush BorderBrush => _isMoveMode ? MoveBorderBrush : IdleBorderBrush;

    public string MoveModeHint { get; private set; } = string.Empty;

    /// <summary>True while the help screen replaces the step list.</summary>
    public bool IsHelpVisible
    {
        get => _isHelpVisible;
        set
        {
            if (Set(ref _isHelpVisible, value))
            {
                OnPropertyChanged(nameof(ShowSteps));
            }
        }
    }

    public bool ShowSteps => !_isHelpVisible;

    public IReadOnlyList<HelpSectionItem> HelpSections { get => _helpSections; private set => Set(ref _helpSections, value); }

    public string HelpTitle { get; private set; } = string.Empty;

    public void ApplySettings(AppSettings settings)
    {
        FontSize = settings.FontSize;
        var alpha = (byte)Math.Round(settings.BackgroundOpacity * 255);
        Background = Frozen(Color.FromArgb(alpha, 0x12, 0x12, 0x14));
        MoveModeHint = $"이동 모드 — 드래그해서 옮기세요 ({settings.Hotkeys.ToggleMoveMode} 로 종료)";
        OnPropertyChanged(nameof(MoveModeHint));

        var closeKey = HotkeyGesture.TryParse(settings.Hotkeys.ToggleHelp, out var key) && key is not null
            ? $"{key} 로 닫기"
            : "트레이 메뉴에서 닫기";
        HelpTitle = $"도움말 — {closeKey}";
        OnPropertyChanged(nameof(HelpTitle));
        HelpSections = HelpContent.Build(settings.Hotkeys)
            .Select(s => new HelpSectionItem(
                s.Title,
                s.Lines.Select(l => new HelpLineItem(
                    l.Key ?? (l.Tag is { } tag ? ProgressTracker.TagOf(tag) : string.Empty),
                    l.Key is not null || l.Tag is not null,
                    l.Tag is { } t ? KindBrush(t) : KeyBrush,
                    l.Text)).ToList()))
            .ToList();
    }

    public void Update(GuideView view, ViewMode mode)
    {
        Title = $"{view.Act}장";
        IsManual = view.IsManual;
        Progress = view.TotalSteps == 0 ? string.Empty : $"{Math.Max(view.CurrentNumber, 0)}/{view.TotalSteps} 단계";
        Subtitle = view.CurrentArea is { } area
            ? $"{area.Name} · 레벨 {area.Level}"
            : "지역 정보 없음";

        Steps = view.Steps
            .Select(v =>
            {
                var current = v.State == StepState.Current;
                var done = v.State == StepState.Done;
                return new StepItem(
                    $"{ProgressTracker.TagOf(v.Step.Kind)}",
                    done ? DoneBrush : KindBrush(v.Step.Kind),
                    (v.Step.Key ? "★ " : string.Empty) + v.Step.Text,
                    v.Step.Note,
                    !string.IsNullOrEmpty(v.Step.Note) && !done && (mode != ViewMode.Compact || current),
                    current ? CurrentTextBrush : done ? DoneBrush : v.IsHere ? HereBrush : TextBrush,
                    current ? FontWeights.Bold : FontWeights.Normal,
                    done ? DoneBrush : NoteBrush,
                    current ? CurrentRowBrush : Brushes.Transparent);
            })
            .ToList();
    }

    private static Brush KindBrush(StepKind kind) => kind switch
    {
        StepKind.Main => Frozen(Color.FromRgb(0xE6, 0xC2, 0x7A)),
        StepKind.Trial => Frozen(Color.FromRgb(0xC0, 0x9C, 0xFF)),
        StepKind.Passive => Frozen(Color.FromRgb(0x7C, 0xC7, 0xFF)),
        StepKind.Recipe => Frozen(Color.FromRgb(0x8F, 0xD6, 0x94)),
        _ => Frozen(Color.FromRgb(0xF0, 0xA0, 0x70)),
    };

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed record StepItem(
    string Tag,
    Brush TagBrush,
    string Text,
    string? Note,
    bool ShowNote,
    Brush TextBrush,
    FontWeight Weight,
    Brush NoteBrush,
    Brush RowBrush);

internal sealed record HelpSectionItem(string Title, IReadOnlyList<HelpLineItem> Lines);

internal sealed record HelpLineItem(string Label, bool HasLabel, Brush LabelBrush, string Text);
