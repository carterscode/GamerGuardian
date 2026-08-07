# GamerGuardian feature inventory

**Purpose.** A complete record of every user-facing surface as it exists *before* the
NavigationView overhaul. This is the contract for that work: every entry below must
still exist and still function after the rewrite. Read-only information screens count
— losing a panel that only *shows* something is still a regression.

**Captured at:** commit `58e1cc1` on `feature/ui-overhaul`, 2026-08-07.

**How to read the columns**

- **Displays** — what the user sees, including read-only text.
- **Reads** — config keys (`AppConfig` paths) and live system state.
- **Writes** — config keys and/or system state.
- **Elevates** — whether applying raises UAC. GamerGuardian runs `asInvoker`; HKLM
  writes spawn an elevated child (`Services/ElevatedRegistry.cs`,
  `WindowsServiceController`, `ScheduledTaskController`).
- **Tests** — coverage *today*. No test references any UI type; the column records
  coverage of the logic behind the surface.

---

## Shared window chrome (`UI/SettingsWindow.xaml`, outside the tabs)

| Element | Displays | Reads | Writes | Elevates | Tests |
|---|---|---|---|---|---|
| `WindowTitleBar` | Title "GamerGuardian" + BETA marker under `-p:Beta=true` | `AppIdentity.DisplaySuffix` | — | No | `AppIdentity` |
| `VersionLink` | App version, dev/beta suffix; tooltip with informational/file version, .NET runtime, build flavor; opens releases page | Assembly attributes | — | No | — |
| `PendingStatusText` | "N pending changes" staged-edit counter | in-memory `_pendingCount` | — | No | — |
| `VerifyAllButton` | Runs a full drift check, reports in-sync vs drifting counts, writes a state snapshot to the change log | all monitors, live system | `changes.log` | No | `ChangeLogger` |
| `CancelButton` | Discards staged edits (`_suppressSaveOnClose`) | — | — | No | — |
| `ApplyButton` | Commits draft → config, applies drift, opens Apply Results, raises reboot prompt | full config | config + system + `changes.log` | Per setting | `ChangeApplier` via `ApplyCommand` |
| `SaveButton` | Apply + close; short-circuits to plain close when nothing is staged | as Apply | as Apply | Per setting | as Apply |
| Learn-more `Expander` template | Per-setting long-form docs: what/why/how it helps/scenarios/recommended/risks/reversible; selectable text so commands can be copied | `SettingDocsCatalog` | — | No | `SettingDocsCatalog`, `SettingDocs` |
| Row template (shared) | Per setting: name, description, Current/Default/Recommended line, Monitor checkbox, desired-state radios, Auto-apply checkbox, reboot badge | per-setting pref + live state | draft pref | No (staging only) | `SettingRecommendations` |

---

## Tab 1 — General

| Aspect | Detail |
|---|---|
| **Displays** | Three one-click presets (Recommended / Extreme / Reset to defaults) each with an explanatory confirmation dialog; `RecommendedStatusText` staging summary; theme selector; launch-at-startup; check-for-updates-on-startup; manual "Check now" with result dialog; polling-interval number box; "Open change log" |
| **Reads** | `LaunchAtStartup`, `CheckForUpdatesOnStartup`, `PollIntervalSeconds`, `Theme`; `SkippedUpdateVersion`; installed version |
| **Writes** | Those same root keys; `StartupRegistration` HKCU `Run` value on apply; theme applied live; launches installer on update |
| **Elevates** | No (HKCU only). Update install runs an external installer |
| **Tests** | `RecommendedPreset` (all three presets, idempotency, dual-CCD guardrail, power-plan exclusion), `UpdateService` (selection, prerelease/draft skip, history extraction), `ReleaseNotesFormatter`, `AppConfigCloner` |
| **Notes** | Extreme's confirmation text warns about VBS/Memory Integrity and anti-cheat. Presets stage only — nothing writes until Apply. `CheckUpdatesNowButton_Click` is `#if BETA`-compiled to a "disabled in beta builds" message |

## Tab 2 — Global gaming

