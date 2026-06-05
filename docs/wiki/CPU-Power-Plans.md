# CPU-aware power plans

GamerGuardian detects your CPU and helps you run the right gaming power plan for
it — either by switching to the best-matching **prebuilt** Windows plan, or by
building a **GamerGuardian-authored optimized** plan tuned for your chip.

> **Your built-in Windows plans are never touched.** The app only creates or
> re-tunes its own plan and switches the active plan. Balanced, High Performance,
> Power Saver, and any custom plan you made are left exactly as they are.

## What is detected

A single cached registry read of your processor name at startup (no WMI, no
hardware probing). From it the app extracts vendor, model, and family. CCD
topology and parking strategy come from the tune catalog, not core counts.

## How a tune is chosen

Exact model → CPU family → safe generic. The **optimized** option is always
available; only its precision changes. The **prebuilt** suggestion is always
offered alongside.

## The core principle: cache asymmetry, not CCD count

| CPU class | Examples | Parking |
|---|---|---|
| Single-CCD X3D | 9850X3D, 9800X3D, 7800X3D | Off (min cores 100) |
| Asymmetric dual-CCD X3D | 9950X3D, 9900X3D, 7950X3D, 7900X3D | Park frequency CCD (min cores 50) |
| Dual-V-cache X3D | 9950X3D2 | Off (both CCDs have cache) |
| Non-X3D Ryzen | 7700X, 9700X, 7950X, 9950X | Off |
| Intel hybrid | 12th–14th gen, Core Ultra | Leave Thread Director's default |
| Unknown | anything else | Default (generic tune) |

The "park one CCD" rule applies **only** to asymmetric dual-CCD X3D (one V-cache
CCD among two). Everything else wants parking off.

## Prebuilt vs optimized

- **Suggest best prebuilt** — switches to the best installed Windows plan
  (Balanced for modern CPUs). Creates nothing.
- **Build optimized** — clones Balanced into a `GamerGuardian Gaming [<model>]`
  plan with aggressive boost and the correct parking. Idempotent (re-tunes its
  own plan instead of stacking duplicates) and machine-bound.

Every action is logged and verified (active scheme **and** each setting value).

## Dual-CCD: necessary but not sufficient

For asymmetric dual-CCD X3D the power plan depends on a stack the app can't set:
BIOS **CPPC Dynamic Preferred Cores = Driver**, the **AMD 3D V-Cache Optimizer
service**, and **Xbox Game Bar**. The CPU / Power tab surfaces these; the app
never claims a bare "optimized ✓", and the Recommended preset won't disable Game
Bar or the AMD service on these chips.

## Why not High Performance?

It's outdated advice for modern CPUs. High Performance pins clocks to 100% and
disables core parking, which steals single-core boost headroom, runs hotter, and
breaks Thread Director / X3D cache-CCD routing. GamerGuardian's Balanced-clone
tune gets the benefit without the cost — so it builds Balanced-based plans and
recommends Balanced, never High Performance.

See also: [Verification](Verification), [Logging](Logging).
