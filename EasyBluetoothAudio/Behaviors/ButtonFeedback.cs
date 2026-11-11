using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;

namespace EasyBluetoothAudio.Behaviors;

/// <summary>Provides interruptible mouse-press feedback without changing control layout or keyboard response.</summary>
public static class ButtonFeedback
{
    /// <summary>Identifies whether subtle pointer feedback is enabled for a button.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(ButtonFeedback), new PropertyMetadata(false, OnIsEnabledChanged));
    private static readonly DependencyProperty ScaleProperty = DependencyProperty.RegisterAttached(
        "Scale", typeof(ScaleTransform), typeof(ButtonFeedback));

    /// <summary>Reads whether mouse feedback is enabled.</summary>
    /// <param name="element">The button to inspect.</param>
    /// <returns>Whether mouse feedback is enabled.</returns>
    public static bool GetIsEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    /// <summary>Enables or disables mouse feedback without affecting keyboard activation.</summary>
    /// <param name="element">The button to update.</param>
    /// <param name="value">Whether pointer feedback should be enabled.</param>
    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not Button button)
        {
            return;
        }

        if ((bool)args.NewValue)
        {
            button.PreviewMouseLeftButtonDown += OnPressed;
            button.PreviewMouseLeftButtonUp += OnReleased;
            button.MouseLeave += OnReleased;
            button.LostMouseCapture += OnReleased;
            button.Unloaded += OnUnloaded;
            button.IsEnabledChanged += OnButtonEnabledChanged;
        }
        else
        {
            button.PreviewMouseLeftButtonDown -= OnPressed;
            button.PreviewMouseLeftButtonUp -= OnReleased;
            button.MouseLeave -= OnReleased;
            button.LostMouseCapture -= OnReleased;
            button.Unloaded -= OnUnloaded;
            button.IsEnabledChanged -= OnButtonEnabledChanged;
            Reset(button);
        }
    }

    private static void OnPressed(object sender, MouseButtonEventArgs args)
    {
        if (sender is Button button && button.IsEnabled && MotionAnimation.CanAnimate(button))
        {
            var scale = (ScaleTransform)MotionAnimation.GetTransform(button, ScaleProperty, scale: true);
            MotionAnimation.Animate(scale, ScaleTransform.ScaleXProperty, .98, 100);
            MotionAnimation.Animate(scale, ScaleTransform.ScaleYProperty, .98, 100);
        }
    }

    private static void OnReleased(object sender, RoutedEventArgs args)
    {
        if (sender is Button button && button.GetValue(ScaleProperty) is ScaleTransform scale)
        {
            if (MotionAnimation.CanAnimate(button))
            {
                MotionAnimation.Animate(scale, ScaleTransform.ScaleXProperty, 1, 120);
                MotionAnimation.Animate(scale, ScaleTransform.ScaleYProperty, 1, 120);
            }
            else
            {
                Reset(button);
            }
        }
    }

    private static void OnUnloaded(object sender, RoutedEventArgs args)
    {
        Reset((Button)sender);
    }

    private static void OnButtonEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (!(bool)args.NewValue)
        {
            Reset((Button)sender);
        }
    }

    private static void Reset(Button button)
    {
        if (button.GetValue(ScaleProperty) is ScaleTransform scale)
        {
            MotionAnimation.Set(scale, ScaleTransform.ScaleXProperty, 1);
            MotionAnimation.Set(scale, ScaleTransform.ScaleYProperty, 1);
        }
    }
}
