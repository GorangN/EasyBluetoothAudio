using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// ViewModel responsible for checking and installing application updates.
/// </summary>
/// <param name="updateService">The service for checking and installing updates.</param>
public partial class UpdateViewModel(IUpdateService updateService) : ObservableObject
{
    private UpdateInfo? _latestUpdate;

    /// <summary>
    /// Gets the application version string derived from assembly metadata.
    /// </summary>
    public string AppVersion { get; } = ResolveAppVersion();

    /// <summary>
    /// Gets a value indicating whether an update is available for download.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallUpdateCommand))]
    private bool _updateAvailable;

    /// <summary>
    /// Gets a value indicating whether the application is currently checking for updates.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallUpdateCommand))]
    private bool _isCheckingForUpdate;

    /// <summary>
    /// Event raised when the update status changes to alert the main UI.
    /// </summary>
    public event Action<string>? StatusTextChanged;

    /// <summary>
    /// Check for updates and update the UI if one is found.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CheckForUpdateAsync()
    {
        if (IsCheckingForUpdate)
        {
            return;
        }

        try
        {
            IsCheckingForUpdate = true;
            var update = await updateService.CheckForUpdateAsync();

            if (update is not null)
            {
                _latestUpdate = update;
                UpdateAvailable = true;
            }
        }
        catch (Exception ex) when (ex.Message == "Rate limit exceeded")
        {
            Debug.WriteLine("[CheckForUpdate] Rate limit exceeded");
            StatusTextChanged?.Invoke("RATE LIMIT EXCEEDED");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CheckForUpdate] Error: {ex.Message}");
        }
        finally
        {
            IsCheckingForUpdate = false;
        }
    }

    /// <summary>
    /// Downloads and silently installs the latest release, then shuts down the app.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [CommunityToolkit.Mvvm.Input.RelayCommand(CanExecute = nameof(CanInstallUpdate))]
    public async Task InstallUpdateAsync()
    {
        if (_latestUpdate is null)
        {
            return;
        }

        try
        {
            StatusTextChanged?.Invoke($"DOWNLOADING UPDATE {_latestUpdate.TagName}...");
            await updateService.DownloadAndInstallAsync(_latestUpdate);
        }
        catch (Exception ex)
        {
            StatusTextChanged?.Invoke($"UPDATE FAILED: {ex.Message}");
            Debug.WriteLine($"[InstallUpdate] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines whether the install-update command can execute.
    /// </summary>
    /// <returns><see langword="true"/> if an update is available and not currently checking; otherwise <see langword="false"/>.</returns>
    private bool CanInstallUpdate()
    {
        return UpdateAvailable && _latestUpdate is not null && !IsCheckingForUpdate;
    }

    /// <summary>
    /// Resolves the application version string from assembly metadata.
    /// </summary>
    /// <returns>The formatted version string (e.g. "v.1.2.3").</returns>
    private static string ResolveAppVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var gitVersion = assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(gitVersion))
        {
            var version = assembly?.GetName().Version;
            return version != null ? $"v.{version.Major}.{version.Minor}.{version.Build}" : "v.?.?.?";
        }

        var parts = gitVersion.Split('+');
        return "v." + parts[0];
    }
}
