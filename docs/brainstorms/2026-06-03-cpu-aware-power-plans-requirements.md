---
date: 2026-06-03
topic: cpu-aware-power-plans
---

# CPU-Aware Power Plans + BIOS Guidance — Requirements

## Summary

Detect the CPU once at startup, then let the user choose between switching to
the best-matching **prebuilt** Windows power plan or having GamerGuardian
**build an optimized** plan tuned for that CPU. The optimized recipe is tiered
(exact model → CPU family → safe generic) and built as a Balanced clone with a
minimal set of overrides; the catalog ships with distinct single-CCD (9800X3D)
and dual-CCD (9950X3D) X3D entries because they require opposite core-parking
behavior. A companion advisory-only **Recommended BIOS settings** tab, keyed to
the detected CPU, documents the firmware-level settings the app cannot set
itself. The feature stays within a measured size/working-set budget and ships
first as a dev build for local beta before public release.

## Problem Frame

The app's current power-plan handling recommends **High Performance for every
machine** (the one-click Recommended preset switches to it when installed).
That recommendation is wrong for a large and growing slice of modern gaming
CPUs:

- On a **single-CCD X3D** (e.g. Ryzen 7 9800X3D), High Performance pins core
  parking and processor state in ways that fight the chip's boost/scheduling
  behavior. The gaming-optimal tune is *Balanced plus a few boost-friendly
  overrides*, not High Performance. This is observed on the developer's own
  9800X3D machine, whose hand-built "Gaming Balanced" plan is exactly a Balanced
  clone with five processor overrides.
- On a **dual-CCD X3D** (e.g. Ryzen 9 9950X3D), High Performance is actively
  harmful: it disables the core parking that the Xbox Game Bar + AMD 3D V-Cache
  Optimizer stack relies on to keep game threads on the cache CCD. When parking
  fails, game threads spill onto the frequency CCD and frame rates collapse
  (reports of 170–190 FPS where the chip should deliver 300+).

So the same blunt recommendation is wrong in *two opposite directions*, and the
correct value depends on the specific silicon. The developer already maintains
hand-tuned custom plans per machine; the goal is to teach the app to detect the
CPU and produce the right plan automatically — precisely on known chips,
sensibly on unknown ones — without becoming heavier or claiming optimizations
it can't actually deliver.

## Key Decisions

- **Detect and offer; never silently switch.** After detection the app presents
  a choice — *suggest best prebuilt plan* or *build optimized plan*. The app
  does not auto-apply a plan on the user's behalf. This preserves user agency
  and the app's "you decide, the app proves what it did" posture.

- **Catalog-authored tunes, not capture-from-machine.** Optimized recipes come
  from an in-app catalog (mirroring `ServiceCatalog` / `WindowsAiAppCatalog`),
  not by snapshotting the user's existing custom plan. This makes the tune
  reproducible on a freshly-rebuilt PC the app has never seen, at the cost of
  the catalog being the maintainer's responsibility to keep correct.

- **Tiered matching: exact model → CPU family → safe generic.** The optimized
  option is always offerable. A recognized exact model gets its precise recipe;
  a recognized family gets a family recipe; an unknown CPU gets a conservative
  generic tune, clearly labeled "generic, not CPU-specific." The prebuilt
  suggestion is always offered alongside.

- **Optimized plans are a Balanced clone with minimal overrides — never a
  High-Performance personality.** On dual-CCD X3D the High-Performance
  personality disables the very core parking the tune depends on, so the base
  plan and its power-scheme personality must remain Balanced.

- **The catalog keys on CCD topology, not the marketing name.** "X3D" is not a
  single tune. Single-CCD wants core parking *off*; dual-CCD wants the frequency
  CCD *parked*. Topology is the discriminator.

- **BIOS guidance is advisory-only.** The app cannot set, and largely cannot
  read, BIOS state from user mode. The BIOS tab shows recommended settings with
  rationale, explicitly framed as "verify against your motherboard manual." It
  never displays a current-state/"already set" column and never claims to have
  applied anything.

- **Slim-app constraint is a design gate, not a nicety.** No heavy new
  dependencies; one-time cached detection; static catalog data; reuse of the
  existing 30s poll for drift. Size and working-set deltas are measured as part
  of the work.

