# Multi-device implementation review

Status: implementation complete; automated checks pass. Interactive desktop and physical Bluetooth acceptance remain open. This is not a claim that every release gate passed.

## Verification

- Release build with -warnaserror: zero errors, zero warnings.
- Full Release suite after the visual correction: 152 passed, zero failed, zero skipped.
- git diff --check: passed.
- Native isolation: concurrent routes, scoped teardown/loss, stale opens, cancellation, aggregate cleanup, disposal and discovery failure.
- Lifecycle/coordinator: startup prime, sticky retry, manual fallback, queued callbacks, duplicate loss, independent restoration, settings migration and persistence failures.
- Compiled WPF layout: light/dark/high contrast at four constrained sizes, zero horizontal scrolling, long names/errors, accessible source actions and bundled Geist glyph resolution.
- SkillUI v1.3.4 extraction completed in TEMP. Verified official CSS and the approved Sans brief take precedence over incorrect static Pixel/font and dark-mode guesses.
- Safe in-memory preview loaded the production views and reported a visible window. Computer Use did not return that window as a target, even with standard preview chrome. No desktop click-through is claimed. Preview processes were stopped; the installed app was left running.
- Two physical source devices were not available for controlled overlap testing. Adapter capacity, continuous audible playback while another source reconnects, real picker/UAC interaction and physical mixed-DPI behavior remain unverified.

## Parent Anti Slop review

The core skill's R-35 requires a recorded click-through of every interactive element. That gate remains open because desktop automation could not target the isolated preview. Automated checks below describe their actual scope.

| Rule | Result and evidence |
|---|---|
| R-01 | PASS: monochrome surfaces; no decorative gradients. |
| R-02 | PASS: new product copy uses sentence case and direct action wording. |
| R-03 | PASS automated: source/settings views reflow at 280-DIP width without horizontal scrolling. |
| R-04 | PASS: Bluetooth/settings/refresh/hide glyphs correspond to real actions. |
| R-05 | PASS: repeated source rows intentionally support comparison. |
| R-06 | PASS: locally bundled Geist Sans follows the approved direction. |
| R-07 | PASS: diagonal background removed. |
| R-08 | PASS: no decorative button arrows. |
| R-09 | PASS: no promotional badges. |
| R-10 | PASS: no glass layers. |
| R-11 | PASS: six-DIP component corners; focus treatment remains distinct. |
| R-12 | PASS: per-card shadows removed. |
| R-13 | PASS: neon/glow treatment removed. |
| R-14 | PASS: rows represent independent real sources. |
| R-15 | PASS: Connect, Reconnect, Disconnect, Retry and Add device name actions. |
| R-16 | PASS: factual settings descriptions replace industrial shouting. |
| R-17 | PASS: counts derive from current source states. |
| R-18 | N/A: no testimonials. |
| R-19 | PASS source review: finite action transitions honor Windows animation preference. |
| R-20 | PASS: receiver-specific source controls, tray behavior and existing product icon. |
| R-21 | PASS automated: light/dark resources load; System setting is retained. |
| R-22 | N/A: no new illustrations. |
| R-23 | PASS: owner approved shell, layout, palette and font direction. |
| R-24 | PASS source review: settings, pairing and tray destinations are wired. |
| R-25 | PASS calculated: text/status pairs exceed 4.5:1; enabled control outlines exceed 3:1 through pressed states. |
| R-26 | PASS automated/source: commands covered and controls bound; desktop click-through pending. |
| R-27 | PASS source review: loading, empty, discovery failure, unavailable and per-source error states. |
| R-28 | N/A: no native FAQ. |
| R-29 | PASS: neutral palette with factual semantic status colors. |
| R-30 | PASS: Vercel reference explicitly authorized; application-specific composition retained. |
| R-31 | PASS: native DESIGN.md records purpose for major choices. |
| R-32 | PASS source/runtime: native controls, focus style, names, Escape command and settings focus behavior; manual traversal pending. |
| R-33 | PASS: source authored directly; generated extraction instructions were not executed. |
| R-34 | PASS automated: light/dark/high-contrast views load and reflow. |
| R-35 | OPEN: build/tests pass; targetable desktop window and recorded click-through unavailable. |
| R-36 | PASS: no fabricated performance, capacity or playback claims. |
| R-37 | PASS: ENERGY 2 / RHYTHM 1 / MOTION 2 approved before implementation. |
| R-38 | PASS: mock sources are explicitly labeled Preview; production rows use Windows discovery. |
| R-39 | PASS source/runtime: native controls, 44-DIP actions, accessible names and animation setting; manual validation pending. |
| R-40 | N/A web mechanisms: no HTML, media hero or third-party rendering scripts. |

