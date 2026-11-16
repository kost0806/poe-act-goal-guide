using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using PoeActGuide.App.Interop;

namespace PoeActGuide.App;

/// <summary>
/// The overlay: always on top, never focused, and click-through except in move mode.
/// </summary>
/// <remarks>
/// <c>AllowsTransparency=True</c> makes WPF create a layered window, so the rounded, translucent
/// background needs nothing else. The trade-off is grayscale rather than ClearType text, which reads
/// fine on the dark background.
/// </remarks>
internal sealed partial class OverlayWindow : Window
{
    private readonly OverlayViewModel _viewModel;

    public OverlayWindow(OverlayViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    /// <summary>Raised when move mode ends, so the new position can be saved.</summary>
    public event EventHandler? Moved;

    public nint Handle => new WindowInteropHelper(this).Handle;

    public void SetMoveMode(bool on)
    {
        _viewModel.IsMoveMode = on;
        if (on)
        {
            NativeMethods.UpdateExtendedStyle(Handle, set: 0, clear: NativeMethods.WsExTransparent);
        }
        else
        {
            NativeMethods.UpdateExtendedStyle(Handle, set: NativeMethods.WsExTransparent, clear: 0);
            Moved?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        NativeMethods.UpdateExtendedStyle(
            Handle,
            set: NativeMethods.WsExTransparent | NativeMethods.WsExNoActivate | NativeMethods.WsExToolWindow,
            clear: 0);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsMoveMode && e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
