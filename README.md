# <img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/headphones.svg" width="32" height="32" style="vertical-align: middle;" /> EasyBluetoothAudio

**Turn your Windows PC into a high-fidelity Bluetooth Speaker.**

EasyBluetoothAudio is a lightweight Windows utility that enables A2DP Sink functionality. It allows users to stream audio from mobile devices (iOS/Android) directly to their PC's output speakers with low latency. Built with **.NET 10 (Preview)** and **WPF**, adhering to strict Clean Code and MVVM architectural standards.

![Build Status](https://img.shields.io/badge/build-passing-success?style=flat-square&color=CCCF00&labelColor=000000) ![Platform](https://img.shields.io/badge/platform-Windows_10%2F11-blue?style=flat-square&color=B0B0B0&labelColor=000000) ![.NET](https://img.shields.io/badge/.NET-10_Preview-purple?style=flat-square&color=000000&labelColor=CCCF00)

---

## <img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/list-check.svg" width="24" height="24" style="vertical-align: middle;" /> Core Features

* **<img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/bluetooth.svg" width="16" height="16" style="vertical-align: middle;" /> Bluetooth A2DP Sink:** Routes audio from connected smartphones to the default Windows audio output device.
* **<img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/activity.svg" width="16" height="16" style="vertical-align: middle;" /> Low Latency Engine:** Optimized audio buffer (25-50ms) using NAudio/WASAPI to minimize delay and prevent desync.
* **<img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/layout-bottombar.svg" width="16" height="16" style="vertical-align: middle;" /> System Tray Workflow:** Runs silently in the background. The UI appears as a flyout from the notification area (similar to native Windows flyouts).
* **<img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/palette.svg" width="16" height="16" style="vertical-align: middle;" /> Cyberpunk Aesthetic:** Custom High-Contrast Dark Mode (Black/Acid Yellow) for optimal visibility and style.
* **<img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/git-branch.svg" width="16" height="16" style="vertical-align: middle;" /> Smart Versioning:** Automatic semantic versioning based on Git tags via MinVer.

---

## <img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/stack-2.svg" width="24" height="24" style="vertical-align: middle;" /> Technical Stack

This project serves as a reference implementation for modern Windows desktop development.

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Framework** | .NET 10 (Preview) | Bleeding-edge performance and runtime features. |
| **UI System** | WPF | Windows Presentation Foundation with hardware acceleration. |
| **Architecture** | MVVM | Strict Model-View-ViewModel separation. No logic in Code-Behind. |
| **Audio Core** | NAudio | Low-level access to WASAPI Loopback interfaces. |
| **Dependency Injection** | Microsoft.Extensions.DI | Centralized service lifetime management. |
| **Installer** | Inno Setup | Script-based installation for self-contained deployment. |

---

## <img src="https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/terminal-2.svg" width="24" height="24" style="vertical-align: middle;" /> Development

### Prerequisites

* Visual Studio 2022 (Preview) or VS Code.
* .NET 10 SDK installed.

### Build Instructions

The application is configured for **Self-Contained** deployment (no client-side runtime required).

```powershell
# Restore dependencies
dotnet restore

# Build for release (Single File)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