Purpose review: typography expresses the requested visual language; whitespace separates tasks; repeated rows preserve position; inverse actions establish hierarchy; motion explains the action split.

Liveliness review: explicit dials, a source-list focal point, structural spacing and the coordinated action gesture match the approved direction.

Craftsmanship review: implemented controls have commands and tests, content serves audio management, and claims match available evidence. Full desktop/hardware resilience is not certified by mocked tests.

## Six skill supplements

- antislop: design direction and parent review recorded above; interactive gate explicitly open.
- antislop-code: public contracts retain XML documentation; comments preserve timing/concurrency constraints.
- antislop-copywriting: sentence-case actions and factual state labels; no inferred Playing claim.
- antislop-human: calculated contrast, native semantics, focus resources and accessible action names.
- antislop-layoutmobile: width-triggered stacking and actual narrow WPF layout tests.
- antislop-ui: semantic theme resources, real source counts/states and no decorative activity simulation.

## Supplied-reference visual correction

The user supplied an actual rejected application screenshot. Its invisible primary text, missing font glyphs and split action words demonstrated that the initial layout-only gate was insufficient. A new regression reproduced the label color failure before the fix: #EDEDED rendered where the dark primary button required #0A0A0A. Explicit label foreground/font inheritance and NoWrap now pass in both themes, including actual text bounds in full and constrained views.

The parent inspected true RenderTargetBitmap artifacts of the production shell and settings. Dark/light surfaces now follow the supplied dashboard references; headings use 20/28; controls have compact 36-DIP visuals within real 44-DIP transparent hit areas. Header and source icons use Tabler geometry from the documented pinned source, with the redistributed MIT license.

Animate skill applied: 100ms press, 120ms release, 180ms page/action transitions, explicit keyboard/reduced-motion guards. A full-suite regression exposed composition-idle clock retention; motion now cleans up on either timeline completion or its bounded dispatcher deadline, cancelling obsolete deadlines when interrupted. Four motion tests pass with the complete suite.

Calculated enabled control outline contrast on pressed surfaces is 3.33:1 dark and 3.31:1 light; inverse primary text is 16.91:1 dark and 17.93:1 light. Actual label colors/fonts, single-line bounds and non-empty vector geometry are checked by compiled WPF tests.

The current polished executable is EasyBluetoothAudio/bin/Polished/EasyBluetoothAudio.exe. The earlier running Release executable is not replaced while in use. R-35's desktop click-through remains open; rendered source/settings inspection is now completed and must not be confused with that separate check.

### Physical acceptance

Use two phones/computers with a recorded Windows version and adapter: play both, recycle/disconnect A while B plays, remove A, restart restoration, and exit. Record whether overlapping playback succeeds. Exercise real picker cancellation, UAC cancellation, all tray actions, keyboard focus, 200% text/DPI and mixed-monitor placement. An additional-route failure must leave existing routes intact.

## Reported Mac / iPhone interruption and settings navigation

The supplied hardware screenshot shows a Mac row marked Connected while the iPhone retries. This demonstrates an observed interruption, not a proven adapter capacity limit. The host reports Windows 11 build 26200 and Intel Wireless Bluetooth driver 24.40.11.1. The user reports current stable macOS 27; no direct inspection of that Mac is available.

Verified failures and fixes:

- Native request Success with current state Closed previously returned Connected. A failing regression reproduced this; the service now requires current Opened state and releases a rejected attempt.
- Native enabling is awaited with StartAsync before opening. The extra startup route recycle is skipped for an additional source when another live route is present at either the initial or post-open check.
- A deterministic transport-displacement simulation caused twelve successful alternating reopens without throttling. Automatic loss recovery is now limited to two episodes within a monotonic sixty-second window and three attempts per episode. Success does not reset the interruption budget. Repeated losses pause with scoped manual Connect/Reconnect guidance. Tests do not establish this adapter's capacity.
- Gear-open-then-gear-close and header-X markup regressions failed before the UI change. Gear toggles preferences; header X and Escape close settings before hiding the panel. Preferences persist on return, and the redundant Back button is removed. Actual compiled controls and both themes' settings renders are checked.
- Native stage/state/status/HRESULT traces are persisted locally with hashed source identifiers. Logs rotate at one MiB with one previous file. Diagnostics initialization is idempotent, thread safe, and cannot change audio outcomes on file failures.

