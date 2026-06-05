# GamerGuardian Wiki

A Windows 11 tray app that watches gaming-related display and system settings, alerts you (or auto-fixes) when they drift from your preferences, and stays out of the way during gameplay and benchmarks.

## What it looks like

Settings is split into nine tabs. See the **[Settings & tabs guide](Settings-and-tabs)** for what every tab and setting does and means.

| Tab | Covers |
|---|---|
| **General** | Theme, launch-at-startup, polling interval, update check, change log, one-click Recommended setup |
| **Global gaming** | Game Mode, Game DVR, HAGS, Memory Integrity/VBS, MMCSS, USB Selective Suspend, VRR, Fast Startup, Visual effects, power plan |
| **Privacy** | Advertising ID, Tailored experiences, Cross-Device Platform, Activity History |
| **Network** | Network Throttling, Nagle's algorithm, NIC power management |
| **Windows services** | Curated catalog with one-click Gaming-optimized preset, per-service Default/Manual/Disabled |
| **Windows AI** | Policy disables for Copilot, Recall, Click-to-Do, Edge AI, Notepad/Paint AI, search AI, AI Actions, Office Copilot + optional AI UWP removal |
| **Display** | Per-display HDR, Dynamic Refresh Rate (DRR), refresh rate, resolution |
| **CPU / Power** | Detected CPU, Power Throttling, CPU-aware gaming power plan, dual-CCD routing dependencies |
| **Recommended BIOS** | Firmware checklist (guidance only) |

Screenshots: [General](https://raw.githubusercontent.com/carterscode/GamerGuardian/main/docs/screenshots/settings-general.png) · [Global gaming](https://raw.githubusercontent.com/carterscode/GamerGuardian/main/docs/screenshots/settings-global-gaming.png) · [Windows services](https://raw.githubusercontent.com/carterscode/GamerGuardian/main/docs/screenshots/settings-windows-services.png) · [Display](https://raw.githubusercontent.com/carterscode/GamerGuardian/main/docs/screenshots/settings-display.png)

## Getting started

- **[Installation](Installation)** — download, install, uninstall
- **[Verifying your download](Verifying-your-download)** — three independent integrity checks (VirusTotal, SHA-256, SLSA provenance) anyone can run before installing
- **[Verification](Verification)** — how to confirm GamerGuardian is doing what it says at runtime
- **[File locations](File-locations)** — where config, logs, and the installed binary live
- **[Logging](Logging)** — `changes.log` schema with worked examples

## Build & contribute

- **[Build from source](Build-from-source)** — local debug, local release, local installer
- **[Source file reference](Source-file-reference)** — what every `.cs` file does
- **[Architecture rationale](Architecture-rationale)** — why GamerGuardian looks the way it does (no kernel driver, polling, single-file publish, etc.)

## Trust & security

- **[Security](Security)** — how the build pipeline keeps the app trustworthy, and how you can verify it yourself
- **[OpenSSF Scorecard](OpenSSF-Scorecard)** — what each Scorecard check measures, what the project scores, and why

> **Source of truth note:** these pages are mirrored from [`docs/wiki/`](https://github.com/carterscode/GamerGuardian/tree/main/docs/wiki) in the main repo. PRs that touch wiki content go to that directory; a sync workflow pushes them here.
