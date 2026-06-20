# Staged Apply + Verbose Logging Architecture

This document describes the architecture introduced in v0.1.38 that fixed the "every click immediately writes to disk and triggers a UAC prompt" bug from v0.1.37. The key idea: the Settings UI mutates a **draft copy** of `AppConfig`; the live config and `config.json` are untouched until the user explicitly clicks **Apply** or **Save & close**. Cancel (and closing the window without Save) discards the draft.

## Diagram

```
+-------------------------------------------------------+
|                  SettingsWindow                       |
|                                                       |
|  on open:                                             |
|     _config = _store.Load();                          |
|     _draft  = AppConfigCloner.Clone(_config);  -------+----+
|                                                       |    |
|  every row binds to _draft.* references.              |    |  the UI
|  on toggle: row setter writes to _draft, increments   |    |  is fully
|             _pendingCount, logs PREF-STAGE.           |    |  isolated
|                                                       |    |  from disk
|  Cancel:    _suppressSaveOnClose = true; Close()      |    |  and from
|             (nothing committed -- draft discarded)    |    |  the
|                                                       |    |  background
|  Apply / Save & close:                                |    |  monitor
|     AppConfigCloner.CopyInto(_draft, _config);  -+    |    |  until the
|     _store.Save(_config);                        |    |    |  user
|     CheckDrift + Apply + Verify pass             |    |    |  commits.
|     RebaseDraftFromConfig();                     |    |    |
|     LoadGlobals / Services / WindowsAi (...);    |    |    |
+--------------------------------------------------|----|----|
                                                   |    |
+-------------------+        +--------------------+|    |
|  MonitorService   |        |   ConfigStore      ||    |
|                   |        |                    ||    |
|  on each tick:    | reads  |  config.json       |<+   |
|     _store.Load()  ------> |                    |     |
|     CheckDrift                                  |     |
|     (auto-apply if AutoApply)                   |     |
|                                                 |     |
|     tracks _lastVerified[settingId] for         |     |
|     external-reset detection                    |     |
+-------------------+        +--------------------+     |
                                                        |
+----------------+ writes verbose lines for             |
| ChangeLogger   | every commit, drift, external reset, |
|                | session start, etc.                  |
| changes.log    |<--------------------------------------+
+----------------+
```

## Data shapes

### `AppConfig` (`Models/AppConfig.cs`)

