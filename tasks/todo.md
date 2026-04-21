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

## Pre-Connect Settle Timing Review

- [x] Validate whether `TearDownAudioConnection("pre-connect")` incorrectly resets `_lastDisconnectTime` before the settle-delay decision.
- [x] Preserve the teardown cleanup for `pre-connect` while keeping `_lastDisconnectTime` tied only to real disconnect/failure paths.
- [x] Re-run build and tests after the timestamp fix.

- Pre-connect timing review:
- The review finding is valid: `ConnectBluetoothAudioAsync()` called `TearDownAudioConnection("pre-connect")` before evaluating `timeSinceDisconnect`, and the shared teardown helper unconditionally set `_lastDisconnectTime = DateTime.UtcNow`.
- That meant every connect/reconnect attempt observed an almost-zero disconnect age and therefore re-applied the full settle window, even when the previous physical disconnect had happened much earlier.
- `EasyBluetoothAudio/Services/AudioService.cs` now calls `TearDownAudioConnection("pre-connect", updateDisconnectTimestamp: false)`, so the pre-connect cleanup still disposes stale state without rewriting the settle reference timestamp used by the subsequent `needsSettle` check.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors after the timestamp fix.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 91/91 tests green after the timestamp fix.

## Idle Zombie Backoff

- [x] Analyze the idle log and confirm whether repeated silence-triggered zombie recycles are delaying resume after inactivity.
- [x] Add a cooldown so continued silence after one zombie recycle does not immediately trigger the next recycle again.
- [x] Re-run build and tests after the idle-backoff refinement.

- Idle-zombie backoff review:
- The new user log shows the matched iPhone session staying at `peak=0,0000` for minutes while the phone is simply idle, and the monitor therefore recycles every roughly 30 seconds (`12:46:56`, `12:47:28`, `12:48:00`, `12:48:32`, `12:49:03`).
- That repeated recycle pattern is enough to explain why resuming audio can feel delayed: the app keeps tearing down and reopening the route even though the silence is not proof of a zombie.
- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now adds a `ZombieRecycleBackoffMs` window so one silence-triggered recycle is allowed, but continued silence must remain stable through a longer cooldown before another recycle is even considered.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors after the idle-backoff refinement.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 93/93 tests green after the idle-backoff refinement.

## Zombie Backoff Regression

- [x] Analyze the new log and confirm whether the idle-backoff change can leave the route silent after a failed first zombie recycle.
- [x] Refine the backoff so the first failed zombie recovery may still retry promptly, while long repeated recycle storms remain suppressed afterwards.
- [x] Re-run build and tests after the regression fix.

- Zombie-backoff regression review:
- The new log shows a healthy stream with non-zero peaks up to `13:23:17`, then a real zero-peak transition, then one zombie recycle at `13:23:47`, and afterwards sustained `peak=0,0000` with no further recovery attempt.
- That behavior matches the current backoff exactly: after the first recycle, `_lastZombieRecycleTime` blocks all further zombie retries for two minutes, so a failed first recovery leaves the user stuck in silence.
- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now applies the long `ZombieRecycleBackoffMs` cooldown only after the monitor has already accumulated the configured number of failed zombie recycles, instead of after the very first attempt.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 0 warnings and 0 errors after the regression fix.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out\\bin\\"` passed with 94/94 tests green after the regression fix.

## Manual-First Recovery UX

- [x] Remove the peak-/keepalive-based zombie recovery path from `MainViewModel` and `AudioService`.
- [x] Add bounded auto-reconnect only for real connection loss and failed initial connects.
- [x] Add `Reconnect` to the main UI and tray menu, and surface explicit manual reconnect status text.
- [x] Rewrite the affected tests for bounded reconnects and manual recovery, then re-run build and tests.

## Review

- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now uses a manual-first recovery flow: idle silence no longer triggers hidden recycling, real loss gets at most two auto-reconnect attempts spaced by 3 seconds, and exhausted recovery lands in `AUDIO LOST - CLICK RECOVER`.
- `EasyBluetoothAudio/Services/Interfaces/IAudioService.cs`, `EasyBluetoothAudio/Services/AudioService.cs`, and `EasyBluetoothAudio/EasyBluetoothAudio.csproj` no longer expose or depend on the peak-meter/NAudio path; recovery decisions are now based only on real connection state and explicit user action.
- `EasyBluetoothAudio/Views/BluetoothConfigView.xaml`, `EasyBluetoothAudio/Views/MainWindow.xaml`, and `EasyBluetoothAudio/App.xaml.cs` now expose `Reconnect` in both the main window and tray menu, wired directly to `MainViewModel.ReconnectCommand`. The main window swaps the left-slot button between `CONNECT` and `RECONNECT` based on `IsConnected` so the primary action slot always reflects the next useful step.
- `EasyBluetoothAudio.Tests/MainViewModelTests.cs` now validates bounded reconnect behavior, manual recovery, exhausted-retry fallback, and the absence of idle-time hidden recycling.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out-final\\bin\\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="%TEMP%\\EasyBluetoothAudio-codex-out-final\\bin\\"` passed with 95/95 tests green.

## Split-Button Animation

- [x] Replace the always-visible DISCONNECT button with a single full-width CONNECT button while disconnected.
- [x] On IsConnected transition, animate DISCONNECT fading and sliding in from the left while CONNECT collapses to RECONNECT on the left half.
- [x] Re-run build and tests after the UI change.

- Split-button animation review:
- `EasyBluetoothAudio/Views/BluetoothConfigView.xaml` replaces the previous `UniformGrid` that always showed DISCONNECT with a 2-column `Grid` in which a single full-width CONNECT button is shown while disconnected; when `IsConnected` flips to `True`, CONNECT collapses, RECONNECT occupies column 0 at half width, and DISCONNECT in column 1 animates `Opacity` from 0 to 1 and `TranslateTransform.X` from `-40` to `0` over 0.35 s with a cubic `EaseOut` easing, producing the "button splits into two" effect. A symmetric 0.25 s exit animation reverses the fade and slide when the app disconnects.
- No ViewModel or command changes were required; `ConnectCommand`, `ReconnectCommand`, and `DisconnectCommand` and their `CanExecute` gating remain intact, so the new layout preserves all prior behavior (including bounded auto-reconnect and manual recovery).
- `dotnet build C:/dev/EasyBluetoothAudio/EasyBluetoothAudio.slnx -p:BaseOutputPath="$TEMP/EasyBluetoothAudio-splitbtn-out/bin/"` passed with 0 warnings and 0 errors.
- `dotnet test C:/dev/EasyBluetoothAudio/EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="$TEMP/EasyBluetoothAudio-splitbtn-out/bin/"` passed with 95/95 tests green.

## Git Commit Grouping Review

- [x] Inspect the current workspace diff file by file.
- [x] Separate the changes into coherent commit-sized groups by purpose and dependency.
- [x] Record the proposed commit plan and rationale in the review section.

## Review

- Recommended primary commit 1: `feat: switch recovery to manual reconnect` for the `MainViewModel` / `AudioService` recovery redesign, NAudio removal, tray reconnect wiring, tests, and the matching lesson/task notes.
- Recommended primary commit 2: `feat: animate split reconnect controls` for the `BluetoothConfigView.xaml` transition from a static connect/disconnect pair to the full-width connect state plus animated reconnect/disconnect split layout.
- `EasyBluetoothAudio/Views/BluetoothConfigView.xaml` and `tasks/todo.md` need partial staging if the implementation and UI-animation changes are split cleanly into those two commits.

## Center-Split Button Animation

- [x] Review the current `BluetoothConfigView.xaml` button transition and isolate why the existing motion does not read as a center split.
- [x] Rework the action-button animation so the full-width `CONNECT` button collapses from the center while `RECONNECT` and `DISCONNECT` expand outward from that center line.
- [x] Re-run build and tests after the XAML update.

## Review

- The previous transition did not read as a single button splitting in two because `RECONNECT` snapped into its final left slot immediately while only `DISCONNECT` animated in. That produced a layout swap plus a slide, not a center-origin split.
- `EasyBluetoothAudio/Views/BluetoothConfigView.xaml` now overlays the full-width `CONNECT` button on top of the two connected-state buttons. On `IsConnected = true`, `CONNECT` collapses on `ScaleX` around its center while `RECONNECT` and `DISCONNECT` each animate from `ScaleX = 0` at the center line outward to their final half-width positions.
- No ViewModel or command logic changed; the update is isolated to the action-button XAML animation.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-center-split-out\bin\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-center-split-out\bin\"` passed with 95/95 tests green.

