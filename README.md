# <img src="https://api.iconify.design/tabler:headphones.svg?color=%23E0E0E0" width="32" height="32" style="vertical-align: middle;" /> EasyBluetoothAudio

**Turn your Windows PC into a high-fidelity Bluetooth Speaker.**

EasyBluetoothAudio is a lightweight Windows utility that enables A2DP Sink functionality. It allows users to stream audio from mobile devices (iOS/Android) directly to their PC's output speakers with low latency. Built with **.NET 10** and **WPF**, adhering to strict Clean Code and MVVM architectural standards.

![Build Status](https://img.shields.io/badge/build-passing-success?style=flat-square&color=2ea44f&labelColor=121212) ![Platform](https://img.shields.io/badge/platform-Windows_10%2F11-blue?style=flat-square&color=B0B0B0&labelColor=121212) ![.NET](https://img.shields.io/badge/.NET-10-purple?style=flat-square&color=121212&labelColor=CCCF00)

---

## <img src="https://api.iconify.design/tabler:help-circle.svg?color=%23E0E0E0" width="24" height="24" style="vertical-align: middle;" /> Quick Summary

**Stream music from your phone to your PC.**
EasyBluetoothAudio allows you to hear everything from your smartphone through your computer's speakers or headphones.

* **Target Audience:** Gamers, Developers, and Power-Users who want unified audio.
* **How it works:** Connect your phone via Bluetooth &rarr; Run EasyBluetoothAudio &rarr; Enjoy your mobile audio on your PC.

<p align="center">
  <img src="ReadMeData/EasyBluetoothAudio.gif" alt="EasyBluetoothAudio Demonstration: Clicking Engage to start streaming" width="600" />
</p>

---

## <img src="https://api.iconify.design/tabler:list-check.svg?color=%23E0E0E0" width="24" height="24" style="vertical-align: middle;" /> Core Features

* **<img src="https://api.iconify.design/tabler:bluetooth.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Bluetooth A2DP Sink:** Routes audio from connected smartphones to the default Windows audio output device via the native WinRT `AudioPlaybackConnection` API.
* **<img src="https://api.iconify.design/tabler:refresh.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Smart Auto-Reconnect:** Automatically reconnects on connection loss with an intelligent delay strategy — 5 s settle time after a full disconnect, instant reconnect when the radio link is still intact.
* **<img src="https://api.iconify.design/tabler:layout-bottombar.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> System Tray Workflow:** Runs silently in the background. The UI appears as a flyout from the notification area (similar to native Windows flyouts).
* **<img src="https://api.iconify.design/tabler:device-mobile.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Native Device Picker:** Opens the built-in Windows Bluetooth pairing dialog directly from the app via WinRT `DevicePicker` — no manual detour through Settings required.
* **<img src="https://api.iconify.design/tabler:cpu.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Low-End Hardware Mode:** Reduces the SBC bitpool in the Windows registry (`MaximumBitpool` / `DefaultBitpool` → 15) to stabilise audio on congested or weak radios. Requires admin rights — UAC elevation is handled automatically.
* **<img src="https://api.iconify.design/tabler:cloud-download.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Automatic Updates:** Checks the GitHub Releases API for new stable versions and notifies the user in-app. Pre-releases are skipped automatically.
* **<img src="https://api.iconify.design/tabler:palette.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Dark & Light Themes:** Cyberpunk High-Contrast Dark Mode (Black / Acid Yellow) plus a clean redesigned Light Mode — switchable at runtime.
* **<img src="https://api.iconify.design/tabler:git-branch.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Smart Versioning:** Automatic semantic versioning based on Git tags via MinVer.

---

## <img src="https://api.iconify.design/tabler:settings.svg?color=%23E0E0E0" width="24" height="24" style="vertical-align: middle;" /> Settings

All preferences are persisted across sessions:

| Setting | Description |
| :--- | :--- |
| **Auto-Start** | Launch EasyBluetoothAudio automatically with Windows (registry startup entry). |
| **Auto-Connect** | Reconnect to the last used device immediately on startup. |
| **Theme** | Switch between Dark (Cyberpunk) and Light mode. |
| **Toast Notifications** | Show / hide Windows toast notifications on connection events. |
| **Connection Sound** | Play an audible chime when a device connects successfully. |
| **Low-End Hardware Mode** | Reduce SBC bitpool for congested radios. Requires administrator privileges. |

---

## <img src="https://api.iconify.design/tabler:stack-2.svg?color=%23E0E0E0" width="24" height="24" style="vertical-align: middle;" /> Technical Stack

This project serves as a reference implementation for modern Windows desktop development.

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Framework** | .NET 10 | High-performance runtime features. |
| **UI System** | WPF | Windows Presentation Foundation with hardware acceleration. |
| **Architecture** | MVVM | Strict Model-View-ViewModel separation. No logic in Code-Behind. |
| **Audio Core** | Windows.Media.Audio (WinRT) | Native `AudioPlaybackConnection` API for A2DP Sink. |
| **Messaging** | CommunityToolkit.Mvvm 8.4.0 | Decoupled Messenger / Mediator pattern between components. |
| **Dependency Injection** | Microsoft.Extensions.DI | Centralized service lifetime management. |
| **Installer** | Inno Setup + MSIX | Script-based installer for standalone deployment and packaged distribution. |