Plain POCO. JSON-serialized to `%APPDATA%\GamerGuardian\config.json`. Fully deep-cloneable via JSON round-trip (that's what `AppConfigCloner.Clone` does). Background components like `MonitorService` keep a reference to the live `_config` instance; that's why `AppConfigCloner.CopyInto(source, target)` exists -- it commits the draft's field values into the existing live reference without breaking the captured pointer.

### `IMonitoredSetting` (`Monitors/IMonitoredSetting.cs`)

```csharp
public interface IMonitoredSetting
{
    string Id { get; }
    IEnumerable<DriftItem> CheckDrift(AppConfig config);
}
```

One implementation per managed setting kind (`HagsMonitor`, `WindowsServiceMonitor`, `CopilotMonitor`, etc.). `CheckDrift` reads the current OS state and yields `DriftItem` records only when current != desired. Each `DriftItem` carries an `Apply` lambda the runner calls; the lambda is responsible for performing the change (often shelling out to `sc.exe`, `reg.exe`, or AppX cmdlets).

### `DriftItem` (`Models/DriftReport.cs`)

```csharp
public sealed record DriftItem(
    string SettingId, string DisplayKey, string DisplayLabel, string Description,
    string CurrentValue, string DesiredValue, bool AutoApply, Func<Task> Apply,
    bool RequiresReboot = false, bool IsMonitored = true,
    string RawBefore = "", string RawDesired = "");
```

`RawBefore` / `RawDesired` carry the actual underlying values (registry dwords, sc-start-types, package names) so the log can record both display and raw forms.

### `ApplyResult` (`Models/ApplyResult.cs`)

The verbose record per applied change. Fields the log writes: `SettingId`, `Description`, `Before/Desired/After` (both display + raw), `Mechanism`, `ApplyCommand` (PowerShell), `VerifyCommand` (PowerShell), `ElapsedMs`, `Source` (manual / auto / auto-revert), `SessionId`, `ErrorMessage`, `ExternalResetDetected`, `StickinessCount`.

### `SettingDetails` (`Models/SettingDetails.cs`)

Long-form documentation per setting. Populated in `Services/SettingDocsCatalog`. Rendered to `docs/SETTINGS-REFERENCE.md` by `Services/SettingsReferenceGen` and surfaced in the UI's "Learn more" expander.

## Lifecycle traces

### User opens Settings, toggles a service to Disabled, clicks Apply

1. `SettingsWindow` ctor: `_config = _store.Load(); _draft = AppConfigCloner.Clone(_config);`.
2. `LoadServices()` populates `ServiceRows` with `ServiceRow` instances whose `_pref` references point at `_draft.Services[name]`.
3. User clicks the "Disabled" radio. `ServiceRow.DesiredDisabled` setter calls `SetDesired(ServiceTargetState.Disabled)` which mutates `_draft.Services[name].Desired`. Fires `_onPrefChanged` callback.
4. `SettingsWindow.OnRowPrefChanged` logs `[PREF-STAGE]` to `changes.log` and increments `_pendingCount`. Status text shows "1 pending change."
5. (Nothing else happens. No disk write. No UAC. Background `MonitorService` still sees the un-modified `_config` via `_store.Load()` on next tick.)
6. User clicks Apply. `ApplyButton_Click` -> `ApplyChangesAsync(closeAfter: false)`:
   - `PersistFormToDraft()` flushes the form-level controls (LaunchAtStartup, PollSeconds, Theme, PowerPlan combo) into `_draft`.
   - `AppConfigCloner.CopyInto(_draft, _config)` commits the draft.
   - `_store.Save(_config)` writes `config.json`.
   - `CheckDrift` runs against `_config` for every monitor; returns the list of `DriftItem`s the user just caused.
   - `ChangeApplier.ApplyAndVerifyAsync(...)` runs each `Apply` lambda, then re-runs `CheckDrift` to verify. Per-item timings captured.
   - `ChangeLogger.LogApplyResults(results, "manual")` emits the `[APPLY-START]` / per-change record / `[APPLY-END]` lines.
   - `_monitorService.RecordVerifiedApplies(results)` seeds the in-memory `_lastVerified` table so the next background tick can correctly detect external resets without a one-cycle blind spot.
   - `RebaseDraftFromConfig()` re-clones `_config` into a fresh `_draft` and resets `_pendingCount`.
   - UI rebuilds rows from the new `_draft`. `ApplyResultsWindow.Show(...)` displays the per-change verify result.

### User clicks Save & close after a successful Apply

1. `SaveButton_Click`: `if (_pendingCount == 0) { Close(); return; }`.
2. Because the previous Apply rebased the draft and reset the pending count, this guard short-circuits. No drift check, no UAC, just close.

### Background tick auto-applies a drifted setting and detects an external reset

1. `MonitorService.TickAsync` runs every `PollIntervalSeconds`. Loads `_config` from disk and runs `CheckDrift` for every monitor.
2. For each `DriftItem` where `_lastVerified` has an entry, that's by definition an external reset (Windows or another tool changed a value we'd previously applied). `_stickiness[settingId]` is incremented and `[EXTRESET]` is logged.
3. `auto` (the subset with `AutoApply == true` and not in the 15-minute backoff window) is split into `corrective` (settings with EXTRESET) and `initial`.
4. Each gets its own session id. Corrective applies log as `source=auto-revert` with `ExternalResetDetected = true` and the current stickiness count, so a single `grep '\[EXTRESET'` / `grep 'auto-revert'` answers "what does Windows keep undoing, and what's GamerGuardian doing about it?"
5. Verification failures move the setting into the 15-minute backoff so a stubborn setting doesn't pop UAC every 30 seconds.

### User clicks one of the three General-tab preset buttons

The General tab has three one-click preset buttons. **All three stage into the draft only** -- they never touch the live `_config` or `config.json`. Like every other staged change, they commit only when the user later clicks **Apply** or **Save & close**. All three route through the shared `RunPreset(presetName, apply)` helper in `SettingsWindow.xaml.cs`:

