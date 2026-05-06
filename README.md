<div align="center">

<img src="src/GamerGuardian/Assets/AppIcon-128.png" width="96" alt="GamerGuardian" />

# GamerGuardian

A lightweight Windows 11 tray app that watches gaming-related display and system settings, alerts you (or auto-fixes) when they drift from your preferences, and stays out of the way during gameplay and benchmarks.

[![Latest release](https://img.shields.io/github/v/release/carterscode/GamerGuardian?label=latest&color=brightgreen)](https://github.com/carterscode/GamerGuardian/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/carterscode/GamerGuardian/total?color=blue)](https://github.com/carterscode/GamerGuardian/releases)
[![Build](https://img.shields.io/github/actions/workflow/status/carterscode/GamerGuardian/release.yml?branch=main&label=build)](https://github.com/carterscode/GamerGuardian/actions/workflows/release.yml)
[![License](https://img.shields.io/github/license/carterscode/GamerGuardian?color=lightgrey)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2011-0078d4)](#compatibility)
[![.NET](https://img.shields.io/badge/.NET-8.0-512bd4)](https://dotnet.microsoft.com/)

[Install](#install) · [What it watches](#what-it-watches) · [Performance](#performance--gaming-impact) · [How it works](#how-it-works) · [Build from source](#build-from-source)

</div>

---

## Why this exists

If you've ever fired up a game and realized 30 minutes later that HDR turned itself off after the last driver update, or that you've been gaming at 60Hz instead of your monitor's actual max — GamerGuardian is for that. It periodically compares Windows settings against your preferences and either prompts you to fix drift in one click, or silently corrects it in the background.

It's also paranoid about not making your gaming worse. Polling pauses entirely during fullscreen games (including borderless windowed) and benchmark runs. Working set is trimmed back to ~25 MB at idle. No process spawning, no kernel hooks, no DPC callbacks.

## Highlights

- 🎯 **17 monitored settings** spanning display, security, performance, capture, and input
- 🎮 **Pauses during gameplay** — fullscreen, borderless, *and* during benchmark runs (3DMark, Cinebench, Geekbench, etc.)
- ⚡ **One-click apply** with a per-setting auto-apply opt-in
- 🪟 **Native Win11 Fluent design** with light / dark / system themes
- 🔄 **Auto-update** — checks GitHub Releases on startup, one-click install
- 🪶 **~23 MB idle working set**, ~10ms per polling tick

## Table of contents

- [Install](#install)
- [What it watches](#what-it-watches)
- [Usage](#usage)
- [Performance & gaming impact](#performance--gaming-impact)
- [How it works](#how-it-works)
- [Build from source](#build-from-source)
- [Compatibility](#compatibility)
- [Contributing](#contributing)
- [License](#license)

## Install

1. Grab the latest **`GamerGuardian-Setup-x.y.z.exe`** from the [Releases page](https://github.com/carterscode/GamerGuardian/releases/latest).
2. Run it. Windows SmartScreen will warn that the installer is unsigned — click **More info** → **Run anyway**. (Code signing via [SignPath OSS](https://signpath.io/) is on the roadmap.)
3. Per-user install — no admin needed, lands at `%LOCALAPPDATA%\Programs\GamerGuardian`.
4. The app launches and opens its settings window the first time. Pick what to monitor per display, save, and forget about it.

A portable single-file `GamerGuardian.exe` is also attached to each release if you don't want the installer.

After v0.1.14, every running copy auto-checks for new releases on startup and offers a one-click in-place upgrade.

## What it watches

For each setting you choose: **monitor or not**, **desired value**, and whether to **auto-apply silently** when it drifts. Setting names below link to authoritative documentation — open an [issue](https://github.com/carterscode/GamerGuardian/issues) if any are stale.

### Per-display

| Setting | Notes |
|---|---|
| [**HDR**](https://support.microsoft.com/en-us/windows/hdr-settings-in-windows-2d767185-38ec-7fdc-6f97-bbc6c5ef24e6) | On/off via Windows Display Configuration (CCD) API. |
| [**Refresh rate**](https://support.microsoft.com/en-us/windows/change-the-refresh-rate-on-your-monitor-in-windows-c8ea729e-0678-015c-c415-f806f04aae5a) | Maximum supported, or pin a specific Hz. |
| [**Resolution**](https://support.microsoft.com/en-us/windows/change-your-screen-resolution-and-layout-in-windows-5effefe3-2eac-e306-0b5d-2073b765876b) | Pin to a specific resolution (opt-in). |

### Global gaming settings

| Setting | What it does | Reboot |
|---|---|:---:|
| [**HAGS** (Hardware-accelerated GPU Scheduling)](https://devblogs.microsoft.com/directx/hardware-accelerated-gpu-scheduling/) | Lets the GPU manage its own command queue. Lower latency on supported GPUs. | ✓ |
| [**Memory Integrity / VBS**](https://support.microsoft.com/en-us/windows/core-isolation-e30ed737-17d8-42f3-a2a9-87521df09b78) | Hypervisor-Enforced Code Integrity. Disabling recovers ~5–15% gaming perf at the cost of reduced malware protection. | ✓ |
| [**Game Mode**](https://support.xbox.com/en-US/help/games-apps/game-setup-and-play/use-game-mode-gaming-on-pc) | Tells Windows to prioritize the running game and suppress background work. | |
| [**Game DVR background recording**](https://support.xbox.com/help/games-apps/game-dvr/game-dvr-windows-10) | Always-on game capture. Costs CPU/GPU during gameplay; off is gaming-recommended. | |
| [**Mouse "Enhance pointer precision"**](https://support.microsoft.com/en-us/windows/change-mouse-settings-e81356a4-0e74-fe38-7d01-9d79fbf8712b) | Acceleration curve applied to mouse movement. Most gamers want this off for consistent aim. | |
| [**Fullscreen optimizations**](https://devblogs.microsoft.com/directx/demystifying-full-screen-optimizations/) | Borderless-windowed compositing layer for fullscreen apps. | |
| [**Variable Refresh Rate (Windows)**](https://devblogs.microsoft.com/directx/os-variable-refresh-rate/) | DirectX user-pref VRR toggle. Smoother frame pacing on G-Sync / FreeSync displays. | |
| [**Power plan**](https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings) | Active Windows power scheme. High Performance keeps CPU clocks elevated. | |
| [**System Responsiveness**](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service) | CPU percentage Windows reserves for non-multimedia tasks. Default 20 → 10 frees that headroom for games. | ✓ |
| [**Network Throttling**](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service) | Multimedia packet pacing. Disabling reduces network jitter for online games. | |
| [**USB Selective Suspend (global)**](https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/usb-selective-suspend) | Lets Windows suspend idle USB devices. Disabling keeps mice/keyboards/headsets always responsive. | ✓ |
| [**Games multimedia task profile**](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service) | Priority + scheduling values for processes registered with the MMCSS Games task. | |

Reboot ✓ means the change is written immediately to the registry but Windows requires a restart to apply it.

## Usage

- **Tray icon** (right-click): *Settings* · *Check now* · *Pause monitoring* · *Exit*. Double-click → settings.
- **Settings window**: per-monitor and global gaming settings. Each card has *Monitor* / *Want* / *Auto-apply silently*. Bottom buttons: *Cancel* · *Apply* (saves + applies + refreshes without closing) · *Save & close*.
- **Drift popup** (bottom-right) when something differs from your preference and you haven't enabled auto-apply.
- **Reboot prompt** appears after Apply if any reboot-required setting was changed. Auto-apply cases get a non-modal "reboot pending" popup instead of a forced confirmation dialog.
- **Manual pause** from the tray menu suspends polling for one-off tasks the auto-detector doesn't recognize.
- **Self-test**: `GamerGuardian.exe --test` writes every monitor's current state to `%TEMP%\gamerguardian_selftest.txt`. Useful for QA on weird hardware or for bug reports.

Config lives in `%APPDATA%\GamerGuardian\config.json`.

## Performance & gaming impact

Designed to be invisible during gameplay. Concrete numbers from the Release build:

### Memory footprint

| State | Working set | Private memory |
|---|---|---|
| Idle in tray (no window) | ~23 MB | ~30 MB |
| Settings window open | ~70 MB | ~158 MB |
| Settings window closed | ~18 MB | ~136 MB |

The ~136 MB private (committed) memory is the irreducible cost of bundling .NET 8 + WPF + WPF-UI. Windows pages it out under memory pressure so games can use the physical RAM.

### CPU during polling

- Default 30s interval. Each poll runs ~10 ms of in-memory API calls — below the noise floor on any modern CPU.
- No process spawning. Power plan reads/writes go through `powrprof.dll` P/Invoke directly (microseconds, no disk I/O).

### Pauses automatically during gameplay or benchmarks

Three independent detectors. If any fires, the poll tick is skipped — no drift checks, no notifications, no registry calls.

1. **Exclusive fullscreen / presentation mode** — `SHQueryUserNotificationState` from `shell32.dll`. Catches games in true fullscreen and Win11 UWP fullscreen apps.
2. **Borderless fullscreen** — covers most modern titles (Apex, Valorant, CS2, Fortnite, etc.). Compares the foreground window's rect to the monitor's full bounds via `GetForegroundWindow` + `MonitorFromWindow` + `GetMonitorInfo`. Edge-to-edge match (`rcMonitor`, not `rcWork`) → fullscreen-like. Maximized regular windows match `rcWork` and are correctly ignored.
3. **Known benchmarks** — process list checked against an allow-list of common benchmark executables (3DMark, Cinebench, Geekbench, AIDA64, FurMark, Unigine Heaven/Valley/Superposition, OCCT, Prime95, y-cruncher, CrystalDiskMark, PCMark, PassMark, etc.). Polling pauses while any are running.

You can also **manually pause** monitoring from the tray menu for tools the auto-detector doesn't recognize.

### Working-set trimming

- After closing the settings window, the WPF visual tree is GC'd (Gen 2, compacting) and `EmptyWorkingSet` is called to return resident pages to the OS.
- A periodic trim runs every ~5 minutes from the polling timer to keep the idle footprint minimal.
- Workstation GC mode + `RetainVMGarbageCollection=false` so the runtime returns memory to the OS aggressively.

### Wake-up rate / interrupts

- One timer wake every 30 seconds (configurable), skipped when paused / fullscreen / benchmark detected.
- No high-frequency event subscriptions, no DPC callbacks, no kernel hooks.
- Only persistent threads are the tray icon's message loop and the polling timer.

**Net effect**: during gameplay or benchmarks, GamerGuardian is paused. While you're on the desktop, it's a ~25 MB tray app that wakes for ~10 ms every 30 seconds.

## How it works

Pure user-mode P/Invoke — no drivers, no kernel modules, no admin required for monitoring (only for applying changes to `HKLM` registry hives, which trigger UAC).

| API surface | Used for |
|---|---|
| Connecting and Configuring Displays (CCD) | HDR state, VRR, display enumeration |
| `EnumDisplaySettingsEx` / `ChangeDisplaySettingsEx` | Refresh rate, resolution |
| `powrprof.dll` (`PowerGetActiveScheme`, `PowerEnumerate`) | Power plan |
| `SystemParametersInfo` | Mouse precision |
| Direct `HKCU` / `HKLM` registry access | All registry-backed settings |
| `SHQueryUserNotificationState` | Fullscreen game / presentation detection |
| `Process.GetProcesses` | Benchmark detection |
| `EmptyWorkingSet` (psapi) | Working-set trimming |

Each monitored setting is an `IMonitoredSetting` implementation in [`src/GamerGuardian/Monitors/`](src/GamerGuardian/Monitors/). Adding a new one is ~30 lines of code following the [`HdrMonitor`](src/GamerGuardian/Monitors/HdrMonitor.cs) pattern.

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/carterscode/GamerGuardian.git
cd GamerGuardian
dotnet build
```

To produce the installer locally, install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then:

```powershell
dotnet publish src/GamerGuardian/GamerGuardian.csproj `
    -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=0.0.0 -o publish

& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DAppVersion=0.0.0 installer\GamerGuardian.iss
```

CI runs the same flags on every push to `main` (touching `src/`, `installer/`, or the release workflow) — see [`.github/workflows/release.yml`](.github/workflows/release.yml).

## Compatibility

- **Windows 11** (any version). Windows 10 support is on the roadmap.
- **x64** only.

## Contributing

Issues and PRs welcome. New monitor modules just need to implement [`IMonitoredSetting`](src/GamerGuardian/Monitors/IMonitoredSetting.cs) — see [`HdrMonitor.cs`](src/GamerGuardian/Monitors/HdrMonitor.cs) for the canonical example.

If a setting reference link in the table above 404s, please open an issue or PR with the corrected URL.

## License

[MIT](LICENSE) © GamerGuardian Contributors

---

<div align="center">

Made with care for gamers tired of Windows silently changing their settings.

</div>
