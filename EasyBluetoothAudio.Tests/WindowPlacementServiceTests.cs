using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
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

    /// <summary>Native expansion keeps the existing left and bottom edges at common monitor scales.</summary>
    /// <param name="width">The compact native width at the tested scale.</param>
    /// <param name="height">The compact native height at the tested scale.</param>
    /// <param name="expandedHeight">The native height corresponding to 800 DIPs.</param>
    [Theory]
    [InlineData(386, 322, 800)]
    [InlineData(483, 403, 1000)]
    [InlineData(579, 483, 1200)]
    [InlineData(772, 644, 1600)]
    public void CalculateBottomAnchoredPlacement_ExpandedSize_PreservesNativeAnchor(int width, int height, int expandedHeight)
    {
        var workArea = new Rectangle(-3000, -1800, 2800, 2400);
        var compact = new Rectangle(-1200, 400 - height, width, height);

        var expanded = WindowPlacementService.CalculateBottomAnchoredPlacement(workArea, compact, new Size(width, expandedHeight));

        Assert.Equal(compact.Left, expanded.Left);
        Assert.Equal(compact.Bottom, expanded.Bottom);
        Assert.Equal(expandedHeight, expanded.Height);
        Assert.True(workArea.Contains(expanded));
    }

    /// <summary>Expansion uses only the space above the current bottom and respects reserved desktop edges.</summary>
    [Fact]
    public void CalculateBottomAnchoredPlacement_LimitedWorkArea_CapsGrowthAboveExistingBottom()
    {
        var workArea = new Rectangle(40, -300, 960, 700);
        var compact = new Rectangle(600, 100, 386, 250);

        var expanded = WindowPlacementService.CalculateBottomAnchoredPlacement(workArea, compact, new Size(386, 1000));

        Assert.Equal(new Rectangle(600, -300, 386, 650), expanded);
    }

    /// <summary>Restoring a compact size follows a moved anchor while fitting its width inside a smaller work area.</summary>
    [Fact]
    public void CalculateBottomAnchoredPlacement_RestoreOnSmallerMonitor_FitsWithoutMovingBottom()
    {
        var workArea = new Rectangle(-500, 40, 400, 600);
        var moved = new Rectangle(-450, 80, 600, 500);

        var restored = WindowPlacementService.CalculateBottomAnchoredPlacement(workArea, moved, new Size(483, 403));

        Assert.Equal(new Rectangle(-500, 177, 400, 403), restored);
        Assert.Equal(moved.Bottom, restored.Bottom);
    }

    /// <summary>Every rounded animation frame shares one exact native bottom edge and reaches both endpoints.</summary>
    /// <param name="progress">Elapsed animation progress before easing.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(.1)]
    [InlineData(.25)]
    [InlineData(.5)]
    [InlineData(.75)]
    [InlineData(1)]
    public void InterpolateBottomAnchoredPlacement_KeepsExactBottom(double progress)
    {
        var compact = new Rectangle(-900, -170, 483, 403);
        var expanded = new Rectangle(-900, -767, 483, 1000);

        var opening = WindowPlacementService.InterpolateBottomAnchoredPlacement(compact, expanded, progress);
        var closing = WindowPlacementService.InterpolateBottomAnchoredPlacement(expanded, compact, progress);

        Assert.Equal(233, opening.Bottom);
        Assert.Equal(233, closing.Bottom);
        Assert.InRange(opening.Height, compact.Height, expanded.Height);
        Assert.InRange(closing.Height, compact.Height, expanded.Height);
        if (progress == 0)
        {
            Assert.Equal(compact, opening);
            Assert.Equal(expanded, closing);
        }
        else if (progress == 1)
        {
            Assert.Equal(expanded, opening);
            Assert.Equal(compact, closing);
        }
        else if (progress == .5)
        {
            Assert.True(opening.Height > (compact.Height + expanded.Height) / 2);
            Assert.True(closing.Height < (compact.Height + expanded.Height) / 2);
        }
    }

    /// <summary>Visible native animation holds its bottom edge through interruptions, then releases frame handlers on hide and close.</summary>
    [Fact]
    public void SetSettingsExpanded_VisibleWindow_AnimatesReversesAndCleansUp()
    {
        StaTest.Run(() =>
        {
            var window = new Window
            {
                Width = 386, Height = 322, WindowStyle = WindowStyle.None,
                ShowInTaskbar = false, ShowActivated = false
            };
            var service = new WindowPlacementService();
            try
            {
                service.PositionNearCursor(window);
                window.Show();
                var handle = new WindowInteropHelper(window).Handle;
                Assert.True(GetWindowRect(handle, out var compact));
                service.SetSettingsExpanded(window, true, animate: false);
                Assert.True(GetWindowRect(handle, out var expanded));
                service.SetSettingsExpanded(window, false, animate: false);
                var observedHeights = new List<int>();
                void Observe()
                {
                    Assert.True(GetWindowRect(handle, out var frame));
                    Assert.Equal(compact.Bottom, frame.Bottom);
                    observedHeights.Add(frame.Bottom - frame.Top);
                }

                service.SetSettingsExpanded(window, true);
                PumpDispatcher(90, Observe);
                service.SetSettingsExpanded(window, false);
                PumpDispatcher(50, Observe);
                service.SetSettingsExpanded(window, true);
                PumpDispatcher(330, Observe);
                Assert.True(GetWindowRect(handle, out var completed));
                Assert.Equal(expanded, completed);
                Assert.False(service.IsSettingsResizeAnimating);
                if (SystemParameters.ClientAreaAnimation && expanded.Top < compact.Top)
                {
                    Assert.Contains(observedHeights, height => height > compact.Bottom - compact.Top && height < expanded.Bottom - expanded.Top);
                }

                service.SetSettingsExpanded(window, false, animate: false);
                Assert.True(GetWindowRect(handle, out completed));
                Assert.Equal(compact, completed);
                service.SetSettingsExpanded(window, true);
                window.Hide();
                Assert.False(service.IsSettingsResizeAnimating);
                service.SetSettingsExpanded(window, false);
                Assert.True(GetWindowRect(handle, out completed));
                Assert.Equal(compact, completed);
                window.Show();
                service.SetSettingsExpanded(window, true);
                window.Close();
                Assert.False(service.IsSettingsResizeAnimating);
                PumpDispatcher(40, () => { });
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static void PumpDispatcher(int milliseconds, Action observe)
    {
        var frame = new DispatcherFrame();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) =>
        {
            observe();
            if (clock.ElapsedMilliseconds >= milliseconds)
            {
                timer.Stop();
                frame.Continue = false;
            }
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    /// <summary>Settings expansion is reversible and repeated opens cannot overwrite the user's compact size.</summary>
    [Fact]
    public void SetSettingsExpanded_HiddenWindow_RestoresCompactAndManualGeometry()
    {
        StaTest.Run(() =>
        {
            var window = new Window
            {
                Width = 386, Height = 322, WindowStyle = WindowStyle.None,
                ShowInTaskbar = false, ShowActivated = false
            };
            var service = new WindowPlacementService();
            try
            {
                service.PositionNearCursor(window);
                var handle = new WindowInteropHelper(window).Handle;
                Assert.True(GetWindowRect(handle, out var original));
                service.SetSettingsExpanded(window, false);
                service.SetSettingsExpanded(window, true);
                Assert.True(GetWindowRect(handle, out var expanded));
                Assert.Equal(original.Left, expanded.Left);
                Assert.Equal(original.Bottom, expanded.Bottom);
                Assert.True(expanded.Bottom - expanded.Top >= original.Bottom - original.Top);
                service.SetSettingsExpanded(window, true);
                Assert.True(GetWindowRect(handle, out var repeated));
                Assert.Equal(expanded, repeated);
                service.SetSettingsExpanded(window, false);
                Assert.True(GetWindowRect(handle, out var restored));
                Assert.Equal(original, restored);

                window.Width = 420;
                window.Height = 460;
                service.PositionNearCursor(window);
                Assert.True(GetWindowRect(handle, out var manual));
                service.SetSettingsExpanded(window, true);
                Assert.True(GetWindowRect(handle, out expanded));
                var workArea = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;
                int movedLeft = Math.Max(workArea.Left, expanded.Left - 20);
                int movedTop = Math.Max(workArea.Top, expanded.Top - 20);
                Assert.True(SetWindowPos(handle, IntPtr.Zero, movedLeft, movedTop, 0, 0, 0x0001 | 0x0004 | 0x0010));
                Assert.True(GetWindowRect(handle, out var moved));
                service.SetSettingsExpanded(window, false);
                Assert.True(GetWindowRect(handle, out restored));
                Assert.Equal(manual.Right - manual.Left, restored.Right - restored.Left);
                Assert.Equal(manual.Bottom - manual.Top, restored.Bottom - restored.Top);
                Assert.Equal(moved.Left, restored.Left);
                Assert.Equal(moved.Bottom, restored.Bottom);
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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

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
