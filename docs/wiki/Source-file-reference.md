# Source file reference

Every meaningful file in [`src/GamerGuardian/`](https://github.com/carterscode/GamerGuardian/tree/main/src/GamerGuardian), grouped by directory.

## Top level

| File | What it does |
|---|---|
| [`App.xaml(.cs)`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/App.xaml.cs) | App entry. Single-instance mutex, theme bootstrap, monitor list, tray, exception logging, auto-update check. Dispatches `--test` and `--show-settings` CLI flags. |
| `GamerGuardian.csproj` | Build config — target framework, package refs (WPF-UI, ServiceProcess), single-file/self-contained settings. |
| `app.manifest` | Per-monitor V2 DPI awareness, `requestedExecutionLevel asInvoker` (no UAC at startup). |

## `Models/`

Pure data — no behavior, no I/O.

| File | Contents |
|---|---|
| [`AppConfig.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Models/AppConfig.cs) | The JSON-serialized config root. Per-display preferences, global gaming preferences, services preferences, theme, polling interval. |
| [`DriftReport.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Models/DriftReport.cs) | `DriftItem` record — a single setting that's drifted from preference, with the Apply lambda baked in. |
| [`ApplyResult.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Models/ApplyResult.cs) | Returned by `ChangeApplier` after Apply+verify. Drives both the Apply Results window and `changes.log`. |
| [`ServiceDefinition.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Models/ServiceDefinition.cs) | Static metadata for a Windows service in the catalog (name, display name, default start type, recommended target). |

## `Monitors/`

Each `IMonitoredSetting` is ~30 lines — read raw, compute desired, yield a `DriftItem`. Each file maps 1:1 to a row in the Settings UI.

| File | What it watches |
|---|---|
| [`IMonitoredSetting.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/IMonitoredSetting.cs) | The interface — `Id` + `CheckDrift(config)`. |
| [`HdrMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/HdrMonitor.cs) | Per-display HDR via the CCD `DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO` API. |
| [`RefreshRateMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/RefreshRateMonitor.cs) | Per-display refresh rate via `EnumDisplaySettingsEx`/`ChangeDisplaySettingsEx`. |
| [`ResolutionMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/ResolutionMonitor.cs) | Per-display resolution. |
| [`HagsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/HagsMonitor.cs) | Hardware-accelerated GPU Scheduling (HKLM, reboot required). The canonical example for new monitors. |
| [`MemoryIntegrityMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/MemoryIntegrityMonitor.cs) | VBS / HVCI (HKLM, reboot required). |
| [`SystemResponsivenessMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/SystemResponsivenessMonitor.cs) | MMCSS reservation percentage (HKLM, reboot required). |
| [`NetworkThrottlingMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/NetworkThrottlingMonitor.cs) | MMCSS network throttling index (HKLM). Surfaced on the Network tab. |
| [`UsbSelectiveSuspendMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/UsbSelectiveSuspendMonitor.cs) | Global USB selective suspend toggle (HKLM, reboot required). |
| [`GamesTaskProfileMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/GamesTaskProfileMonitor.cs) | MMCSS Games task profile values (HKLM). |
| [`GameModeMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/GameModeMonitor.cs) | Windows Game Mode (HKCU). |
| [`GameDvrMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/GameDvrMonitor.cs) | Game DVR background recording (HKCU). |
| [`MousePrecisionMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/MousePrecisionMonitor.cs) | Mouse acceleration ("Enhance pointer precision") via `SystemParametersInfo` + HKCU. |
| [`FullscreenOptimizationsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/FullscreenOptimizationsMonitor.cs) | Global FSE compositor toggle (HKCU). |
| [`VrrMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/VrrMonitor.cs) | DirectX Variable Refresh Rate (HKLM). |
| [`PowerPlanMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/PowerPlanMonitor.cs) | Active power scheme via `powrprof` P/Invoke. Enumerates installed schemes for the dropdown. |
| [`DrrMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/DrrMonitor.cs) | Per-display Dynamic Refresh Rate via `DrrInterop` (CCD `SetDisplayConfig` boost-refresh-rate flag). Skips unsupported displays. |
| [`AdvertisingIdMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/AdvertisingIdMonitor.cs) | Advertising ID (HKCU). Privacy tab. |
| [`TailoredExperiencesMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/TailoredExperiencesMonitor.cs) | Tailored experiences with diagnostic data (HKCU). Privacy tab. |
| [`CdpMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/CdpMonitor.cs) | Cross-Device Platform `EnableCdp` policy (HKLM). Privacy tab. |
| [`ActivityHistoryMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/ActivityHistoryMonitor.cs) | Activity History / Timeline policy — three HKLM values batched. Privacy tab. |
| [`NagleMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/NagleMonitor.cs) | Nagle's algorithm per active interface (HKLM `TcpAckFrequency`/`TCPNoDelay`). Network tab. |
| [`NicPowerMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/NicPowerMonitor.cs) | NIC power management (`PnPCapabilities` per adapter class instance, HKLM, reboot). Network tab. |
| [`PowerThrottlingMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/PowerThrottlingMonitor.cs) | Windows Power Throttling off (HKLM). CPU/Power tab. |
| [`FastStartupMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/FastStartupMonitor.cs) | Fast Startup / hybrid boot `HiberbootEnabled` (HKLM, reboot). |
| [`VisualEffectsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/VisualEffectsMonitor.cs) | Visual effects "best performance" (`VisualFXSetting` + binary `UserPreferencesMask`, HKCU). |
| [`WindowsServiceMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/WindowsServiceMonitor.cs) | One instance per service in `ServiceCatalog`. Maps `ServicePref.Desired` (Default/Manual/Disabled) to the matching elevated `sc.exe` call. |

## `Native/`

P/Invoke wrappers — every Windows API the app touches.

| File | Surface |
|---|---|
| [`DisplayConfig.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/DisplayConfig.cs) | CCD APIs (`QueryDisplayConfig`, `DisplayConfigGet/SetDeviceInfo`, `SetDisplayConfig`) for HDR, display enumeration, and DRR (boost-refresh-rate flag). |
| [`DisplayHelper.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/DisplayHelper.cs) | Higher-level enumeration that pairs CCD source/target info with GDI device names. |
| [`DrrInterop.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/DrrInterop.cs) | Read/validate/set Dynamic Refresh Rate per target via `SetDisplayConfig` + `SDC_VIRTUAL_REFRESH_RATE_AWARE` (user-mode). |
| [`NetworkAdapters.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/NetworkAdapters.cs) | Active physical adapter enumeration (managed `NetworkInterface`) with a short time-boxed cache, shared by the Nagle and NIC-power monitors. |
| [`User32.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/User32.cs) | `EnumDisplaySettingsEx`, `ChangeDisplaySettingsEx`, `SystemParametersInfo`, foreground-window helpers. |
| [`Powrprof.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/Powrprof.cs) | `PowerGetActiveScheme`, `PowerEnumerate`, `PowerSetActiveScheme`, `PowerReadFriendlyName`. |
| [`Shell32.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/Shell32.cs) | `SHQueryUserNotificationState` for fullscreen detection, plus borderless-fullscreen detection via foreground rect / monitor rect comparison. |
| [`Psapi.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/Psapi.cs) | `EmptyWorkingSet` for working-set trimming. |

## `Services/`

Behavior — orchestration, polling, IPC, persistence.

| File | What it does |
|---|---|
| [`ConfigStore.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ConfigStore.cs) | JSON load/save for `AppConfig`. |
| [`MonitorService.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/MonitorService.cs) | The 30s polling loop. Pause-detection (fullscreen / benchmark / manual), drift collection, auto-apply dispatch, periodic working-set trim. |
| [`ChangeApplier.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ChangeApplier.cs) | Shared apply+verify path used by manual Apply *and* the auto-apply loop. Re-runs `CheckDrift` after applying to confirm the value landed. |
| [`ChangeLogger.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ChangeLogger.cs) | Writes `changes.log` entries. Handles rotation. |
| [`SettingDocs.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/SettingDocs.cs) | Per-setting `MechanismFor`/`VerifyCommandFor` — drives both the Apply Results window and the change log "Mechanism" + "Verify" lines. |
| [`BenchmarkDetector.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/BenchmarkDetector.cs) | Process list scan against an allowlist of common benchmark executables. |
| [`ElevatedRegistry.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ElevatedRegistry.cs) | Spawns `reg.exe`/`cmd.exe` with `Verb=runas` for HKLM writes (single UAC prompt). Batched add (`SetHklmMulti`) and delete (`DeleteHklmMulti`), with an allowlist guard that rejects shell metacharacters in any segment. |
| [`SettingDocsCatalog.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/SettingDocsCatalog.cs) | Long-form per-setting docs (What/Why/Scenarios/Risks/Reversal) shown in the Learn-more expander; source for the generated SETTINGS-REFERENCE.md. |
| [`SettingsReferenceGen.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/SettingsReferenceGen.cs) | Renders `SettingDocsCatalog` to `docs/SETTINGS-REFERENCE.md` (`--gen-docs`); a unit test asserts the committed file matches. |
| [`WindowsServiceController.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/WindowsServiceController.cs) | Reads service start type from registry; spawns `sc.exe` with `Verb=runas` for stop+disable / stop+set-manual / restore-default. |
| [`ServiceCatalog.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ServiceCatalog.cs) | The static list of Windows services GamerGuardian knows about. |
| [`TempCleanup.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/TempCleanup.cs) | Sweeps stale auto-update installer EXEs from `%TEMP%` (>1 day old). |
| [`ThemeService.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ThemeService.cs) | Light/dark/system theme switch via `Wpf.Ui.Appearance.ApplicationThemeManager`. |
| [`UpdateService.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/UpdateService.cs) | GitHub Releases API check + installer download. Strips prerelease suffixes when comparing semver. |
| [`StartupRegistration.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/StartupRegistration.cs) | Adds/removes the `HKCU\...\Run\GamerGuardian` autostart entry. |
| [`Notifier.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/Notifier.cs) | Bottom-right drift popup window. |

## `Tray/`

| File | What it does |
|---|---|
| [`TrayIconHost.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Tray/TrayIconHost.cs) | Win32 NotifyIcon wrapper. Right-click menu, double-click → settings, paused-icon variant. |

## `UI/`

WPF windows, all using WPF-UI Fluent styles.

| File | Window |
|---|---|
| [`SettingsWindow.xaml(.cs)`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/UI/SettingsWindow.xaml.cs) | The main UI — nine tabs (General / Global gaming / Privacy / Network / Windows services / Windows AI / Display / CPU·Power / Recommended BIOS). Holds `GlobalToggleRow`, `ServiceRow`, `DisplayRow`, `WindowsAiAppRow` view-models. |
| [`NotificationWindow.xaml(.cs)`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/UI/NotificationWindow.xaml.cs) | Bottom-right drift popup with one-click Apply + Dismiss. |
| [`ApplyResultsWindow.xaml(.cs)`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/UI/ApplyResultsWindow.xaml.cs) | Per-setting verification with copyable PowerShell snippets. |
| [`UpdateAvailableWindow.xaml(.cs)`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/UI/UpdateAvailableWindow.xaml.cs) | Auto-update prompt with download progress and one-click install-and-restart. |
| [`RebootPendingWindow.xaml(.cs)`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/UI/RebootPendingWindow.xaml.cs) | Non-modal "reboot pending" notice shown after auto-apply of reboot-required settings. |

## `Assets/`

| File | Purpose |
|---|---|
| `AppIcon.ico` | Multi-res application icon (tray + window + installer) |
| `AppIcon-128.png` | Banner version used in README |

Both are generated from `tools/generate-icon.ps1` — see that script if you want to regenerate.
