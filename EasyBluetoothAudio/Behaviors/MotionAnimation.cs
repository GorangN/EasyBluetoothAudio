using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace EasyBluetoothAudio.Behaviors;

internal static class MotionAnimation
{
    private static readonly ConditionalWeakTable<DependencyObject, Dictionary<DependencyProperty, DispatcherTimer>> Active = new();
    internal static KeySpline EaseOut { get; } = CreateEaseOut();

    private static KeySpline CreateEaseOut()
    {
        var spline = new KeySpline(.23, 1, .32, 1);
        spline.Freeze();
        return spline;
    }

    internal static bool CanAnimate(FrameworkElement element)
    {
        return element.IsLoaded && SystemParameters.ClientAreaAnimation
            && InputManager.Current.MostRecentInputDevice is MouseDevice;
    }

    internal static Transform GetTransform(FrameworkElement element, DependencyProperty storage, bool scale)
    {
        if (element.GetValue(storage) is Transform existing)
        {
            return existing;
        }

        var group = new TransformGroup();
        if (element.RenderTransform != Transform.Identity)
        {
            group.Children.Add(element.RenderTransform);
        }

        Transform transform = scale ? new ScaleTransform(1, 1) : new TranslateTransform();
        group.Children.Add(transform);
        element.RenderTransform = group;
        element.SetValue(storage, transform);
        return transform;
    }

    internal static void Set(DependencyObject target, DependencyProperty property, double value)
    {
        Cancel(target, property);
        target.SetValue(property, value);
    }

    internal static void Animate(DependencyObject target, DependencyProperty property,
        double to, int milliseconds, double? from = null, Action? completed = null)
    {
        var start = from ?? (double)target.GetValue(property);
        Cancel(target, property);
        target.SetValue(property, to);
        var duration = TimeSpan.FromMilliseconds(milliseconds);
        var animation = new DoubleAnimationUsingKeyFrames { Duration = duration, FillBehavior = FillBehavior.Stop };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(start, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(duration), EaseOut));
        var timer = new DispatcherTimer(DispatcherPriority.Render, target.Dispatcher) { Interval = duration };
        Active.GetOrCreateValue(target)[property] = timer;
        void Finish()
        {
            var active = Active.GetOrCreateValue(target);
            if (active.TryGetValue(property, out var current) && ReferenceEquals(current, timer))
            {
                active.Remove(property);
                timer.Stop();
                Begin(target, property, null);
                completed?.Invoke();
            }
        }
        animation.Completed += (_, _) => Finish();
        timer.Tick += (_, _) => Finish();
        Begin(target, property, animation);
        // An idle compositor can defer Completed; finite interaction feedback must still settle.
        timer.Start();
    }

    private static void Cancel(DependencyObject target, DependencyProperty property)
    {
        if (Active.GetOrCreateValue(target).Remove(property, out var timer))
        {
            timer.Stop();
        }
        Begin(target, property, null);
    }

    private static void Begin(DependencyObject target, DependencyProperty property, AnimationTimeline? animation)
    {
        if (target is UIElement element)
        {
            element.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
        }
        else if (target is Animatable animatable)
        {
            animatable.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
        }
    }
}
