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
}