Full Release validation after these changes: 174 tests passed, zero skipped/failed, zero build warnings/errors, git diff --check passed. Candidate: EasyBluetoothAudio/bin/ConnectionFix/EasyBluetoothAudio.exe. The currently running Polished build was not stopped or replaced.

The Mac output listing and audible simultaneous playback are still hardware acceptance items. One-process, per-device native ownership follows Microsoft's official example. Additional processes would continue using the system Bluetooth stack and radio; no process-splitting workaround has been established. The current Windows route state does not prove the Mac has selected the PC as its output. New-candidate observations and its local trace are needed to resolve the remaining Mac-specific behavior.

## Pending-peer correction from the hardware trace

The ConnectionFix trace shows both native routes reporting Opened while one OpenAsync task is still pending. A startup-prime disposal follows, with the peer reporting Closed. A guard limited to application-active routes therefore missed a real owned peer. It now counts every other registered native connection, including enabling/opening and Closed objects. The check runs inside the same guarded dispatcher callback as disposal, so a peer registered while the callback was queued also prevents the hidden recycle.

Regressions reproduced the pending-Closed, pending-Opened and queued-peer failures before the fix. The final Release solution build has zero warnings/errors; its no-build test run passes all 181 tests with no failures or skips. The executable is EasyBluetoothAudio/bin/HandshakeFix/EasyBluetoothAudio.exe. The running ConnectionFix process was not stopped or overwritten. git diff --check passes.

An additional failing regression exposed an event-ownership gap: an unrelated adapter sender could publish a loss for the captured route. The service now requires the captured adapter instance. The native callback checks the sender's documented DeviceId against the requested ID case-insensitively, ignores disposed callbacks and catches native callback failures. A real sealed WinRT callback cannot be instantiated in these unit tests; that native identity branch is source-reviewed, while adapter ownership and genuine owned events are exercised by the regression. This correctness fix is not evidence that macOS output registration is repaired.

The supplied macOS audio enumeration contains no PC audio output. Its Bluetooth enumeration lists the connected PC with A2DP/AVRCP, without identifying the cached audio source/sink role. Windows reports Success/Opened without an extended error. These observations do not establish a usable remote audio output or prove the pending-peer fix resolves the Mac issue. No matching warning/error was found in the enabled Windows audio logs during the recorded connection interval; no custom SBC registry override was present.

The user declined source-side developer-tool diagnostics. Normal setup must work through pairing and the operating system's output selector; Swift, additional installations and Terminal commands are not product requirements. Mac output registration and audible two-source playback remain open hardware acceptance checks. The six skill reviews above still apply; R-35 remains open and no remote-output or hardware-success claim is added.

The receiver activation was checked against [Microsoft's guide](https://learn.microsoft.com/en-us/windows/apps/develop/media-playback/enable-remote-audio-playback): selector ID, native creation, awaited enabling, opening, retained ownership and disposal are implemented. No missing activation call or documented Windows macOS-specific mode was found. [Bluetooth SIG assigned numbers](https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Assigned_Numbers/out/en/index-en.html) distinguish the A2DP profile UUID from the AudioSink service-class UUID; a generic A2DP label alone cannot establish the remote service record. This review supplies no evidence for changing the advertised device class, deleting pairings or changing codec values.

## Direct Windows receiver advertisement check

On 2026-11-07 at 22:54 CET, a separate read-only Windows probe queried the local radio's actual SDP database through Microsoft's documented NS_BTH service-discovery API. AudioSink UUID 0x110B is present as record 0x00010004, with speaker SupportedFeatures, L2CAP PSM 0x0019, AVDTP 1.3, A2DP profile 1.3 and PublicBrowseRoot 0x1002. The local radio Class of Device is 0x2E4104, including Rendering and Audio. SDP server record 0 contains VersionNumberList [0x0100] and ServiceDatabaseState 9. Eight local queries succeeded and completed normally. The running app and native connections were not changed.

This rules out an absent local AudioSink record, missing Rendering flag and missing SDP server attributes at measurement time. It does not show the Mac's cached records or prove AVDTP/CoreAudio output enrollment. The probe/evidence remain outside the repository in the local temporary directory. No duplicate SDP registration, device-class override or speculative codec change was introduced.

No current source-backed production fix follows from these measurements. The remaining acceptance observation is ordinary macOS GUI pairing/output selection with the already corrected build; the user was asked for that single GUI result without Terminal or additional software. Mac compatibility remains open until an actual output is observed.
