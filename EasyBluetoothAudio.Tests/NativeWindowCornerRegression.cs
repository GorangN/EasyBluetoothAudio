using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EasyBluetoothAudio.Tests;

internal static class NativeWindowCornerRegression
{
    internal static void Verify(Window window, bool rounded)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var region = CreateRectRgn(0, 0, 0, 0);
        Assert.NotEqual(IntPtr.Zero, region);
        try
        {
            VerifyRegion(handle, region, rounded);
            Assert.True(GetWindowRect(handle, out var initialBounds));
            var width = initialBounds.Right - initialBounds.Left;
            var height = initialBounds.Bottom - initialBounds.Top;
            Assert.True(SetWindowPos(handle, IntPtr.Zero, 0, 0, width, height + 300, 0x0016));
            VerifyRegion(handle, region, rounded);
            Assert.True(GetWindowRect(handle, out var expandedBounds));
            Assert.Equal(height + 300, expandedBounds.Bottom - expandedBounds.Top);
            Assert.True(SetWindowPos(handle, IntPtr.Zero, 0, 0, width, height, 0x0016));
            VerifyRegion(handle, region, rounded);
            Assert.True(GetWindowRect(handle, out var restoredBounds));
            Assert.Equal(height, restoredBounds.Bottom - restoredBounds.Top);
        }
        finally
        {
            DeleteObject(region);
        }
    }

    private static void VerifyRegion(IntPtr handle, IntPtr region, bool rounded)
    {
        var regionType = GetWindowRgn(handle, region);
        if (!rounded && regionType == 0)
        {
            return;
        }

        Assert.NotEqual(0, regionType);
        Assert.NotEqual(0, GetRgnBox(region, out var bounds));
        Assert.True(PtInRegion(region, bounds.Right / 2, bounds.Bottom / 2));
        Assert.True(PtInRegion(region, bounds.Right / 2, 0));
        Assert.Equal(!rounded, PtInRegion(region, 0, 0));
        Assert.Equal(!rounded, PtInRegion(region, bounds.Right - 1, 0));
        Assert.Equal(!rounded, PtInRegion(region, 0, bounds.Bottom - 1));
        Assert.Equal(!rounded, PtInRegion(region, bounds.Right - 1, bounds.Bottom - 1));
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("user32.dll")]
    private static extern int GetWindowRgn(IntPtr window, IntPtr region);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out RectBounds bounds);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("gdi32.dll")]
    private static extern int GetRgnBox(IntPtr region, out RectBounds bounds);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PtInRegion(IntPtr region, int x, int y);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr value);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectBounds
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }
}
