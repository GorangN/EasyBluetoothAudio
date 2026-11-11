using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using EasyBluetoothAudio.Behaviors;
using EasyBluetoothAudio.Services.Interfaces;
using Size = System.Drawing.Size;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Keeps monitor bounds, window measurements, and placement in the same native coordinate space.
/// </summary>
public sealed class WindowPlacementService : IWindowPlacementService
{
    private const int EdgeMargin = 10;
    private const double SettingsHeight = 800;
    private const int ResizeDurationMilliseconds = 240;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private System.Windows.Size? _compactSize;
    private bool _isSettingsExpanded;
    private Action? _stopResizeAnimation;
    internal bool IsSettingsResizeAnimating
    {
        get { return _stopResizeAnimation != null; }
    }

    /// <inheritdoc />
    public void PositionNearCursor(Window window)
    {
        var workArea = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var minimumWidth = window.MinWidth;
        var minimumHeight = window.MinHeight;
        window.MinWidth = 0;
        window.MinHeight = 0;
        if (!TryPositionWindow(handle, workArea))
        {
            PositionInPrimaryWorkArea(window);
        }
        var scale = Math.Max(96, GetDpiForWindow(handle)) / 96.0;
        window.MinWidth = Math.Min(minimumWidth, workArea.Width / scale);
        window.MinHeight = Math.Min(minimumHeight, workArea.Height / scale);
    }

    /// <inheritdoc />
    public void SetSettingsExpanded(Window window, bool expanded, bool animate = true)
    {
        window.Dispatcher.VerifyAccess();
        if ((expanded && _isSettingsExpanded && (animate || _stopResizeAnimation == null))
            || (!expanded && !_compactSize.HasValue)
            || (!expanded && !_isSettingsExpanded && _stopResizeAnimation != null && animate))
        {
            return;
        }

        _stopResizeAnimation?.Invoke();
        var handle = new WindowInteropHelper(window).EnsureHandle();
        if (!GetWindowRect(handle, out var nativeBounds))
        {
            Debug.WriteLine($"[WindowPlacement] GetWindowRect failed: {Marshal.GetLastWin32Error()}");
            return;
        }

        var bounds = Rectangle.FromLTRB(nativeBounds.Left, nativeBounds.Top, nativeBounds.Right, nativeBounds.Bottom);
        var scale = Math.Max(96, GetDpiForWindow(handle)) / 96.0;
        var compactSize = _compactSize ?? new System.Windows.Size(bounds.Width / scale, bounds.Height / scale);
        _compactSize = compactSize;
        _isSettingsExpanded = expanded;
        if (animate && window.IsVisible && SystemParameters.ClientAreaAnimation
            && bounds != CalculateSettingsPlacement(handle, bounds, compactSize, expanded))
        {
            AnimateSettingsResize(window, handle, bounds, compactSize, expanded);
        }
        else
        {
            CompleteSettingsResize(handle, bounds, compactSize, expanded);
        }
    }

