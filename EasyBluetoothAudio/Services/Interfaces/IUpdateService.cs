namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Holds information about an available update retrieved from GitHub Releases.
/// </summary>
/// <param name="TagName">The raw git tag name (e.g. "v1.2.3").</param>
/// <param name="Version">The parsed version string without the leading "v" (e.g. "1.2.3").</param>
/// <param name="InstallerUrl">The direct download URL for the installer .exe asset.</param>
/// <param name="ReleaseNotes">The release body / changelog text.</param>
public record UpdateInfo(string TagName, string Version, string InstallerUrl, string ReleaseNotes);

/// <summary>
/// Defines the contract for checking and applying application updates from GitHub Releases.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Queries the GitHub Releases API and returns update information if a newer version is
    /// available, or <see langword="null"/> if the app is already up to date or the check fails.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default);

    /// <summary>
    /// Downloads the installer from <paramref name="info"/> to a temporary path, launches it
    /// silently, and shuts down the current application.
    /// </summary>
    /// <param name="info">The update information returned by <see cref="CheckForUpdateAsync"/>.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task DownloadAndInstallAsync(UpdateInfo info, CancellationToken ct = default);
}
