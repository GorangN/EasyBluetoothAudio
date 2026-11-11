using System.Windows;

namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Positions the tray window within the working area of the monitor containing the pointer.
/// </summary>
public interface IWindowPlacementService
{
    /// <summary>
    /// Fits and anchors a window to the monitor's bottom-right corner using native screen coordinates,
    /// falling back to the primary working area if Windows cannot position it.
    /// </summary>
    /// <param name="window">The restored window to position on its owning UI thread.</param>
    /// <exception cref="InvalidOperationException">The window is closed or accessed from a different thread.</exception>
    void PositionNearCursor(Window window);

    /// <summary>
    /// Expands the tray window upward for settings and restores its previous compact size when settings close.
    /// Preserves its current left and bottom edges when the monitor working area permits.
    /// </summary>
    /// <param name="window">The tray window to resize on its owning UI thread.</param>
    /// <param name="expanded">Whether settings need the expanded window height.</param>
    /// <param name="animate">Whether a visible window may animate over 240 milliseconds when Windows permits motion.
    /// Pass false for hiding, repositioning, and other immediate lifecycle changes.</param>
    /// <exception cref="InvalidOperationException">The window is closed or accessed from a different thread.</exception>
    void SetSettingsExpanded(Window window, bool expanded, bool animate = true);
}