    private void CompleteSettingsResize(IntPtr handle, Rectangle anchor, System.Windows.Size compactSize, bool expanded)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            if (!ApplyNativeBounds(handle, CalculateSettingsPlacement(handle, anchor, compactSize, expanded)))
            {
                return;
            }
        }

        if (!expanded)
        {
            _compactSize = null;
        }
    }

    private static Rectangle CalculateSettingsPlacement(IntPtr handle, Rectangle anchor, System.Windows.Size compactSize, bool expanded)
    {
        var scale = Math.Max(96, GetDpiForWindow(handle)) / 96.0;
        var workArea = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;
        var requestedSize = new Size((int)Math.Round(compactSize.Width * scale),
            (int)Math.Round((expanded ? Math.Max(compactSize.Height, SettingsHeight) : compactSize.Height) * scale));
        return CalculateBottomAnchoredPlacement(workArea, anchor, requestedSize);
    }

    private void AnimateSettingsResize(Window window, IntPtr handle, Rectangle start, System.Windows.Size compactSize, bool expanded)
    {
        var clock = Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Render, window.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(ResizeDurationMilliseconds)
        };
        bool stopped = false;
        void Stop()
        {
            if (stopped) { return; }
            stopped = true;
            CompositionTarget.Rendering -= OnFrame;
            timer.Tick -= OnTimer;
            timer.Stop();
            window.IsVisibleChanged -= OnVisibilityChanged;
            window.Closed -= OnClosed;
            _stopResizeAnimation = null;
        }
        void Finish()
        {
            if (stopped) { return; }
            Stop();
            CompleteSettingsResize(handle, start, compactSize, expanded);
        }
        void OnFrame(object? sender, EventArgs args)
        {
            if (stopped) { return; }
            if (!SystemParameters.ClientAreaAnimation) { Finish(); return; }
            var progress = Math.Min(1, clock.Elapsed.TotalMilliseconds / ResizeDurationMilliseconds);
            var target = CalculateSettingsPlacement(handle, start, compactSize, expanded);
            if (!ApplyNativeBounds(handle, InterpolateBottomAnchoredPlacement(start, target, progress)))
            {
                Stop();
            }
            else if (progress >= 1)
            {
                Finish();
            }
        }
        void OnTimer(object? sender, EventArgs args) { Finish(); }
        void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (!window.IsVisible) { Finish(); }
        }
        void OnClosed(object? sender, EventArgs args)
        {
            Stop();
            _compactSize = null;
            _isSettingsExpanded = false;
        }
        _stopResizeAnimation = Stop;
        CompositionTarget.Rendering += OnFrame;
        timer.Tick += OnTimer;
        window.IsVisibleChanged += OnVisibilityChanged;
        window.Closed += OnClosed;
        timer.Start();
    }

    private static bool ApplyNativeBounds(IntPtr handle, Rectangle bounds)
    {
        if (SetWindowPos(handle, IntPtr.Zero, bounds.X, bounds.Y, bounds.Width, bounds.Height, SwpNoZOrder | SwpNoActivate))
        {
            return true;
        }
        Debug.WriteLine($"[WindowPlacement] SetWindowPos failed: {Marshal.GetLastWin32Error()}");
        return false;
    }

    internal static Rectangle InterpolateBottomAnchoredPlacement(Rectangle start, Rectangle target, double progress)
    {
        var eased = MotionAnimation.EaseOut.GetSplineProgress(Math.Clamp(progress, 0, 1));
        int width = (int)Math.Round(start.Width + (target.Width - start.Width) * eased);
        int height = (int)Math.Round(start.Height + (target.Height - start.Height) * eased);
        int left = (int)Math.Round(start.Left + (target.Left - start.Left) * eased);
        return new Rectangle(left, target.Bottom - height, width, height);
    }

    internal static void PositionInPrimaryWorkArea(Window window)
    {
        var workArea = SystemParameters.WorkArea;
        window.Width = Math.Min(window.Width, workArea.Width);
        window.Height = Math.Min(window.Height, workArea.Height);
        window.Left = Math.Max(workArea.Left, workArea.Right - window.Width - EdgeMargin);
        window.Top = Math.Max(workArea.Top, workArea.Bottom - window.Height - EdgeMargin);
    }

    internal static bool TryPositionWindow(IntPtr handle, Rectangle workArea)
    {
        // Moving to another monitor can synchronously change the HWND's DPI and size.
        // Fit it there first, then measure again before the final bottom-right placement.
        for (int pass = 0; pass < 2; pass++)
        {
            if (!GetWindowRect(handle, out var bounds))
            {
                Debug.WriteLine($"[WindowPlacement] GetWindowRect failed: {Marshal.GetLastWin32Error()}");
                return false;
            }

            var size = new Size(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
            var placement = CalculatePlacement(workArea, size);
            if (!SetWindowPos(handle, IntPtr.Zero, placement.X, placement.Y, placement.Width, placement.Height,
                SwpNoZOrder | SwpNoActivate))
            {
                Debug.WriteLine($"[WindowPlacement] SetWindowPos failed: {Marshal.GetLastWin32Error()}");
                return false;
            }
        }

        return true;
    }

    internal static Rectangle CalculatePlacement(Rectangle workArea, Size windowSize)
    {
        int width = Math.Min(windowSize.Width, workArea.Width);
        int height = Math.Min(windowSize.Height, workArea.Height);
        int left = Math.Max(workArea.Left, workArea.Right - width - EdgeMargin);
        int top = Math.Max(workArea.Top, workArea.Bottom - height - EdgeMargin);
        return new Rectangle(left, top, width, height);
    }

    internal static Rectangle CalculateBottomAnchoredPlacement(Rectangle workArea, Rectangle bounds, Size windowSize)
    {
        if (bounds.Bottom <= workArea.Top)
        {
            return CalculatePlacement(workArea, windowSize);
        }

        int width = Math.Min(windowSize.Width, workArea.Width);
        int bottom = Math.Min(bounds.Bottom, workArea.Bottom);
        int height = Math.Min(windowSize.Height, bottom - workArea.Top);
        int left = Math.Clamp(bounds.Left, workArea.Left, workArea.Right - width);
        return new Rectangle(left, bottom - height, width, height);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetDpiForWindow(IntPtr handle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRect bounds);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        /// <summary>The left edge in native screen coordinates.</summary>
        public int Left;

        /// <summary>The top edge in native screen coordinates.</summary>
        public int Top;

        /// <summary>The exclusive right edge in native screen coordinates.</summary>
        public int Right;

        /// <summary>The exclusive bottom edge in native screen coordinates.</summary>
        public int Bottom;
    }
}
