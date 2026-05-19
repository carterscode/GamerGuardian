# feat/gaming-optimize — execution plan

Scope tracker for the `feat/gaming-optimize` branch. Integrates features from `~/Downloads/GamingOptimize-Guide.md` (the user's PowerShell optimization script) as `IMonitoredSetting` implementations in GamerGuardian, where the model fits.

## Goal

Pull what's in `GamingOptimize.ps1` into the app so the "ensure these stay correct after every Windows / driver update" promise covers the script's permanent-state targets, not just the existing HDR / refresh rate / Game Mode / etc.

## Infrastructure already in place on this branch

- `App.IsDevBuild()` returns true for:
  - `#if DEBUG` local builds, and
  - CI dev-builds whose `InformationalVersion` contains `-dev` (stamped by `.github/workflows/dev-build.yml`).
- Dev builds skip `StartupRegistration.Sync()` (won't clobber the installed app's `HKCU\Run` entry) and skip the auto-update check (won't try to "upgrade" themselves to whatever `/releases/latest` returns).
- Settings version label and tooltip show `(dev)` for any dev flavor.
- `.github/workflows/dev-build.yml` is already wired by an earlier change on main — pushes to any non-`main` branch produce a single-file EXE + Inno installer as workflow artifacts (no GitHub Release, no tag). Retention 30 days. Version = `<next-stable-patch>-dev.<sha7>`.

## Audit — guide feature vs current state

| Guide feature | Already in GG | In-flight elsewhere | Candidate for this branch |
|---|---|---|---|
| HAGS / Game Mode / Game DVR (basic) | ✓ | — | — |
| Game Bar full lockdown (5 reg keys, including HKLM `AllowGameDVR` policy) | partial | — | **extend** `GameDvrMonitor` |
| Cross-Device Platform (EnableCdp) | ✗ | — | **new monitor** |
| Phone Link UWP installed | ✗ | — | new monitor |
| NVIDIA Overlay scheduled task | ✗ | — | new monitor |
| Defender exclusions for Steam libs | ✗ | — | new monitor (list-based) |
| DiagTrack / SysMain / vendor services | ✗ | `feature/disable-services` (and `fix/services-*` branches) | skip — separate branch's territory |
| Run-key startup item cleanup | partial overlap with "Launch at startup" | — | maybe — list-based, different model |
| One-shot process kills, restore point | ✗ | — | "Tools" panel, not a monitor — defer |

## Implementation order

### Tier 1 — registry-only monitors, fit existing pattern

**A. Cross-Device Platform** (~35 LOC)
- Settings: `HKLM\SOFTWARE\Policies\Microsoft\Windows\System\EnableCdp` (DWORD)
- Also writes: `HKCU\...\CDP\CdpSessionUserAuthzPolicy`, `RomeSdkChannelUserAuthzPolicy`
- Toggle: gaming-recommended OFF (Win11 default ON)
- HKLM write → UAC via `ElevatedRegistry.SetHklmDword`
- Reboot: **not** required, takes effect immediately

**B. Full Game Bar lockdown** (~25 LOC to extend)
- Extends existing `GameDvrMonitor` from 2 keys to all 5 the script writes:
  - `HKCU\Software\Microsoft\GameBar\ShowStartupPanel = 0`
  - `HKCU\Software\Microsoft\GameBar\UseNexusForGameBarEnabled = 0`
  - `HKCU\Software\Microsoft\GameBar\AllowAutoGameMode = 0` *(already monitored by `GameModeMonitor`, leave alone)*
  - `HKCU\System\GameConfigStore\GameDVR_Enabled = 0` *(already monitored)*
  - `HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR\AllowGameDVR = 0` *(new — HKLM policy override)*
- Decision: split the new HKLM policy lock into its own monitor (`GameDvrPolicyMonitor`) or roll into `GameDvrMonitor`? Probably its own monitor — the policy is a separate intent ("admin-locked DVR off" vs "user-pref DVR off").

### Tier 2 — new patterns, write after Tier 1 lands

**C. Phone Link installed/not** (~50 LOC, new "UWP package state" pattern)
- Detect: enumerate `Windows.Management.Deployment.PackageManager`-equivalent via WinRT projection (already available in `net8.0-windows10.0.22000.0`)
- Apply: `PackageManager.RemovePackageAsync`
- "Reverse" path: install from Store URI — probably out of scope, mark as one-way

**D. NVIDIA Overlay scheduled task** (~50 LOC, new "scheduled task state" pattern)
- Detect: `Microsoft.Win32.TaskScheduler` (via WinRT/COM `taskschd.dll`)
- Match: `TaskName -like 'NvNodeLauncher*' -and State -ne 'Disabled'`
- Apply: `Disable-ScheduledTask` equivalent
- "Reverse" path: `Enable-ScheduledTask`

**E. Defender exclusions for Steam libraries** (~80 LOC, new list-based pattern)
- Detect: `Get-MpPreference` exclusion paths (via `Microsoft.Management.Infrastructure` or shell out to `Get-MpPreference`)
- Discover: parse `libraryfolders.vdf` like the script does
- Apply: `Add-MpPreference -ExclusionPath` (shell-out is simplest)
- Decision: UI model — see open question 2 below

### Tier 3 — different model entirely, defer

- **System restore point creation** — one-shot button, not a monitor
- **Process kills** (Firefox / Steam web helpers) — one-shot tool, not a monitor
- **Run-key startup item cleanup** — list-based; partial overlap with `StartupRegistration` for GG itself

## Testing procedure

1. Push commit to `feat/gaming-optimize`.
2. `dev-build.yml` produces installer + portable EXE as workflow artifacts.
3. Download from `github.com/carterscode/GamerGuardian/actions` → latest dev-build run → Artifacts.
4. Install the dev installer (or run the portable EXE). It shows `(dev)` next to the version. Auto-update is disabled. Won't touch your `HKCU\Run` entry.
5. Open Settings → exercise the new monitor: read Current, flip Want, click Apply, confirm verification window + `changes.log` entry.
6. Quit the dev app. Your production installed app's behavior is unchanged because dev didn't touch the Run key.

Iterating: every commit triggers a new dev-build with a unique `-dev.<sha>` version. Test the artifact, then push the next fix.

## First-test checklist (before writing any new monitor)

After the `dev-build.yml` artifact for this branch lands (Actions tab → latest run → Artifacts), install it and verify these four invariants. If any fail, fix `App.IsDevBuild()` before anything else — the rest of the branch builds on it being reliable.

1. **Version label** in Settings reads `v0.1.30-dev.<sha7> (dev)` (or the next-stable-patch the auto-bump computed, with this commit's short SHA).
2. **`HKCU\Run\GamerGuardian`** still points to the installed production app, not the dev install. Confirm:
   ```powershell
   (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run').GamerGuardian
   ```
3. **No "Update available" popup** at startup.
4. **`%APPDATA%\GamerGuardian\changes.log`** still records normal `[manual]`, `[auto ]`, `[ui ]`, `[pause ]` entries when you exercise the existing settings via the dev build.

## Local conventions to honor when adding monitors here

- **Action SHA-pin** any new step. The repo's existing actions all look like `uses: actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6.0.2` for OpenSSF Scorecard. Don't introduce floating `@v4` refs.
- **Add tests** when the change touches `ServiceCatalog`, `SettingDocs`, or service-controller logic — those have existing coverage in `tests/GamerGuardian.Tests/` and `build.yml` runs `dotnet test` on every PR.
- **Follow the auto-save pattern** introduced in earlier sessions: row models invoke `onPrefChanged` from each setter, which both persists config and writes a `[ui ] PREF` line to `changes.log`. New monitors get this for free if they extend `GlobalToggleRow`.
- **Use `ElevatedRegistry.SetHklmDword` / `SetHklmString` / `SetHklmMulti`** for any HKLM write — they spawn `reg.exe` with `Verb=runas` (UAC). HKCU writes go through `Microsoft.Win32.Registry` directly.

## Open questions before implementation

1. **Tier 1 only first, or A+B+C bundled?** My recommendation: ship A then B as separate commits so each gets its own dev artifact for isolated testing.
2. **UI for list-based monitors (D, E):** add-remove pane vs. "auto-discover paths, single toggle"? Auto-discover is closer to the script's behavior (`Get-SteamLibraries`) but loses precision. The pane is more flexible. Pick one.
3. **HKLM `AllowGameDVR` policy** — separate monitor (own card) or extend `GameDvrMonitor` (single card with more registry writes)? My take: separate monitor — different intent.

## What's *not* in scope

- Touching services. `feature/disable-services` and its three `fix/services-*` siblings own that.
- Anything in the script's "What this script does NOT do" list (power plan, CPU parking, network adapter offloads, `bcdedit`, etc.) — those are explicitly avoided per the guide's authoritative tone.
- DRR (Dynamic Refresh Rate) — separate from VRR, deferred from previous session, still deferred here.
