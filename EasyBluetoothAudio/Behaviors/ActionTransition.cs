using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Control = System.Windows.Controls.Control;

namespace EasyBluetoothAudio.Behaviors;

/// <summary>
/// Coordinates the finite source-action split while removing outgoing buttons from keyboard and pointer input.
/// </summary>
public static class ActionTransition
{
    /// <summary>
    /// Identifies whether the action is currently offered by its source ViewModel.
    /// </summary>
    public static readonly DependencyProperty IsShownProperty = DependencyProperty.RegisterAttached(
        "IsShown", typeof(bool), typeof(ActionTransition), new PropertyMetadata(false, OnIsShownChanged));
    private static readonly DependencyProperty ScaleProperty = DependencyProperty.RegisterAttached(
        "Scale", typeof(ScaleTransform), typeof(ActionTransition));

    /// <summary>
    /// Reads whether an action is currently offered.
    /// </summary>
    /// <param name="element">The action control.</param>
    /// <returns>Whether the control should be shown.</returns>
    public static bool GetIsShown(DependencyObject element)
    {
        return (bool)element.GetValue(IsShownProperty);
    }

    /// <summary>
    /// Updates whether an action is offered and animates its visual transition when Windows permits animation.
    /// </summary>
    /// <param name="element">The action control.</param>
    /// <param name="value">Whether the control should be shown.</param>
    public static void SetIsShown(DependencyObject element, bool value)
    {
        element.SetValue(IsShownProperty, value);
    }

    private static void OnIsShownChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        bool isShown = (bool)args.NewValue;
        element.IsHitTestVisible = isShown;
        if (element is Control control)
        {
            control.IsTabStop = isShown;
        }

        bool hadFocus = !isShown && element.IsKeyboardFocusWithin;
        var scale = (ScaleTransform)MotionAnimation.GetTransform(element, ScaleProperty, scale: true);
        element.Unloaded -= OnUnloaded;
        element.Unloaded += OnUnloaded;
        if (!MotionAnimation.CanAnimate(element))
        {
            ApplyFinalState(element, scale, isShown);
        }
        else
        {
            var wasCollapsed = element.Visibility == Visibility.Collapsed;
            element.Visibility = Visibility.Visible;
            MotionAnimation.Animate(scale, ScaleTransform.ScaleXProperty, isShown ? 1 : .96, 180,
                from: wasCollapsed ? .96 : null);
            MotionAnimation.Animate(element, UIElement.OpacityProperty, isShown ? 1 : 0, 180,
                from: wasCollapsed ? 0 : null, completed: () =>
            {
                if (!GetIsShown(element))
                {
                    element.Visibility = Visibility.Collapsed;
                }
            });
        }

        if (hadFocus)
        {
            _ = element.Dispatcher.BeginInvoke(() => element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
        }
    }

    private static void OnUnloaded(object sender, RoutedEventArgs args)
    {
        var element = (FrameworkElement)sender;
        if (element.GetValue(ScaleProperty) is ScaleTransform scale)
        {
            ApplyFinalState(element, scale, GetIsShown(element));
        }
    }

    private static void ApplyFinalState(FrameworkElement element, ScaleTransform scale, bool isShown)
    {
        MotionAnimation.Set(element, UIElement.OpacityProperty, isShown ? 1 : 0);
        MotionAnimation.Set(scale, ScaleTransform.ScaleXProperty, isShown ? 1 : .96);
        element.Visibility = isShown ? Visibility.Visible : Visibility.Collapsed;
    }
}
