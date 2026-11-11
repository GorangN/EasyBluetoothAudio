# Compact Windows interface

The user's supplied screenshot is the visual reference for this WPF application. The repository-root DESIGN.md describes the separate website.

Reading this as a compact Bluetooth audio tray utility for Windows, with olive surfaces and a lime connection action. ENERGY 1 / RHYTHM 1 / MOTION 2.

- The 386 by 278 DIP window keeps the compact reference's body spacing while removing the separate title bar, per the user's latest screenshot correction. Refresh, Settings and X share the top toolbar; X sits directly after Settings. The 44-DIP caption interaction region remains draggable around controls. Minimum size is 320 by 236 DIPs; vertical scrolling preserves access to errors and settings.
- Dark mode uses #111510 for the canvas, #30372C for secondary actions, #282F24 for the device selector, #D8EC44 for the primary action and connected status, and #F0F2EB for text. Light mode retains readable dark foregrounds. High contrast uses Windows system colors.
- Segoe UI matches the reference and the native Windows utility. The Audio sink heading is 15 DIP, device names are 13 DIP, and connection controls are 12 DIP.
- The dropdown selects an existing source view model. Changing selection changes presentation only and leaves the active route unchanged. Explicit Connect or Reconnect performs an exclusive handoff to the selected device; one audio route is active at a time.
- Selected source status appears above full-width Connect or the paired Reconnect/Disconnect actions. Static connection bars indicate an open route; they make no claim about audio playback or measured sound level.
- Add device sits below connection actions and invokes the existing native Windows pairing picker. Device discovery refreshes afterward. Empty, refreshing, unavailable and error states retain actionable text.
- Connection buttons retain 44-DIP hit targets with compact 36-DIP faces. Every action has a real command, accessible name, visible keyboard focus and pointer feedback. Native window dragging and resizing remain available.
- Existing finite midpoint transitions explain the change from Connect to Reconnect/Disconnect; Windows animation preferences disable motion. No decorative looping effects run.
- Existing Bluetooth, refresh, settings, download and close vectors identify actual actions. A phone outline and dropdown chevron identify source selection. No new branding assets or fabricated content are needed.
- Settings and Escape keep their existing context-aware close behavior. The X beside Settings returns from settings before hiding the source panel in the system tray; audio continues in the background.
- Opening Settings grows the same window upward toward an 800-DIP height while preserving its width and bottom anchor. The current monitor's available space limits expansion; scrolling remains available on smaller work areas. Closing Settings restores the prior compact or manually resized dimensions at the current anchor.
- Settings resize uses a finite 240ms strong ease-out transition with the shared (.23,1,.32,1) curve. Every frame updates native bounds atomically around one bottom anchor. Rapid toggles retarget from current native bounds; reduced motion, hidden windows and tray repositioning settle immediately. This preserves spatial consistency while revealing or retracting settings content.
- Window borders and outer shadows are left to Windows through native WPF WindowChrome framing. Custom gradient bands inside settings are removed. This follows [Microsoft's window-style guidance](https://learn.microsoft.com/en-us/windows/apps/get-started/best-practices#window-behavior-and-style) to let the system draw window borders and shadows; actual native shadow appearance follows Windows settings.
