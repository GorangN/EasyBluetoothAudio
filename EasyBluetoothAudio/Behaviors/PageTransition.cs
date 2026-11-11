using System.Windows;
using System.Windows.Media;

namespace EasyBluetoothAudio.Behaviors;

/// <summary>Softens pointer-initiated page changes while keeping initial layout and keyboard navigation immediate.</summary>
public static class PageTransition
{
    /// <summary>Identifies whether this page participates in short visibility transitions.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(PageTransition), new PropertyMetadata(false, OnIsEnabledChanged));
    private static readonly DependencyProperty TranslationProperty = DependencyProperty.RegisterAttached(
        "Translation", typeof(TranslateTransform), typeof(PageTransition));

    /// <summary>Reads whether page transitions are enabled.</summary>
    /// <param name="element">The page to inspect.</param>
    /// <returns>Whether transitions are enabled.</returns>
    public static bool GetIsEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    /// <summary>Enables short, finite transitions for pointer-initiated navigation.</summary>
    /// <param name="element">The page to update.</param>
    /// <param name="value">Whether transitions should be enabled.</param>
    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if ((bool)args.NewValue)
        {
            element.IsVisibleChanged += OnVisibilityChanged;
            element.Unloaded += OnUnloaded;
        }
        else
        {
            element.IsVisibleChanged -= OnVisibilityChanged;
            element.Unloaded -= OnUnloaded;
            Reset(element);
        }
    }

    private static void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        var element = (FrameworkElement)sender;
        if (!(bool)args.NewValue || !MotionAnimation.CanAnimate(element))
        {
            Reset(element);
            return;
        }

        var translation = (TranslateTransform)MotionAnimation.GetTransform(element, TranslationProperty, scale: false);
        MotionAnimation.Animate(element, UIElement.OpacityProperty, 1, 180, from: .7);
        MotionAnimation.Animate(translation, TranslateTransform.YProperty, 0, 180, from: 6);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs args)
    {
        Reset((FrameworkElement)sender);
    }

    private static void Reset(FrameworkElement element)
    {
        MotionAnimation.Set(element, UIElement.OpacityProperty, 1);
        if (element.GetValue(TranslationProperty) is TranslateTransform translation)
        {
            MotionAnimation.Set(translation, TranslateTransform.YProperty, 0);
        }
    }
}
