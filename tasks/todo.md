# Zombie-Peak-Refinement

## Publish all pending work to main (2026-11-11)

Acceptance: commit all pending application, tests, documentation, assets and installed repository skills from the active worktree and main checkout; publish the complete result to origin/main without rewriting remote history. Preserve unrelated clean worktrees and ignored build outputs. Validate the actual final source, installer notices and remote head.

- [x] Review the complete tracked/untracked payload and prepare publication. Verify: independent inventory/credential-pattern/resource review found no generated outputs or secrets; installer includes both asset notices; README and DESIGN match the current selector/exclusive-route implementation.
- [x] Commit repository skills and integrate the complete app worktree. Verify: skills commit `9cbc56f` and application commit `199cc8f` both reference this task and passed the existing pre-commit build with zero warnings/errors. Fresh Release build and all 211 tests passed; both active checkouts were clean after integration.
- [x] Push main and verify publication. Verify: GitHub accepted the normal fast-forward push, remote main matched the complete source commit, and the push-triggered Build & Test workflow was identified and checked. This final publication record is committed afterward and its final remote head is checked in the chat.

### Review

- Prepared the complete application worktree and all installed skills from the main checkout. Other inspected worktrees were clean. Preserved all existing source and ignored development builds; no remote history rewrite or release/tag creation is planned.
- Committed repository skills/lockfile as `9cbc56f` on main, with the existing pre-commit build passing with zero warnings/errors; fast-forwarded that commit into the dirty app worktree without changing its pending application files. Application commit will use that same parent so main can fast-forward afterward.
- Corrected stale README/DESIGN descriptions to match the current tested implementation: selection alone leaves the active route unchanged, explicit Connect hands off to one source, and startup restores the last remembered source. Updated compact theme/settings wording. This publication did not change connection behavior.
- Added explicit Tabler MIT and font OFL installer entries. Self-contained win-x64 single-file publish and Inno Setup compilation succeeded; verified published license files and the generated development installer. The installer was built for verification, not run or published as a release.
- Fresh Release solution build passed with zero warnings/errors and the full suite passed 211/211, no skips. Independent review found no remaining publication blocker. Application whitespace check passes apart from the preserved upstream OFL text; installed upstream skill reference files also retain their original whitespace.
- Committed the complete application/source payload as `199cc8f22a8e60ecc465b28003fd0ae1ab4eb5a9`, then fast-forwarded the main checkout to that commit. Normal push advanced origin/main from `d3cdff5` to `199cc8f`; `git ls-remote` verified the exact matching SHA. Both the main checkout and application worktree were clean at that point.
- GitHub launched [Build & Test run 42938141630](https://github.com/GorangN/EasyBluetoothAudio/actions/runs/42938141630) for the published source commit; its first checked status was in progress. Local Release, 211-test and installer results above remain the completed validation evidence until the hosted run finishes. No release, tag, installer execution or history rewrite was performed.

## Animate Settings resizing and remove the title bar (2026-11-11)

Acceptance: Settings expands/collapses smoothly with an unchanged native bottom edge throughout. Rapid reversal continues from the current size; reduced motion and hidden/tray repositioning settle immediately. Remove the screenshot's separate app-name/minimize/close bar and place the sole X immediately after Settings in the content toolbar. Native dragging, resizing, shadow, page-aware X/Escape behavior and independent connections remain usable.

- [x] Add interruptible bottom-anchored native resize motion. Verify: pure eased interpolation and a real visible, non-activating HWND test cover intermediate/final fixed-bottom frames, rapid reversal, animate:false, hide/close cleanup and existing hidden/manual-size restoration.
- [x] Remove the title bar and move X beside Settings. Verify: compact 386x278/minimum 320x236 renders, sole X placement/command, native chrome hit-testing, icon bounds and heading spacing pass, including a visible update action at minimum width.
- [x] Verify final behavior and candidate. Verify: all 211 tests pass, zero skips; fresh Release solution/candidate builds have zero warnings/errors. Three-theme PNGs and scoped diff checked; existing running ExpandedSettings executable left intact.

Motion decision: occasional Settings transition; spatial consistency and preventing a jarring size change. Reuse the existing strong ease-out curve (.23,1,.32,1), 240ms (drawer range, below300ms), with frame-paced atomic native rectangle updates. The actual HWND must resize to expose more content, so geometric native resize is required rather than a content-only transform. Windows reduced-motion preference disables size motion. Existing page/action animation remains intact.

### Review

- Native Settings resize runs 240ms using frame-paced atomic SetWindowPos updates and the existing shared, frozen (.23,1,.32,1) ease-out spline. Every rounded intermediate frame derives Top from one fixed Bottom. Compact DIP size and requested expanded state are kept separate so rapid reversals retain the original compact dimensions and start from measured current native bounds.
- CompositionTarget.Rendering drives frames with an elapsed-time clock; a finite completion guard settles idle rendering. Completion, replacement, hiding and closing unsubscribe handlers and stop the guard. Windows disabled motion, invisible windows and explicit lifecycle calls snap immediately. Tray hide/reposition/reopen is explicitly non-animated.
- Removed the app-name/Bluetooth/minimize/separate-close bar. The single X now follows Settings in the shared toolbar and retains CloseOrHideCommand, accessible labels, focus indication and chrome hit-testing. Caption dragging, native resize edges and system-managed frame/shadow remain configured. Updated only relevant presentation, placement, shared easing, tests and design/task notes.
- Verification: 211/211 full-suite tests passed with no skips. Fresh Release solution build and `EasyBluetoothAudio/bin/AnimatedSettings/EasyBluetoothAudio.exe` candidate build passed with 0 warnings/0 errors. Native tests observe intermediate heights and exact bottom-edge coordinates across reversals, plus final dimensions and handler cleanup. A style warning found on an expression-bodied diagnostic property was corrected with the repository's block getter style.
- Three-theme compact/expanded/update-visible renders are under `C:/Users/gorang/AppData/Local/Temp/eba-animated-settings`. Inspected dark at 386px, light at 320px with update, and expanded dark at 800px height: no title strip, sole X next to Settings, readable icons and no heading overlap. Preview data remains explicitly named Preview. `git diff --check` passes.
- Motion gate: occasional interaction, spatial consistency/preventing a jarring change; shared ease-out, 240ms; finite native geometry rather than scaling the page, because the real window must expose additional settings content. Reduced motion and interruption behavior are covered. R-26/R-32/R-34/R-35/R-39 checks pass within the tested scope through command/focus assertions, all-theme renders, actual HWND animation frames, build and full suite. Feel on physical displays with differing refresh rates was not manually assessed; no such claim is made.

## Expand Settings upward and use Microsoft window shadows (2026-11-10)

Acceptance: opening Settings grows the current window upward toward an 800-DIP preferred height, preserving the bottom edge and width while respecting the current monitor's working area and DPI. Closing Settings restores its prior compact/user-resized size at the current anchor. Repeated toggles, tray hide/reopen and moved windows remain stable. Replace the custom dark scroll-shadow bands with system-managed window framing/shadows, following Microsoft's guidance; scrolling stays available when space is limited.

- [x] Cover the geometry contract. Verify: pure native-coordinate cases and real hidden-window tests cover upward growth, negative monitor origins, 100/125/150/200% scale, work-area limits, repeat toggles, prior manual sizing, restored dimensions and a moved anchor.
- [x] Wire page changes to monitor-aware placement and remove custom scroll shadows. Verify: IsSettingsOpen changes call the existing placement-service interface; toggle, page close, Escape and deactivation share restoration, while tray hide/reopen collapses/repositions/re-expands as needed. View code-behind is unchanged; WindowChrome uses a one-DIP native frame.
- [x] Verify and build a runnable candidate. Verify: all 204 tests pass; final candidate build has zero warnings/errors; compact/expanded renders in all three themes pass, and the scoped diff is clean.

Design basis: Microsoft recommends system-drawn window borders/shadows and retaining scroll access at constrained sizes: https://learn.microsoft.com/en-us/windows/apps/get-started/best-practices#window-behavior-and-style . Fluent elevation denotes separation between surfaces: https://fluent2.microsoft.design/elevation . WPF glass-frame configuration: https://learn.microsoft.com/en-us/dotnet/api/system.windows.shell.windowchrome.glassframethickness . The requested upward expansion is the user's interaction direction, not a Microsoft-prescribed resize pattern.

### Review

- Added a small reversible native-coordinate resize operation to the existing placement service/interface. It snapshots prior size in DIPs, preserves the current native left/bottom anchor, uses the window's actual monitor and DPI, and caps upward growth to available space. Existing 800-DIP-or-taller user sizes do not shrink when opening settings; repeated calls do not overwrite the compact snapshot. Native resizing uses no-activate/no-z-order flags.
- App composition observes the existing ViewModel page state. All existing settings close paths restore size; hiding/restoring through the tray preserves page state and handles repositioning before expansion. No connection logic or ViewModel changes were required.
- Removed the previous scroll-edge overlays, their converter and all three obsolete shadow resources. Enabled the native glass frame via `WindowChrome.GlassFrameThickness=1` with no custom window-region corner radius; Windows manages external framing/shadows according to its settings. No manually painted content shadows remain.
- Validation: full Release suite 204/204 passed, zero skips. Native hidden-window tests preserve visibility/activation while verifying repeated expansion, exact compact restoration and moved anchors. Compiled views verify more settings are visible at expanded height, and constrained viewports still scroll to the end. Expanded and compact theme PNGs are under `C:/Users/gorang/AppData/Local/Temp/eba-expanded-settings`; dark/light expanded renders were visually inspected. Native external shadow appearance and physical monitor transitions were not manually screen-tested.
- Runnable candidate: `EasyBluetoothAudio/bin/ExpandedSettings/EasyBluetoothAudio.exe`, built with zero warnings/errors in a separate directory because CompactDesignScroll is currently running. The running process was left intact. `git diff --check` passes; prior unrelated work is retained.
- Delivery checks PASS for this scoped implementation: Microsoft's native-border/shadow guidance is cited above and in DESIGN.md; R-12 has a system-elevation purpose, R-26/R-32 existing commands/focus remain covered, R-34 all three themes render, and R-35 is backed by the build, full suite, native HWND geometry checks and compiled UI renders. No physical click-through or external-shadow screenshot is claimed.

## Settings scroll shadows (2026-11-10)

Acceptance: subtle shadows at the top/bottom of the settings viewport indicate hidden content in that direction. At the top only the bottom shadow appears, between ends both appear, at the bottom only the top appears, and neither appears when all settings fit. Shadows follow scrolling/resizing, respect all themes and do not intercept pointer input.

- [x] Cover directional scroll affordances. Verify: the compiled-view regression first failed on missing shadow elements, then passed start/middle/end/back-to-top/no-overflow states in all three themes.
- [x] Add settings-only shadow overlays tied to scroll position. Verify: a view-only converter drives two pointer-transparent overlays; no ViewModel or code-behind changes.
- [x] Verify and rebuild the candidate. Verify: compiled-view checks and three-theme renders pass; final Release build has zero warnings/errors. The existing CompactDesign executable is now running and locked, so the updated runnable candidate is built separately in CompactDesignScroll.

### Review

- Added 20-DIP fade shadows within the settings viewport. Each shadow appears only while content remains beyond that edge; both disappear when resizing removes overflow. They avoid the scrollbar and do not intercept clicks or scrolling.
- Dark/light strengths come from theme resources; high contrast follows the Windows text color. The design document records their purpose as scroll affordances. Existing controls, command bindings and connection logic are unchanged.
- Verification: all 185 tests pass with zero skips. The compiled-view regression passes in Dark, Light and HighContrast, including returning to the top and enlarging the viewport. Inspected rendered settings screenshots in all three themes under `C:/Users/gorang/AppData/Local/Temp/eba-settings-scroll-shadows`. Independent read-only review found no issue. `git diff --check` passes.
- Final runnable build: `EasyBluetoothAudio/bin/CompactDesignScroll/EasyBluetoothAudio.exe`, zero warnings/errors. The attempt to rebuild CompactDesign found the currently running executable locked; building into the separate directory resolved it without stopping the running app.
- Delivery checks PASS for this change: R-12 shadow purpose recorded; R-26/R-32/R-39 input/focus unaffected because overlays are non-interactive; R-34 all three themes construct/render; R-35 build and compiled UI states verified. Native manual control and hardware testing were unnecessary for this visual-only change; no such test or live restart is claimed.

## Restore the reference UI in the running multi-device version (2026-11-10)

Acceptance: restore the compact olive/charcoal Windows design with a device dropdown, selected device status and Connect or Reconnect/Disconnect, then `Add device` below. Selection changes presentation only; independent routes, restoration, tray actions and settings remain intact. Work in the explicitly selected `0d96` worktree and preserve its preexisting changes.

- [x] Identify the correct target and preserve its existing implementation. Verify: user confirmed the Windows app and running multi-device worktree; pre-edit UI snapshots saved outside the repository.
- [x] Restore the reference layout with stable source selection. Verify: choices reference existing source VMs, switching leaves all routes intact, and Add device invokes the existing native pairing command.
- [x] Validate compiled views and candidate automatically. Verify: zero-warning build, all 185 regressions, three themes, 24 rendered views, actual compiled command bindings and icon bounds pass. Manual computer use was stopped by the user with Escape; no live pairing, hardware click-through or candidate restart is claimed.

Design read: compact Bluetooth audio tray utility following the user's olive/charcoal reference. ENERGY 1 / RHYTHM 1 / MOTION 2. Segoe UI matches native Windows; #111510 window, #30372C title/secondary action, #282F24 selector and #D8EC44 primary action restore the supplied identity. 24px insets group the controls. A 44px hit target contains a 36px button face. Existing finite shared-midpoint transitions explain connection changes and respect Windows animation preferences. Static connected bars represent an open route, not an audio meter. Light status text retains readable contrast; high contrast uses system colors.

### Review

- Changed only the selected worktree's presentation, semantic colors, design direction, minimal source selection and relevant tests. The initial main-checkout attempt was fully removed; `C:/dev/EasyBluetoothAudio` has no tracked/staged diff. The target's existing multi-device hardware/lifecycle implementation and unrelated uncommitted work were retained.
- Restored the compact 386x322 window, olive title bar, dark source selector, lime action, connected status/bars and bottom Add device. Existing source VMs remain stable; four new tests prove selection preservation, discovery/removal fallback and switching between two live routes without connect/disconnect, cancellation or settings writes.
- Release solution build: zero warnings/errors. Final full suite: 185/185 passed, zero skips. Runnable candidate: `EasyBluetoothAudio/bin/CompactDesign/EasyBluetoothAudio.exe`, built separately with zero warnings/errors. Existing running app was not replaced by an untested binary.
- Compiled-view tests cover empty, connected, ready, failed and long-error states; all three themes; narrow layouts; Add device below source actions; full-width controls; focus/tab eligibility; selection bindings; page-aware settings close; and vector icons fitting their actual presenters. Corrected a runtime dictionary-scope error and clipped small toolbar icons found during verification.
- Visual evidence: 24 PNG renders under `C:/Users/gorang/AppData/Local/Temp/eba-compact-renders`, including `sources-DarkTheme-386.png`, light/high-contrast equivalents, minimum-width, settings and each selected source. Preview names explicitly identify test data. Default connected view needs no scrolling; errors wrap and scroll to Add device.

Delivery checks:

- R-02/R-03/R-17/R-18/R-23/R-24/R-27/R-28/R-36/R-37/R-38/R-40 PASS within this desktop change: supplied direction followed, no fabricated claims/assets/navigation or media, real empty/loading/error states, vertical scrolling and finite motion retained.
- R-25/R-34 PASS: dark primary text 16.34:1, secondary text 10.06:1, connected status 14.05:1, primary action label 13.59:1 and required dark control outline 3.86:1; light mode retains dark readable status; all three theme resources construct and render correctly.
- R-26/R-32/R-39 PASS for automated coverage: selector binds existing source identities; Connect/Reconnect/Disconnect bind each selected route's commands; Add device binds native pairing; refresh/settings/caption bind existing commands; model tests exercise lifecycle, pairing, cancellation and close/toggle behavior. Hidden actions leave tab navigation; focus resources, accessible names, real control bounds and pointer/disabled styles remain present.
- R-35 build/render PASS; manual click-through incomplete: the user stopped Computer Use with physical Escape. No further Computer Use calls, real-device connection changes or candidate restart were attempted afterward. Exit the running tray app and launch the candidate to use the updated UI.
- Purpose/Liveliness/Quality PASS for the reviewed implementation: the application DESIGN.md records palette, typography, spacing, hierarchy, vectors, motion purpose and ENERGY 1 / RHYTHM 1 / MOTION 2; reference screenshots and compiled control assertions support those choices. Native resizing/caption and existing independent route behavior remain covered. `git diff --check` passes.

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

## Multi-device audio and Geist native redesign
- [x] Add device-scoped native ownership and regression tests -> verify isolation and stale-result cleanup.
- [x] Extract per-device lifecycle -> verify recovery, cancellation, and startup priming.
- [x] Add restoration migration and aggregate controls -> verify independent startup and saved preferences.
- [x] Build Geist source list/settings/tray -> verify responsive layout, themes, and keyboard access.
- [x] Run Release warnings-as-errors build and complete tests -> zero warnings/errors, 148 tests passed.
- [x] Record hardware validation status and review final diff -> see tasks/multi-device-review.md; diff check passed.
- [ ] Complete interactive desktop and two-source physical playback acceptance -> automation could not target the isolated preview; two controlled source devices unavailable.

### Review

- Native routes, recovery, cancellation, startup restoration and tray actions are device scoped.
- Settings migration distinguishes missing/null restoration lists from an explicit empty list. Preference-write failure does not change route truth or prevent aggregate teardown.
- Geist fonts are bundled with their license; light/dark/high-contrast resources and constrained source/settings layouts are covered by compiled WPF tests.
- Automated verification is complete. The parent Anti Slop report records the open interactive/hardware gate without claiming a click-through or audible mixing result.

## Visual correction from supplied Vercel references
- [x] Reproduce invisible labels and wrapping controls with rendered regression tests -> original dark primary label rendered #EDEDED instead of owning button foreground #0A0A0A.
- [x] Apply reference density/tonality and replace font glyphs with Tabler vectors -> explicit label inheritance, single-line controls, compact visual buttons and licensed Tabler paths.
- [x] Add finite mouse feedback and page/action transitions, honoring reduced motion -> interrupted clocks have both completion and dispatcher-deadline cleanup.
- [x] Inspect real WPF dark/light renders and complete Release verification -> parent inspected exported production-view renders; 152 tests passed; polished app built with zero warnings/errors.

### Visual correction review

- Build for this correction: EasyBluetoothAudio/bin/Polished/EasyBluetoothAudio.exe. The running instance and active audio were not stopped or overwritten.
- Real WPF render exports cover dark/light 600-DIP and 420-DIP shells, 280-DIP source views and settings. They use explicitly labeled sample devices, not hardware playback claims.
- New visual checks inspect the actual generated button label foreground, font, one-line text bounds and vector header paths. The prior layout-only tests were insufficient and the rejected appearance is recorded as a regression.
- Desktop interaction and physical two-source playback remain separate manual checks. Offscreen render inspection is complete; it is not described as a native desktop click-through.

## Mac audio / multi-source recovery and settings navigation
- [x] Reproduce false-connected native state, recovery ping-pong, and settings-toggle/header-X failures -> failing regressions established before fixes.
- [x] Await native registration, verify opened state, and bound automatic recovery without disrupting other routes -> native state/enable and contention regression suites pass.
- [x] Make gear/X/Escape close settings consistently and preserve preferences -> command, markup and compiled-view action checks pass.
- [x] Record diagnosable native status/HRESULT evidence and Mac setup limitations -> rotated local traces contain stage, native state, HRESULT and hashed source tags; README describes source/output distinction.
- [x] Run full Release regression checks, render navigation states, and build an unlocked candidate -> 174 tests pass; zero warnings/errors; bin/ConnectionFix candidate built; settings render inspected.
- [ ] Verify the Mac output listing and actual two-source playback with the user's hardware -> candidate shared; direct Mac observations and new trace pending.

### Connection and settings review

- One process remains the architecture, with independent native connection objects. Microsoft documents this ownership model; a separate process does not provide a separate Windows Bluetooth stack or radio.
- Native open request success is checked against current opened state. Native registration is awaited. Extra startup priming is skipped for additional sources when a peer is already open.
- Automatic loss recovery is limited to two episodes in a monotonic 60-second window, with three opens per episode; repeated interruptions fall back to explicit Connect/Reconnect. A simulated one-transport conflict proves that indefinite successful takeover loops stop. The simulation is not a diagnosis of this particular adapter.
- Header X and Escape close preferences before hiding the tray panel. The gear toggles, preferences save on return, and the redundant Back button is removed. Both themes' actual view actions and render artifacts were checked.
- Mac listing success remains a hardware acceptance item. The app cannot inspect the Mac's audio-output list from Windows and does not equate Windows Opened with remote output selection.

## Mac output follow-up from the live trace
- [x] Correlate current process and native trace -> ConnectionFix running; Windows reports actual Mac Success/Opened without HRESULT errors; iPhone and Mac opens overlap.
- [x] Reproduce pending-peer startup recycle -> both pending-Closed and pending-Opened cases failed before the fix.
- [x] Guard against all registered native peers -> opening/enabling peers now prevent startup recycling; no artificial OS capacity claim.
- [x] Verify the dispatcher-side peer guard and run final Release regressions -> queued-peer regression failed before the fix; full suite 180/180 passes with zero warnings/errors.
- [x] Build a new candidate without replacing the running app -> bin/HandshakeFix/EasyBluetoothAudio.exe.
- [x] Inspect macOS audio enumeration -> user's SPAudioDataType confirms no PC Bluetooth output; SPBluetoothDataType shows connected PC with generic A2DP/AVRCP services.
- [ ] Verify ordinary Mac pairing creates a usable output -> user declined developer-tool diagnostics; investigate the supported Windows receiver lifecycle using existing traces and primary documentation. Generic A2DP services do not distinguish the cached source/sink roles.

### Current acceptance boundary

- [x] Validate native/adapter event-sender ownership explicitly -> foreign-sender loss regression failed before the fix; unrelated events are ignored and genuine owned events remain covered. Final solution build has zero warnings/errors, all 181 tests pass, and the HandshakeFix executable has been rebuilt.
- The pending-peer/startup-prime race is reproduced and fixed, including a peer registered before a queued dispatcher callback. The HandshakeFix executable exists in its separate output directory; the running ConnectionFix process was left intact.
- macOS reports a Bluetooth connection and generic A2DP/AVRCP services for the PC, but no PC audio output. Neither that service mask nor the Windows Opened state proves remote audio-output registration. The normal pairing/output flow remains unverified; no further Mac Terminal steps are required from the user.
- Primary-source review confirmed the implemented selector/create/StartAsync/OpenAsync/retain/dispose flow follows Microsoft's receiver guide. No missing activation call or documented macOS-specific Windows switch was found. Changing the radio's advertised class, deleting pairing data, or changing codecs without evidence is not a supported fix.

## Mac output registration through ordinary Bluetooth pairing

Acceptance: with EasyBluetoothAudio running, ordinary Bluetooth pairing/connection exposes this PC under macOS Sound → Output, without a Mac toolchain, Terminal commands or additional software. Windows Opened and mock tests alone cannot satisfy this acceptance.

- [x] Establish Windows-side receiver advertisement and registration evidence -> read-only local NS_BTH queries confirm AudioSink 0x110B, L2CAP/AVDTP/A2DP descriptors, Rendering/Audio CoD flags and SDP ServiceDatabaseState. Existing app/routes were left intact; evidence retained outside the repository.
- [ ] Identify and reproduce a supported application-side cause -> compare the evidence with actual A2DP/SDP requirements; add a failing regression before changing applicable lifecycle logic. Do not substitute speculative device-class or codec registry edits.
- [ ] Implement the smallest supported fix and prepare the runnable candidate -> Release build with zero warnings/errors, relevant/full regressions, and clean diff check.
- [ ] Verify ordinary Mac output selection through GUI only -> require observed PC output registration; document precisely if the environment cannot establish or repair the cause.

### Windows receiver evidence

- Local fresh SDP query at 2026-11-07 22:54 CET: AudioSink record 0x00010004 with service class 0x110B, L2CAP PSM 0x0019, AVDTP 1.3, A2DP profile 1.3, speaker SupportedFeatures and PublicBrowseRoot 0x1002. Radio CoD 0x2E4104 includes Rendering and Audio.
- SDP server record 0 has VersionNumberList [0x0100] and ServiceDatabaseState 9. Sink and source roles are enabled in the Windows registry; the relevant drivers/services are present and running. No changes to that configuration were made.
- Missing current Windows sink registration is ruled out. What the Mac cached and whether its AVDTP/CoreAudio connection is established remain unobserved. A single normal GUI re-pair with the corrected candidate was requested; no Mac Terminal or software is involved.
- User completed the normal GUI test: PC still absent. HandshakeFix is now the running executable (PID 54904); its startup trace contains no hidden prime teardown, but again reports both routes Opened while only the iPhone has an active Windows capture endpoint. Fresh pairing did not repair the observed Mac output failure; do not repeat it as the remedy.

### Next isolation step

- [ ] Separate the Mac-only route from the still-restored iPhone route using ordinary app controls, then restore prior connection intent -> verify whether a per-Mac Windows audio endpoint is ever created and correlate native state without treating a shared Opened state as success.

## Restore one-device connection limit while preserving the current UI

Acceptance: only one source may own an active or pending audio route. Connecting another source cancels the old source's connect/recovery intent, releases its native route, and switches automatically. Startup restores exactly one remembered source. This task makes no changes to existing UI files, themes, assets, or animations.

- [x] Reproduce concurrent-device behavior and capture the existing UI -> verify focused exclusive-switch regressions fail before the fix and retain source/UI hashes.
- [x] Enforce exclusive native ownership and cancel previous source operations on handoff; restore/save one device -> verify connected, pending, cancellation, recovery and stale completion paths.
- [x] Run focused and complete tests plus a Release build -> verify zero warnings/errors, review only this task's delta against the existing uncommitted work, and compare every UI hash.

### Review

- The chat worktree was an older clean checkout. The newer multi-device implementation and user-approved UI were found in the existing 0d96 worktree; changes are applied there without importing or overwriting its unrelated work.
- Baseline newer-worktree suite passed 185/185. Before implementation, 11 native ownership regressions and 7 coordinator regressions failed as expected. Handoff now invalidates all earlier device generations and serializes native release before replacement; the coordinator cancels the old source's retries, monitor and recoverable intent before the new source opens. Startup and successful persistence retain one restore target.
- Independent review caught a startup-prime ordering regression during switches. Reproduced it with real service/controller integration, then moved the priming decision after peer cancellation. Connected and pending handoffs now preserve the existing startup-prime safeguard; stale callbacks cannot reclaim the old route.
- Final full suite passed 204/204, with no skipped tests. Release solution build with warnings treated as errors passed with 0 warnings and 0 errors. The separate runnable output is `C:\Users\gorang\AppData\Local\Temp\EBA-single-device-final\bin\EasyBluetoothAudio\release\EasyBluetoothAudio.exe`; the running app was left intact. Hardware handoff has not been exercised.
- This task changed only five connection/service source files, four connection test files and the task records. Before unrelated concurrent settings-window work started, all 31 captured UI/setup hashes matched the baseline. The active "Altes Design wiederherstellen" chat subsequently changed some UI/window files; those edits were preserved and were included in the successful final build/tests. No UI file was edited by this task.
- Source snapshots and hashes are retained under `C:\Users\gorang\AppData\Local\Temp\EBA-single-device-20261110`. Git whitespace checks passed; changes remain local and uncommitted in the existing 0d96 worktree.

## Keep update status visible and tray menu text readable

Acceptance: UpdateStatusText stays below the source ScrollViewer at unchanged window dimensions; native tray menu labels remain readable in all app themes.

- [x] Reproduce both defects with focused WPF layout/render checks.
- [x] Dock update status below the scrolling content and restore native menu text colors within the tray menu.
- [x] Run relevant tests and a Release build with warnings as errors; review rendered artifacts and final diff.
- [x] Disable manual resizing/maximizing at the unchanged dimensions -> verify native resize mode and custom chrome resize border in the WPF regression.

### Review

- Reproduced footer containment in the ScrollViewer, menu text escaping native system colors, and the enabled manual resize mode before their respective fixes.
- Footer stays below the viewport at narrow/normal compact sizes, wraps, and collapses when empty. Window remains 386 x 278; NoResize and a zero chrome resize border disable user resizing/maximizing. Existing programmatic settings expansion is preserved.
- Tray menu keeps native Windows styling; explicit header foreground bindings prevent the application TextBlock style from overriding menu colors. Dynamic device entries and disabled-state foreground propagation are covered.
- Final Release suite: 211/211 passing. Solution build with warnings as errors: 0 warnings, 0 errors. Whitespace check passed. Reviewed rendered dark footer and tray menu artifacts under C:/Users/gorang/AppData/Local/Temp/EBA-footer-menu-review.
- Running installed Test Installer v1.2.15 was left intact. Runnable updated build: EasyBluetoothAudio/bin/Release/net10.0-windows10.0.19041.0/EasyBluetoothAudio.exe in this checkout.

## Main view without scrolling, errors in the footer

Acceptance: the fixed main view has no scrolling viewport; source and discovery errors appear in its bottom footer, with device actions and Add device visible at 386 x 278. Settings and selector dropdown keep their existing scrolling.

- [x] Reproduce main-view overflow/error placement with a focused layout regression.
- [x] Move notifications to one bounded footer and remove main-view scrolling, adjusting only necessary spacing.
- [x] Verify error, update, refresh and empty states; inspect rendered output and run Release tests/build.

### Review

- The focused regression failed against the main ScrollViewer before implementation. Removed that viewport entirely; selector popup and Settings scrolling remain intact.
- Source errors, discovery errors, refresh status, offline guidance and update text now share one bottom footer. Errors take priority. The footer stays one line, trims long text and exposes the complete message as a tooltip; empty status collapses.
- Reduced heading/status spacing and removed the Add device top margin so all actions fit above the footer at the unchanged 386 x 278 window size. Manual resizing/maximizing stays disabled.
- Verified selection changes and notification priority with real WPF bindings, button bounds, and empty-footer collapse in Light, Dark and High Contrast themes. Reviewed dark error/update PNGs in C:/Users/gorang/AppData/Local/Temp/EBA-no-scroll-footer-review.
- Full suite: 211/211 passing. Release solution build with warnings as errors: zero warnings/errors. Git whitespace check passed. Running installed app was left intact; updated runnable executable remains in this checkout’s Release output.

## Fix native window corners after the ineffective radius-only change

Acceptance: all four corners of the native window are outside its region, while the center and straight edges remain inside. Preserve compact dimensions and the smaller footer.

- [x] Reproduce the native corner region failure with the production MainWindow.
- [x] Disable the glass frame so WindowChrome applies its existing 12-DIP corner radius.
- [x] Verify native region before/after sizing, run Release checks, and prepare the updated runnable build.

### Review

- Microsoft documents that WindowChrome.CornerRadius is ignored under DWM unless GlassFrameThickness is zero. The prior radius-only fix retained GlassFrameThickness=1 and could not remove the rectangular corners.
- Reproduced against the production MainWindow HWND: GetWindowRgn returned zero before the fix. With GlassFrameThickness=0, all four corner pixels are excluded and the center/top midpoint are included. Repeated after native height expansion and restoration to cover settings resizing, in all three application themes.
- Full Release suite passed 211/211. Solution build with warnings as errors passed with 0 warnings and 0 errors; whitespace check passed. No hardware route or installed process was restarted. Candidate: C:/dev/EasyBluetoothAudio/EasyBluetoothAudio/bin/Release/net10.0-windows10.0.19041.0/EasyBluetoothAudio.exe.
