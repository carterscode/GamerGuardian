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
- **Single-instance** tray app, dark UI, no nags.
- **`--test` CLI flag** — runs every monitor's read path and writes results to `%TEMP%\gamerguardian_selftest.txt` (no UI). Useful for QA / debugging on weird hardware.

## Roadmap

Color bit depth (currently no public Windows API for setting it without driver SDKs — read-only support is possible later) and Focus Assist / DND. Windows 10 support. Optional code signing via SignPath OSS.

## Install

1. Grab the latest `GamerGuardian-Setup-x.y.z.exe` from the [Releases page](../../releases).
2. Run it. Windows SmartScreen will show **"Windows protected your PC"** because the installer is unsigned — click **More info** → **Run anyway**. (Code signing via [SignPath](https://signpath.io/) is on the roadmap.)
3. The installer is per-user — no admin required, installs to `%LOCALAPPDATA%\Programs\GamerGuardian`.
4. The app launches automatically and opens its settings window the first time. Pick your desired HDR / refresh rate per monitor, save, and forget about it.

## Usage

- **Tray icon** → right-click for *Settings*, *Check now*, *Exit*. Double-click → settings.
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
- No process spawning. The original v0.1.x build invoked `powercfg.exe` per poll; this was replaced with direct `powrprof.dll` P/Invoke (microseconds vs ~50ms, no disk I/O, no process creation).
- All registry/API reads are blocking but synchronous and microsecond-scale.

**Pauses entirely during fullscreen gameplay:**

`SHQueryUserNotificationState` from `shell32.dll` (the same API Windows uses for its own Do-Not-Disturb logic) detects when a fullscreen D3D / exclusive-fullscreen / presentation app is in the foreground. When it is, GamerGuardian:

- Skips drift checks
- Skips notifications
- Doesn't touch the registry, doesn't call `powrprof.dll`, doesn't enumerate displays

Polling resumes the next tick after you alt-tab back to the desktop. So during actual gameplay, the app is genuinely doing nothing — no CPU, no syscalls, no allocations.

**Working-set trimming:**

- After closing the settings window, the WPF visual tree is GC'd (Gen 2, compacting) and `EmptyWorkingSet` is called to return resident pages to the OS.
- A periodic trim runs every ~5 minutes from the polling timer to keep the idle footprint minimal.
- Workstation GC mode + `RetainVMGarbageCollection=false` so the runtime returns memory to the OS aggressively.

**Wake-up rate / interrupts:**

- One timer wake every 30 seconds (configurable), skipped when fullscreen.
- No high-frequency event subscriptions, no DPC callbacks, no kernel hooks.
- The tray icon's message loop is the only persistent thread besides the polling timer.

**Net effect:** during a gaming session, GamerGuardian is paused. While you're on the desktop, it's a ~25 MB tray app that wakes for 10ms every 30s. You shouldn't notice it.

## Compatibility

- Windows 11 (any version). Windows 10 support is planned for v2.
- x64 only.

## License

[MIT](LICENSE).

## Contributing

Issues and PRs welcome. New monitor modules just need to implement `IMonitoredSetting` (see [`Monitors/HdrMonitor.cs`](src/GamerGuardian/Monitors/HdrMonitor.cs) for a reference implementation).
