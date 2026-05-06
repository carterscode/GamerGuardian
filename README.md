# GamerGuardian

A lightweight Windows tray app that watches gaming-related display settings and alerts you when they drift from what you actually want.

Tired of HDR silently turning itself off, or the refresh rate dropping to 60Hz after a driver update, and only noticing hours later? GamerGuardian sits in the tray, periodically checks your display state against your preferences, and pops up a one-click "Apply" prompt when something's wrong. Or — if you opt in per-setting — silently fixes it for you.

## What it watches

**Per-monitor:**
- **HDR** — on/off state via the Windows Display Configuration (CCD) API
- **Refresh rate** — target the maximum supported, or pin a specific Hz
- **Resolution** — pin to a specific resolution (opt-in)

**Global gaming settings:**
- **Hardware-accelerated GPU Scheduling (HAGS)** — `HKLM` registry; apply uses elevated `reg.exe` (UAC prompt)
- **Memory Integrity / VBS** (Core Isolation) — Windows 11's Hypervisor-Enforced Code Integrity. Disabling can recover ~5–15% gaming performance at the cost of reduced protection against advanced malware. Apply uses elevated `reg.exe`; takes effect on reboot.
- **System Responsiveness** — `HKLM\...\Multimedia\SystemProfile\SystemResponsiveness`. Default 20 reserves 20% CPU for non-multimedia work. Gaming target 10 frees up that headroom.
- **Network Throttling Index** — `HKLM\...\Multimedia\SystemProfile\NetworkThrottlingIndex`. Default 10 paces non-multimedia network packets; disabling (0xFFFFFFFF) reduces jitter for online games.
- **USB Selective Suspend (global)** — `HKLM\SYSTEM\CurrentControlSet\Services\USB\DisableSelectiveSuspend`. Default 0 lets Windows suspend idle USB devices; setting 1 keeps mice/keyboards/headsets always responsive.
- **Games multimedia task profile** — `HKLM\...\Multimedia\SystemProfile\Tasks\Games`. Three values (`Priority`, `Scheduling Category`, `SFIO Priority`) tell the multimedia class scheduler to give game processes higher CPU/I-O priority.
- **Windows Game Mode**
- **Game DVR background recording** — typically a perf killer when left on
- **Mouse "Enhance pointer precision"** — most gamers want this off
- **Fullscreen optimizations** (global setting)
- **Variable Refresh Rate (Windows-level)** — the DirectX user-pref toggle
- **Power plan** — Balanced / High Performance / Power Saver / Ultimate Performance (whichever are installed)

For each setting you choose: monitor or not, desired value, and whether to auto-apply silently when it drifts. Otherwise you get a one-click "Apply" popup.

## Common features

- **Drift notifications** — discreet bottom-right popup with all drifted settings; one-click "Apply All".
- **Auto-apply per setting** — opt in to silent correction.
- **Launches at Windows startup** — registers itself in `HKCU\...\Run`.
- **Manual pause** from the tray — right-click → *Pause monitoring*. Useful for benchmarks the auto-detector doesn't know about.
- **Light / Dark / System** theme toggle. Native Win11 Fluent design via WPF-UI.
- **Single-instance** tray app, no nags.
- **`--test` CLI flag** — runs every monitor's read path and writes results to `%TEMP%\gamerguardian_selftest.txt` (no UI). Useful for QA / debugging on weird hardware.

## Roadmap

Color bit depth (no public Windows API for setting it without driver SDKs — read-only support is possible later). Focus Assist / DND. Windows 10 support. Optional code signing via SignPath OSS.

## Install