- **Dev-build-first.** The feature ships to the dev-build channel for local beta
  validation (on the developer's 9800X3D and 9950X3D) before any public
  release.

## Actors

- A1. **User** — chooses prebuilt vs optimized, reads the BIOS guidance, applies
  the selected plan.
- A2. **GamerGuardian** — detects the CPU, resolves the catalog tier, builds or
  suggests the plan, monitors drift, surfaces dependencies and BIOS guidance,
  and proves what it changed.
- A3. **Windows power subsystem** — owns the active scheme and the power-setting
  values the app reads/writes.
- A4. **AMD gaming stack (dual-CCD X3D only)** — the 3D V-Cache Performance
  Optimizer service + Xbox Game Bar + BIOS CPPC mode that actually route game
  threads to the cache CCD. The app depends on, but cannot install or configure,
  this stack.

## Requirements

**CPU detection**

- R1. The app detects the installed CPU at startup and caches the result for the
  process lifetime. Detection identifies at minimum: vendor, model, family/
  microarchitecture, and — for AMD X3D — whether the part is single-CCD or
  dual-CCD (cache topology).
- R2. Detection uses lightweight built-in means only (e.g. WMI / registry /
  a small CPUID query). No hardware-monitoring library or other heavy dependency
  is introduced.
- R3. Detection runs exactly once per app launch. It adds no new timer, polling
  loop, or always-running background thread.

**Plan choice (suggest vs build)**

- R4. After detection, the app presents the user two distinct actions for the
  detected CPU: *Suggest best prebuilt plan* and *Build optimized plan*.
- R5. The app never switches the active power plan without explicit user action.
- R6. *Suggest best prebuilt plan* selects the best-matching already-installed
  Windows plan for the detected CPU (e.g. Balanced for modern AMD/Intel hybrid
  parts) and, on user action, sets it active. It does not create a new plan.
- R7. *Build optimized plan* creates a GamerGuardian-authored plan for the
  detected CPU and, on user action, sets it active.

**Tune catalog**

- R8. The optimized recipe is resolved by tier: exact model match → CPU family
  match → safe generic tune. The optimized option is available at every tier.
- R9. A generic tune (unknown CPU) is conservative and boost-friendly, makes no
  topology-dependent assumptions (it does not force a core-parking value), and
  is labeled to the user as generic / not CPU-specific.
- R10. The catalog ships seeded with a single-CCD X3D entry (9800X3D) and a
  dual-CCD X3D entry (9950X3D) as distinct recipes.
- R11. The single-CCD X3D recipe is a Balanced clone with: processor boost mode
  = Aggressive; core parking min cores = 100 (no parking); processor performance
  increase threshold = 60; idle demote threshold = 10; minimum processor state
  = 5; maximum processor state = 100.
- R12. The dual-CCD X3D recipe is a Balanced clone (Balanced personality) with:
  processor boost mode = Aggressive; core parking min cores = 50 (parks the
  frequency CCD); core parking max cores = 100. It must NOT set core parking min
  cores = 100, and must NOT use a High-Performance personality.
- R13. Optimized plans are always built on a Balanced base scheme; the resulting
  scheme's power-plan personality remains Balanced.

**Plan creation behavior**

- R14. Building an optimized plan is idempotent: re-running it reuses or replaces
  the app's own previously-created plan for that CPU rather than creating a
  duplicate. The app never stacks multiple identical plans across runs.
- R15. Building a plan is additive and reversible: it never mutates the stock
  Balanced (or any other existing) scheme, and switching back to the prior plan
  is always possible.
- R16. Every build/suggest/apply action is logged to `changes.log` and shown in
  the Apply Results surface with before / target / after-verify values and a
  copyable verify command, consistent with existing apply behavior.
- R17. Once an optimized plan is built and selected, drift monitoring reuses the
  existing power-plan poll; no additional monitoring mechanism is added.

**Dual-CCD dependency surfacing**

- R18. For a dual-CCD X3D CPU, the app communicates that the power plan is
  necessary but not sufficient, and surfaces the dependencies it cannot set:
  BIOS CPPC Dynamic Preferred Cores = Driver; the AMD 3D V-Cache Performance
  Optimizer service running; Xbox Game Bar enabled.
- R19. Where a dependency's state is cheaply observable from data the app already
  has (e.g. the AMD optimizer service running/stopped via the existing service
  controller), the app may reflect it; it must not claim "optimized" purely on
  the basis of the power plan when dependencies are unmet.

**Guardrails (interaction with the app's own debloat features)**

- R20. The Recommended preset and any service-disable catalog entry must not
  disable Xbox Game Bar or the AMD 3D V-Cache Performance Optimizer service on a
  dual-CCD X3D machine, because doing so breaks the CCD routing this feature
  configures.
- R21. The Recommended preset's power-plan step becomes CPU-aware: it no longer
  blindly selects High Performance. It applies (or offers) the CPU-appropriate
  choice instead.

**BIOS guidance tab**

- R22. The app provides a Recommended BIOS settings surface keyed to the detected
  CPU, listing the recommended firmware settings with rationale.
- R23. The BIOS surface is advisory-only: it does not read or verify current BIOS
  state, shows no current-state/"already set" column, and is explicitly labeled
  to verify against the motherboard manual.
- R24. BIOS recommendations are part of the same per-CPU catalog data (static),
  adding no runtime overhead.

**Documentation**

- R25. The feature ships with detailed documentation across all three existing
  surfaces: in-app (the `SettingDocs` / Learn More pattern), committed `docs/`,
  and the GitHub wiki (`tools/sync-wiki.ps1`).
- R26. Documentation explains, per CPU/option: what was detected, what each plan
  sets (the exact overrides and why), the single- vs dual-CCD divergence, the
  dual-CCD dependency stack, and the prebuilt-vs-optimized choice — so a user
  understands what they are selecting before applying it.

**Footprint**

- R27. Published single-file binary size and idle working set must stay within
  budget versus the pre-feature baseline (current `main` release build): **no
  measurable increase in idle working set**, and **≤ ~0.5 MB increase in the
  published single-file binary**. The delta is measured and reported as part of
  the work; the budget may be tightened if measurement shows headroom.
- R28. The work must not regress the existing software-rendered-WPF / Mica-off
  working-set wins.

**Release sequencing**

- R29. The feature is delivered first via the dev-build channel (where
  `IsDevBuild()` is true) for local beta testing on the developer's 9800X3D and
  9950X3D, and is promoted to a public release only after sign-off.

## Key Flows

- F1. CPU detection
  - **Trigger:** App startup.
  - **Actors:** A2, A3
  - **Steps:** Read CPU identity via lightweight means; classify vendor / model /
    family / CCD topology; cache for the process lifetime.
  - **Covered by:** R1, R2, R3

- F2. Present the choice
  - **Trigger:** User opens the power-plan UI.
  - **Actors:** A1, A2
  - **Steps:** Show detected CPU and tier; offer *Suggest best prebuilt plan* and
    *Build optimized plan*; for dual-CCD X3D, show the dependency status/notes.
  - **Covered by:** R4, R5, R8, R18, R19

- F3. Build optimized plan
  - **Trigger:** User picks *Build optimized plan*.
  - **Actors:** A1, A2, A3
  - **Steps:** Resolve recipe by tier; reuse/replace the app's own prior plan if
    present (idempotent); clone Balanced; write the recipe's overrides; set
    active on user action; log + show verify.
  - **Covered by:** R7, R8, R11, R12, R13, R14, R15, R16

- F4. Suggest prebuilt plan
  - **Trigger:** User picks *Suggest best prebuilt plan*.
  - **Actors:** A1, A2, A3
  - **Steps:** Resolve best-matching installed plan; on user action set it active;
    log + show verify.
  - **Covered by:** R6, R16

- F5. View BIOS guidance
  - **Trigger:** User opens the Recommended BIOS settings tab.
  - **Actors:** A1, A2
  - **Steps:** Show static per-CPU BIOS recommendations with rationale and the
    advisory label; no state reading.
  - **Covered by:** R22, R23, R24

## Acceptance Examples

- AE1. Single-CCD exact match
  - **Given** a Ryzen 7 9800X3D, **when** the user builds the optimized plan,
    **then** the app produces a Balanced clone with core parking min cores = 100
    (no parking) and the boost/threshold/state overrides in R11.
  - **Covers R1, R8, R11.**

- AE2. Dual-CCD exact match
  - **Given** a Ryzen 9 9950X3D, **when** the user builds the optimized plan,
    **then** the app produces a Balanced-personality clone with core parking min
    cores = 50 (frequency CCD parked) and max cores = 100 — never min cores = 100
    and never a High-Performance personality.
  - **Covers R10, R12, R13.**

- AE3. Unknown CPU
  - **Given** a CPU not in the catalog, **when** the user opens the power-plan
    UI, **then** both options are still offered; *Build optimized* produces the
    generic tune labeled "generic, not CPU-specific," and *Suggest prebuilt*
    recommends the best-matching installed plan.
  - **Covers R8, R9, R6.**

- AE4. Idempotent build
  - **Given** the app already created its optimized plan for this CPU, **when**
    the user builds again, **then** the app reuses or replaces that plan and does
    not create a second identical scheme.
  - **Covers R14.**

- AE5. Dual-CCD with unmet dependency
  - **Given** a 9950X3D where the AMD 3D V-Cache Optimizer service is stopped,
    **when** the user views the power-plan UI, **then** the app surfaces the
    dependency and does not report a plain "optimized" state based on the power
    plan alone.
  - **Covers R18, R19.**

- AE6. Debloat guardrail
  - **Given** a dual-CCD X3D machine, **when** the user runs the Recommended
    preset, **then** Xbox Game Bar and the AMD 3D V-Cache Optimizer service are
    not disabled.
  - **Covers R20, R21.**

- AE7. Advisory BIOS tab
  - **Given** any detected CPU, **when** the user opens the BIOS settings tab,
    **then** recommended settings appear with rationale and an advisory label,
    and no current-state column or "applied" claim is shown.
  - **Covers R22, R23.**

## Scope Boundaries

- Capturing or importing the user's existing custom plan, or importing/exporting
  `.pow` files — out. Tunes are catalog-authored.
- Per-game or per-application dynamic plan switching — out.
- GPU / graphics-driver-level tuning (NVIDIA/AMD control panel) — out.
- Reading or applying BIOS settings — out; BIOS guidance is advisory-only.
- In-app authoring/editing of custom recipes by the user — out (the app authors
  from the catalog; the user picks).
- Windows 10 support — out (repo-wide scope-out).

## Dependencies / Assumptions

- Building a custom scheme requires power APIs beyond today's read/activate-only
  wrapper (scheme duplication + writing AC value indices). Whether these require
  an elevation/UAC step on the target machine must be confirmed during planning;
  the app already has an elevated-write path for HKLM if needed.
- Setting `CPMINCORES=50 / CPMAXCORES=100` via the value-index API is the
  documented, community-proven, processor-safe way to park the frequency CCD on
  dual-CCD X3D (it only signals thread-placement preference; no voltage/frequency
  change). Assumed equivalent to `powercfg -setacvalueindex`.
- Dual-CCD X3D CCD routing depends on a stack outside the app's control: BIOS
  CPPC Dynamic Preferred Cores = Driver, the AMD 3D V-Cache Performance Optimizer
  service, and Xbox Game Bar. The feature surfaces these but cannot guarantee
  them.
- The catalog's correctness is an ongoing maintenance commitment; recipes will
  need revision as new silicon ships.

## Outstanding Questions

**Deferred to planning / discoverable in code**

- Does scheme duplication + value-index writing require elevation on the target
  machine, and if so, how does the UAC step fit the existing apply flow?
  (Discoverable by testing the power APIs; the elevated-write path already exists
  if needed.)
- Exact CPU-detection source (WMI class vs registry vs CPUID) and how cache
  topology (single- vs dual-CCD) is reliably determined.
- The initial breadth of family-tier entries beyond the two seeded exact models
  (which families ship in v1).
- Exact in-app placement of the choice UI and the new BIOS tab within the
  existing Settings window.

## Sources / Research

- Live diff on the developer's 9800X3D: the active "Gaming Balanced [9850X3D]"
  scheme is a Balanced clone with five processor overrides (boost mode Aggressive,
  core parking min cores 100, perf increase threshold 60, idle demote threshold
  10, minimum processor state 5) — the basis for the single-CCD recipe (R11).
- Existing power-plan code to extend: `src/GamerGuardian/Monitors/PowerPlanMonitor.cs`,
  `src/GamerGuardian/Native/Powrprof.cs` (read/activate only today),
  `src/GamerGuardian/Services/RecommendedPreset.cs` (currently hardcodes High
  Performance; note the hardcoded GUID there despite the repo's "never hardcode
  power-scheme GUIDs" rule).
- Catalog pattern to mirror: `src/GamerGuardian/Services/ServiceCatalog.cs`,
  `WindowsAiAppCatalog`; per-setting docs via `src/GamerGuardian/Services/SettingDocsCatalog.cs`.
- Dual-CCD 9950X3D power/parking best practice and the High-Performance failure
  mode: [techreviewguide — 9950X3D core parking powercfg fix](https://techreviewguide.com/fix-ryzen-9-9950x3d-core-parking-issue-powercfg-command/),
  [Phoronix — AMD 3D V-Cache Optimizer driver](https://www.phoronix.com/review/amd-3d-vcache-optimizer-9950x3d),
  [XDA — 9950X3D review (Balanced + Game Bar parking)](https://www.xda-developers.com/amd-ryzen-9-9950x3d-review/),
  [Overclock.net — Zen 5 X3D core parking thread](https://www.overclock.net/threads/how-i-fixed-core-parking-on-my-9950x3d-and-taichi-lite.1815819/).
</content>
</invoke>