| Aspect | Detail |
|---|---|
| **Displays** | 13 toggle rows: Game Mode, Game DVR, HAGS, Memory Integrity, VBS, System Responsiveness, USB Selective Suspend, Games Task Profile, Mouse Precision, Fullscreen Optimizations, VRR, Fast Startup, Visual Effects. Memory Integrity's row shows "overridden by the VBS full-stack toggle below" when VBS owns the key |
| **Reads** | `Global.{GameMode,GameDvr,Hags,MemoryIntegrity,Vbs,SystemResponsiveness,UsbSelectiveSuspend,GamesTaskProfile,MousePrecision,FullscreenOptimizations,Vrr,FastStartup,VisualFx}`; live registry per monitor |
| **Writes** | Those prefs; on apply, HKCU and HKLM registry per monitor |
| **Elevates** | Yes for HAGS, Memory Integrity, VBS, System Responsiveness, USB Selective Suspend, Games Task Profile, Fast Startup, VRR, Game DVR (policy). No for Game Mode, Mouse Precision, FSO |
| **Tests** | `VbsMonitor` (compliance predicates, disable/enable op building, UEFI-lock detect, HVCI-blocked), `VisualEffectsMask`, `SettingSectionMap`, `SettingRecommendations`, `ApplyCommand` |
| **Notes** | VBS ↔ Memory Integrity defer rule (`MemoryIntegrityMonitor.DefersToVbs`) must survive. Several rows use inverted Gaming/Default labels |

## Tab 3 — Privacy

| Aspect | Detail |
|---|---|
| **Displays** | 6 toggle rows: Advertising ID, Tailored experiences, Cross-Device Platform, Activity History, Online speech recognition, Inking & typing personalization |
| **Reads** | `Global.{AdvertisingId,TailoredExperiences,Cdp,ActivityHistory,OnlineSpeech,InkingTyping}`; live registry |
| **Writes** | Those prefs; HKCU and HKLM policy values on apply |
| **Elevates** | Yes for CDP and Activity History (HKLM policy). No for the other four (HKCU) |
| **Tests** | `SettingSectionMap`, `SettingRecommendations`, `SettingDocsCatalog` |

## Tab 4 — Debloat

| Aspect | Detail |
|---|---|
| **Displays** | Two grouped lists — **Ads & suggestions** (`DebloatAdsList`) and **Background & bloat** (`DebloatBackgroundList`) — covering Suggested content, Lock screen tips, Finish-setup nag, Start recommendations, File Explorer ads, Feedback popups, Widgets, Edge startup boost/background |
| **Reads** | `Global.{SuggestedContent,LockScreenSpotlight,FinishSetupNag,StartRecommendations,ExplorerAds,FeedbackNag,Widgets,EdgeBackground}`; live registry |
| **Writes** | Those prefs; registry on apply |
| **Elevates** | Yes for Widgets and Edge background (HKLM policy). No for the rest (HKCU) |
| **Tests** | `Debloat` (defaults, drift, clone round-trip), `SettingSectionMap` |
| **Notes** | The two-list split is presentational and must be preserved as two groups |

## Tab 5 — Network

| Aspect | Detail |
|---|---|
| **Displays** | 3 rows: Network Throttling, Nagle's algorithm, NIC power management |
| **Reads** | `Global.{NetworkThrottling,Nagle,NicPower}`; live registry; enumerated physical adapters |
| **Writes** | Those prefs; per-adapter HKLM values on apply |
| **Elevates** | Yes, all three. Nagle and NIC power assert across every active physical adapter in one elevation prompt |
| **Tests** | `NetworkAdapters`, `SettingSectionMap`, `SettingDocsCatalog` |
| **Notes** | Network Throttling lives here (moved in v0.1.46) despite being stored in `AppConfig`'s ungrouped block |

## Tab 6 — Windows services

| Aspect | Detail |
|---|---|
| **Displays** | Two preset radio buttons (Gaming optimized / Windows default) reflecting current state; `ServicesList` with 28 catalog services — display name, description, current start type, policy-managed indicator, recommended badge, tri-state Default/Manual/Disabled, Monitor, Auto-apply, reboot badge. Plus `ScheduledTasksList` with 5 Application Experience tasks (Monitor, Disabled, Auto-apply) |
| **Reads** | `Services[name]`, `ScheduledTasks[path]`; live SCM start types; live task enabled state; `ServiceCatalog`, `ScheduledTaskCatalog` |
| **Writes** | Those prefs; on apply, service start type via `sc.exe`, or the documented Group Policy override for policy-managed services (e.g. DoSvc); scheduled task enabled flag via `schtasks` |
| **Elevates** | Yes for both services and tasks |
| **Tests** | `ServiceCatalog`, `WindowsServiceController`, `ScheduledTaskCatalog`, `ScheduledTaskController`, `ScheduledTaskMonitor`, `ScheduledTaskConfig` |
| **Notes** | Preset radios are two-way — they reflect state as well as set it. Policy-override display state must survive |

