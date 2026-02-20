using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Core;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// ViewModel responsible for checking and installing application updates.
/// </summary>
public class UpdateViewModel : ViewModelBase
{
    private readonly IUpdateService _updateService;
    private bool _isCheckingForUpdate;
    private bool _updateAvailable;
    private UpdateInfo? _latestUpdate;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateViewModel"/> class.
    /// </summary>
    /// <param name="updateService">The service for checking and installing updates.</param>
    public UpdateViewModel(IUpdateService updateService)
    {
        _updateService = updateService;
        AppVersion = ResolveAppVersion();
        InstallUpdateCommand = new AsyncRelayCommand(InstallUpdateAsync, CanInstallUpdate);
    }

    /// <summary>
    /// Gets the application version string derived from assembly metadata.
    /// </summary>
    public string AppVersion { get; }

    /// <summary>
    /// Gets a value indicating whether an update is available for download.
    /// </summary>
    public bool UpdateAvailable
    {
        get => _updateAvailable;
        private set
        {
            if (SetProperty(ref _updateAvailable, value))
            {
                CommandManager.InvalidateRequerySuggested();
                StatusTextChanged?.Invoke("UPDATE AVAILABLE");
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the application is currently checking for updates.
    /// </summary>
    public bool IsCheckingForUpdate
    {
        get => _isCheckingForUpdate;
        private set
        {
            if (SetProperty(ref _isCheckingForUpdate, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Gets the command to download and install the update.
    /// </summary>
    public ICommand InstallUpdateCommand { get; }

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
            var update = await _updateService.CheckForUpdateAsync();

            if (update is not null)
            {
                _latestUpdate = update;
                UpdateAvailable = true;
            }
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
    public async Task InstallUpdateAsync()
    {
        if (_latestUpdate is null)
        {
            return;
        }

        try
        {
            StatusTextChanged?.Invoke($"DOWNLOADING UPDATE {_latestUpdate.TagName}...");
            await _updateService.DownloadAndInstallAsync(_latestUpdate);
        }
        catch (Exception ex)
        {
            StatusTextChanged?.Invoke("UPDATE FAILED");
            Debug.WriteLine($"[InstallUpdate] Error: {ex.Message}");
        }
    }

    private bool CanInstallUpdate()
    {
        return UpdateAvailable && _latestUpdate is not null && !IsCheckingForUpdate;
    }

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
