# Zombie-Peak-Refinement

- [x] Review the existing `AudioService` peak-meter flow and confirm the current default-render aggregation fallback.
- [x] Define the refinement scope: keep the capture-endpoint best-case path, replace only the default-render fallback with render-session matching, and add one-shot session diagnostics plus teardown reset.
- [x] Add session-dump state tracking and swap the render fallback in `EasyBluetoothAudio/Services/AudioService.cs`.
- [x] Verify the change with a clean build and test run.

## Review

- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 91/91 tests green.
- Verification used a temporary output path outside the repo because the normal app output is currently in use by a running `EasyBluetoothAudio.exe`.

## Follow-Up

- [x] Analyze the user log and confirm why no `[PeakMeter]` lines appeared in the captured window.
- [x] Refine `AudioService.IsBluetoothDeviceConnectedAsync()` so an already opened `AudioPlaybackConnection` for the `\SNK` endpoint is not rejected solely because `System.Devices.Aep.IsConnected` reports `false`.
- [x] Re-run build and tests after the connectivity-gate refinement.

- Follow-up review:
- The user log showed the first connect succeeded at `11:54:52.981`, but manual disconnect happened at `11:55:01.489`, which is before the monitor's first 10 s poll at `11:55:02.981`; therefore no `[PeakMeter]` lines could appear in that excerpt yet.
- The same log also showed `DeviceDiscover ... Connected: False` even after `AudioPlaybackConnection Success!`, confirming that the WinRT `System.Devices.Aep.IsConnected` property is unreliable for this active `\SNK` endpoint and would have short-circuited the zombie/session path on the first poll.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors after the refinement.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 91/91 tests green after the refinement.

## Tray Exit Fix

- [x] Inspect the tray icon context menu binding and application exit path.
- [x] Route the tray context menu directly to the tray icon's `DataContext` and harden the explicit shutdown path.
- [x] Re-run build and tests for the tray-exit fix.

- Tray-exit review:
- `EasyBluetoothAudio/Views/MainWindow.xaml` now binds the tray context menu directly to `TrayIcon.DataContext` via `x:Reference`, avoiding the brittle `PlacementTarget` lookup for the Hardcodet tray-hosted context menu.
- `EasyBluetoothAudio/App.xaml.cs` now stops the refresh timer, disposes the tray icon if needed, closes the main window, and only then calls `Current.Shutdown()` when `RequestExit` fires.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 91/91 tests green.

## Tray Exit Correction

- [x] Analyze the startup crash caused by the tray-exit binding change.
- [x] Replace the cyclic XAML tray-menu binding with post-construction `ContextMenu.DataContext` wiring in `App.xaml.cs`.
- [x] Re-run build and tests after the startup-fix correction.

- Tray-exit correction review:
- `EasyBluetoothAudio/Views/MainWindow.xaml` no longer uses `x:Reference TrayIcon` inside the tray-hosted `ContextMenu`, removing the XAML cycle that crashed startup.
- `EasyBluetoothAudio/App.xaml.cs` now assigns `mainWindow.TrayIcon.ContextMenu.DataContext = mainViewModel;` immediately after `mainWindow.DataContext = mainViewModel;`, so `OpenCommand` and `ExitCommand` still resolve correctly without markup-time recursion.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 91/91 tests green.

## Tray Binding Cleanup

- [x] Analyze the remaining startup binding errors for `OpenCommand` and `ExitCommand`.
- [x] Replace post-construction tray-menu `DataContext` mutation with direct `PlacementTarget.DataContext.<Command>` bindings on the `MenuItem`s.
- [x] Re-run build and tests after the binding cleanup.

- Tray-binding cleanup review:
- `EasyBluetoothAudio/Views/MainWindow.xaml` now binds tray menu commands directly through `PlacementTarget.DataContext.OpenCommand` and `PlacementTarget.DataContext.ExitCommand` on the ancestor `ContextMenu`, so the menu no longer tries to resolve commands on the `TaskbarIcon` object itself during startup.
- `EasyBluetoothAudio/App.xaml.cs` no longer mutates `TrayIcon.ContextMenu.DataContext` after construction, removing the source of the remaining startup binding noise while preserving the explicit shutdown path.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 91/91 tests green.

## Tray Exit Reliability

- [x] Analyze the repeated tray-exit regression after the binding cleanup.
- [x] Replace tray-menu command bindings with direct click wiring to `MainViewModel.OpenCommand` / `ExitCommand` after window creation.
- [x] Re-run build and tests after the reliability fix.

- Tray-exit reliability review:
- `EasyBluetoothAudio/Views/MainWindow.xaml` now exposes named tray menu items instead of relying on tray-hosted WPF command bindings for `Open` and `Exit`.
- `EasyBluetoothAudio/App.xaml.cs` wires `OpenTrayMenuItem.Click` and `ExitTrayMenuItem.Click` directly to `MainViewModel.OpenCommand` and `ExitCommand`, and logs `[App] RequestExit received from tray.` / `[App] OnExit code=...` so the next run will tell us unambiguously whether the exit click path fired.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 91/91 tests green.
