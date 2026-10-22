using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using EasyBluetoothAudio.Services;
using Size = System.Drawing.Size;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Verifies native-coordinate placement across DPI sizes, monitor layouts, and limited work areas.
/// </summary>
[Collection("Window layout runtime")]
public class WindowPlacementServiceTests
{
    /// <summary>
    /// Ensures DPI-scaled window sizes remain inside the native working area with the expected edge gap.
    /// </summary>
    /// <param name="width">The native width of a 500-DIP window at the tested scale.</param>
    /// <param name="height">The native height of a 380-DIP window at the tested scale.</param>
    [Theory]
    [InlineData(500, 380)]
    [InlineData(625, 475)]
    [InlineData(750, 570)]
    [InlineData(1000, 760)]
    public void CalculatePlacement_ScaledWindow_FitsNativeWorkArea(int width, int height)
    {
        var workArea = new Rectangle(0, 0, 1920, 1040);

        var placement = WindowPlacementService.CalculatePlacement(workArea, new Size(width, height));

        Assert.Equal(new Rectangle(1910 - width, 1030 - height, width, height), placement);
        Assert.True(workArea.Contains(placement));
    }

    /// <summary>
    /// Ensures negative monitor origins and reserved taskbar space on any edge are respected.
    /// </summary>
    /// <param name="x">The monitor work area's left edge.</param>
    /// <param name="y">The monitor work area's top edge.</param>
    /// <param name="width">The monitor work area's width.</param>
    /// <param name="height">The monitor work area's height.</param>
    [Theory]
    [InlineData(-1920, 0, 1920, 1040)]
    [InlineData(-1920, -1080, 1920, 1040)]
    [InlineData(1920, 200, 2560, 1400)]
    [InlineData(40, 0, 1880, 1080)]
    [InlineData(0, 40, 1920, 1040)]
    [InlineData(0, 0, 1880, 1080)]
    [InlineData(0, 0, 1920, 1040)]
    public void CalculatePlacement_OffsetWorkArea_PreservesScreenCoordinates(int x, int y, int width, int height)
    {
        var workArea = new Rectangle(x, y, width, height);

        var placement = WindowPlacementService.CalculatePlacement(workArea, new Size(625, 475));

        Assert.Equal(workArea.Right - 10, placement.Right);
        Assert.Equal(workArea.Bottom - 10, placement.Bottom);
        Assert.True(workArea.Contains(placement));
    }

    /// <summary>
    /// Verifies that a window larger than the available desktop shrinks instead of remaining off-screen.
    /// </summary>
    [Fact]
    public void CalculatePlacement_OversizedWindow_FillsAvailableWorkArea()
    {
        var workArea = new Rectangle(-400, 50, 400, 260);

        var placement = WindowPlacementService.CalculatePlacement(workArea, new Size(1000, 760));

        Assert.Equal(workArea, placement);
    }

    /// <summary>
    /// Verifies that an edge gap is sacrificed when a window fits but leaves insufficient margin.
    /// </summary>
    [Fact]
    public void CalculatePlacement_NearlyFullWorkArea_PreservesWindowSize()
    {
        var workArea = new Rectangle(-500, -400, 505, 385);

        var placement = WindowPlacementService.CalculatePlacement(workArea, new Size(500, 380));

        Assert.Equal(new Rectangle(-500, -400, 500, 380), placement);
    }

    /// <summary>
    /// Ensures fitting one oversized dimension preserves the other dimension.
    /// </summary>
    [Fact]
    public void CalculatePlacement_OnlyHeightExceedsWorkArea_PreservesWidth()
    {
        var placement = WindowPlacementService.CalculatePlacement(new Rectangle(50, 20, 1000, 300), new Size(500, 380));

        Assert.Equal(new Rectangle(540, 20, 500, 300), placement);
    }

    /// <summary>
    /// Ensures a failed native window lookup can fall back without throwing from a tray callback.
    /// </summary>
    [Fact]
    public void TryPositionWindow_InvalidHandle_ReturnsFalse()
    {
        Assert.False(WindowPlacementService.TryPositionWindow(IntPtr.Zero, new Rectangle(0, 0, 1920, 1040)));
    }

    /// <summary>
    /// Verifies fallback placement repairs off-screen bounds on repeated opens without changing visibility.
    /// </summary>
    [Fact]
    public void PositionInPrimaryWorkArea_OffScreenWindow_FitsOnEveryCall()
    {
        StaTest.Run(() =>
        {
            var workArea = SystemParameters.WorkArea;
            var window = new Window
            {
                Width = workArea.Width + 100,
                Height = workArea.Height + 100,
                Left = workArea.Right + 500,
                Top = workArea.Bottom + 500
            };
            try
            {
                _ = new WindowInteropHelper(window).EnsureHandle();
                WindowPlacementService.PositionInPrimaryWorkArea(window);
                Assert.Equal(workArea, new Rect(window.Left, window.Top, window.Width, window.Height));

                window.Left = workArea.Left - 1000;
                window.Top = workArea.Top - 1000;
                WindowPlacementService.PositionInPrimaryWorkArea(window);
                Assert.Equal(workArea, new Rect(window.Left, window.Top, window.Width, window.Height));
                Assert.False(window.IsVisible);
                Assert.False(window.IsActive);
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// Exercises the real HWND path while leaving the test window hidden and inactive.
    /// </summary>
    [Fact]
    public void TryPositionWindow_HiddenWindow_FitsAndCanBePlacedAgain()
    {
        StaTest.Run(() =>
        {
            var window = new Window
            {
                Width = 500,
                Height = 380,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false
            };
            try
            {
                var handle = new WindowInteropHelper(window).EnsureHandle();
                var workArea = new Rectangle(0, 0, 800, 600);
                Assert.True(WindowPlacementService.TryPositionWindow(handle, workArea));
                Assert.True(GetWindowRect(handle, out var bounds));
                Assert.True(workArea.Contains(Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)));
                Assert.False(window.IsVisible);
                Assert.False(window.IsActive);

                var smallerWorkArea = new Rectangle(-300, -200, 300, 200);
                Assert.True(WindowPlacementService.TryPositionWindow(handle, smallerWorkArea));
                Assert.True(GetWindowRect(handle, out bounds));
                Assert.True(smallerWorkArea.Contains(Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)));
                Assert.False(window.IsVisible);
                Assert.False(window.IsActive);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRect bounds);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        /// <summary>The native left edge.</summary>
        public int Left;

        /// <summary>The native top edge.</summary>
        public int Top;

        /// <summary>The native right edge.</summary>
        public int Right;

        /// <summary>The native bottom edge.</summary>
        public int Bottom;
    }
}
