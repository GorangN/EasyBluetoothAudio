using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using EasyBluetoothAudio.Services.Interfaces;
using Size = System.Drawing.Size;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Keeps monitor bounds, window measurements, and placement in the same native coordinate space.
/// </summary>
public sealed class WindowPlacementService : IWindowPlacementService
{
    private const int EdgeMargin = 10;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    /// <inheritdoc />
    public void PositionNearCursor(Window window)
    {
        var workArea = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
        var handle = new WindowInteropHelper(window).EnsureHandle();
        if (!TryPositionWindow(handle, workArea))
        {
            PositionInPrimaryWorkArea(window);
        }
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
