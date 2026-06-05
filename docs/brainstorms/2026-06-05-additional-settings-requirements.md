---
date: 2026-06-05
topic: additional-settings
---

# Additional Monitored Settings (Privacy, Network, System) — Requirements

## Summary

Add **12 new monitored settings** to GamerGuardian, each following the app's
existing contract — **monitor? / desired value / auto-apply on drift**, polled
on the existing 30s loop, reversible, and verifiable via `changes.log` + the
Apply Results surface. The additions are deliberately chosen to fit the current
registry/service monitor model: **no new monitoring patterns** (list-based,
scheduled-task, or UWP-removal monitors are explicitly out) and **no one-shot
actions** (process kills, restore points, GPU driver-profile tweaks are out).

The set splits into two groups. **Tier A** is safe, high-drift, low-risk
registry/service work: Cross-Device Platform off, full Game Bar / Game DVR
lockdown, Advertising ID off, Activity History off, Tailored experiences off,
expanded Windows-service catalog entries, and Power Throttling off. **Tier C**
fills new gaming-relevant categories with more surface or contested benefit:
Nagle's algorithm off, NIC power management off, Dynamic Refresh Rate (DRR),
Fast Startup off, and Visual effects → best performance.

## Problem Frame

GamerGuardian's value isn't "apply gaming tweaks once" — plenty of scripts
(including the developer's own `GamingOptimize.ps1`) do that. Its value is the
**drift-guard**: keep a setting at the value the user chose and re-assert it
after Windows updates, driver installs, or other apps silently change it. That
lens defines what's worth adding: **settings that drift back on their own are
the highest-value additions**, because a one-shot script already handles
set-once toggles fine.

Today the app covers display, security/perf, capture, input, Windows AI, a
service catalog, and CPU-aware power plans across seven tabs. Three gaps remain
that fit the drift-guard model:

1. **Privacy/telemetry toggles that Windows re-enables** — Cross-Device
   Platform, Advertising ID, Activity History, Tailored experiences. These reset
   on feature updates, which is exactly when the drift-guard earns its keep.
2. **Game Bar / Game DVR is only partially locked down** — the HKLM
   `AllowGameDVR` policy and the related capture toggles are re-enabled after
   updates and aren't fully covered by the current Game DVR card.
3. **Gaming-relevant categories the app doesn't touch at all** — network latency
   (Nagle, NIC power management), Dynamic Refresh Rate, and a couple of
   system-level toggles (Power Throttling, Fast Startup, Visual effects).

The work is additive: most items reuse the existing `GlobalToggleRow` /
registry-monitor pattern or add rows to the existing service catalog. A small
number need a slightly different read/write surface (per-NIC registry, a device
power-management property, the Win11 DisplayConfig DRR API), but none introduce
a new *monitoring* model.

## Key Decisions

- **Drift propensity is the selection lens.** Settings that Windows/driver
  updates silently re-enable are prioritized; low-drift set-once toggles (Fast
  Startup, Visual effects) are included because the user asked for them, but they
  are understood to add less value over a one-shot script.

- **Everything follows the existing monitor/desired/auto-apply triple.** No new
  card layout, no new monitoring mechanism. New settings reuse `GlobalToggleRow`
  (or the service-catalog row for Tier A #6), so auto-save, `changes.log`
  `[ui ]` lines, and Apply Results come for free.

- **Network tweaks ship at FULL parity** — monitor / desired / auto-apply,
  identical to HDR or HAGS. This was an explicit user choice over an
  advisory/monitor-only treatment. It is recorded as an **accepted liability**:
  Nagle and NIC power management have contested, per-hardware benefit and can
  make some connections *worse*. Mitigation is honest Learn More copy and
  accurate read-back, not a softer UX.

- **No new monitoring patterns.** Defender exclusions (list-based),
  scheduled-task disable, and non-AI UWP removal were considered and cut. They
  each need a new pattern; this work stays within the registry/service model.

- **No one-shot actions.** A setting is a steady *state*, not an *action*.
  Process kills, restore points, and GPU driver-profile tweaks do not fit
  `IMonitoredSetting` and are out.

- **Each new setting defines its exact reversal.** Consistent with the app's
  "every change is reversible" posture: delete-the-value, restore default service
  start type, re-enable the device power-management property, etc. A setting that
  can't be cleanly read back and reversed doesn't ship.

- **Service-catalog additions stay on the safe side of "don't blindly
  disable."** New service rows are limited to well-understood, broadly-safe
  entries; anything that risks breaking audio/input/GPU drivers on some hardware
  is excluded (per the cautions in `GamingOptimize-Guide.md`).

## Actors

- A1. **User** — chooses which new settings to monitor, sets desired values,
  opts into auto-apply, reads Learn More copy.
- A2. **GamerGuardian** — reads current state, detects drift on the 30s poll,
  applies + verifies on demand or auto, logs and proves every change.
- A3. **Windows subsystems** — registry (HKLM policy + HKCU), the Service Control
  Manager, the network stack / NIC driver, the display subsystem (DisplayConfig),
  and the power subsystem own the actual state the app reads/writes.

## Requirements

**Cross-cutting (apply to every new setting)**

- R1. Each new setting implements the existing `IMonitoredSetting` contract
  (read current → compare to desired → yield a `DriftItem`) and surfaces in the
  UI via the existing row model (`GlobalToggleRow` for toggles; the service-
  catalog row for #6).
- R2. Each new setting supports the full triple: **Monitor** on/off, **desired
  value**, and **Auto-apply silently** on drift — identical semantics to existing
  settings.
- R3. Each new setting is reversible and defines its exact reversal (e.g. delete
  the registry value to restore the Windows default; restore the service's
  default start type; re-enable the NIC power-management property).
- R4. Every apply/auto-apply writes a `changes.log` entry and appears in the
  Apply Results surface with before / target / after-verify values and a copyable
  verify command, consistent with existing behavior.
- R5. HKLM writes go through the existing `ElevatedRegistry` (UAC) path; HKCU
  writes are direct; service changes go through the existing elevated
  service-controller path. No new elevation mechanism is introduced.
- R6. `DesiredOn = true` always means "gaming-optimized" regardless of the
  underlying registry semantics; UI labels follow the existing
  Enabled/Disabled vs Gaming/Default convention where semantics are inverted.
- R7. Reboot-required settings set `RequiresReboot=true` and reuse the existing
  reboot-notice behavior (MessageBox on manual apply, `RebootPendingWindow` on
  auto-apply).

**Tier A — safe, high-drift registry/service settings**

- R8. **Cross-Device Platform (CDP) off** — gaming-recommended OFF (Windows
  default ON). Monitors the CDP enable policy/value(s). Drifts back on feature
  updates.
- R9. **Full Game Bar / Game DVR lockdown** — extends the existing Game DVR
  coverage to also assert the HKLM `AllowGameDVR` policy and the related capture
  toggles, so the lockdown holds after updates re-enable capture. Presented as an
  extension of the existing Game DVR card (single intent), not a duplicate card,
  unless planning finds a separate card clearer.
- R10. **Advertising ID off** — disables the per-user advertising identifier.
- R11. **Activity History / Timeline off** — disables activity-feed collection
  and publishing.
- R12. **Tailored experiences / diagnostic-data tailoring off** — disables
  tailored experiences built on diagnostic data.
- R13. **Expanded Windows-service catalog entries** — add safe, well-understood
  service rows (e.g. SysMain/Superfetch and common vendor updater services) to
  the existing `ServiceCatalog`, monitored/applied through the existing service
  monitor. Limited to entries that are broadly safe to set Manual/Disabled.
- R14. **Power Throttling off** — disables Windows power throttling
  (gaming-recommended for sustained performance). Lives alongside the CPU/Power
  feature set.

**Tier C — new categories**

- R15. **Nagle's algorithm off** — asserts the TCP no-delay / ack-frequency
  values. Per-network-interface. Ships at full monitor/desired/auto-apply parity;
  Learn More copy states the contested, per-hardware nature of the tweak.
- R16. **NIC power management off** — asserts "do not allow the computer to turn
  off this device to save power" on the active network adapter(s). Full parity;
  Learn More copy states the contested nature.
- R17. **Dynamic Refresh Rate (DRR) off/on** — monitors and sets the Win11 22H2+
  DRR state via the DisplayConfig path. Must detect panel/OS support and degrade
  gracefully (clearly "not supported on this display" rather than failing) where
  DRR is unavailable. Distinct from VRR and from the existing refresh-rate
  setting.
- R18. **Fast Startup off** — asserts hybrid-boot (`HiberbootEnabled`) OFF.
  Low-drift (set-once) but included by user request.
- R19. **Visual effects → best performance** — asserts the "adjust for best
  performance" visual-effects profile. The read-back must handle the binary
  preferences mask correctly so verify is accurate.

**Footprint & quality**

- R20. The work must not regress the existing software-rendered-WPF / Mica-off
  working-set wins, and adds no new always-running background thread (it reuses
  the existing 30s poll).
- R21. Changes touching `ServiceCatalog`, `SettingDocs`/`SettingDocsCatalog`, or
  service-controller logic add or extend tests in `tests/GamerGuardian.Tests`,
  per repo convention (CI gates on `dotnet test`). The generated
  `docs/SETTINGS-REFERENCE.md` is regenerated so its sync test stays green.

**Documentation**

- R22. Each new setting ships with in-app Learn More copy (the `SettingDocs` /
  `SettingDocsCatalog` pattern): what it does, the gaming rationale, the risk
  (especially for the network tweaks), and how to reverse it. Committed `docs/`
  and the wiki are updated to reflect the new settings and any new tab(s).

## Key Flows

- F1. Monitor + drift-correct a new toggle
  - **Trigger:** 30s poll (or manual Verify all).
  - **Actors:** A2, A3
  - **Steps:** Read current value → compare to desired → if drifted and
    auto-apply on, apply via the existing applier and verify; else raise the
    drift notification.
  - **Covered by:** R1, R2, R3, R4

- F2. Manual apply of a new setting
  - **Trigger:** User sets desired value and clicks Apply.
  - **Actors:** A1, A2, A3
  - **Steps:** Write (HKLM via UAC, HKCU direct, or service/NIC/display surface)
    → re-read → show before/target/after + verify command; reboot notice if
    `RequiresReboot`.
  - **Covered by:** R4, R5, R7

- F3. DRR on an unsupported display
  - **Trigger:** User opens the setting on hardware/OS without DRR support.
  - **Actors:** A1, A2, A3
  - **Steps:** Detect lack of support → present a clear "not supported on this
    display" state rather than offering a no-op toggle or erroring.
  - **Covered by:** R17

- F4. Network tweak with contested benefit
  - **Trigger:** User enables Nagle-off or NIC-power-off.
  - **Actors:** A1, A2, A3
  - **Steps:** Apply per-NIC/device value → verify → Learn More copy makes the
    per-hardware risk explicit; auto-apply is available at parity if the user
    opts in.
  - **Covered by:** R15, R16, R22

## Acceptance Examples

- AE1. CDP drift correction
  - **Given** CDP monitored with desired=off and auto-apply on, **when** a
    feature update re-enables CDP, **then** the next poll detects drift, re-asserts
    off, and logs before/after + a verify command.
  - **Covers R2, R3, R4, R8.**

- AE2. Game Bar lockdown holds the HKLM policy
  - **Given** the Game Bar lockdown monitored, **when** capture is re-enabled,
    **then** the app re-asserts the `AllowGameDVR` policy and related toggles to
    the locked-down state.
  - **Covers R9.**

- AE3. Network tweak ships at full parity with honest copy
  - **Given** Nagle-off, **when** the user views it, **then** it exposes
    Monitor / desired / Auto-apply identical to other settings, and the Learn
    More copy states the contested, per-hardware nature.
  - **Covers R2, R15, R22.**

- AE4. DRR degrades gracefully
  - **Given** a display without DRR support, **when** the user opens the DRR
    setting, **then** the app shows a clear "not supported" state and does not
    offer a no-op apply or throw.
  - **Covers R17.**

- AE5. Visual-effects verify is accurate
  - **Given** Visual effects → best performance applied, **when** the app
    re-reads, **then** the binary preferences mask is parsed correctly and the
    after-verify reflects the real applied state.
  - **Covers R19.**

- AE6. Service addition stays safe and tested
  - **Given** a new service-catalog entry, **when** it ships, **then** it is a
    broadly-safe service and `tests/GamerGuardian.Tests` covers the catalog/
    controller change; `docs/SETTINGS-REFERENCE.md` is regenerated.
  - **Covers R13, R21.**

## Scope Boundaries

**Outside this product's identity (not built here)**

- New *monitoring patterns*: Defender exclusions (list-based), scheduled-task
  disable (NVIDIA overlay etc.), non-AI UWP removal (Phone Link etc.). Each needs
  a new pattern; explicitly cut.
- One-shot *actions*: process kills, system restore points, GPU driver-profile
  tweaks (NVIDIA/AMD control-panel/nvapi). These are not steady states and don't
  fit the drift-guard model. A future "Tools" panel is a different product
  surface, not part of this work.
- Windows 10 support — repo-wide scope-out.

**Deferred for later**

- A dedicated advisory/monitor-only treatment for contested tweaks — the user
  chose full parity for the network items, so the advisory variant is not built
  now.

## Dependencies / Assumptions

- **Exact registry paths/values are deferred to planning and must be verified
  against a live machine** before implementation (this doc names the setting and
  the gaming-recommended direction, not authoritative key paths). Applies to CDP,
  Advertising ID, Activity History, Tailored experiences, Power Throttling, Fast
  Startup, Visual effects, and the Game Bar policy.
- **Nagle/TCP settings are per-network-interface** (keyed under each interface
  GUID); the app must resolve the active interface(s). Read-back and reversal
  semantics per-NIC need confirmation in planning.
- **NIC power management** is a device power-management property, not a simple
  HKCU value; how it's read/written from user mode (registry under the device
  instance vs. a device API) is a planning-time unknown.
- **DRR** requires the Win11 22H2+ DisplayConfig API surface
  (`DISPLAYCONFIG_*` refresh-rate range), which is new to the app; panel-support
  detection is required.
- **Visual effects** is stored partly in a binary `UserPreferencesMask`; correct
  bit-level read-back is required for accurate verify.
- The expanded service list's "safe to disable" judgment is a maintenance
  commitment; entries should be conservative.

## Outstanding Questions

**Deferred to planning / discoverable in code**

- Exact registry locations and value semantics for each Tier A toggle and Fast
  Startup / Visual effects (verify on a live machine).
- Whether Nagle and NIC power management can be read/written reliably from user
  mode for the *active* adapter, and how per-NIC enumeration interacts with the
  monitor/verify model.
- The DisplayConfig API path for DRR read/write and the support-detection method.
- UI/IA placement: a new **Privacy** grouping (CDP, Advertising ID, Activity
  History, Tailored experiences), a new **Network** tab (Nagle, NIC power), Power
  Throttling on the CPU/Power tab, DRR on the Display tab, Fast Startup + Visual
  effects on Global/System, Game Bar extending the existing card, service rows on
  the Services tab — exact tab structure is a planning decision.
- Which specific services to add in the expanded catalog (the safe subset).

## Sources / Research

- App model + constraints (this session's deep dive): drift-guard contract,
  user-mode-only, reversible/verifiable; monitors implement `IMonitoredSetting`
  and register in `src/GamerGuardian/App.xaml.cs`.
- Roadmap + candidate origin: `docs/feat-gaming-optimize-plan.md` (Tier 1
  registry monitors incl. CDP and Game Bar lockdown) and the developer's
  `GamingOptimize.ps1` / `GamingOptimize-Guide.md` (privacy toggles, Windows
  feature toggles, "don't blindly disable" cautions).
- Patterns to mirror: `src/GamerGuardian/Monitors/` (e.g.
  `GameDvrMonitor.cs`, `VrrMonitor.cs`), `src/GamerGuardian/UI/SettingsWindow.xaml(.cs)`
  (`GlobalToggleRow`), `src/GamerGuardian/Services/ServiceCatalog.cs`,
  `src/GamerGuardian/Services/SettingDocsCatalog.cs`,
  `src/GamerGuardian/Services/ElevatedRegistry.cs`.
</content>
</invoke>
