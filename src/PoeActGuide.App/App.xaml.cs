using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using PoeActGuide.Core.Guide;
using PoeActGuide.Core.Log;
using PoeActGuide.Core.Progress;
using PoeActGuide.Core.Settings;

namespace PoeActGuide.App;

/// <summary>Wires the guide, the log watcher, the overlay, hotkeys and the tray icon together.</summary>
public partial class App : Application, ITrayCommands
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan LocateRetryInterval = TimeSpan.FromSeconds(5);

    private readonly OverlayViewModel _viewModel = new();
    private Mutex? _singleInstance;
    private SettingsStore? _settingsStore;
    private AppSettings _settings = new();
    private ProgressTracker? _tracker;
    private OverlayWindow? _window;
    private HotkeyManager? _hotkeys;
    private TrayIcon? _tray;
    private ClientLogTailer? _tailer;
    private DispatcherTimer? _locateTimer;
    private string _dataDirectory = string.Empty;
    private string? _hotkeyProblems;

    public bool IsOverlayVisible => _window?.IsVisible ?? false;

    public ViewMode ViewMode => _settings.ViewMode;

    public bool IsMoveMode => _viewModel.IsMoveMode;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(initiallyOwned: true, @"Local\PoeActGuide.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("PoE 액트 가이드가 이미 실행 중입니다. 작업 표시줄 오른쪽의 트레이 아이콘을 확인하세요.", "PoE 액트 가이드");
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        GuideData data;
        try
        {
            data = GuideLoader.LoadFromDirectory(_dataDirectory);
        }
        catch (GuideDataException ex)
        {
            MessageBox.Show(ex.Message, "PoE 액트 가이드 — 데이터 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var problems = GuideValidator.Validate(data);
        if (problems.Count > 0)
        {
            MessageBox.Show(
                "가이드 데이터에 문제가 있습니다 (계속 실행합니다):\n\n" + string.Join('\n', problems.Take(15)),
                "PoE 액트 가이드 — 데이터 경고",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _settingsStore = new SettingsStore(SettingsStore.DefaultPath());
        _settings = _settingsStore.Load();

        _tracker = new ProgressTracker(data);
        _tracker.Changed += (_, _) => Refresh();

        _viewModel.ApplySettings(_settings);
        _window = new OverlayWindow(_viewModel) { Width = _settings.Width };
        PlaceWindow(_window);
        _window.Moved += (_, _) => SavePosition();
        _window.Show();

        _hotkeys = new HotkeyManager(_window.Handle);
        RegisterHotkeys();
        _tray = new TrayIcon(this);
        if (_hotkeyProblems is not null)
        {
            _tray.ShowBalloon("일부 단축키를 등록하지 못했습니다:\n" + _hotkeyProblems);
        }

        ConnectLog();
        Refresh();

        if (_settings.ShowHelpOnStart)
        {
            _viewModel.IsHelpVisible = true;
            _settings.ShowHelpOnStart = false;
            _settingsStore.TrySave(_settings);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _locateTimer?.Stop();
        _tailer?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        if (_window is not null && _settingsStore is not null)
        {
            SavePosition();
        }

        _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    public void ToggleVisible()
    {
        if (_window is null)
        {
            return;
        }

        if (_window.IsVisible)
        {
            if (IsMoveMode)
            {
                _window.SetMoveMode(false);
            }

            _window.Hide();
        }
        else
        {
            _window.Show();
        }
    }

    public void SetViewMode(ViewMode mode)
    {
        _settings.ViewMode = mode;
        _settingsStore?.TrySave(_settings);
        Refresh();
    }

    public void CycleViewMode() => SetViewMode(_settings.ViewMode switch
    {
        ViewMode.Normal => ViewMode.Compact,
        ViewMode.Compact => ViewMode.Full,
        _ => ViewMode.Normal,
    });

    public void ToggleMoveMode()
    {
        if (_window is null)
        {
            return;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
        }

        _window.SetMoveMode(!IsMoveMode);
    }

    public void PreviousAct() => _tracker?.ShowPreviousAct();

    public void NextAct() => _tracker?.ShowNextAct();

    public void BackToDetectedAct() => _tracker?.ClearManualAct();

    public void NextStep() => _tracker?.NextStep();

    public void PreviousStep() => _tracker?.PreviousStep();

    public void ChooseLogFile()
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Path of Exile 로그 파일 선택 (logs 폴더의 Client.txt 또는 KakaoClient.txt)",
            Filter = "PoE 로그 (Client.txt;KakaoClient.txt)|Client.txt;KakaoClient.txt|텍스트 파일 (*.txt)|*.txt",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        _settings.ClientLogPath = dialog.FileName;
        _settingsStore?.TrySave(_settings);
        ConnectLog();
    }

    public void OpenSettingsFile()
    {
        if (_settingsStore is null)
        {
            return;
        }

        _settingsStore.TrySave(_settings);
        OpenWithShell(_settingsStore.Path);
    }

    public void OpenDataFolder() => OpenWithShell(_dataDirectory);

    public void ToggleHelp()
    {
        if (_window is not null && !_window.IsVisible)
        {
            _window.Show();
            _viewModel.IsHelpVisible = true;
            return;
        }

        _viewModel.IsHelpVisible = !_viewModel.IsHelpVisible;
    }

    public void ExitApp() => Shutdown();

    private static void OpenWithShell(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            MessageBox.Show($"열 수 없습니다: {path}\n{ex.Message}", "PoE 액트 가이드");
        }
    }

    private void RegisterHotkeys()
    {
        if (_hotkeys is null)
        {
            return;
        }

        var keys = _settings.Hotkeys;
        var problems = new[]
            {
                _hotkeys.Register(keys.ToggleVisible, ToggleVisible),
                _hotkeys.Register(keys.PreviousAct, PreviousAct),
                _hotkeys.Register(keys.NextAct, NextAct),
                _hotkeys.Register(keys.NextStep, NextStep),
                _hotkeys.Register(keys.PreviousStep, PreviousStep),
                _hotkeys.Register(keys.CycleViewMode, CycleViewMode),
                _hotkeys.Register(keys.ToggleMoveMode, ToggleMoveMode),
                _hotkeys.Register(keys.ToggleHelp, ToggleHelp),
            }
            .Where(p => p is not null)
            .ToList();
        _hotkeyProblems = problems.Count > 0 ? string.Join('\n', problems) : null;
    }

    /// <summary>Finds the log, jumps to the last area in it, and starts following it.</summary>
    private void ConnectLog()
    {
        _tailer?.Dispose();
        _tailer = null;

        var path = ClientLogLocator.Locate(_settings.ClientLogPath);
        if (path is null)
        {
            _viewModel.Status = "Client.txt 를 찾지 못했습니다. 게임을 실행하면 자동으로 찾고, 트레이 메뉴에서 직접 지정할 수도 있습니다.";
            _locateTimer ??= CreateLocateTimer();
            _locateTimer.Start();
            return;
        }

        _locateTimer?.Stop();
        _viewModel.Status = null;

        // Replaying the recent log rebuilds the route position, not just the act.
        foreach (var entered in ClientLogParser.ReadAreaEntries(path))
        {
            _tracker?.OnAreaEntered(entered.AreaId);
        }

        _tailer = new ClientLogTailer(path, startAtEnd: true);
        _tailer.LineAppended += (_, line) =>
        {
            if (ClientLogParser.TryParseAreaEntered(line, out var entered) && entered is not null)
            {
                Dispatcher.BeginInvoke(() => _tracker?.OnAreaEntered(entered.AreaId));
            }
        };
        _tailer.Start(PollInterval);
    }

    private DispatcherTimer CreateLocateTimer()
    {
        var timer = new DispatcherTimer { Interval = LocateRetryInterval };
        timer.Tick += (_, _) =>
        {
            if (ClientLogLocator.Locate(_settings.ClientLogPath) is not null)
            {
                ConnectLog();
            }
        };
        return timer;
    }

    private void Refresh()
    {
        if (_tracker is not null)
        {
            _viewModel.Update(_tracker.BuildView(_settings.ViewMode), _settings.ViewMode);
        }
    }

    private void PlaceWindow(Window window)
    {
        var left = _settings.Left;
        var top = _settings.Top;
        var screenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        var screenBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;

        // Keep at least a corner reachable if a monitor was unplugged since last time.
        if (left < SystemParameters.VirtualScreenLeft || left > screenRight - 80
            || top < SystemParameters.VirtualScreenTop || top > screenBottom - 80)
        {
            left = 20;
            top = 160;
        }

        window.Left = left;
        window.Top = top;
    }

    private void SavePosition()
    {
        if (_window is null || _settingsStore is null)
        {
            return;
        }

        _settings.Left = _window.Left;
        _settings.Top = _window.Top;
        _settingsStore.TrySave(_settings);
    }
}
