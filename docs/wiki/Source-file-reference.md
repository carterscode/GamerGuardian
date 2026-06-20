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
| [`SettingDetails.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Models/SettingDetails.cs) | Per-setting long-form detail record (What / Why / HowItHelps / Scenarios / Recommended / Risks / ReversibleVia) backing the Learn-more expander and `SETTINGS-REFERENCE.md`, plus the `ChoiceTradeoff(Choice, Pro, Con)` record powering the per-choice pros/cons. |
| [`WindowsAiAppDefinition.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Models/WindowsAiAppDefinition.cs) | Static metadata for a Windows-AI UWP package in the catalog (package name, display name, description); per-user prefs live in `AppConfig.WindowsAiApps`. |
| [`CpuInfo.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Models/CpuInfo.cs) | Detected-CPU shape (`CpuVendor`, raw name, normalized model token, coarse family) produced by `CpuDetector`. CCD topology / parking strategy are decided in `CpuTuneCatalog`, not here. |
| [`CpuTuneDefinition.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Models/CpuTuneDefinition.cs) | CPU tune recipe types — `TuneTier`, `CcdTopology`, `ParkingStrategy`, `PowerOverride`, and the recipe/result records the CPU·Power tab builds plans from. |

## `Monitors/`

Each `IMonitoredSetting` is ~30 lines — read raw, compute desired, yield a `DriftItem`. Each file maps 1:1 to a row in the Settings UI.

