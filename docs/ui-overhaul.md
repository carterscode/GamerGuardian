# UI overhaul — recorded decisions

Decisions made for the UI and navigation overhaul. This file exists so the
decisions survive across sessions. It records what was decided, not why, and not
what remains to be built.

Status: decisions recorded. Tab extraction not started.

## Framework

- Staying on **WPF** with **WPF-UI 3.0.5**.
- Not moving to WinUI 3. Not moving to Avalonia.

## Window and navigation

- **One window.** The existing `TabControl` is replaced by the WPF-UI
  **`NavigationView`**.
- Navigation is grouped by **user intent, not subsystem**:

```
Status                      (pinned, above the groups)
Performance
  ├─ Gaming
  ├─ Display
  └─ CPU and power
Privacy
  ├─ Telemetry
  ├─ Windows AI
  └─ Network
Cleanup
  ├─ Debloat
  └─ Services
Reference
  └─ BIOS
General                     (footer)
```

- **Network under Privacy is provisional**, pending what that section actually
  contains.

## Setting model

- Every managed setting surfaces **three properties** in the UI:
  1. **Desired state**
  2. **Current state**
  3. **Enforcement mode** — one of *auto apply* / *monitor only* / *unmanaged*
- Enforcement mode has a **section-level default with per-setting override**.

## Monitoring surface

- **Drifted count is the only status surface.** It is shown in the window, per
  section, and in aggregate.
- Pause remains visible in the window, not tray-only.

### Decided against

Cut from the plan. Not deferred — decided against.

- Managed count.
- Last-scan timestamp.
- Pause reason exposure (fullscreen / benchmark / user).
- Persisting user pause across restarts.
- Exposing the verify-backoff and circuit-breaker suspended set.

## Elevation

- **UAC stays per-action, `asInvoker`.** No manifest elevation change.

## Testing

- **FlaUI + xUnit** for UI tests.

## Search

- **Search index metadata is built during the tab extraction**, not added later.

---

## Open questions

Items from the enforcement-model investigation where the code does not currently
support a decision recorded above.

### Section-level enforcement default is not supported today

There is no section, group, or category concept anywhere in the config layer.
`GlobalPreferences` (`src/GamerGuardian/Models/AppConfig.cs:97-173`) is a flat
list of 40 individually-named `ToggleSettingPref` properties. Grouping exists
only as C# comments in that file (lines 124, 138, 148, 153, 165, 170) and as
which XAML tab a row is built into. There is no inheritance or override
mechanism to build on.

Open: where the section default is stored, and how an unset per-setting override
is represented (the current `bool Monitor` / `bool AutoApply` cannot express
"inherit" without becoming nullable or moving to an enum).

### Enforcement mode is two independent booleans, not a tri-state

The three modes exist and work per setting today, but they are derived from two
independent flags (`Monitor`, `AutoApply`) rather than one value. The
combination `Monitor=false, AutoApply=true` is representable in config and
resolves to unmanaged (`Monitor=false` wins, filtered at
`src/GamerGuardian/Services/MonitorService.cs:238`).

Open: whether the UI models enforcement as a single tri-state that is projected
onto the two booleans, or the config is migrated to an explicit enum, and what
happens to existing config files containing the fourth combination.

### Pause state is not persisted and its reason is not readable

`MonitorService.IsUserPaused` (`MonitorService.cs:65`) and the `PauseChanged`
event (line 66) are public, so the window can read and display user pause today.
Two gaps:

- The paused state is **not persisted** — `_userPaused` (line 31) is a private
  field with no `AppConfig` counterpart, so it resets to false on restart.
- The **automatic** pause reasons (fullscreen app, benchmark running) are held in
  the private `_activePauseReason` (line 32) with no accessor. The UI cannot show
  *why* monitoring is paused, only that the user paused it.

Open: whether pause should persist across restarts, and whether the automatic
pause reason needs to be exposed.

### There is no last-scan timestamp to bind to

No last-scan, last-check, or scan-time value is recorded anywhere in the
codebase. `_lastVerified` (`MonitorService.cs:51`) is a private per-setting
record of the last successful *apply*, not a scan time, and `ChangeLogger`
timestamps go to `changes.log` only.

Open: whether the Status page shows a last-scan time, which requires adding that
state to `MonitorService`.

### There is no single settings catalog for the search index

Setting metadata is scattered across `SettingDocsCatalog`, `SettingDocs`,
`SettingRecommendations`, `ServiceCatalog` (28 entries), `ScheduledTaskCatalog`
(5), `WindowsAiAppCatalog` (4), `CpuTuneCatalog`, and inline string literals in
`src/GamerGuardian/UI/SettingsWindow.xaml.cs`. The display names and
descriptions actually shown in the UI are the inline literals, not a catalog.
Section membership exists nowhere in data.

Open: the shape of the unified catalog the search index is built from, given the
decision that it is built during tab extraction.

### Requires-restart and needs-elevation are not queryable metadata

- **Requires restart** is per-`DriftItem` at runtime
  (`src/GamerGuardian/Models/DriftReport.cs:12`), set by 9 monitors, plus
  `ServiceDefinition.RequiresReboot`
  (`src/GamerGuardian/Models/ServiceDefinition.cs:15`). It cannot be read for a
  setting without running `CheckDrift`.
- **Needs elevation** has no flag anywhere. It is implicit in whether a monitor
  calls `ElevatedRegistry` (25 monitor files do) and appears as free prose in
  some `SettingDocsCatalog` `What:` text.

Open: whether the UI needs these as static per-setting metadata, which would
require adding them to a catalog.

### Network section contents

The Network tab today contains two settings: Nagle's algorithm and NIC power
management (`AppConfig.cs:170-172`), both latency/throughput tweaks. This is the
input to the provisional decision to place Network under Privacy.

### ConsolidateNotifications is stored but never read

`AppConfig.ConsolidateNotifications` (`AppConfig.cs:9`) is persisted, cloned
(`AppConfigCloner.cs:45`), and bound to a checkbox
(`SettingsWindow.xaml.cs:75, 1214`), but no code reads it to change notification
behavior. `Notifier` does not consult it.

Open: whether this setting is implemented, removed, or carried forward as-is.
