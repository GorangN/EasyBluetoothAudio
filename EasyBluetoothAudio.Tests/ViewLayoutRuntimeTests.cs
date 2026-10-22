using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shell;
using EasyBluetoothAudio.Views;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Checks the compiled views and scrollbar templates under constrained layout sizes.
/// </summary>
[Collection("Window layout runtime")]
public class ViewLayoutRuntimeTests
{
    /// <summary>
    /// Verifies both views preserve access to content and caption buttons when the window is resized.
    /// </summary>
    [Fact]
    public void Views_SmallViewport_ContentScrollsAndHeaderActionsRemainInteractive()
    {
        StaTest.Run(() =>
        {
            var app = new App();
            app.InitializeComponent();
            try
            {
                VerifyLayout(new BluetoothConfigView());
                VerifyLayout(new SettingsView());
            }
            finally
            {
                app.Shutdown();
            }
        });
    }

    private static void VerifyLayout(UserControl view)
    {
        view.Measure(new Size(280, 180));
        view.Arrange(new Rect(0, 0, 280, 180));
        view.UpdateLayout();

        var scrollViewer = Assert.Single(Descendants<ScrollViewer>(view),
            scroll => scroll.Style == view.FindResource("CustomScrollViewerStyle"));
        Assert.True(scrollViewer.ScrollableWidth > 0);
        Assert.True(scrollViewer.ScrollableHeight > 0);
        Assert.Equal(Visibility.Visible, scrollViewer.ComputedHorizontalScrollBarVisibility);
        Assert.Equal(Visibility.Visible, scrollViewer.ComputedVerticalScrollBarVisibility);

        var horizontalBar = Assert.IsType<ScrollBar>(scrollViewer.Template.FindName("PART_HorizontalScrollBar", scrollViewer));
        Assert.Equal(Orientation.Horizontal, horizontalBar.Orientation);
        Assert.True(horizontalBar.ActualWidth > 20);

        scrollViewer.ScrollToRightEnd();
        scrollViewer.ScrollToBottom();
        view.UpdateLayout();
        Assert.True(scrollViewer.HorizontalOffset > 0);
        Assert.True(scrollViewer.VerticalOffset > 0);

        var headerButtons = Descendants<Button>(view).Where(button => button.ToolTip != null).ToArray();
        Assert.NotEmpty(headerButtons);
        Assert.All(headerButtons, button => Assert.True(WindowChrome.GetIsHitTestVisibleInChrome(button)));

        view.Measure(new Size(460, 310));
        view.Arrange(new Rect(0, 0, 460, 310));
        view.UpdateLayout();
        Assert.Equal(0, scrollViewer.ScrollableWidth);
        Assert.Equal(Visibility.Collapsed, scrollViewer.ComputedHorizontalScrollBarVisibility);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in Descendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