## Tab 7 — Windows AI

| Aspect | Detail |
|---|---|
| **Displays** | `WindowsAiRows` with 9 policy toggles (Copilot, Recall, Click-to-Do, Edge AI, Notepad/Paint AI, Search-box AI, AI Actions, Input insights, Office Copilot) using Enabled/Disabled labels; `WindowsAiAppRows` with 4 UWP packages offering removal (Monitor, Remove, Auto-apply) |
| **Reads** | `Global.{Copilot,Recall,ClickToDo,EdgeAi,NotepadPaintAi,SettingsSearchAi,AiActions,InputInsights,OfficeCopilot}`, `WindowsAiApps[package]`; live registry policy; installed AppX packages |
| **Writes** | Those prefs; HKLM+HKCU policy values; AppX package removal |
| **Elevates** | Yes for the policy toggles (HKLM). App removal uses the AppX APIs |
| **Tests** | `WindowsAi`, `SettingSectionMap`, `SettingDocsCatalog` |
| **Notes** | App removal is effectively irreversible without the Store — the warning text must survive |

## Tab 8 — Display

| Aspect | Detail |
|---|---|
| **Displays** | One card per active display: label, and a "Now" status line with current HDR / refresh / resolution / DRR. Controls for HDR (Monitor/On/Auto-apply), DRR (shown only when supported), Refresh (Maximum vs Fixed + rate dropdown), Resolution (dropdown + Monitor/Auto-apply) |
| **Reads** | `Displays[stableKey].{Hdr,RefreshRate,Resolution,Drr}`; live display state — HDR support/enabled, current + supported refresh rates, max supported, current + supported resolutions, DRR support |
| **Writes** | Those prefs; on apply, display configuration via the CCD/DisplayConfig APIs and `ChangeDisplaySettingsEx` |
| **Elevates** | No — display APIs are user-mode |
| **Tests** | `DisplayPreferenceResolver` (stable keys, dedupe), `DrrInterop` |
| **Notes** | Per-display keys must stay stable (v0.1.41 fix). A saved Fixed refresh target is kept selectable even if the panel momentarily reports fewer modes. Displays are the only `Volatile`-tier settings — they drive the 30s poll |

## Tab 9 — CPU / Power

| Aspect | Detail |
|---|---|
| **Displays** | Detected CPU model; recipe tier line (exact/family/generic, topology, parking strategy, recommended prebuilt); Power Throttling toggle row; power-plan card with Current plan, Recommended plan, Monitor, Want dropdown of all installed plans, Auto-apply; plan-build card with status text and two actions (Suggest best prebuilt, Build optimized plan); **"What this plan changes" expander** with the base-plan summary, the Windows-vs-GamerGuardian side-by-side comparison chart, and the per-CPU rationale; **CCD dependency card** (asymmetric X3D only) listing AMD 3D V-Cache Optimizer service state, Xbox Game Bar state, advisory BIOS CPPC note, and a met/unmet summary |
| **Reads** | `Global.PowerThrottling`, `Global.PowerPlan.*`, `Global.CpuPlan.*`; `CpuDetector`, `CpuTuneCatalog`; installed power schemes via `Powrprof`; live AC values of the base scheme for the comparison chart; AMD service + Game Bar state |
| **Writes** | Those prefs; on action, creates/re-tunes a GG power scheme, writes processor overrides on both rails, sets the active scheme, persists scheme identity |
| **Elevates** | Yes for Power Throttling (HKLM). Plan build/activate uses `Powrprof` (may prompt depending on operation) |
| **Tests** | `CpuDetector`, `CpuTuneCatalog`, `CpuPlanBuilder` (create/reuse/re-tune, delete guard, machine-token binding), `CpuPlanDetails` (comparison rows, rationale, plan naming), `CpuPlanStatus`, `PowerPlanMonitor` |
| **Notes** | **Densest tab by far.** The comparison chart and CCD dependency card are read-only information surfaces and are the highest-risk items to lose. Power-plan combo preselects the CPU-recommended plan and must not stage a phantom pending change on load |

## Tab 10 — Recommended BIOS

