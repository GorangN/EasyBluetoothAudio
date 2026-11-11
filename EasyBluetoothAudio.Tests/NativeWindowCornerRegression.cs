using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EasyBluetoothAudio.Tests;

internal static class NativeWindowCornerRegression
{
    internal static void Verify(Window window)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var region = CreateRectRgn(0, 0, 0, 0);
        Assert.NotEqual(IntPtr.Zero, region);
        try
        {
            VerifyRegion(handle, region);
            GetRgnBox(region, out var initialBounds);
            Assert.True(SetWindowPos(handle, IntPtr.Zero, 0, 0, initialBounds.Right, initialBounds.Bottom + 300, 0x0016));
            VerifyRegion(handle, region);
            GetRgnBox(region, out var expandedBounds);
            Assert.Equal(initialBounds.Bottom + 300, expandedBounds.Bottom);
            Assert.True(SetWindowPos(handle, IntPtr.Zero, 0, 0, initialBounds.Right, initialBounds.Bottom, 0x0016));
            VerifyRegion(handle, region);
        }
        finally
        {
            DeleteObject(region);
        }
    }

    private static void VerifyRegion(IntPtr handle, IntPtr region)
    {
        Assert.NotEqual(0, GetWindowRgn(handle, region));
        Assert.NotEqual(0, GetRgnBox(region, out var bounds));
        Assert.True(PtInRegion(region, bounds.Right / 2, bounds.Bottom / 2));
        Assert.True(PtInRegion(region, bounds.Right / 2, 0));
        Assert.False(PtInRegion(region, 0, 0));
        Assert.False(PtInRegion(region, bounds.Right - 1, 0));
        Assert.False(PtInRegion(region, 0, bounds.Bottom - 1));
        Assert.False(PtInRegion(region, bounds.Right - 1, bounds.Bottom - 1));
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("user32.dll")]
    private static extern int GetWindowRgn(IntPtr window, IntPtr region);

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
