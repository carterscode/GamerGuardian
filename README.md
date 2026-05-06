# GamerGuardian

A lightweight Windows tray app that watches gaming-related display settings and alerts you when they drift from what you actually want.

Tired of HDR silently turning itself off, or the refresh rate dropping to 60Hz after a driver update, and only noticing hours later? GamerGuardian sits in the tray, periodically checks your display state against your preferences, and pops up a one-click "Apply" prompt when something's wrong. Or — if you opt in per-setting — silently fixes it for you.

## Features (v1)

- **Per-monitor HDR enforcement** — detect on/off state, prompt or auto-apply.
- **Per-monitor refresh rate enforcement** — target the maximum supported, or pin a specific Hz.
- **Drift notifications** — discreet bottom-right popup with all drifted settings; one-click "Apply All".
- **Auto-apply per setting** — opt in to silent correction.
- **Launches at Windows startup** — registers itself in `HKCU\...\Run`.
- **Single-instance** tray app, dark UI, no nags.

## Roadmap (v2)

VRR, Hardware-accelerated GPU Scheduling (HAGS), Game Mode, Game DVR, power plan, mouse "Enhance pointer precision", fullscreen optimizations, resolution, color bit depth, Focus Assist. Each is a pluggable `IMonitoredSetting` module — easy to add more.

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

GamerGuardian uses the Windows Connecting and Configuring Displays (CCD) API (`QueryDisplayConfig`, `DisplayConfigGetDeviceInfo`, `DisplayConfigSetDeviceInfo`) for HDR state, and the legacy `EnumDisplaySettingsEx` / `ChangeDisplaySettingsEx` for refresh rate. Both are pure user-mode P/Invoke — no drivers, no admin, no kernel modules.

The tray process polls every 30 seconds (configurable). If a monitored setting differs from your preference, it either:
- shows a consolidated drift popup, or
- silently calls the corresponding API to fix it (when "auto-apply" is enabled for that setting).

## Compatibility

- Windows 11 (any version). Windows 10 support is planned for v2.
- x64 only.

## License

[MIT](LICENSE).

## Contributing

Issues and PRs welcome. New monitor modules just need to implement `IMonitoredSetting` (see [`Monitors/HdrMonitor.cs`](src/GamerGuardian/Monitors/HdrMonitor.cs) for a reference implementation).
