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

---

## <img src="https://api.iconify.design/tabler:list-check.svg?color=%23E0E0E0" width="24" height="24" style="vertical-align: middle;" /> Core Features

* **<img src="https://api.iconify.design/tabler:bluetooth.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Bluetooth A2DP Sink:** Routes audio from connected smartphones to the default Windows audio output device.
* **<img src="https://api.iconify.design/tabler:activity.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Low Latency Engine:** Optimized audio buffer (25-50ms) using NAudio/WASAPI to minimize delay and prevent desync.
* **<img src="https://api.iconify.design/tabler:layout-bottombar.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> System Tray Workflow:** Runs silently in the background. The UI appears as a flyout from the notification area (similar to native Windows flyouts).
* **<img src="https://api.iconify.design/tabler:palette.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Cyberpunk Aesthetic:** Custom High-Contrast Dark Mode (Black/Acid Yellow) for optimal visibility and style.
* **<img src="https://api.iconify.design/tabler:git-branch.svg?color=%23E0E0E0" width="16" height="16" style="vertical-align: middle;" /> Smart Versioning:** Automatic semantic versioning based on Git tags via MinVer.

---

## <img src="https://api.iconify.design/tabler:stack-2.svg?color=%23E0E0E0" width="24" height="24" style="vertical-align: middle;" /> Technical Stack

This project serves as a reference implementation for modern Windows desktop development.

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Framework** | .NET 10 | High-performance runtime features. |
| **UI System** | WPF | Windows Presentation Foundation with hardware acceleration. |
| **Architecture** | MVVM | Strict Model-View-ViewModel separation. No logic in Code-Behind. |
| **Audio Core** | NAudio | Low-level access to WASAPI Loopback interfaces. |
| **Dependency Injection** | Microsoft.Extensions.DI | Centralized service lifetime management. |
| **Installer** | Inno Setup | Script-based installation for self-contained deployment. |