1. Grab the latest `GamerGuardian-Setup-x.y.z.exe` from the [Releases page](../../releases).
2. Run it. Windows SmartScreen will show **"Windows protected your PC"** because the installer is unsigned — click **More info** → **Run anyway**. (Code signing via [SignPath](https://signpath.io/) is on the roadmap.)
3. The installer is per-user — no admin required, installs to `%LOCALAPPDATA%\Programs\GamerGuardian`.
4. The app launches automatically and opens its settings window the first time. Pick your desired HDR / refresh rate per monitor, save, and forget about it.

## Usage

- **Tray icon** → right-click for *Settings*, *Check now*, *Pause monitoring*, *Exit*. Double-click → settings.
- **Settings window** lets you toggle "monitor this setting", set your desired value, and opt into "auto-apply silently when it drifts".
- **Drift popup** appears when something differs from your preference and you haven't enabled auto-apply.

Config lives in `%APPDATA%\GamerGuardian\config.json`.

## Build from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
git clone https://github.com/<owner>/GamerGuardian.git
cd GamerGuardian
dotnet build
```

To produce the installer locally, install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then:

```powershell
dotnet publish src/GamerGuardian/GamerGuardian.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer/GamerGuardian.iss
```

## How it works

GamerGuardian uses the Windows Connecting and Configuring Displays (CCD) API (`QueryDisplayConfig`, `DisplayConfigGetDeviceInfo`, `DisplayConfigSetDeviceInfo`) for HDR state, and the legacy `EnumDisplaySettingsEx` / `ChangeDisplaySettingsEx` for refresh rate and resolution. Power plan reads/writes go through `powrprof.dll` (`PowerGetActiveScheme`, `PowerSetActiveScheme`, `PowerEnumerate`). Mouse and registry settings use `SystemParametersInfo` and direct registry access. All pure user-mode P/Invoke — no drivers, no admin, no kernel modules.

The tray process polls every 30 seconds (configurable). If a monitored setting differs from your preference, it either:
- shows a consolidated drift popup, or
- silently calls the corresponding API to fix it (when "auto-apply" is enabled for that setting).

## Performance & gaming impact

GamerGuardian is designed to have effectively zero impact on gaming. Concrete measurements on a Release build:

**Memory footprint (Working Set / Private):**

| State | Working set | Private memory |
|---|---|---|
| Idle in tray (no window) | ~23 MB | ~30 MB |
| Settings window open | ~70 MB | ~158 MB |
| Settings window closed | ~18 MB | ~136 MB |

The ~136 MB private (committed) memory at runtime is the irreducible cost of bundling .NET 8 + WPF + WPF-UI. Windows pages it out under memory pressure so games can use the physical RAM. The working set is what actually competes for RAM.

**CPU during polling:**

- Default 30s interval. Each poll runs ~10ms of in-memory API calls — below the noise floor on any modern CPU.
- No process spawning. Power plan reads/writes go through `powrprof.dll` P/Invoke directly (microseconds, no disk I/O).
- All registry / Win32 reads are synchronous and microsecond-scale.

**Pauses automatically during gameplay or benchmarks:**

Three independent detectors. If any fires, the poll tick is skipped — no drift checks, no notifications, no registry / API calls.

1. **Exclusive fullscreen / presentation mode.** `SHQueryUserNotificationState` from `shell32.dll` (the same API Windows uses for its own Do-Not-Disturb logic) detects exclusive-fullscreen D3D, presentation mode, and fullscreen UWP apps.
2. **Borderless fullscreen.** The above API misses borderless windowed games — the default for most modern titles (Apex, Valorant, CS2, Fortnite, etc.). For that, GamerGuardian compares the foreground window's rect to the monitor's full bounds via `GetForegroundWindow` + `MonitorFromWindow` + `GetMonitorInfo`. A window covering the entire monitor edge-to-edge (matching `rcMonitor`, not `rcWork`) is treated as fullscreen-like. A maximized regular window (Chrome, VSCode, etc.) matches `rcWork` and is correctly ignored.
3. **Known benchmarks.** A list of common benchmark executables (3DMark, Cinebench, Geekbench, AIDA64, FurMark, Unigine Heaven/Valley/Superposition, OCCT, Prime95, y-cruncher, CrystalDiskMark, PCMark, PassMark, Time Spy, etc.) is checked against `Process.GetProcesses()`. If any of them is running, polling is paused until they exit.

You can also **manually pause** monitoring from the tray menu (right-click → *Pause monitoring*). Useful for any tool the auto-detector doesn't know about. Pause is in-memory only — restarting the app resumes monitoring.

**Working-set trimming:**

- After closing the settings window, the WPF visual tree is GC'd (Gen 2, compacting) and `EmptyWorkingSet` is called to return resident pages to the OS.
- A periodic trim runs every ~5 minutes from the polling timer to keep the idle footprint minimal.
- Workstation GC mode + `RetainVMGarbageCollection=false` so the runtime returns memory to the OS aggressively.

**Wake-up rate / interrupts:**

- One timer wake every 30 seconds (configurable), skipped when paused / fullscreen / benchmark detected.
- No high-frequency event subscriptions, no DPC callbacks, no kernel hooks.
- The tray icon's message loop is the only persistent thread besides the polling timer.

**Net effect:** during a gaming or benchmark session, GamerGuardian is paused. While you're on the desktop, it's a ~25 MB tray app that wakes for 10ms every 30s.

## Compatibility

- Windows 11 (any version).
- x64 only.

## License

[MIT](LICENSE).

## Contributing

Issues and PRs welcome. New monitor modules just need to implement `IMonitoredSetting` (see [`Monitors/HdrMonitor.cs`](src/GamerGuardian/Monitors/HdrMonitor.cs) for a reference implementation).