| Aspect | Detail |
|---|---|
| **Displays** | Advisory-only list of BIOS recommendations for the detected CPU — name → recommended value, plus rationale per item. Falls back to "No CPU-specific BIOS recommendations" when the catalog has none |
| **Reads** | `CpuTuneCatalog` `BiosRecommendation` entries for the resolved CPU |
| **Writes** | Nothing |
| **Elevates** | No |
| **Tests** | `CpuTuneCatalog` |
| **Notes** | **Pure read-only information screen.** Hosts no managed setting, so it will never show a drift count. Easiest surface to accidentally drop |

---

## Windows

### `SettingsWindow` (`UI/SettingsWindow.xaml`)
Covered above. Single instance owned by `App`; minimize sends it to the tray; closing with staged edits discards them and logs the discard.

### `NotificationWindow` (`UI/NotificationWindow.xaml`)
| Aspect | Detail |
|---|---|
| **Displays** | Drift toast, bottom-right: header summarising the drifted set, list of drifted items, Apply and Dismiss |
| **Reads** | `DriftReport` from `MonitorService` (notify-only settings) |
| **Writes** | Applies the listed items on Apply; raises the reboot prompt for reboot-required items that applied |
| **Elevates** | Per setting |
| **Tests** | `NotificationHeader`, `MonitorService` (`SelectNotifiable` — auto-apply settings must never reach here) |

### `ApplyResultsWindow` (`UI/ApplyResultsWindow.xaml`)
| Aspect | Detail |
|---|---|
| **Displays** | Per-setting before → want → now, success/failure icon, "reboot to take effect" badge, mechanism line, copyable verify command, Open change log, Close |
| **Reads** | `ApplyResult` list |
| **Writes** | Clipboard on copy; opens `changes.log` |
| **Elevates** | No |
| **Tests** | `ApplyCommand`, `SettingDocs` (mechanism/verify strings) |

### `RebootPendingWindow` (`UI/RebootPendingWindow.xaml`)
| Aspect | Detail |
|---|---|
| **Displays** | Bottom-right prompt listing settings that need a restart; Reboot now / Later |
| **Reads** | Descriptions passed by `RebootPrompt` |
| **Writes** | Triggers `shutdown /r /f /t 0` |
| **Elevates** | No (shutdown as the current user) |
| **Tests** | — (raised from `MonitorService` reboot path and `SettingsWindow` apply path) |

### `UpdateAvailableWindow` (`UI/UpdateAvailableWindow.xaml`)
| Aspect | Detail |
|---|---|
| **Displays** | New version header, current version, scrollable full release-notes history, download progress, Skip this version / Later / Download and install |
| **Reads** | `UpdateInfo`; `SkippedUpdateVersion` |
| **Writes** | `SkippedUpdateVersion`; downloads and launches the installer |
| **Elevates** | No (installer is per-user) |
| **Tests** | `UpdateService`, `ReleaseNotesFormatter` |
| **Notes** | **Excluded from the compile entirely under `-p:Beta=true`** — the beta flavor has no update path |

---

## Cross-cutting behaviour that must survive

1. **Staged apply.** Toggles mutate a draft, not live config. Nothing is written until Apply or Save & close. Cancel discards; closing with pending edits discards and logs it.
2. **Apply ignores the Monitor checkbox.** Clicking Apply means "do it now" regardless of whether the setting is monitored.
3. **Silent auto-apply stays silent.** A setting with Auto-apply on must never raise a notification — including when it is in verify-backoff or breaker cooldown (`MonitorService.SelectNotifiable`).
4. **Per-action UAC, `asInvoker`.** No manifest elevation. HKLM writes batch into one elevated child where possible.
5. **Reboot prompt fires once per apply**, at the end, from an unowned window that survives the Settings window closing.
6. **Drifted count** is the only status surface (per `docs/ui-overhaul.md`). No managed count, last-scan timestamp, pause reason, or pause persistence.
7. **Memory hygiene.** `OnClosed` releases `Content`, `DataContext`, and every `ItemsSource`; periodic GC + LOH compaction. Regressing this took working set from 23 MB to 135 MB historically.
8. **WPF-UI 3.0.5** — do not downgrade.

## Test coverage summary

- **UI surfaces: zero automated coverage.** No test source references `SettingsWindow`, any other window, or `System.Windows.*`.
- **Logic behind the surfaces: well covered** — 37 test files, 682 tests at `58e1cc1`.
- Consequence for the overhaul: the compiler and the test suite will *not* catch a lost UI surface. This document is the only check. Verification must be manual, entry by entry.
