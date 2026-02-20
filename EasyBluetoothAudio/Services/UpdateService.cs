using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Checks for application updates via the GitHub Releases API and applies them by
/// downloading and silently launching the new installer.
/// </summary>
public class UpdateService : IUpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/GorangN/EasyBluetoothAudio/releases/latest";
    private const string UserAgent = "EasyBluetoothAudio-Updater";

    private readonly HttpClient _http;

    /// <summary>
    /// Initialises a new instance of <see cref="UpdateService"/> with the provided <see cref="HttpClient"/>.
    /// </summary>
    public UpdateService(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    /// <inheritdoc />
    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            var release = await _http.GetFromJsonAsync<GitHubRelease>(ApiUrl, ct);
            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return null;
            }

            var remoteVersion = ParseVersion(release.TagName);
            if (remoteVersion is null)
            {
                return null;
            }

            var localVersion = GetLocalVersion();

            if (remoteVersion <= localVersion)
            {
                return null;
            }

            // Find the first .exe asset in the release
            var asset = Array.Find(release.Assets ?? [], a =>
                a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true);

            if (asset?.BrowserDownloadUrl is null)
            {
                return null;
            }

            return new UpdateInfo(
                TagName: release.TagName,
                Version: remoteVersion.ToString(),
                InstallerUrl: asset.BrowserDownloadUrl,
                ReleaseNotes: release.Body ?? string.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] CheckForUpdate failed: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DownloadAndInstallAsync(UpdateInfo info, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"EasyBluetoothAudioSetup_{info.Version}.exe");

        try
        {
            // Download the installer to a temp file
            using var response = await _http.GetAsync(info.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Download failed: {ex.Message}");
            throw;
        }

        // Launch the installer silently. /CLOSEAPPLICATIONS tells Inno to close any running
        // instances of the app before copying files. /NORESTART suppresses any reboot prompt.
        var psi = new ProcessStartInfo(tempPath)
        {
            Arguments = "/VERYSILENT /CLOSEAPPLICATIONS /NORESTART",
            UseShellExecute = true,
            // Run elevated so the installer can write to Program Files if needed
            Verb = "runas"
        };

        Process.Start(psi);

        // Shut down immediately so our files are not locked when Inno tries to overwrite them.
        System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Version? ParseVersion(string tag)
    {
        // Strip leading 'v' or 'V' (e.g. "v1.2.3" → "1.2.3")
        var clean = tag.TrimStart('v', 'V');

        // Strip any pre-release suffix (e.g. "1.2.3-alpha.1" → "1.2.3")
        var dashIndex = clean.IndexOf('-');
        if (dashIndex >= 0)
        {
            clean = clean[..dashIndex];
        }

        return Version.TryParse(clean, out var v) ? v : null;
    }

    private static Version GetLocalVersion()
    {
        var assembly = Assembly.GetEntryAssembly();

        // MinVer sets the InformationalVersion to the full semver string (e.g. "1.2.3+abc1234")
        var infoVersion = assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(infoVersion))
        {
            return ParseVersion(infoVersion.Split('+')[0]) ?? new Version(0, 0, 0);
        }

        var version = assembly?.GetName().Version;
        // Fall back to 0.0.0 when no assembly version is available (e.g. in test runners)
        return version is null ? new Version(0, 0, 0) : new Version(version.Major, version.Minor, version.Build);
    }

    // ── Private DTOs for JSON deserialization ─────────────────────────────────

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("body")]
        public string? Body { get; init; }

        [JsonPropertyName("assets")]
        public GitHubAsset[]? Assets { get; init; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; init; }
    }
}
