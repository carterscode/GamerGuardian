# Changelog

All notable, user-facing changes to GamerGuardian. Newest first. This is the
release-notes file: the text you see when the app offers you an update comes
straight from here, and each [GitHub Release](https://github.com/carterscode/GamerGuardian/releases)
shows the same notes.

**How this stays current:** the release workflow looks for the section matching
the version it's building and uses it as the release body (falling back to
`[Unreleased]` if that version isn't listed yet). So when you make a user-facing
change, add a plain-English bullet under `[Unreleased]`; whoever cuts the next
release renames that heading to the new version number.

The format is loosely based on [Keep a Changelog](https://keepachangelog.com/).
Versions before 1.0.0 are pre-release: features and defaults may still change.

## [Unreleased]

### Changed
- **Clearer upgrade notes.** The "what's new" text shown when a new version is
  offered is now plain-English highlights only — no more raw list of internal
  pull-request titles — and Markdown formatting is cleaned up so it reads
  properly in the update window instead of showing `#` and `**` symbols.
- **A fuller history.** These release notes now carry a short, plain-English
  description of every version going all the way back to the first release, so
  you can see how GamerGuardian has grown over time.

## [0.1.63] - 2026-08-07

### Changed
- **The recommended Windows power plan is now Balanced, not High Performance.**
  This matches GamerGuardian's per-CPU advice — High Performance switches off core
  parking system-wide, which actually hurts modern multi-cluster chips like the
  Ryzen 9 9950X3D. The power-plan dropdown also preselects the recommended plan
  when you haven't chosen one yourself, instead of contradicting the "Recommended"
  hint shown right above it.
- **Custom plans are now named after the Windows plan they're based on**, e.g.
  `GamerGuardian Gaming [9950X3D · Balanced]`, so you can tell at a glance what
  your optimized plan started from. Existing plans are renamed in place, not
  duplicated.

### Added
- A **"What this plan changes"** section on the CPU / Power tab that spells out,
  in plain English, exactly which processor settings the optimized plan changes
  versus stock Windows — and why those changes suit your specific CPU (for
  example, parking the higher-frequency cluster on an X3D chip so games stay on
  the cache cluster).

## [0.1.62] - 2026-08-07

### Fixed
- **You're now reliably prompted to restart** after applying settings that need a
  reboot — including bulk actions like the Extreme preset, where the prompt could
  previously vanish before you saw it. A single restart prompt now appears once at
  the end of an apply, no matter how many settings changed.
- **"Silently apply" now truly means silent.** A setting you've set to auto-apply
  no longer pops a notification when it drifts — even in the cases where it briefly
  couldn't be re-applied — it's corrected quietly in the background, the way you
  asked.

## [0.1.61] - 2026-06-27

### Added
- **Disable the Windows "Application Experience" scheduled tasks** — the
  background telemetry and compatibility-housekeeping tasks — from a new Scheduled
  tasks group on the Windows services tab, with the same Monitor / Auto-apply
  controls as everything else.

## [0.1.60] - 2026-06-20

### Fixed
- The one-click presets (Recommended, Extreme, Reset) **no longer change your
  active Windows power plan.** Your power-plan choice is deliberately left alone
  and stays a one-time setup on the CPU / Power tab, so a quick preset can never
  quietly switch your plan out from under you.

## [0.1.59] - 2026-06-20

### Fixed
- Text in the **"Learn more"** panels is now selectable, so you can highlight and
  copy the verify commands and registry paths instead of retyping them.

## [0.1.58] - 2026-06-20

### Fixed
- **Mouse and keyboard hitch every ~30 seconds — the real cause.** The Dynamic
  Refresh Rate (DRR) "is this display capable?" check was running on every
  30-second poll for each monitor, and that check briefly re-validates the display
  configuration — which on many GPUs stalls the mouse and keyboard for a moment.
  DRR capability never changes while you're using the PC, so it's now checked
  **once** per display instead of every poll, and skipped entirely for displays
  you aren't monitoring. (It ran regardless of whether you were even monitoring
  DRR, which is why turning settings off didn't help.)

## [0.1.57] - 2026-06-20

### Maintenance
- Behind-the-scenes CI dependency update (GitHub Actions `checkout`). No app
  changes.

## [0.1.56] - 2026-06-20

### Fixed
- **Mouse and keyboard no longer hitch every ~30 seconds (auto-apply loop).** A
  monitored, auto-applied setting that Windows kept reverting was re-applied on
  every poll — and without admin rights each re-apply raised a UAC prompt (and
  display changes reconfigured the screen), seizing input. A circuit breaker now
  stops re-applying a setting Windows keeps fighting after a few tries and leaves
  it notify-only for a cooldown (logged as `[CIRCUIT]` in the change log).

### Changed
- **Far less background polling.** Only the display settings (HDR, refresh rate,
  resolution, DRR) — the ones Windows actually changes mid-session — are checked
  on the 30-second poll now. The ~40 set-and-forget registry, policy and service
  settings are re-checked at startup, when you resume from sleep, unlock the PC,
  or change your display, plus a slow 10-minute backstop — instead of every 30
  seconds.

## [0.1.55] - 2026-06-20

### Added
- **Extreme** and **Reset to defaults** one-click buttons alongside the
  Recommended preset: Extreme turns on every gaming tweak GamerGuardian knows (and
  enables Monitor + Auto-apply for each), while Reset stages everything back to
  Windows defaults.
### Changed
- Expanded the **Learn more** panels with more per-setting background so it's
  clearer what each toggle does before you flip it.

## [0.1.54] - 2026-06-18

### Fixed
- The **Windows AI** tab now uses **Enabled / Disabled** for the *Want* choice,
  matching every other tab (it previously said On / Off).
- The **Current / Default / Recommended** line under each setting now wraps
  instead of getting clipped, so the full **Recommended** value is always visible.

## [0.1.53] - 2026-06-18

### Changed
- Release notes are now **curated and user-facing**: each GitHub Release shows a
  hand-written summary of what changed (from this `CHANGELOG.md`) instead of a raw
  list of pull requests.

## [0.1.52] - 2026-06-18

### Changed
- **Recommendations now speak each setting's own language.** The green
  **Recommended** hint is shown in the row's actual wording — *Enabled/Disabled*,
  *Gaming/Default*, *On/Off*, or a service's *Default/Manual/Disabled* — instead
  of a generic "On/Off" that didn't match the buttons.
- **Recommendation values tuned for the typical desktop gamer** and checked
  against current best practice: clear performance wins are recommended; the
  Memory Integrity / VBS security toggles are recommended to stay **on** (some
  anti-cheat requires them); genuinely contested tweaks (Nagle, NIC power
  management) are left at the Windows default; the power-plan recommendation is
  CPU-aware. The one-click setup and the per-row hint now share one source of
  truth, so they can't disagree.

## [0.1.51] - 2026-06-18

### Added
- A **Recommended** value now appears next to *Current* and *Default* on every
  setting, so you can see GamerGuardian's suggested choice at a glance without
  opening the docs.

## [0.1.50] - 2026-06-15

### Added
- New **Debloat** tab: switch off Windows 11 ads, nags, suggested content,
  widgets, lock-screen "fun facts", File Explorer upsell banners, and Edge
  background processes — plus privacy data toggles.
- Optional removal of the **Microsoft 365 Copilot** app and six device-feature
  services for people who want them gone entirely.
### Fixed
- The updater now always offers the **highest** stable version, so you can't get
  stuck being prompted for an in-between build.

## [0.1.49] - 2026-06-14

### Changed
- The manual **Power plan** selector moved onto the **CPU / Power** tab, right
  next to the CPU-aware plan tools, so everything power-related lives in one place.

## [0.1.48] - 2026-06-11

### Fixed
- **"Reboot now" now restarts immediately** instead of silently doing nothing when
  an open app had unsaved state.

## [0.1.47] - 2026-06-11

### Added
- Complete **Virtualization-Based Security (VBS) disable** — the full stack, not
  just Memory Integrity — including every DeviceGuard scenario, Credential Guard,
  and the Group Policy mirror, all in a single UAC prompt, with UEFI-lock detection
  and honest risk docs. (Note: disabling VBS breaks Valorant, and Vanguard requires
  Memory Integrity — the app warns you before you do it.)

## [0.1.46] - 2026-06-05

### Changed
- Moved **Network Throttling** onto the Network tab and tidied the tab layout, and
  documented all nine Settings tabs.

## [0.1.45] - 2026-06-05

### Added
- New **Privacy** tab with telemetry and activity-history toggles, and a new
  **Network** tab with per-adapter **Nagle's algorithm** and **NIC
  power-management** tweaks.
- Per-display **Dynamic Refresh Rate (DRR)**, **Visual Effects (best
  performance)**, **Power Throttling**, and **Fast Startup** controls.
- Game DVR lockdown now also sets the machine-wide policy so it sticks across
  reboots.

## [0.1.44] - 2026-06-04

### Added
- **CPU / Power tab with CPU-aware power plans.** GamerGuardian detects your CPU
  and either suggests the best prebuilt plan or builds a custom optimized plan
  tuned to your chip — including a dual-CCD X3D guardrail that parks the
  frequency cluster so games stay on the cache cluster. Plus an advisory **BIOS**
  recommendations tab.
### Fixed
- Corrected the Windows **Power Saver** scheme GUID (the hardcoded value was
  wrong) by resolving schemes from the live system instead.

## [0.1.43] - 2026-06-04

### Fixed
- Stopped a recurring **"search-box AI" drift popup** by switching to reliable
  Windows 11 registry keys.

## [0.1.42] - 2026-06-04

### Maintenance
- Behind-the-scenes CI dependency updates. No app changes.

## [0.1.41] - 2026-06-03

### Fixed
- Per-display preferences (refresh rate, HDR, resolution) now use **stable display
  keys**, so they no longer reset when Windows re-enumerates your monitors.

## [0.1.40] - 2026-06-03

### Added
- One-click **Recommended** gaming preset on the General tab.
- Closer parity with community Windows-AI removal tooling (safe additions only).
### Fixed
- Removed a spurious silent-apply popup for the Search-box AI setting, fixed a
  config write race, and corrected a notification-header glitch.

## [0.1.39] - 2026-06-03

### Maintenance
- CI dependency updates. No app changes.

## [0.1.38] - 2026-05-19

### Added
- **Per-setting documentation** with a **Learn more** expander, a **Verify all**
  button, and a system-state snapshot, so you can understand and independently
  confirm every change.
- New **Windows AI** tab to lock down Copilot, Recall, Click-to-Do and related
  features, including optional removal of the underlying app packages.
### Changed
- Settings changes are now **staged** and only committed when you click Apply or
  Save & close — nothing is written the moment you flip a toggle.

## [0.1.37] - 2026-05-07

### Changed
- Minimizing the Settings window now sends it to the tray instead of the taskbar.
### Fixed
- The `sync-wiki` documentation tool now works on PowerShell 5.1.

## [0.1.36] - 2026-05-07

### Added
- Clear feedback when **Apply** has nothing to change, and a visible policy-state
  indicator for services that are managed by Group Policy.

## [0.1.35] - 2026-05-07

### Maintenance
- Security and code-quality fixes plus CI dependency bumps.

## [0.1.34] - 2026-05-07

### Fixed
- **Delivery Optimization** is now disabled via Group Policy, so Windows stops
  quietly turning it back on.

## [0.1.33] - 2026-05-07

### Maintenance
- Added a test project and a contributor guide, and turned on warnings-as-errors
  for a cleaner, safer build.

## [0.1.32] - 2026-05-07

### Fixed
- No more repeated **UAC prompts** when Windows reverts a service change — the app
  no longer fights the change in a loop.

## [0.1.31] - 2026-05-07

### Security
- Tightened CI permissions and added **SLSA build provenance** to releases, so a
  downloaded binary can be verified as built from this exact source.

## [0.1.30] - 2026-05-07

### Changed
- Layout polish so long service and setting names fit, tab content anchored to the
  top, and a published **no-data-collection privacy policy**.
### Security
- Pinned all GitHub Actions to exact commit SHAs and made the build deterministic.

## [0.1.29] - 2026-05-06

### Fixed
- Power-plan dropdown is now actually populated from every installed plan (the
  real fix after 0.1.28).

## [0.1.28] - 2026-05-06

### Fixed
- Power-plan dropdown now lists every installed plan — custom plans and Windows
  defaults alike.

## [0.1.27] - 2026-05-06

### Performance
- Lower memory use through large-object-heap compaction and releasing window
  content when windows close.

## [0.1.26] - 2026-05-06

### Added
- Pause and resume events are now recorded in the change log with the reason and
  the foreground game that triggered them.

## [0.1.25] - 2026-05-06

### Fixed
- Preference toggles now persist correctly and are recorded in the change log.

## [0.1.24] - 2026-05-06

### Fixed
- The **VRR** monitor now reads the correct registry value, so it reports and
  applies Variable Refresh Rate accurately.

## [0.1.23] - 2026-05-06

### Changed
- Clarified in the UI that **VRR is not the same as Dynamic Refresh Rate (DRR)**,
  which are easily confused.

## [0.1.22] - 2026-05-06

### Added
- A detailed change log that captures the **raw before/after registry values** for
  every applied setting, so you can see and reverse exactly what changed.

## [0.1.21] - 2026-05-06

### Fixed
- Fixed a critical settings-display binding bug and cleaned up temp-file handling.

## [0.1.20] - 2026-05-06

### Added
- A **persistent change log** of every setting GamerGuardian applies, saved to
  disk so you have a running history.

## [0.1.19] - 2026-05-06

### Added
- **Apply results** window showing each setting's before / wanted / actual value
  after apply, with a copy-paste command to verify it yourself.

## [0.1.18] - 2026-05-06

### Fixed
- **Apply** now applies regardless of the Monitor checkbox (clicking Apply means
  "do it now"), and each setting's *Want* syncs to the current value on load.

## [0.1.17] - 2026-05-06

### Added
- A **Check now** button in Settings for an on-demand update check.

## [0.1.16] - 2026-05-06

### Docs
- Professional README with a banner, badges, and per-setting references.

## [0.1.15] - 2026-05-06

### Changed
- Renamed "Check interval" to **"Polling interval"** to avoid confusion with the
  update check.

## [0.1.14] - 2026-05-06

### Added
- **Update check on startup** with one-click download and install, so you can stay
  current without visiting GitHub.

## [0.1.13] - 2026-05-06

### Added
- A non-modal **reboot popup** for settings that were auto-applied and need a
  restart, plus clearer Enabled/Disabled labels.

## [0.1.12] - 2026-05-06

### Fixed
- Fixes to the Apply button and reboot prompt, and clearer radio-button labels.

## [0.1.11] - 2026-05-06

### Changed
- Cleaner per-setting row layout with **Current** and **Default** status lines.

## [0.1.10] - 2026-05-06

### Added
- Tier 1 performance tweaks — four new monitored settings.

## [0.1.9] - 2026-05-06

### Added
- Monitor **Memory Integrity / VBS** (Core Isolation) so you can see and control
  its state.

## [0.1.8] - 2026-05-06

### Added
- Automatic **benchmark detection** (pauses monitoring during benchmarks), a
  manual pause, and a new app icon.

## [0.1.7] - 2026-05-06

### Fixed
- Development builds are now clearly distinguished from releases in the version
  label.

## [0.1.6] - 2026-05-06

### Added
- Monitoring now also pauses for **borderless-fullscreen** games, not just
  exclusive fullscreen.

## [0.1.5] - 2026-05-06

### Added
- The app version is now shown in Settings.

## [0.1.4] - 2026-05-06

### Performance
- Gaming-friendly polling and a large (~87%) reduction in memory use.

## [0.1.3] - 2026-05-06

### Added
- WPF-UI theming with a **Light / Dark / System** toggle.

## [0.1.2] - 2026-05-06

### Added
- Eight more monitored settings and the global settings UI.

## [0.1.1] - 2026-05-06

### Maintenance
- Release automation: builds publish automatically on a push to main.

## [0.1.0] - 2026-05-06

### Added
- **Initial release.** A Windows 11 tray app that watches gaming-related Windows
  settings and either prompts you when one drifts or silently re-applies your
  chosen value — pure user-mode, no drivers, no data collection.
