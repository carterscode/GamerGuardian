# Installation

## Quick install

1. Grab the latest **`GamerGuardian-Setup-x.y.z.exe`** from the [Releases page](https://github.com/carterscode/GamerGuardian/releases/latest).
2. Run it. Windows SmartScreen will warn that the installer is unsigned — click **More info** → **Run anyway**. Code signing via the [SignPath Foundation](https://signpath.org/) is on the roadmap.
3. Per-user install — no admin needed, lands at `%LOCALAPPDATA%\Programs\GamerGuardian`.
4. The app launches and opens its settings window the first time:

   ![Settings window — General tab](https://raw.githubusercontent.com/carterscode/GamerGuardian/main/docs/screenshots/settings-general.png)

   Click through the four tabs to set per-display, global gaming, and Windows services preferences. **Save & close** persists your choices; **Apply** applies them immediately and re-checks the system.

A portable single-file `GamerGuardian.exe` is also attached to each release if you don't want the installer. Drop it anywhere and run it.

## Auto-update

After v0.1.14, every running copy auto-checks for new releases on startup and offers a one-click in-place upgrade. The downloader writes to `%TEMP%`, then runs the installer in upgrade mode. If you decline, the prompt is suppressed for that version (skip-this-version semantics).

You can also trigger a check manually from **Settings → General → Check now**.

## Uninstall

Standard Windows: **Settings → Apps → Installed apps → GamerGuardian → Uninstall**, or `appwiz.cpl`.

The uninstaller:
- Stops any running `GamerGuardian.exe` process
- Removes the install folder
- Removes the `HKCU\...\Run\GamerGuardian` autostart entry
- Cleans `%APPDATA%\GamerGuardian\` (config + logs)

It does **not** touch the registry-backed Windows settings GamerGuardian had been monitoring — those stay at whatever value was last applied.

## Compatibility

- **Windows 11** (any version). Windows 10 support is on the roadmap.
- **x64** only.
- Requires .NET 8 — the installer ships a self-contained build, so no separate runtime install needed.

## Where things land

See [File locations](File-locations) for the full inventory of paths GamerGuardian reads or writes.
