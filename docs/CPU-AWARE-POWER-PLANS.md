# CPU-aware power plans

GamerGuardian detects your CPU and helps you run the right gaming power plan for
it — either by switching to the best-matching **prebuilt** Windows plan, or by
building a **GamerGuardian-authored optimized** plan tuned for your specific
chip. This document explains exactly what is detected, what each option does,
and why.

> **Your built-in Windows plans are never touched.** GamerGuardian only ever
> *creates* or *re-tunes* its own plan, and *switches* the active plan. It never
> modifies or deletes Balanced, High Performance, Power Saver, Ultimate
> Performance, or any custom plan you made.

## What is detected

At startup the app reads your processor name from the registry
(`HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0\ProcessorNameString` and
`VendorIdentifier`) — a single cached read, no WMI, no hardware probing, no new
dependency. From the name it extracts:

- **Vendor** (AMD / Intel)
- **Model** (e.g. `9850X3D`, `9950X3D`, `7800X3D`, `14700K`)
- **Family** (Zen4 / Zen5 / Intel hybrid)

CCD topology and parking strategy are then decided by the tune catalog (below),
not by raw core counts.

## How a tune is chosen (exact → family → generic)

1. **Exact model** — a precise, verified recipe for that chip.
2. **CPU family** — a recipe for that class of chip.
3. **Generic** — a safe, clearly-labeled fallback for anything unrecognized.

The **optimized** option is always available; only its precision changes. The
**prebuilt** suggestion is always offered alongside.

## The core principle: cache asymmetry, not CCD count

The single most important decision is core parking, and it keys on **where the
3D V-Cache lives**, not how many CCDs the chip has:

| CPU class | Examples | Parking | Why |
|---|---|---|---|
| Single-CCD X3D | 9850X3D, 9800X3D, 7800X3D | **Off** (min cores 100) | Every core is under the cache — never park. |
| Asymmetric dual-CCD X3D | 9950X3D, 9900X3D, 7950X3D, 7900X3D | **Park frequency CCD** (min cores 50) | Keeps game threads on the cache CCD; spilling to the frequency CCD tanks FPS. |
| Dual-V-cache X3D | 9950X3D2 | **Off** (min cores 100) | Cache on *both* CCDs — nothing to consolidate, so don't park. |
| Non-X3D Ryzen (single or symmetric dual) | 7700X, 9700X, 7950X, 9950X | **Off** (min cores 100) | No cache CCD to prefer; parking would just disable cores. |
| Intel hybrid (P+E) | 12th–14th gen, Core Ultra 200S | **Default** | Leave Balanced's parking so Thread Director manages P/E cores. |
| Unknown | anything else | **Default** | Safe generic tune. |

Setting `min cores = 100` (no parking) on an asymmetric dual-CCD X3D — the value
that's *correct* for single-CCD — is actively harmful there. That's why the
catalog distinguishes them.

## The optimized recipes

Every recipe is a **Balanced clone** (never a High Performance personality), with
overrides written to both the AC and DC rails so battery behavior doesn't silently
revert. Processor overrides used:

- **Boost mode = Aggressive** — every recipe. Lets the chip reach and hold its
  gaming clocks.
- **Core parking min cores** — `100` (no parking) or `50` (park frequency CCD),
  per the table above.
- **Single-CCD recipe** also sets: perf increase threshold `60`, idle demote
  threshold `10`, minimum processor state `5`, maximum processor state `100`
  (the tune verified live on the developer's 9850X3D).

## Prebuilt vs optimized

- **Suggest best prebuilt plan** — switches the active plan to the best-matching
  *already-installed* Windows plan (Balanced for all modern CPUs). Creates
  nothing.
- **Build optimized plan** — clones Balanced into a `GamerGuardian Gaming
  [<model>]` plan, writes the recipe, and activates it. Idempotent: re-running
  reuses or re-tunes the existing plan instead of stacking duplicates, and it is
  bound to your machine so a copied config never deletes another machine's plan.

Every action is logged to `changes.log` and shown in the Apply Results window
with before/target/after values and a copyable verify command. The app verifies
the active scheme **and reads each override back** — a half-applied plan is
reported as not verified.

## Dual-CCD: the plan is necessary but not sufficient

For asymmetric dual-CCD X3D, the power plan alone doesn't route games to the
cache CCD. It depends on a stack the app **cannot set**:

- **BIOS: CPPC Dynamic Preferred Cores = Driver** (advisory — not readable from
  Windows).
- **AMD 3D V-Cache Performance Optimizer service** running (the app can read
  this).
- **Xbox Game Bar enabled** — the game-detection signal (the app can read this).

The CPU / Power tab surfaces these honestly: checkable items show met/unmet, the
BIOS item is labeled advisory, and the app never claims a bare "optimized ✓".
Because of this, the Recommended preset will **not** disable Xbox Game Bar or the
AMD V-Cache service on a dual-CCD X3D machine.

## Why not High Performance / Ultimate?

The "gaming = High Performance" belief is outdated for modern boost-managed CPUs.
High Performance pins the minimum processor state to 100% and disables core
parking, which:

- **Steals boost headroom** — keeping every core pegged high eats the shared
  power/thermal budget, leaving less for the 1–2 cores a game wants at maximum
  single-core boost. It can *lower* your gaming clocks.
- **Runs hotter** → earlier thermal throttling → lower sustained clocks.
- **Breaks scheduling** — on Intel hybrid it fights Thread Director; on dual-CCD
  X3D it disables the parking that routes games to the cache CCD.

The Balanced-clone tune captures High Performance's only real benefit (no
downclock stutter, via a raised minimum state where appropriate) without the
costs. That's why GamerGuardian builds Balanced-based plans and recommends
Balanced as the prebuilt for every modern CPU — and never builds or recommends a
High Performance personality.

## Footprint

Baseline: `main` at the commit this feature branched from
(`f14b371`). The feature keeps the app slim:

- **No new dependencies** — the project still references only WPF-UI and
  `System.ServiceProcess.ServiceController`. CPU detection is a registry read
  (no WMI / `System.Management`, no CPUID library).
- **No new always-on work** — detection is a one-time cached `Lazy<CpuInfo>`;
  the catalog is static data; drift monitoring reuses the existing 30s
  power-plan poll; building/suggesting a plan is an event-driven button action.

The published single-file binary-size delta (budget ≤ ~0.5 MB) and idle
working-set delta (budget ≤ 3 MB mean over three 5-minute runs, software-rendered
WPF / Mica-off intact) are measured against the baseline build during dev-build
beta validation and recorded here at that time.
