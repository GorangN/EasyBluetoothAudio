# Zombie-Peak-Refinement

## GitHub Pages build cancellation — 2026-10-23

Acceptance: identify the cancelled step from the reported run, address the demonstrated timeout without bypassing website tests, and verify the workflow and production website checks.

- [x] Diagnose the cancellation using GitHub job logs and the workflow configuration. Verify: distinguish a timeout from concurrency cancellation or a website failure.
- [x] Apply the smallest workflow correction supported by the logs. Verify: browser provisioning has sufficient time, production tests remain required, and unrelated pushes do not needlessly deploy the website.
- [x] Validate the workflow and run website checks, build, and browser tests. Verify: zero check errors/warnings, all tests pass, and the final diff is limited to the workflow, its deployment documentation, and task record.

### Review

- Run `39822395245`, job `119976773263`, exceeded the configured 10-minute build limit. Astro check/build passed; Ubuntu's package mirror took 9 minutes 42 seconds to fetch 24.5 MB at 42.1 kB/s during `playwright install --with-deps chromium`, leaving no time for browser provisioning/tests. GitHub's check annotation explicitly confirms the timeout.
- Raised only the build budget to 20 minutes and selected Playwright's supported `--only-shell` mode, matching CI's default headless Chromium. Kept system dependency installation and required production tests. Added website/workflow path filters, excluding the review record, so the reported task/review-only push will not redeploy. Updated the deployment documentation.
- YAML parsing and 14 trigger-path scenarios pass, including Markdown inside website source, lockfile, assets, tests, workflow changes, and excluded task/review records. Fresh `npm ci`, Astro check (zero errors/warnings/hints), production build, and all 14 browser tests pass. Tests used CI mode and Chromium headless shell; an unrelated service occupied port 4321, so a temporary config ran the unchanged suite on port 4397 and was removed afterward. `git diff --check` passes. Independent review found no consequential issue; its documentation correction is included.
- Confirmed the runtime website matches the last successful deployment, then retried [run 39822395245](https://github.com/GorangN/EasyBluetoothAudio/actions/runs/39822395245). The retry succeeded: build took 7m37s, all 14 Linux browser tests passed, and Pages deployment took 9s. Its Ubuntu package download still took 6m38s (25.9 MB at 65.2 kB/s), reinforcing the timeout adjustment. This retry used the previously committed workflow, not these local edits. The live website returns HTTP 200 with its expected title and download link. Workflow changes remain local and uncommitted.

### Publication — 2026-10-23

Acceptance: publish the reviewed workflow and documentation to `main`, verify the new commit's GitHub Pages build/tests/deployment and application CI, and preserve unrelated local files.

- [x] Review the exact pending diff and remote head. Verify: only the workflow, deployment README, and this task record belong to the publication; remote `main` still matches the local parent.
- [x] Commit and push the reviewed files. Verify: the remote branch contains the intended commit and no unrelated files.
- [x] Wait for the new GitHub Actions runs and inspect their results. Verify: website check/build, all browser tests, Pages deployment, and Build & Test pass; record the run URLs and final publication result.

### Publication result

- Published `5c3046b` to `origin/main`, containing only `.github/workflows/pages.yml`, `website/README.md`, and this task record. The pre-commit build passed with zero warnings and errors; the remote SHA matched the local commit. Existing untracked skill files were preserved.
- [Pages run 39937466529](https://github.com/GorangN/EasyBluetoothAudio/actions/runs/39937466529) passed for that exact commit: build in 46 seconds, all 14 browser tests passed, and deployment in 10 seconds. Logs confirm the new `--only-shell` command ran and downloaded Chromium headless shell without full Chromium. Astro check reported zero errors and warnings.
- [Build & Test run 39937466421](https://github.com/GorangN/EasyBluetoothAudio/actions/runs/39937466421) also passed in 1 minute 29 seconds. The deployed site returned HTTP 200 with its expected product title and installer link.

## GitHub Pages landing page — 2026-10-22

Acceptance: complete Astro static landing page in website; truthful free Windows receiver offer; Magic UI components; responsive keyboard-accessible page with reduced motion, metadata and favicon; repository base path verified; successful build, committed Actions workflow, live Pages deployment.

- [x] Inspect product, release, local skills, agents, and deployment state. Verify: source-backed copy and real installer URL; all three project design skills consulted. Ruflo inventory and policy calls both blocked by policy-anchor-log-missing; native subagents handle the parallel work.
- [x] Build static Astro page with React Magic UI islands. Verify: astro check/build, direct installer CTAs, no fabricated proof, functional preview and FAQs.
- [x] Review desktop/mobile accessibility, motion, performance and paths. Verify: browser tests, rendered screenshots, asset checks and diff review.
- [x] Commit and deploy the initial site using GitHub Actions. Verify: workflow 39816120117 succeeded; public metadata, preview after hydration, FAQ, assets, accessibility, and mobile layout pass. Exact-plugin refinement publication is tracked separately below.

### Direction and assumptions

The user delegates unspecified decisions and asks not to stall, so the design-skills interview/approval rounds are replaced by evidence-based decisions. Preserve charcoal/acid yellow from the native app. Taste direction: Dressed as something else, seed 1955114603, borrowing a hi-fi product editorial composition. Impeccable surface seed: 9884df3f4c87d440. On this page, the app's phone-to-PC connection becomes an interactive listening diagram. Use Chivo, a substantial product preview, quiet factual copy, and flat surfaces. Requested Magic UI primitives take precedence over incompatible generic skill bans on component geometry/gradients. No invented testimonials; use the verified GitHub community and inspectable MIT source. No stock imagery needed: the product and its audio route are the visual subject.

Page argument: phone audio through PC speakers (native receiver) → retain phone controls and tray access (source) → install/pair/connect (README) → free and transparent (MIT/GitHub) → compatibility/privacy/support questions → repeat download. One primary CTA: Download for Windows. The preview switches output illustrations only and explicitly identifies itself as a demo. Success is functional conversion readiness, not an unmeasured lift claim.

### Landing-page review

### Exact Anti Slop plugin follow-up

The user clarified that Anti Slop means the installed miqdadbadjuber/anti-slop Codex plugin, version 3.2.21. The initial design skills were not that plugin. Its core, UI, mobile-layout, human/accessibility, copywriting, and comment rules are now loaded. No saved mode exists; the user's existing instruction to make reasonable decisions without questions authorizes applying during mode while finishing. No plugin installation, global preference, or AGENTS.md change is needed.

Reading this as a free Windows audio utility landing page for people listening at a PC, in the app's charcoal and acid-yellow visual language. Dials: ENERGY 2 / RHYTHM 2 / MOTION 2. The purpose is clear downloads with a bounded, user-triggered audio-path demonstration.

- [x] Apply the exact plugin's concrete findings. Verify: 44px mobile controls, purposeful icons, specific link copy, bounded opt-in preview motion.
- [x] Run contrast, click-through, responsive/zoom and existing regression checks; record the four-block delivery gate with evidence.
- [x] Commit the refinement and verify its GitHub Pages deployment and public behavior.

Public verification caught an early click before the visible React island hydrated. Re-plan: keep preview controls disabled until their handlers are mounted, add a delayed-module regression, and repeat local and public click-through. Downloads and native FAQs remain independent of JavaScript.

Exact-plugin local review: check/build pass without diagnostics, 14 Playwright tests pass, all 17 links were clicked (including four real installer downloads cancelled without execution), six FAQ rows and all preview controls work. Contrast calculations pass and every visible interactive target is at least 44px at five viewport sizes. The 720px test checks 200%-zoom-equivalent reflow. The delayed-module test failed against the previous production build and passes with the hydration fix. All four required delivery-gate blocks are recorded with item-level evidence in `website/ANTI_SLOP_REVIEW.md`. Initial GitHub Pages workflow 39816120117 completed successfully; final refinement publication remains to be verified.

### Publication recovery checkpoint (before final verification)

- Initial site commit `de0d6b6` is live at https://gorangn.github.io/EasyBluetoothAudio/. Public browser verification passes for the title, interactive output selection after hydration, FAQ, favicon, social image, both sitemap files, zero axe violations, zero console/asset errors, and no phone-width overflow.
- Exact-plugin refinements and the hydration fix are committed and pushed as `e58cc09`. Workflow 39818629162 completed its build, all 14 tests, and artifact upload successfully, but its publish job received no hosted runner for over 13 minutes. GitHub's status API reported degraded Actions service.
- A one-time publish-job retry was attempted. GitHub marked the job cancelled but left the run in progress; rerun requests were rejected as already running, while force-cancel returned HTTP 409 claiming it was not in progress. No Pages approval is pending and Pages remains configured for Actions with HTTPS.
- This checkpoint commit triggers a fresh automatic publication of the same tested website source. The initial page remains available throughout. The refined version must not be described as publicly verified until its final headline, initially paused preview, and 44px controls are observed at the public URL.
- If GitHub does not recover automatically, the remaining operator action is Actions > Deploy website to GitHub Pages > Run workflow > main after the service recovers. No source, build, secret, or Pages-setting change is required.

### Final publication verification

- Recovery succeeded: [workflow 39821721191](https://github.com/GorangN/EasyBluetoothAudio/actions/runs/39821721191) completed build and deployment successfully for `34280d3`. All 14 production browser tests passed in GitHub's Linux runner. Deployment ID 7258847478 targets the `github-pages` environment.
- Verified the refined public page at https://gorangn.github.io/EasyBluetoothAudio/: the new final headline, initially paused preview, working Headphones toggle, native FAQ, correct title, favicon/social image and both sitemap URLs, all visible mobile targets at least 44px, zero axe violations, zero console/asset errors, and no horizontal overflow at 390px.
- All website source and the Actions workflow are committed and pushed to `main`. This final record-only commit does not change the tested website source. Pages uses the Actions source with HTTPS; no manual setup or recovery step remains.
- The pre-existing untracked `.agents/skills/` directory and `skills-lock.json` were preserved and not included in website commits. Ruflo remained unavailable because of its policy-anchor error; no security settings or unrelated project configuration were changed to bypass it.

### Initial review evidence

- Astro 7.4.2 static build and TypeScript/Astro check pass with zero errors, warnings, or hints. React is hydrated only for the interactive audio preview; Magic UI Bento and Shimmer markup is server-rendered. Components are adapted from pinned upstream MIT source, with license and attribution retained.
- 11 Playwright tests pass: 320/390/768/1440 widths, no horizontal overflow, correct base-path assets and metadata, consistent download destination, keyboard FAQ and skip link, interactive preview, pause, reduced motion, and no-JavaScript fallback. Axe finds zero violations on desktop and mobile. Fixed the visible/accessible pause-label mismatch and the orphan tablet connector; the latter is covered by a responsive regression assertion.
- Local Lighthouse mobile report: performance 99, accessibility 100, best practices 100, SEO 100; LCP 1.8s, CLS 0.001, TBT 0ms. Its report was produced successfully; the CLI later hit a Windows temporary-directory cleanup EPERM. These are lab measurements, not field Core Web Vitals or conversion results.
- Read-only independent review disposition: ship after viewing desktop, tablet, and phone captures and verifying the tablet correction. An available fresh-context reviewer substituted for the unavailable named Impeccable reviewer role. Documenter records the actual web system separately from WPF.
- Taste inspection confirms two-line main headings at 1440/768/390 and body copy at 16px. Its generic four-photo quota remains deliberately unmet: a light, accurate product diagram and UI previews better serve this utility. Shimmer gradient/shadow lint flags are intentional upstream Magic UI effects required by the brief. Small text remains within decorative UI representations; the original small marketing notes were enlarged. One copy-cadence advisory remains stylistic. No claim that these generic style gates fully pass is made.
- Official installer endpoint verified HTTP 200 with application/octet-stream. GitHub proof verified at 14 stars and 3 forks and dated explicitly. Pages source changed from legacy to workflow; HTTPS enabled. Workflow actions pinned to verified official commits and actionlint passes. Build artifact is published only after build and browser/accessibility tests.

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

## Initial Connect Cancellation Handling

- [x] Inspect the initial `ConnectAsync()` retry path and confirm where stale retries can outlive a later disconnect/stop request.
- [x] Add a cancellable operation scope/generation guard for the initial connect auto-reconnect flow so stale retries and stale fallback UI updates are discarded.
- [x] Add a regression test for disconnecting during the initial auto-reconnect delay, then re-run the targeted `MainViewModelTests` suite.

## Review

- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now gives user-triggered initial connects their own cancellation/generation scope. `Disconnect()` cancels that scope, `ConnectWithAutoReconnectAsync(...)` discards stale late-success results, and `ApplyManualFallbackStateAsync(...)` now re-checks whether the original connect attempt is still current before applying fallback UI guidance.
- `EasyBluetoothAudio.Tests/MainViewModelTests.cs` now includes `ConnectAsync_DoesNotRetry_WhenUserDisconnectsDuringPendingRetryDelay`, which starts `ConnectAsync()`, waits for the auto-reconnect delay window, disconnects, and asserts the original connect task exits without running the delayed retry or applying stale fallback state.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.Tests\EasyBluetoothAudio.Tests.csproj -c Release --no-restore --filter MainViewModelTests -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-initial-connect-cancel-out\bin\"` passed with 36/36 `MainViewModelTests` green.

## Reconnect Fallback Gating

- [x] Inspect the reconnect fallback path in `MainViewModel` and the current `AudioService.IsBluetoothPhysicallyConnectedAsync(...)` implementation.
- [x] Replace the unreliable AEP-based physical-link check with selector-based audio-playback device presence that matches the app's supported endpoint model.
- [x] Add a focused `AudioService` regression test for reconnect fallback gating, then re-run targeted verification.

## Review

- `EasyBluetoothAudio/Services/AudioService.cs` now funnels both device discovery and `IsBluetoothPhysicallyConnectedAsync(...)` through `AudioPlaybackConnection.GetDeviceSelector()` enumeration, so reconnect fallback guidance is based on whether Windows still exposes the source as an available remote audio-playback device instead of on `System.Devices.Aep.IsConnected`.
- `EasyBluetoothAudio/Services/Interfaces/IAudioService.cs` now documents the selector-based contract explicitly, matching the WinRT `\SNK` behavior the codebase has already seen in logs and lessons.
- `EasyBluetoothAudio.Tests/AudioServiceTests.cs` and `EasyBluetoothAudio.Tests/TestAudioService.cs` add service-level regression coverage for selector-present, selector-missing, and selector-discovery cases so this fallback bug is no longer masked behind `MainViewModel` mocks.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-selector-fallback-out\bin\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx --no-build -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-selector-fallback-out\bin\"` passed with 104/104 tests green.

## Persistent Reconnect + Silent Route Fixes

- [x] Replace bounded reconnect with a sticky reconnect loop that keeps trying until `Disconnect`.
- [x] Add one-time startup-prime recycle after the first successful fresh connect.
- [x] Reintroduce trusted route-specific peak sampling and long-idle silent-route recovery.
- [x] Update the main button visibility so `Disconnect` remains available during automatic reconnecting.
- [x] Extend `MainViewModel` and `AudioService` tests for startup-prime, persistent reconnect, and silent-route recovery.
- [x] Re-run build and full tests, then add a review section with results.

## Review

- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now keeps connect intent sticky until `Disconnect`, performs a one-time `startup-prime` recycle after the first successful fresh connect, and distinguishes three cases cleanly: real out-of-range loss keeps retrying forever, startup silence is handled by the hidden prime recycle, and long-idle silence gets one trusted hidden recovery before falling back to `AUDIO LOST - CLICK RECONNECT`.
- `EasyBluetoothAudio/Services/Interfaces/IAudioService.cs`, `EasyBluetoothAudio/Services/AudioService.cs`, and `EasyBluetoothAudio/EasyBluetoothAudio.csproj` now restore route-specific peak sampling via `GetActiveDevicePeakLevel()` and `NAudio`, but only from a trusted capture endpoint or matched render session. The old aggregate default-render fallback was not reintroduced.
- `EasyBluetoothAudio/Views/BluetoothConfigView.xaml` now keeps `Disconnect` visible whenever the app is actively reconnecting, while `Reconnect` stays reserved for connected or explicit recoverable-loss states.
- `EasyBluetoothAudio.Tests/MainViewModelTests.cs`, `EasyBluetoothAudio.Tests/AudioServiceTests.cs`, and `EasyBluetoothAudio.Tests/TestAudioService.cs` now cover startup-prime, sticky reconnect after real loss, manual reconnect fallback, trusted silent-route recovery, null-peak no-judgment behavior, and rearms after fresh audio activity.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -c Release -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-persistent-reconnect\bin\"` passed with 104/104 tests green.

## Startup Silent + Idle Monitor Follow-Up

- [x] Rework fresh-connect silent-route logic so a new startup session may use one monitored hidden recovery before the first positive peak sample.
- [x] Fix the background monitor fallback path so continued silence after the one hidden recovery lands in manual reconnect state without cross-thread WPF exceptions.
- [x] Extend `MainViewModelTests` for fresh-connect silent recovery and the post-recovery manual fallback path, then re-run full verification.

## Review

- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now preserves fresh-connect route-health state when `StartConnectionMonitor(...)` replaces an older monitor, so the monitor can still spend one hidden silent-route recovery before the first positive peak sample arrives. That startup-only allowance is consumed once, then suppressed until healthy audio is observed.
- The continued-silence fallback after a hidden recovery now stops the monitor infrastructure from the dispatcher-owned UI path instead of calling `StopConnectionMonitor()` directly from the worker loop. That removes the cross-thread WPF exception seen in the user log while keeping the long-idle behavior at "one hidden recovery, then explicit reconnect".
- `EasyBluetoothAudio.Tests/MainViewModelTests.cs` now covers the fresh-connect hidden recovery and the "do not loop or prompt before first healthy audio" startup case in addition to the existing post-healthy idle-silence coverage.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.Tests\EasyBluetoothAudio.Tests.csproj -c Release --no-restore --filter MainViewModelTests -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-startup-silent-fix\bin\"` passed with 35/35 `MainViewModelTests` green.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -c Release -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-startup-idle-followup\bin\"` passed with 105/105 tests green.

## False Audio-Lost Follow-Up

- [x] Refine the route-health API so the ViewModel can distinguish trusted capture silence from weaker render-session silence.
- [x] Prevent weak session-zero samples from forcing `AUDIO LOST - CLICK RECONNECT` while audio is still healthy, while preserving the hidden startup/idle recovery behavior.
- [x] Extend `AudioService` and `MainViewModel` tests for source-confidence behavior, then re-run focused and full verification.

## Review

- The new user log is not fully good: startup recovery worked and the cross-thread monitor crash is gone, but the app still promoted weak `session match=... peak=0` telemetry into the final red `AUDIO LOST - CLICK RECONNECT` state even while audio remained healthy.
- `EasyBluetoothAudio/Models/AudioRoutePeakSample.cs`, `EasyBluetoothAudio/Services/Interfaces/IAudioService.cs`, and `EasyBluetoothAudio/Services/AudioService.cs` now carry source confidence with each route-health sample. Capture-endpoint silence is marked authoritative, while render-session silence is explicitly weak; positive audio from either source still counts as proof that audio exists.
- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` now allows weak silence to trigger at most one hidden startup/idle recovery, but only authoritative silence may escalate to the final manual-reconnect UI state. This preserves the startup fix while preventing the false red status the user just reported.
- `EasyBluetoothAudio.Tests/AudioServiceTests.cs`, `EasyBluetoothAudio.Tests/TestAudioService.cs`, and `EasyBluetoothAudio.Tests/MainViewModelTests.cs` now cover weak-session confidence, startup weak-silence recovery, authoritative-silence fallback, and the new regression where weak silence after recovery must not flip the UI to `AUDIO LOST`.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.Tests\EasyBluetoothAudio.Tests.csproj -c Release --no-restore --filter "AudioServiceTests|MainViewModelTests" -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-false-audio-lost-fix\bin\"` passed with 42/42 focused tests green.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -c Release -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-false-audio-lost-full\bin\"` passed with 107/107 tests green.

## Telemetry Rollback For Stability

- [x] Inspect the current peak/idle-based recovery path and the older pre-telemetry reconnect flow to identify the minimum rollback that preserves sticky reconnect and startup-prime.
- [x] Remove all sound/idle telemetry-driven recovery from `MainViewModel` while keeping sticky reconnect for real connection loss and the one-time startup-prime recycle for fresh connects.
- [x] Remove the peak-sampling API, `NAudio` dependency, and related model/service code so the app no longer makes connection decisions from untrustworthy Windows audio telemetry.
- [x] Rewrite the affected unit tests around connection-state behavior only, then re-run focused and full verification.

## Review

- `EasyBluetoothAudio/ViewModels/MainViewModel.cs` no longer evaluates route-health or idle state at all. The background monitor only checks real connection state, sticky reconnect still runs until `Disconnect`, and the one-time `startup-prime` recycle remains in place for fresh connects.
- `EasyBluetoothAudio/Services/Interfaces/IAudioService.cs`, `EasyBluetoothAudio/Services/AudioService.cs`, `EasyBluetoothAudio/EasyBluetoothAudio.csproj`, and the deleted `EasyBluetoothAudio/Models/AudioRoutePeakSample.cs` remove the peak-sampling API and `NAudio` dependency completely, so the app no longer makes reconnect decisions from weak Windows audio telemetry.
- `EasyBluetoothAudio.Tests/MainViewModelTests.cs`, `EasyBluetoothAudio.Tests/AudioServiceTests.cs`, and `EasyBluetoothAudio.Tests/TestAudioService.cs` now cover connection-state behavior only: startup-prime, sticky reconnect, transient-loss cross-checking, selector-based physical connection checks, and manual reconnect fallback.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-telemetry-rollback-build\bin\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.Tests\EasyBluetoothAudio.Tests.csproj -c Release --no-restore --filter "AudioServiceTests|MainViewModelTests" -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-telemetry-rollback-focused\bin\"` passed with 32/32 focused tests green.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -c Release -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-telemetry-rollback-full\bin\"` passed with 97/97 tests green.

## Connection Lifecycle Audit

- [x] Trace every connect, reconnect, startup-prime, retry, and disconnect path through the ViewModel and audio service.
- [x] Verify whether an existing `AudioPlaybackConnection` is torn down before each new open attempt and whether teardown completion is awaited where necessary.
- [x] Preserve the existing real-disconnect settle policy and prevent stale or overlapping attempts from mutating a newer connection.
- [x] Add regression coverage, run focused and full verification, and record the evidence-backed diagnosis.

## Review

- Confirmed: every connect starts by disposing the current `AudioPlaybackConnection`; manual reconnect and startup-prime also explicitly dispose before opening the replacement.
- Immediate `Start()` / `OpenAsync()` after an internal teardown remains a plausible device-specific failure hypothesis, but Microsoft does not document a mandatory 5-second release window and the repository contains conflicting empirical observations. The proposed blanket settle rule was therefore rolled back pending a real device log.
- `AudioService` now uses connection-local references plus a generation guard so stale attempts cannot act on or tear down a newer connection. WinRT unsubscribe/dispose runs outside the lifecycle lock to avoid callback/close lock inversion.
- `MainViewModel` now gives manual reconnect the same cancellation/generation protection as initial connect. Recovery re-checks ownership inside dispatcher callbacks before changing UI state or globally disconnecting, and stale result cleanup can no longer close a newer route.
- `dotnet build C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -c Release -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-confidence-review-final\bin\"` passed with 0 warnings and 0 errors.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.Tests\EasyBluetoothAudio.Tests.csproj -c Release --no-build --filter "AudioServiceTests|MainViewModelTests" -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-confidence-review-final\bin\"` passed with 37/37 focused tests green.
- `dotnet test C:\dev\EasyBluetoothAudio\EasyBluetoothAudio.slnx -c Release --no-build -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-confidence-review-final\bin\"` passed with 102/102 tests green.

## Post-Implementation Confidence Review

- [x] Re-review the connection-lifecycle diff for deadlocks, stale-state races, and unnecessary complexity.
- [x] Compare the WinRT lifecycle assumptions with official Microsoft documentation and the repository's prior empirical findings.
- [x] Record which parts are proven, which are defensive, and which still require a real Bluetooth-device test.

## Review

- Proven by deterministic tests: stale manual-reconnect and monitor-recovery results no longer overwrite a later user `Disconnect`; cancellation is checked inside the dispatcher callback where UI/global teardown is actually applied.
- Defensive by code review: `AudioService` candidates are operation-local, generations reject superseded results, and stale failure cleanup only tears down the matching current connection.
- Still requires hardware validation: whether `startup-prime` or manual reconnect needs an additional settle delay on the affected Bluetooth adapter/device. The WinRT API contract documents close/start/open semantics but no release duration.

## Codex Karpathy Guidelines Integration

Adapt the complete behavioral guidance from `multica-ai/andrej-karpathy-skills/CLAUDE.md` into the root `AGENTS.md`, which is the project's Codex instruction entry point. Preserve the project workflow and WPF/MVVM requirements, reconcile the blanket-refactoring rule with focused edits, and keep this change limited to instructions and this task record.

- [x] Read the upstream `CLAUDE.md` and companion skill, existing project instructions, and relevant lessons; verify Codex instruction discovery against official documentation.
- [x] Integrate all four principles, including every actionable upstream guideline, with source attribution and project-specific conflict resolution. Verify: review the resulting instruction text against the upstream source.
- [x] Independently review completeness and consistency. Verify: all upstream behaviors are represented and existing architecture requirements remain intact.
- [x] Check the final diff and document results. Verify: `git diff --check` passes and only the intended documentation files changed.

### Review

- The root `AGENTS.md` now includes all four upstream principles and their actionable guidance, source attribution, the reviewed upstream blob, and the caution/trivial-task tradeoff. No separate skill invocation is required.
- Reconciled the blanket Boy Scout refactoring instruction with focused changes, preserved mandatory external-service interfaces, and provided a written-plan fallback when Codex plan mode is unavailable.
- Independent review checked coverage against both upstream files. Its validation-test wording finding was addressed by explicitly requiring invalid-input tests before implementation.
- A local content check passed: all four principle headings exist; formatting, naming, interfaces, DI, architecture, and WPF/MVVM requirements are unchanged; `AGENTS.md` is below the default 32 KiB instruction limit; no repository `AGENTS.override.md` masks it.
- `git diff --check` passed. Only `AGENTS.md` and `tasks/todo.md` are modified tracked files. Existing untracked Claude configuration files were left untouched.
- Application builds and tests were not run because this change only updates agent instructions and the task record. Instruction coverage and loading location were verified against the source and [official Codex documentation](https://learn.chatgpt.com/docs/agent-configuration/agents-md); a fresh Codex session was not launched.

## Issue #43: Off-screen, immovable tray window

Evidence: read issue #43, all three comments, and its timeline through the GitHub plugin; inspected the attached screenshot through the browser because the plugin cannot return binary attachments. No logs, display settings, or additional reproduction steps were provided. Reviewed related PR #17 and commit 6a25127, which introduced the placement defect. The screenshot shows the upper-left portion of a window extending past the right/bottom edges. App.RequestShow subtracts WPF DIPs from WinForms screen coordinates and assigns those results to WPF Left/Top; at 125% this places a 500x380-DIP window beyond a 1920x1040 work area. NoResize and the missing caption leave no mouse recovery. Startup bypasses positioning entirely.

Acceptance criteria: startup and tray reopening fit the window inside the selected monitor's work area, regardless of DPI, monitor origin, taskbar edge, or an oversized window; the header drags, borders resize, header commands remain clickable, and reduced sizes retain access to controls. Audio behavior remains unchanged.

- [x] Research the issue, screenshot, code, and history; verify the coordinate mismatch and baseline tests (102/102 pass).
- [x] Add failing regression coverage for missing window interaction and reproduce the original scaled-screen overflow before changing application code.
- [x] Implement native-coordinate placement behind a service interface, share startup/reopen behavior, and add XAML chrome plus scrolling. Verify scaled sizes, monitor offsets, work-area limits, and header controls.
- [x] Independently review the fix, run the complete test suite and warning-free build, and inspect the final diff.

Agent note: Ruflo registered a review agent but execution failed because no LLM provider is configured. A native subagent independently confirmed the root cause and reviewed the proposed approach; implementation remains with the parent agent.

### Issue #43 Review

- Root cause was established before application edits: the original 125% calculation produced native bounds (1762.5, 812.5)-(2387.5, 1287.5) for a 1920x1040 working area. The new XAML regression failed on the original `NoResize` setting before the fix. The screenshot is consistent with this overflow; the reporter's actual DPI and monitor arrangement are unknown.
- Added `IWindowPlacementService` and `WindowPlacementService`. Placement now uses `GetWindowRect` and `SetWindowPos` in the same native coordinate space, fits the available work area, and remeasures after moving to the target monitor to account for a DPI-driven size change. Native failures use explicit WPF primary-work-area placement; this also works with an existing HWND.
- `App.xaml.cs` resolves the placement interface through DI and routes initial display and tray reopening through the same command/event path. Audio and connection behavior are untouched.
- `MainWindow.xaml` restores native caption movement and edge resizing through WPF `WindowChrome`. `BluetoothConfigView.xaml` and `SettingsView.xaml` exempt header actions from caption hit-testing and keep small-window content scrollable. `Resources/Theme.xaml` adds the required horizontal scrollbar support; viewport-width bindings preserve normal text wrapping and prevent unnecessary horizontal scrolling at the default size.
- Tests were added in `WindowPlacementServiceTests.cs`, `ViewLayoutRuntimeTests.cs`, and `MainWindowLayoutTests.cs`, with `StaTest.cs`, `WindowLayoutTestCollection.cs`, and the test project's XAML copy items supporting isolated STA/runtime and markup checks. Coverage includes 100/125/150/200% window sizes, negative/nonzero monitor origins, taskbars on all edges, oversized windows, native failure, repeated fallback on an existing HWND, real hidden native placement, compiled view scrolling, and inherited header hit-testing.
- `dotnet build EasyBluetoothAudio.slnx -c Release --no-restore -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-issue43-final\bin\"` passed with 0 warnings and 0 errors.
- `dotnet test EasyBluetoothAudio.slnx -c Release --no-build --no-restore -p:BaseOutputPath="$env:TEMP\EasyBluetoothAudio-issue43-final\bin\"` passed with 121/121 tests. After strengthening the existing-HWND fallback test, the focused window/layout run passed with 19/19 tests and no warnings. `git diff --check` passed.
- Independent review found and resolved the initial fallback and wrapping issues; final review found no remaining consequential implementation issue. Physical mixed-DPI monitor transitions and actual mouse dragging/resizing have not been exercised interactively. No unresolved product decision was identified.
- GitHub evidence: [issue and screenshot](https://github.com/GorangN/EasyBluetoothAudio/issues/43), [introducing commit](https://github.com/GorangN/EasyBluetoothAudio/commit/6a25127a4559dfb9e1848ac2144b1438b937098e), and related PR #17 (its old layout changes do not repair this coordinate mismatch). Implementation was checked against Microsoft's [WindowChrome](https://learn.microsoft.com/en-us/dotnet/api/system.windows.shell.windowchrome?view=windowsdesktop-10.0) and [GetWindowRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect) documentation.
- Ruflo's generated workspace metadata was removed after terminating the unavailable agent; available diagnostic JSON was saved under the temporary issue43 evidence folder. Concurrent README changes and their task record were preserved. Changes remain local and uncommitted.

## README Images And Marketing Skill Recommendation

Acceptance criteria: correct the broken README icon references without changing application behavior; verify image responses and the committed screenshot's exact path on the published branch; provide copyable PNG embeds and evidence-backed Codex marketing-skill recommendations.

- [x] Inspect the attachment, README, Git tracking, and published assets. Verify: reproduce HTTP 403 for colon-form Iconify URLs and HTTP 200 for the slash-form logo and existing GIF.
- [x] Correct only the broken icon URL format. Verify: every unique replacement returns a valid SVG and the existing screenshot resolves with exact filename casing.
- [x] Compare marketing skills using current repository statistics, commit activity, adoption signals, and current installation instructions. Verify: primary sources support the recommendation and exact skill names.
- [x] Review the documentation diff and provide the image troubleshooting checklist, PNG embed, and Codex usage prompt. Verify: no unintended application edits and git diff --check passes.

### Review

- Corrected 17 references from `api.iconify.design/tabler:` to the documented SVG endpoint `api.iconify.design/tabler/`. All 15 distinct corrected URLs returned HTTP 200, SVG content, and `image/svg+xml`; the original logo URL returned HTTP 403 on GET and HEAD.
- Verified `ReadMeData/EasyBluetoothAudio.gif` with exact case in Git and the live `main` contents API; its raw URL returned HTTP 200 with `image/gif`. No `image.png` exists in the checked README assets folder. The supplied attachment shows the README with an embedded app image, so the working GIF was preserved.
- A comparison against HEAD confirmed that README.md changed only by the 17 intended URL replacements. `git diff --check` passed. No application build was needed for URL-only documentation changes. Concurrent application/test edits were left untouched. Changes remain local and uncommitted; GitHub rendering after publication has not been checked.
- Research checked GitHub repository statistics and actual latest default-branch commits on 2026-10-22: coreyhaines31/marketingskills (55,954 stars; 2026-10-19), msitarzewski/agency-agents (163,152 stars; 2026-10-19), and alirezarezvani/claude-skills (28,533 stars; 2026-10-21). The recommended pack's copywriting and cro skills showed 231.8K and 88.1K installs on skills.sh; these are adoption signals, not verified active-user or conversion counts.
- Verified current recommended skill names `product-marketing`, `copywriting`, and `cro`, plus targeted Codex installation flags in vercel-labs/skills. No skills were installed. The release API confirmed v1.3.0 and the asset name `EasyBluetoothAudioSetup.exe` for the proposed download CTA.

## Install Marketing Skills And Improve README Conversion

Acceptance criteria: install and apply the recommended product-marketing, copywriting, and cro skills; make the README headline, benefits, and installer CTA clearer using verified product facts; preserve the working GIF and all corrected icon URLs.

- [x] Install the three skills for Codex and read their instructions. Verify: all three installed SKILL.md files and their supporting resources exist.
- [x] Establish product context from source and release evidence, then revise the headline, benefits, and download CTA. Verify: claims match the product, the primary CTA targets the real installer, and the GIF and icon fixes remain intact.
- [x] Review the copy and focused diff. Verify: image/download links, preserved GIF hash, Markdown structure, and git diff --check; document results without modifying concurrent application work.

### Review

- Installed `product-marketing`, `copywriting`, and `cro` from `coreyhaines31/marketingskills` into `C:\Users\gorang\.agents\skills` using the Codex skill-installer helper. Verified each SKILL.md name and the existence of its linked local reference files; read all three skills and the applicable brand-voice and copywriting self-check guidance.
- Created `.agents/product-marketing.md` with versioned context, released-source evidence, known limits, and explicitly labeled audience assumptions. No customer quotes, adoption figures, or conversion gains were invented.
- Kept the product name and layout, clarified the outcome headline, rewrote the four benefits, and changed top/setup/footer download CTAs to the actual `EasyBluetoothAudioSetup.exe` installer. Added runtime/architecture information and corrected the setup label to Connect. Narrowed unsupported call, latency, universal-compatibility, and instant-reconnect claims; corrected the Windows API minimum from 1903 to 2004/build 19041.
- The direct latest-installer URL returned HTTP 200 with `application/octet-stream`; the updated Download for Windows badge returned HTTP 200 with SVG content type. Confirmed the top installer CTA precedes the GIF and all three download CTAs use the same asset URL.
- Verified all 17 corrected Iconify references match the previous fix exactly. The GIF path and SHA256 are unchanged (`B6D5E66ADE37610F74C301FB2A3590C0AD72E0FD8EBB3FD9410512D48A9E969C`). HTML tags are balanced and `git diff --check` passes.
- An independent source-based review confirmed v1.3.0 claim accuracy and caught a release-history link label mismatch, which was fixed. Application builds/tests were unnecessary for these documentation changes. Concurrent application/test work was left untouched. Changes are local and uncommitted; no conversion measurement or live GitHub rendering is claimed.

## Issue #43 Publication

- [x] Prepare an isolated PR branch from origin/main with only the tested issue #43 application, test, and task-record changes. Verify: compare source hashes and inspect the complete PR diff for unrelated README, marketing, instruction, or configuration changes.
- [x] Commit and push the fix, then open a PR with Closes #43, test results, and the remaining physical mixed-DPI and mouse-interaction checks. Verify: attach the PR and confirm its base, head, file list, and closing keyword.

### Issue #43 Publication Review

- Committed as `21954adba96db4858fa458df98590e81d0bc527f` on `codex/fix-issue-43-window-placement` in the attached isolated worktree, based directly on `origin/main`. Application and test files were verified byte-for-byte against the tested source before committing; the pre-commit build passed with 0 warnings and 0 errors.
- Opened [PR #44](https://github.com/GorangN/EasyBluetoothAudio/pull/44). Verified its base is `main`, head is the committed branch, and GitHub's closing-issue references contain #43. The description records 121/121 full-suite tests, 19/19 focused checks, zero-warning builds, and the remaining physical mixed-DPI and mouse/resize checks.
- Verified the complete 14-file PR diff excludes README changes, marketing task notes, and the unrelated instructions/configuration commit. The original checkout and concurrent work remain intact.
- The GitHub plugin could not create the PR (403: Resource not accessible by integration); the already authenticated GitHub CLI created it, and the PR was attached to this chat.

## Publish All Pending Work And Update PR #44

Acceptance criteria: all current tracked and untracked project changes are committed and pushed; existing PR #44 includes the window fix, README/marketing context, and Codex guideline updates; its title and description match the complete diff and verified test results.

- [x] Inspect both branches and pending files, then run the Release build and complete test suite. Verify: no unexpected artifacts, zero build warnings/errors, and all tests pass.
- [x] Commit the pending checkout changes and integrate this branch into the existing PR branch. Verify: preserve every intended file, resolve any task-record overlap without losing entries, and check the final diff.
- [x] Push both updated branches and revise PR #44. Verify: remote heads match local commits, the PR retains its issue #43 closing reference, and the final working trees are clean.

### Publication Review

- Fresh Release solution build passed with 0 warnings and 0 errors; the complete test suite passed 121/121 with no skipped tests, using temporary output under `EasyBluetoothAudio-pr44-publication`.
- Independent read-only review confirmed all 13 application/test files match the existing PR implementation byte-for-byte. The broader scope includes README, product-marketing context, Codex guidelines, three existing Ruflo configuration files, and task records; no unexpected untracked artifacts or credentials were found in the reviewed changes.
- Physical mixed-DPI and actual mouse dragging/resizing remain manual checks, as documented in the original PR.
- Committed the pending work as `ce08ca8` and combined both existing histories in `1db2b9e`. The only merge overlap was the task record; its complete current history was retained. Verified the merged tree exactly matches the tested checkout, then fast-forwarded the PR worktree and pushed both branches atomically without rewriting history.
- Updated PR #44 to cover the complete 20-file diff, including the documentation and existing automation configuration. Verified its base remains `main`, its head matches the pushed PR branch, and its closing references still include issue #43. GitHub's Build & Test workflow was running at this verification.
- The GitHub connector returned HTTP 403 for the metadata update; the authenticated GitHub CLI successfully updated and read back the title and description. Both worktrees were clean after publication; this final task-record update is included in the follow-up publication commit.
