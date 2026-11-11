using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace EasyBluetoothAudio.Behaviors;

/// <summary>
/// Moves keyboard focus between the settings page and its invoking header button.
/// </summary>
public static class SettingsFocus
{
    /// <summary>Identifies whether this window participates in settings focus restoration.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(SettingsFocus), new PropertyMetadata(false));

    /// <summary>Identifies the visible settings-page state.</summary>
    public static readonly DependencyProperty IsSettingsOpenProperty = DependencyProperty.RegisterAttached(
        "IsSettingsOpen", typeof(bool), typeof(SettingsFocus), new PropertyMetadata(false, OnIsSettingsOpenChanged));

    /// <summary>Reads whether settings focus management is enabled.</summary>
    /// <param name="element">The host window.</param>
    /// <returns>Whether focus should follow settings navigation.</returns>
    public static bool GetIsEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    /// <summary>Enables settings focus management.</summary>
    /// <param name="element">The host window.</param>
    /// <param name="value">Whether to manage settings focus.</param>
    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    /// <summary>Reads whether settings are open.</summary>
    /// <param name="element">The host window.</param>
    /// <returns>Whether settings are visible.</returns>
    public static bool GetIsSettingsOpen(DependencyObject element)
    {
        return (bool)element.GetValue(IsSettingsOpenProperty);
    }

    /// <summary>Updates focus when the visible settings-page state changes.</summary>
    /// <param name="element">The host window.</param>
    /// <param name="value">Whether settings are visible.</param>
    public static void SetIsSettingsOpen(DependencyObject element, bool value)
    {
        element.SetValue(IsSettingsOpenProperty, value);
    }

    private static void OnIsSettingsOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not Window window || !GetIsEnabled(window))
        {
            return;
        }

        _ = window.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (!window.IsVisible)
            {
                return;
            }

            if (GetIsSettingsOpen(window) && window.FindName("SettingsPage") is UserControl settings)
            {
                settings.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
            else if (window.FindName("SettingsButton") is Button button)
            {
                button.Focus();
            }
        });
    }
}