| File | What it watches |
|---|---|
| [`IMonitoredSetting.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/IMonitoredSetting.cs) | The interface — `Id` + `CheckDrift(config)`. |
| [`HdrMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/HdrMonitor.cs) | Per-display HDR via the CCD `DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO` API. |
| [`RefreshRateMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/RefreshRateMonitor.cs) | Per-display refresh rate via `EnumDisplaySettingsEx`/`ChangeDisplaySettingsEx`. |
| [`ResolutionMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/ResolutionMonitor.cs) | Per-display resolution. |
| [`HagsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/HagsMonitor.cs) | Hardware-accelerated GPU Scheduling (HKLM, reboot required). The canonical example for new monitors. |
| [`MemoryIntegrityMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/MemoryIntegrityMonitor.cs) | VBS / HVCI (HKLM, reboot required). Defers to `VbsMonitor` while the full-stack toggle holds VBS disabled. |
| [`VbsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/VbsMonitor.cs) | Full VBS-stack disable — DeviceGuard root + every `Scenarios\*` subkey + `LsaCfgFlags` + the Group Policy mirror, batched into one UAC prompt (HKLM, reboot required). Pure snapshot/ops functions for headless tests. |
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
| [`OnlineSpeechMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/OnlineSpeechMonitor.cs) | Online (cloud) speech recognition opt-out (`HasAccepted`, HKCU). Privacy tab. |
| [`InkingTypingMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/InkingTypingMonitor.cs) | Inking & typing personalization opt-out — `AcceptedPrivacyPolicy` + implicit ink collection + contact harvesting (HKCU). Avoids the value owned by `InputInsightsMonitor`. Privacy tab. |
| [`NagleMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/NagleMonitor.cs) | Nagle's algorithm per active interface (HKLM `TcpAckFrequency`/`TCPNoDelay`). Network tab. |
| [`NicPowerMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/NicPowerMonitor.cs) | NIC power management (`PnPCapabilities` per adapter class instance, HKLM, reboot). Network tab. |
| [`PowerThrottlingMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/PowerThrottlingMonitor.cs) | Windows Power Throttling off (HKLM). CPU/Power tab. |
| [`FastStartupMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/FastStartupMonitor.cs) | Fast Startup / hybrid boot `HiberbootEnabled` (HKLM, reboot). |
| [`VisualEffectsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/VisualEffectsMonitor.cs) | Visual effects "best performance" (`VisualFXSetting` + binary `UserPreferencesMask`, HKCU). |
| [`SuggestedContentMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/SuggestedContentMonitor.cs) | Suggested content + silent app installs — batch of `ContentDeliveryManager` values (HKCU). Debloat tab. |
| [`LockScreenSpotlightMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/LockScreenSpotlightMonitor.cs) | Lock-screen Spotlight tips/ads overlay (HKCU). Debloat tab. |
| [`FinishSetupNagMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/FinishSetupNagMonitor.cs) | "Finish setting up your device" SCOOBE nag (`ScoobeSystemSettingEnabled` + CDM notification, HKCU). Debloat tab. |
| [`StartRecommendationsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/StartRecommendationsMonitor.cs) | Start menu recommendations + recent files (`Start_IrisRecommendations`/`Start_TrackDocs`, HKCU). Debloat tab. |
| [`ExplorerAdsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/ExplorerAdsMonitor.cs) | File Explorer OneDrive/Office ad banners (`ShowSyncProviderNotifications`, HKCU). Debloat tab. |
| [`FeedbackNagMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/FeedbackNagMonitor.cs) | Windows feedback request frequency (`NumberOfSIUFInPeriod`, HKCU). Debloat tab. |
| [`WidgetsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/WidgetsMonitor.cs) | Widgets / News and interests — `Dsh\AllowNewsAndInterests` policy (HKLM) + taskbar button (HKCU). Debloat tab. |
| [`EdgeBackgroundMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/EdgeBackgroundMonitor.cs) | Edge startup boost + background mode — Edge enterprise policies (HKLM). Debloat tab. |
| [`CopilotMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/CopilotMonitor.cs) | Windows Copilot lockdown — `TurnOffWindowsCopilot` policy (HKLM + HKCU) + the Explorer Copilot button + background-access disable (HKCU). Windows AI tab. |
| [`RecallMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/RecallMonitor.cs) | Recall / snapshots opt-out — `WindowsAI\{AllowRecallEnablement, DisableAIDataAnalysis, TurnOffSavingSnapshots}` policy (HKLM). Windows AI tab. |
| [`ClickToDoMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/ClickToDoMonitor.cs) | Click To Do disable — `WindowsAI\DisableClickToDo` policy (HKLM) + Shell ClickToDo key (HKCU). Windows AI tab. |
| [`EdgeAiMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/EdgeAiMonitor.cs) | Edge AI features — Copilot sidebar / page context / local foundational model / inline compose / browse-with-Copilot enterprise policies (HKLM). Windows AI tab. |
| [`NotepadPaintAiMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/NotepadPaintAiMonitor.cs) | Notepad Rewrite + Paint Cocreator/Image Creator/Generative Erase AI features (HKCU) + Paint Image Creator policy (HKLM). Windows AI tab. |
| [`SettingsSearchAiMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/SettingsSearchAiMonitor.cs) | Search-box AI / Bing suggestions — `BingSearchEnabled` (authoritative) + dynamic search box (HKCU) + `DisableSearchBoxSuggestions` policy (best-effort). Windows AI tab. |
| [`AiActionsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/AiActionsMonitor.cs) | AI actions (File Explorer / Share) — two `FeatureManagement\Overrides` `EnabledState` flags forced off (HKLM). Windows AI tab. |
| [`InputInsightsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/InputInsightsMonitor.cs) | Typing insights / implicit text collection — `RestrictImplicitTextCollection` + `input\Settings\InsightsEnabled` (HKCU). Owns the value `InkingTypingMonitor` avoids. Windows AI tab. |
| [`OfficeCopilotMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/OfficeCopilotMonitor.cs) | Microsoft 365 Copilot — Word/Excel `EnableCopilot` + OneNote `CopilotEnabled` (HKCU) + Office AI training opt-out policy (HKLM). Windows AI tab. |
| [`WindowsServiceMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/WindowsServiceMonitor.cs) | One instance per service in `ServiceCatalog`. Maps `ServicePref.Desired` (Default/Manual/Disabled) to the matching elevated `sc.exe` call. |
| [`WindowsAiAppMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/WindowsAiAppMonitor.cs) | One instance per package in `WindowsAiAppCatalog` (Copilot, AI provider, AI Experience, Microsoft 365 Copilot). One-way `Remove-AppxPackage`. Windows AI tab. |

## `Native/`

P/Invoke wrappers — every Windows API the app touches.

| File | Surface |
|---|---|
| [`DisplayConfig.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/DisplayConfig.cs) | CCD APIs (`QueryDisplayConfig`, `DisplayConfigGet/SetDeviceInfo`, `SetDisplayConfig`) for HDR, display enumeration, and DRR (boost-refresh-rate flag). |
| [`DisplayHelper.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/DisplayHelper.cs) | Higher-level enumeration that pairs CCD source/target info with GDI device names. |
| [`DrrInterop.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/DrrInterop.cs) | Read/validate/set Dynamic Refresh Rate per target via `SetDisplayConfig` + `SDC_VIRTUAL_REFRESH_RATE_AWARE` (user-mode). `IsSupported` caches its `SetDisplayConfig(SDC_VALIDATE)` probe per display (via the swappable `SupportProbe` seam; `ClearSupportCache` drops it on a topology change) so it runs once per display instead of every poll — fixing the periodic input hitch. |
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
| [`MonitorService.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/MonitorService.cs) | The monitoring engine. **Two timers** — a fast timer re-checking only the `Volatile` (display) settings at the user's poll interval, and a 10-minute slow backstop for the `Stable` registry/policy/service settings — plus `SystemEvents` triggers (`PowerModeChanged`/`SessionSwitch`/`DisplaySettingsChanged`) that force a re-check at resume / unlock / display-reconfigure instead of polling everything every 30 s. Does pause-detection (fullscreen / benchmark / manual), drift collection, auto-apply dispatch, and periodic working-set trim. Detects **external resets** (drift on a setting it previously verified-applied → EXTRESET log lines + corrective "auto-revert" applies) and feeds an **auto-apply circuit breaker** that trips a setting Windows keeps reverting into notify-only for a cooldown, so it stops spamming UAC / display reconfigures every poll. |
| [`MonitorVolatility.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/MonitorVolatility.cs) | Classifies each monitored setting into a `MonitorTier`: `Volatile` (the display settings — HDR / refresh / resolution / DRR — that Windows actually changes mid-session) vs `Stable` (everything else, which only moves across a reboot / feature update). Lets `MonitorService` poll only the volatile tier on the fast timer. |
| [`AutoApplyCircuitBreaker.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/AutoApplyCircuitBreaker.cs) | Stops auto-applying a setting Windows keeps reverting. Counts external reverts per setting and, after N (default 3), trips it into a cooldown (default 15 min) where it's left to notify-only; one retry after the window, then re-trips. Pure + clock-injected (every method takes `now`) so the whole policy is unit-testable. |
| [`ChangeApplier.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ChangeApplier.cs) | Shared apply+verify path used by manual Apply *and* the auto-apply loop. Re-runs `CheckDrift` after applying to confirm the value landed. |
| [`ChangeLogger.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ChangeLogger.cs) | Writes `changes.log` entries. Handles rotation. |
| [`SettingDocs.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/SettingDocs.cs) | Per-setting `MechanismFor` / `VerifyCommandFor` / `ReverseCommandFor` (restore-default PowerShell) — drives both the Apply Results window and the change log "Mechanism" + "Verify" lines (plus the copy-pasteable reverse command). |
| [`BenchmarkDetector.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/BenchmarkDetector.cs) | Process list scan against an allowlist of common benchmark executables. |
| [`ElevatedRegistry.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ElevatedRegistry.cs) | Spawns `reg.exe`/`cmd.exe` (absolute System32 paths) with `Verb=runas` for HKLM writes (single UAC prompt). Batched add (`SetHklmMulti`), delete (`DeleteHklmMulti`), and mixed add+delete (`ApplyHklmBatch`), with an allowlist guard that rejects shell metacharacters in any segment and whitelists the `REG_*` type token. |
| [`SettingDocsCatalog.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/SettingDocsCatalog.cs) | Long-form per-setting docs (What/Why/Scenarios/Risks/Reversal) plus per-choice pros/cons (`ProsConsFor`/`ProsConsById`, returning `ChoiceTradeoff`s; service/UWP-app rows synthesized from the catalog). `FormatForExpander` renders the Learn-more expander — the long-form doc, the pros/cons, and a "Command line (PowerShell)" block. Source for the generated SETTINGS-REFERENCE.md. |
| [`SettingsReferenceGen.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/SettingsReferenceGen.cs) | Renders `SettingDocsCatalog` to `docs/SETTINGS-REFERENCE.md` (`--gen-docs`); a unit test asserts the committed file matches. |
| [`SettingRecommendations.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/SettingRecommendations.cs) | Single source of truth for each toggle's recommended `DesiredOn`. Also exposes `ExtremeDesiredOn` (the aggressive preset) and `WindowsDefaultDesiredOn` (the reset-to-Windows-defaults map) consumed by `RecommendedPreset`. |
| [`RecommendedPreset.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/RecommendedPreset.cs) | One-click presets that mutate a draft `AppConfig` (no commit until Save/Apply): `ApplyToDraft` (gaming-recommended), `ApplyExtremeToDraft` (aggressive), `ResetToDefaultsToDraft` (Windows defaults). Idempotent; skips security toggles (Memory Integrity / VBS) and UWP-AI removal by design. |
| [`AppConfigCloner.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/AppConfigCloner.cs) | Deep-clones `AppConfig` via JSON round-trip — the draft copy the Settings window mutates freely until Apply / Save&close. |
| [`DisplayPreferenceResolver.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/DisplayPreferenceResolver.cs) | Resolves a display's `DisplayPreference` by `StableKey`, tolerating a monitor that enumerates with/without a `DevicePath` across ticks. |
| [`NotificationHeader.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/NotificationHeader.cs) | Picks the drift-notification header from the report's contents (names the dominant category, else a generic "Monitored settings") instead of the old hard-coded display string. |
| [`RebootHelper.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/RebootHelper.cs) | `ForceRebootNow` — restarts Windows immediately via `shutdown /r /f /t 0` (force flag explicit so unsaved apps can't silently block it). |
| [`CpuDetector.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/CpuDetector.cs) | Detects the installed CPU once at startup via a lightweight registry read (no WMI/CPUID) and caches it. Pure static `Parse` for testability. |
| [`CpuTuneCatalog.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/CpuTuneCatalog.cs) | Static catalog of per-CPU gaming tunes, prebuilt recommendations, and advisory BIOS guidance (mirrors `ServiceCatalog`). Sole owner of CCD topology + parking-strategy classification — parking keys on cache asymmetry, not raw CCD count. |
| [`CpuPlanBuilder.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/CpuPlanBuilder.cs) | Decides whether to create / reuse / re-tune a power scheme and builds a Balanced-clone tuned plan from the recipe's processor overrides. |
| [`CpuPlanApply.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/CpuPlanApply.cs) | Wraps the build / suggest-prebuilt actions as one-off `cpuplan` `DriftItem`s run through `ChangeApplier` (so they inherit the change log + Apply Results window); the Apply lambda self-verifies its post-conditions. |
| [`CpuPlanStatus.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/CpuPlanStatus.cs) | Reports the status of the asymmetric dual-CCD X3D routing dependencies (AMD service state → `CcdDependencyStatus`) for the CPU·Power tab. |
| [`WindowsAiAppCatalog.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/WindowsAiAppCatalog.cs) | Curated list of Windows-AI UWP packages the app can offer to remove (per-user `Remove-AppxPackage`); each can auto-apply so a Windows-Update re-provision is yanked again next tick. |
| [`WindowsServiceController.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/WindowsServiceController.cs) | Reads service start type from registry; spawns `sc.exe` with `Verb=runas` for stop+disable / stop+set-manual / restore-default. |
| [`ServiceCatalog.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ServiceCatalog.cs) | The static list of Windows services GamerGuardian knows about. |
| [`TempCleanup.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/TempCleanup.cs) | Sweeps stale auto-update installer EXEs from `%TEMP%` (>1 day old). |
| [`ThemeService.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ThemeService.cs) | Light/dark/system theme switch via `Wpf.Ui.Appearance.ApplicationThemeManager`. |
| [`UpdateService.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/UpdateService.cs) | GitHub Releases API check + installer download. Enumerates **all** releases and picks the highest stable semver (drops drafts/prereleases) so it always upgrades to the newest version, never an intermediate one. Pure `ParseReleases`/`SelectBestUpdate` core. |
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
