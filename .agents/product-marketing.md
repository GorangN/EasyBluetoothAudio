# Product Marketing Context

**Document version:** v1
**Last updated:** 2026-10-22
**Basis:** Maintainer brief, repository source at v1.3.0, and the GitHub release. Audience and voice guidance below are working assumptions, not customer research.

## Product Overview

**One-liner:** Play your phone's audio through your PC speakers.
**What it does:** EasyBluetoothAudio receives Bluetooth media audio from a paired phone and plays it through the Windows PC's speakers or headphones. Users install the app, pair their phone with Windows, select it, and connect.
**Category:** Bluetooth audio receiver for Windows; Bluetooth A2DP sink.
**Type and pricing:** Free, open-source Windows desktop utility, licensed under MIT.
**Distribution:** GitHub Releases. The x64 installer is `EasyBluetoothAudioSetup.exe`; the release workflow bundles the .NET runtime. The native playback API requires Windows 10 version 2004/build 19041 or later, including Windows 11.

## Target Audience

People with a Windows PC, a Bluetooth adapter, and a phone who want to listen through the speakers or headphones connected to their PC. The README is the only conversion surface.

**Primary use case:** Keep choosing music, podcasts, and other media on the phone while listening through the PC.
**Other jobs:** Keep the app accessible from the tray; reuse a selected phone and automatic connection settings on later sessions.
**Audience evidence:** Maintainer description and product functionality; no interviews or segmented traffic data supplied.

## Problems And Alternatives

**Problem:** The phone holds the playback controls, while the preferred speakers or headphones are connected to the PC.
**Alternatives:** Listen on the phone, switch the playback app to the PC, or use another audio receiver. These are possible alternatives, not researched competitor comparisons.
**Unverified:** Named competitors, customer frustration quotes, time savings, and reasons users choose this app over alternatives.

## Positioning And Benefits

- Receive phone media audio through native Windows playback APIs without installing extra audio drivers.
- Keep using the apps and playback controls on the phone.
- Keep the desktop clear with tray access to the app.
- Remember the selected device and connect at startup when Auto-Connect is enabled. Auto-Connect defaults to on; launching with Windows is a separate optional setting.
- Download a free installer with the runtime included.

## Objections And Boundaries

| Likely question | Supported answer |
|---|---|
| What do I need? | A Windows PC with a working Bluetooth adapter and a phone paired through Windows Bluetooth settings. The published installer is x64. |
| Do I need to install .NET? | The release workflow produces a self-contained installer with the runtime included. |
| What do I click? | v1.3.0 uses Connect, Reconnect, and Disconnect. The preserved GIF shows an earlier interface with Engage and Abort. |
| Will calls or every phone work? | Neither universal compatibility nor phone-call support is established by the source or supplied evidence. Describe media playback. |

Avoid latency, instant-reconnect, setup-time, and guaranteed connection claims without device-specific measurements or testing. Do not describe the app as requiring no configuration: initial pairing and selection are necessary. No customer objections have been collected; the questions above are editorial assumptions.

## Customer Language And Brand Voice

No verbatim customer research was supplied. Use the maintainer's concrete vocabulary: phone, PC speakers, Bluetooth audio receiver, free, open source.

| Attribute | Use | Avoid | Example |
|---|---|---|---|
| Direct | State the listening outcome first | Category jargon in the headline | Play your phone's audio through your PC speakers. |
| Practical | Name the file or action | Vague next steps | Download for Windows. |
| Accurate | Describe actual setup and controls | Absolute compatibility or reliability promises | Select your phone and click Connect. |

For README copy, keep the tone friendly and concise. Support and setup instructions should use the released app's labels. Technical API names belong in the technical sections.

## Proof Points

- MIT license and inspectable source: `LICENSE`.
- Working demonstration: `ReadMeData/EasyBluetoothAudio.gif`; preserve its file and corrected README image URLs.
- Self-contained x64 publishing and installer filename: `.github/workflows/release.yml` at v1.3.0.
- Connection labels: `EasyBluetoothAudio/Views/BluetoothConfigView.xaml` at v1.3.0.
- Connection settings: `EasyBluetoothAudio/Models/AppSettings.cs` and `EasyBluetoothAudio/ViewModels/MainViewModel.cs` at v1.3.0.
- Native receiver: `EasyBluetoothAudio/Services/AudioService.cs`.
- Release: https://github.com/GorangN/EasyBluetoothAudio/releases/tag/v1.3.0
- API minimum: https://learn.microsoft.com/en-us/uwp/api/windows.media.audio.audioplaybackconnection

No verified testimonials, user counts, latency results, or conversion measurements are available for marketing claims.

## Goals

**Primary:** Help repository visitors download, install, and start listening.
**CTA:** Download for Windows, linking to `https://github.com/GorangN/EasyBluetoothAudio/releases/latest/download/EasyBluetoothAudioSetup.exe`.
**Secondary:** GitHub stars after the visitor understands the product.
**Current metrics and traffic mix:** Unknown. Copy changes are editorial improvements, not a measured conversion lift.

## Learnings

No tested marketing results recorded yet.

## Changelog

- v1 (2026-10-22) — Initial context based on the maintainer brief, released source, and installer evidence for the README headline, benefits, and download CTA.
