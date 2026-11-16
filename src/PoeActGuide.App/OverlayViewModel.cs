using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using PoeActGuide.Core.Guide;
using PoeActGuide.Core.Progress;
using PoeActGuide.Core.Settings;

namespace PoeActGuide.App;

/// <summary>Binding source for <see cref="OverlayWindow"/>.</summary>
internal sealed class OverlayViewModel : INotifyPropertyChanged
{
    private static readonly Brush HereBrush = Frozen(Color.FromRgb(0xFF, 0xF1, 0x8A));
    private static readonly Brush TextBrush = Frozen(Color.FromRgb(0xE8, 0xE4, 0xDA));
    private static readonly Brush NoteBrush = Frozen(Color.FromRgb(0x9C, 0x97, 0x8C));
    private static readonly Brush MoveBorderBrush = Frozen(Color.FromRgb(0xFF, 0xC3, 0x5C));
    private static readonly Brush IdleBorderBrush = Frozen(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));

    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private bool _isManual;
    private string? _status;
    private IReadOnlyList<SectionItem> _sections = Array.Empty<SectionItem>();
    private double _fontSize = 13;
    private Brush _background = Brushes.Black;
    private bool _isMoveMode;

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

    public IReadOnlyList<SectionItem> Sections { get => _sections; private set => Set(ref _sections, value); }

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

    public void ApplySettings(AppSettings settings)
    {
        FontSize = settings.FontSize;
        var alpha = (byte)Math.Round(settings.BackgroundOpacity * 255);
        Background = Frozen(Color.FromArgb(alpha, 0x12, 0x12, 0x14));
        MoveModeHint = $"이동 모드 — 드래그해서 옮기세요 ({settings.Hotkeys.ToggleMoveMode} 로 종료)";
        OnPropertyChanged(nameof(MoveModeHint));
    }

    public void Update(GuideView view)
    {
        Title = $"{view.Act}장";
        IsManual = view.IsManual;
        Subtitle = view.CurrentArea is { } area
            ? $"{area.Name} · Lv {area.Level}"
            : "지역 정보 없음";

        Sections = view.Sections
            .Select(s => new SectionItem(
                s.Title,
                KindBrush(s.Kind),
                s.Goals.Select(g => new GoalItem(
                    g.Goal.Text,
                    g.Goal.Note,
                    g.Goal.Key ? "★" : "•",
                    g.IsHere,
                    g.IsHere ? HereBrush : TextBrush,
                    g.IsHere ? FontWeights.Bold : FontWeights.Normal,
                    NoteBrush)).ToList()))
            .ToList();
    }

    private static Brush KindBrush(GoalKind kind) => kind switch
    {
        GoalKind.Main => Frozen(Color.FromRgb(0xE6, 0xC2, 0x7A)),
        GoalKind.Trial => Frozen(Color.FromRgb(0xC0, 0x9C, 0xFF)),
        GoalKind.Passive => Frozen(Color.FromRgb(0x7C, 0xC7, 0xFF)),
        GoalKind.Recipe => Frozen(Color.FromRgb(0x8F, 0xD6, 0x94)),
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

internal sealed record SectionItem(string Title, Brush TitleBrush, IReadOnlyList<GoalItem> Goals);

internal sealed record GoalItem(
    string Text,
    string? Note,
    string Bullet,
    bool IsHere,
    Brush TextBrush,
    FontWeight Weight,
    Brush NoteBrush)
{
    public bool HasNote => !string.IsNullOrEmpty(Note);
}