## Connect/Reconnect Separation

- [x] Refactor `MainViewModel` so `Connect` and `Reconnect` have distinct semantics with an explicit recoverable audio-loss state.
- [x] Apply the physical-aware fallback after exhausted auto-reconnect attempts so the UI guides to `Reconnect` only when Bluetooth is still physically up.
- [x] Update `BluetoothConfigView.xaml` so `CONNECT`, `RECONNECT`, and `DISCONNECT` visibility follows the new recoverable state instead of `IsConnected` alone.
- [x] Extend `MainViewModelTests` for recoverable fallback, physical disconnect fallback, and one-shot manual reconnect behavior.
- [x] Re-run the targeted `MainViewModelTests` suite and record the verification results.

## Review

- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now separates full `Connect` from one-shot manual `Reconnect` via an explicit recoverable-loss state. Exhausted fallback checks `IsBluetoothPhysicallyConnectedAsync(...)` and lands in either `AUDIO LOST - CLICK RECONNECT` or `AUDIO LOST - CLICK CONNECT` accordingly.
- `EasyBluetoothAudio/Views/BluetoothConfigView.xaml` now drives the split-button visibility from `ShowReconnectActions` instead of `IsConnected` alone, so the connected-state controls stay available when only the stream is lost but Bluetooth remains physically linked.
- `EasyBluetoothAudio.Tests/MainViewModelTests.cs` now covers recoverable vs. non-recoverable fallback, the new disconnected-state `Reconnect` no-op, one-shot manual reconnect without hidden retries, and clearing the recoverable state via `Disconnect`.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.Tests\EasyBluetoothAudio.Tests.csproj -c Release --no-restore --filter MainViewModelTests` passed with 34/34 `MainViewModelTests` green.

## Service-Loss Cancellation Handling

- [x] Inspect the `ConnectionLost` event path and confirm where monitor cancellation can escape the `async void` handler.
- [x] Treat monitor cancellation as a normal stop condition during service-triggered reconnect and keep the rest of the reconnect flow unchanged.
- [x] Add a regression test for disconnecting while the service-loss reconnect delay is pending, then re-run the targeted `MainViewModelTests` suite.

## Review

- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now wraps the service-triggered `async void` `OnConnectionLostFromService(...)` path in an `OperationCanceledException` filter keyed to the current monitor token/generation, so `StopConnectionMonitor()` cancellation is treated as a normal shutdown instead of escaping as an unhandled exception.
- `EasyBluetoothAudio.Tests/MainViewModelTests.cs` now includes `ConnectionLost_Event_DoesNotThrow_WhenUserDisconnectsDuringPendingReconnectDelay`, which raises the immediate `ConnectionLost` event, disconnects while the reconnect `Task.Delay(...)` is still pending, and asserts the UI stays in the clean `DISCONNECTED` state without issuing another connect attempt.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.Tests\EasyBluetoothAudio.Tests.csproj -c Release --no-restore --filter MainViewModelTests -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-service-loss-cancel-out\bin\"` passed with 35/35 `MainViewModelTests` green.
