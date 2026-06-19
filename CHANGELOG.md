# Changelog

All notable, user-facing changes to GamerGuardian. Newest first. This file is
the source of the notes attached to each [GitHub Release](https://github.com/carterscode/GamerGuardian/releases);
the release workflow pulls the matching version section into the release body.

The format is loosely based on [Keep a Changelog](https://keepachangelog.com/).
Versions before 1.0.0 are pre-release: features and defaults may still change.

## [Unreleased]

### Changed
- Release notes are now **curated and user-facing**: each GitHub Release shows a
  hand-written summary of what changed (from `CHANGELOG.md`) instead of a raw
  list of pull requests.

## [0.1.52]

### Changed
- **Recommendations now speak each setting's own language.** The green
  **Recommended** hint next to Current/Default is shown in the row's actual
  wording — *Enabled/Disabled*, *Gaming/Default*, *On/Off*, or a service's
  *Default/Manual/Disabled* — instead of a generic "On/Off" that didn't match
  the buttons.
- **Recommendation values tuned for the typical desktop gamer** and validated
  against current best practice: clear performance wins are recommended; the
  Memory Integrity / VBS security toggles are recommended to stay **on** (some
  anti-cheat requires them); genuinely contested tweaks (Nagle, NIC power
  management) are recommended to stay at the Windows default; Privacy and Debloat
  settings lean toward the privacy-respecting / clutter-free value (shown for
  guidance — they remain opt-in). The power-plan recommendation is CPU-aware
  (Balanced on modern CPUs). The one-click setup and the per-row hint now share
  one source of truth, so they can never disagree.

## [0.1.51] - 2026-06-19
### Added
- A **Recommended** value now appears next to *Current* and *Default* on every
  setting, so you can see GamerGuardian's suggested choice at a glance.

## [0.1.50] - 2026-06-15
### Added
- New **Debloat** tab: switch off Windows 11 ads, nags, suggested content,
  widgets, lock-screen "fun facts", File Explorer upsell banners, and Edge
  background processes — plus privacy data toggles.
- Optional removal of the **Microsoft 365 Copilot** app and six device-feature
  services.
### Fixed
- The updater now always upgrades to the highest stable version.

## [0.1.49] - 2026-06-14
### Changed
- The manual **Power plan** selector moved to the **CPU / Power** tab, next to
  the CPU-aware plan tools.

## [0.1.48] - 2026-06-12
### Fixed
- "Reboot now" now triggers the restart immediately.

## [0.1.47] - 2026-06-12
### Added
- Complete **Virtualization-Based Security (VBS) disable** — the full stack, not
  just Memory Integrity — with hardened state handling and honest risk docs.
  (Note: disabling VBS breaks Valorant; Vanguard requires Memory Integrity.)

## [0.1.46] - 2026-06-05
### Changed
- Moved **Network Throttling** onto the Network tab and tidied the tab layout.

## [0.1.45] - 2026-06-05
### Added
- New **Privacy** tab with telemetry/activity toggles, a **Network** tab with
  per-adapter Nagle and NIC power-management tweaks, per-display **Dynamic
  Refresh Rate**, **Visual Effects (best performance)**, **Power Throttling**,
  and **Fast Startup** controls.
- Game DVR lockdown now also sets the machine-wide policy so it sticks.

## [0.1.44] - 2026-06-05
### Added
- **CPU / Power** tab with CPU-aware power plans: GamerGuardian detects your CPU
  and either suggests the best prebuilt plan or builds a custom optimized plan
  (with a dual-CCD X3D guardrail). Plus an advisory **BIOS** recommendations tab.

## [0.1.43] - 2026-06-05
### Fixed
- Stopped a recurring "search-box AI" drift popup by using reliable Windows 11
  registry keys.

## [0.1.42] - 2026-06-05
### Maintenance
- Dependency and CI updates.

## [0.1.41] - 2026-06-05
### Fixed
- Per-display preferences (refresh rate, HDR, resolution) now use stable display
  keys, so they no longer reset.

## [0.1.40] - 2026-06-03
### Added
- One-click **Recommended** gaming preset on the General tab.
- Closer parity with community Windows-AI removal tooling (safe additions only).
### Fixed
- Removed a spurious silent-apply popup and a notification-header glitch.

## [0.1.39] - 2026-06-03
### Maintenance
- CI dependency updates.

## [0.1.38] - 2026-05-19
### Added
- Per-setting documentation with a **Learn more** expander, a **Verify all**
  button, and a system state snapshot.
- New **Windows AI** tab to lock down Copilot, Recall, and related features,
  including optional UWP app removal.
### Changed
- Settings changes are now **staged** and only committed when you click Apply or
  Save & close.

## [0.1.37] - 2026-05-08
### Changed
- Minimizing the Settings window now sends it to the tray.
### Fixed
- `sync-wiki` tooling now works on PowerShell 5.1.

## [0.1.36] - 2026-05-08
### Added
- Clear feedback when **Apply** has nothing to change, and policy-state display
  for policy-managed services.

## [0.1.35] - 2026-05-07
### Maintenance
- Security/code-quality fixes and CI dependency bumps.

## [0.1.34] - 2026-05-07
### Fixed
- **Delivery Optimization** is now disabled via Group Policy, so Windows stops
  reverting it.

## [0.1.33] - 2026-05-07
### Maintenance
- Added a test project and turned on warnings-as-errors for a cleaner build.

## [0.1.32] - 2026-05-07
### Fixed
- No more repeated UAC prompts when Windows reverts a service change.

## [0.1.31] - 2026-05-07
### Security
- Tightened CI permissions and added SLSA build provenance to releases.

## [0.1.30] - 2026-05-07
### Added
- **Windows services** tab: tri-state control (Default / Manual / Disabled) plus
  eight more services.
### Changed
- Layout polish so long service/setting names fit; added a no-data-collection
  privacy policy.

## [0.1.29] - 2026-05-07
### Fixed
- Power-plan dropdown is now actually populated from all installed plans.

## [0.1.28] - 2026-05-07
### Fixed
- Power-plan dropdown now lists every installed plan (custom + Windows defaults).

## [0.1.27] - 2026-05-07
### Performance
- Lower memory use via large-object-heap compaction and window content release.

## [0.1.26] - 2026-05-06
### Added
- Pause/resume events are logged with the reason and the foreground game.

## [0.1.25] - 2026-05-06
### Fixed
- Preference toggles now persist and are recorded in the change log.

## [0.1.24] - 2026-05-06
### Fixed
- VRR monitor now reads the correct registry value.

## [0.1.23] - 2026-05-06
### Changed
- Clarified that VRR is not the same as Dynamic Refresh Rate (DRR).

## [0.1.22] - 2026-05-06
### Added
- Detailed change log capturing raw before/after registry values.

## [0.1.21] - 2026-05-06
### Fixed
- Fixed a critical settings-display binding bug and temp-file cleanup.

## [0.1.20] - 2026-05-06
### Added
- A persistent **change log** of every setting GamerGuardian applies.

## [0.1.19] - 2026-05-06
### Added
- **Apply results** window showing per-setting before/after with a copy-paste
  verify command.

## [0.1.18] - 2026-05-06
### Fixed
- **Apply** now applies regardless of the Monitor checkbox; Want syncs to the
  current value on load.

## [0.1.17] - 2026-05-06
### Added
- A **Check now** button for manual update checks.

## [0.1.16] - 2026-05-06
### Docs
- Professional README with setting references.

## [0.1.15] - 2026-05-06
### Changed
- Renamed "Check interval" to "Polling interval" to avoid confusion with the
  update check.

## [0.1.14] - 2026-05-06
### Added
- Update check on startup with one-click download and install.

## [0.1.13] - 2026-05-06
### Added
- Non-modal reboot popup for auto-applied settings; clearer Enabled/Disabled
  labels.

## [0.1.12] - 2026-05-06
### Fixed
- Apply button, reboot prompt, and clearer radio-button labels.

## [0.1.11] - 2026-05-06
### Changed
- Cleaner per-setting row layout with **Current** and **Default** status lines.

## [0.1.10] - 2026-05-06
### Added
- Tier 1 performance tweaks (four new monitored settings).

## [0.1.9] - 2026-05-06
### Added
- Monitor **Memory Integrity / VBS** (Core Isolation).

## [0.1.8] - 2026-05-06
### Added
- Automatic benchmark detection, manual pause, and a new app icon.

## [0.1.7] - 2026-05-06
### Fixed
- Dev builds are now clearly distinguished from releases in the version label.

## [0.1.6] - 2026-05-06
### Added
- Polling also pauses for borderless-fullscreen games.

## [0.1.5] - 2026-05-06
### Added
- App version shown in Settings.

## [0.1.4] - 2026-05-06
### Performance
- Gaming-friendly polling and a large reduction in memory use.

## [0.1.3] - 2026-05-06
### Added
- WPF-UI theming with a Light / Dark / System toggle.

## [0.1.2] - 2026-05-06
### Added
- Eight more monitored settings and the global settings UI.

## [0.1.1] - 2026-05-06
### Maintenance
- Release automation.

## [0.1.0] - 2026-05-06
### Added
- Initial release: a Windows 11 tray app that watches gaming-related settings and
  prompts on drift or silently re-applies your chosen value.