1. The button's click handler calls `RunPreset("…", () => RecommendedPreset.<Builder>(_draft))`.
2. The builder mutates `_draft` directly (no per-row `PropertyChanged` fires) and returns a `RecommendedPreset.Result(SettingsChanged, SettingsAlreadyCorrect, ChangeDescriptions)`. Each per-setting delta it stages is logged as an ordinary `[PREF-STAGE]` line with `field=preset` (see below) -- **no new line type**.
3. `RunPreset` bumps `_pendingCount += result.SettingsChanged` (so Save & close doesn't short-circuit), calls `UpdatePendingStatus()`, then rebuilds **every** row collection (`LoadGlobals` / `LoadDisplays` / `LoadServices` / `LoadWindowsAi` / `LoadPrivacy` / `LoadDebloat` / `LoadNetwork` / `LoadCpuTabs`) so the UI reflects the mutated draft. The row setters short-circuit on equality, so rebinding doesn't double-log.
4. `RunPreset` updates `RecommendedStatusText` and pops an informational summary MessageBox ("Staged N change(s)… Click Apply or Save & close to commit").

The three buttons (`Services/RecommendedPreset.cs`):

| Button | Builder | What it stages | Confirmation? |
|---|---|---|---|
| Apply recommended | `RecommendedPreset.ApplyToDraft` | Conservative gaming preset: the hand-picked subset of toggles at their recommended gaming value, plus services / displays. **Memory Integrity / VBS and UWP AI-app removal are deliberately excluded** (security tradeoff + irreversible), and **the power plan is excluded** -- it stays a one-time, user-initiated setup on the CPU / Power tab. Monitor + Auto-apply turned **on** for each touched setting. | none |
| Apply extreme | `RecommendedPreset.ApplyExtremeToDraft` | Every managed toggle at its most-aggressive gaming value -- including **Memory Integrity / VBS off** and the contested **Nagle / NIC** tweaks -- plus services and displays. Monitor + Auto-apply **on**. Still excludes irreversible UWP AI-app removal, and like Recommended **does not touch the power plan** (CPU / Power tab only). | yes -- a warning `MessageBox` (kernel-driver protection / anti-cheat / reboot) before `RunPreset` is called |
| Reset all to defaults | `RecommendedPreset.ResetToDefaultsToDraft` | Every managed setting back to its Windows out-of-box value, with Monitor + Auto-apply turned **off** so a subsequent Apply restores defaults and GamerGuardian stops re-asserting. Displays are only un-monitored (their Want is hardware-specific). UWP AI-app removals are not touched, and **the power plan is left exactly as the user set it** (isolated from the presets). | yes -- a warning `MessageBox` before `RunPreset` is called |

All three are **idempotent**: each per-setting helper compares the draft's `(DesiredOn/Want, Monitor, AutoApply)` triple to the target and stages (and logs) only the deltas; a second click when everything already matches stages zero changes and reports "already correct". The `[PREF-STAGE]` `settingName` carries a `[Recommended]` / `[Extreme]` / `[Reset]` tag prefix so the log shows which preset staged each delta.

## Log schema (`changes.log`)

| Marker | Written by | Meaning |
|---|---|---|
| `[SESSION   ]` | `ChangeLogger.LogSessionStart` | App started. Includes version, OS, CLR, machine, user (elevated y/n), PID, config path. |
| `[PREF-STAGE]` | `OnRowPrefChanged` | User toggled a draft preference. Not applied yet. |
| `[APPLY-START]` | `LogApplyResults` | A batch of changes is about to apply. Includes session id, source, count. |
| `[manual    ]` etc. per-record | `LogApplyResults` | One verbose entry per change. Multi-line: settingId, location, before/desired/after, applyCmd, verifyCmd, elapsedMs, status. |
| `[APPLY-END  ]` | `LogApplyResults` | Same session id. Includes `verified=N/M` summary and total elapsed ms. |
| `[EXTRESET  ]` | `LogExternalReset` | Windows or another tool changed a value we'd previously applied. Includes how long the previous applied value held and the current stickiness count. |
| `[CIRCUIT   ]` | `LogCircuitBreaker` | The auto-apply circuit breaker tripped: Windows kept reverting a setting, so GamerGuardian suspended re-applying it for a cooldown (avoids spawning a UAC prompt / display reconfig every poll). Multi-line: `settingId`, `reason` (revert count -- "Windows reverted this N time(s) in a row"), `cooldown` (not auto-applied for D; retries once after), `action` (leave it notify-only, or untick Auto-apply). |
| `[PAUSE     ]` | `LogPauseEvent` | MonitorService entered or left a paused state (fullscreen, benchmark, user manual). |
| `[MEM       ]` | `LogMemorySnapshot` | Periodic process memory snapshot. |

Source tags on per-change records:

| Source | Origin |
|---|---|
| `manual` | User clicked Apply or Save & close. |
| `auto` | Background MonitorService tick auto-applied a setting that had drifted and had no prior verified state (i.e. first time we've seen it drift). |
| `auto-revert` | Background tick auto-applied a setting Windows had externally reset (an EXTRESET line was logged immediately before). |

## Settings-reference doc

`docs/SETTINGS-REFERENCE.md` is **generated** from `Services/SettingDocsCatalog`. To regenerate:

```pwsh
dotnet build src/GamerGuardian/GamerGuardian.csproj -c Debug
.\src\GamerGuardian\bin\Debug\net8.0-windows10.0.22000.0\GamerGuardian.exe --gen-docs docs\SETTINGS-REFERENCE.md
```

A unit test in `tests/GamerGuardian.Tests/SettingsReferenceGenTests.cs` asserts the committed file matches the generated output -- so the doc and the catalog can't drift apart silently.

## File map

| Concern | File |
|---|---|
| Deep clone / commit | `Services/AppConfigCloner.cs` |
| Per-setting docs (data) | `Models/SettingDetails.cs` |
| Per-setting docs (content) | `Services/SettingDocsCatalog.cs` |
| Markdown rendering | `Services/SettingsReferenceGen.cs` |
| Apply orchestration | `Services/ChangeApplier.cs` |
| Verbose logger | `Services/ChangeLogger.cs` |
| Background monitor + external-reset detection | `Services/MonitorService.cs` |
| One-line mechanism / verify / apply PowerShell | `Services/SettingDocs.cs` |
| Draft UI + Apply/Save&close/Cancel + preset buttons | `UI/SettingsWindow.xaml.cs` |
| Recommended / Extreme / Reset preset draft builders | `Services/RecommendedPreset.cs` |
| CPU-aware power-plan recipes (used by `SetPowerPlan` in the presets) | `Services/CpuTuneCatalog.cs` |
| Verbose per-change result UI | `UI/ApplyResultsWindow.xaml.cs` |
