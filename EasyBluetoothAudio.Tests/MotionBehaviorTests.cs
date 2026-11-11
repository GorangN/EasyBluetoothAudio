using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using EasyBluetoothAudio.Behaviors;

namespace EasyBluetoothAudio.Tests;

/// <summary>Verifies motion never hides initial controls, intercepts input, or retains completed clocks.</summary>
[Collection("Window layout runtime")]
public class MotionBehaviorTests
{
    /// <summary>Initial and unmounted action changes settle immediately with usable input and nonzero transforms.</summary>
    [Fact]
    public void ActionTransition_InitialStateIsReadableAndOutgoingControlCannotReceiveInput()
    {
        StaTest.Run(() =>
        {
            var button = new Button();
            ActionTransition.SetIsShown(button, true);
            Assert.Equal(Visibility.Visible, button.Visibility);
            Assert.Equal(1, button.Opacity);
            Assert.True(button.IsHitTestVisible);
            Assert.True(button.IsTabStop);
            Assert.False(button.HasAnimatedProperties);

            ActionTransition.SetIsShown(button, false);
            Assert.Equal(Visibility.Collapsed, button.Visibility);
            Assert.False(button.IsHitTestVisible);
            Assert.False(button.IsTabStop);
            var group = Assert.IsType<TransformGroup>(button.RenderTransform);
            Assert.InRange(Assert.IsType<ScaleTransform>(Assert.Single(group.Children)).ScaleX, .95, 1);
        });
    }

    /// <summary>Enabling page animation does not make offscreen previews or first layouts transparent.</summary>
    [Fact]
    public void PageTransition_InitialAndUnmountedStatesStayOpaque()
    {
        StaTest.Run(() =>
        {
            var page = new Grid();
            PageTransition.SetIsEnabled(page, true);
            page.Visibility = Visibility.Collapsed;
            page.Visibility = Visibility.Visible;
            Assert.Equal(1, page.Opacity);
            Assert.False(page.HasAnimatedProperties);
            PageTransition.SetIsEnabled(page, false);
            Assert.Equal(1, page.Opacity);
        });
    }

    /// <summary>Unmounted buttons ignore synthetic pointer feedback and retain their existing visual transform.</summary>
    [Fact]
    public void ButtonFeedback_DoesNotAnimateBeforeTheControlIsLoaded()
    {
        StaTest.Run(() =>
        {
            var original = new TranslateTransform(3, 4);
            var button = new Button { RenderTransform = original };
            ButtonFeedback.SetIsEnabled(button, true);
            button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent
            });
            Assert.Same(original, button.RenderTransform);
            Assert.False(original.HasAnimatedProperties);
            ButtonFeedback.SetIsEnabled(button, false);
            Assert.Same(original, button.RenderTransform);
        });
    }

    /// <summary>Rapid reversal uses the current value and removes completed clocks instead of retaining visual overrides.</summary>
    [Fact]
    public void Animation_InterruptedFeedbackReturnsToRestWithoutRetainedClock()
    {
        StaTest.Run(() =>
        {
            var scale = new ScaleTransform(1, 1);
            var group = new TransformGroup();
            group.Children.Add(scale);
            var host = new Window
            {
                Width = 1, Height = 1, Left = -10000, Top = -10000,
                ShowInTaskbar = false, ShowActivated = false, WindowStyle = WindowStyle.None,
                Content = new Border { Background = Brushes.Transparent, RenderTransform = group }
            };
            try
            {
                host.Show();
                host.UpdateLayout();
                MotionAnimation.Animate(scale, ScaleTransform.ScaleXProperty, .98, 100);
                MotionAnimation.Animate(scale, ScaleTransform.ScaleXProperty, 1, 120);
                var frame = new DispatcherFrame();
                var elapsed = Stopwatch.StartNew();
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
                timer.Tick += (_, _) =>
                {
                    if (!scale.HasAnimatedProperties || elapsed.Elapsed >= TimeSpan.FromSeconds(2))
                    {
                        timer.Stop();
                        frame.Continue = false;
                    }
                };
                timer.Start();
                Dispatcher.PushFrame(frame);
                Assert.Equal(1, scale.ScaleX);
                Assert.False(scale.HasAnimatedProperties, "The rendered feedback clock did not clear within two seconds.");
            }
            finally
            {
                host.Close();
            }
        });
    }
}
