# Architecture rationale

Why GamerGuardian looks the way it does. Some of these decisions are unusual for a Windows tray app — the rationale here matters more than the code itself.

## Why user-mode P/Invoke instead of a kernel driver

A kernel driver could read and write these settings without UAC prompts, react to changes in real-time via registry filter callbacks, and have a smaller working-set footprint. We deliberately don't go that route.

**Trust costs more than convenience.** Installing a kernel driver:
- Requires admin (UAC at install) and either an EV cert (~$300/yr) or attestation signing
- Survives reboots and runs as `SYSTEM`
- Has full access to every byte of system memory and every file on every volume
- Is impossible for a user to audit without specialized tools (kernel debugger, IDA Pro)
- Inherits the trust profile of every other driver on the system — one signed driver vulnerability becomes a system compromise

User-mode P/Invoke:
- Runs at the logged-in user's privilege level (medium IL by default)
- Prompts UAC for the specific writes that need it (HKLM, service start type)
- Touches only documented Windows APIs that are also reachable from PowerShell
- Is auditable by anyone who can read C# — every API call is in [`src/GamerGuardian/Native/`](https://github.com/carterscode/GamerGuardian/tree/main/src/GamerGuardian/Native), 8 small files

The tradeoff: every HKLM write triggers a UAC prompt, which is mildly annoying but **also a feature** — the user always sees, in advance, that a privileged change is about to happen. There is no silent escalation path.

## Why a two-tier poll plus a few system events

The backbone is still polling — most of the registry values GamerGuardian watches don't broadcast change notifications, and the few that do (via `RegNotifyChangeKeyValue`) require an open handle plus a wait thread per key. But it isn't a flat 30 s sweep over everything. Settings are split by how often Windows actually changes them (see [`MonitorVolatility`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/MonitorVolatility.cs)):

- **Display settings (HDR, refresh rate, resolution, DRR)** are *volatile* — Windows silently resets them after sleep, a monitor hot-plug, or a driver/GPU event. These are polled on the **fast timer**, at the user's poll interval (default 30 s), because catching that mid-session change quickly is the whole point of watching them.
- **The dozens of set-and-forget registry / policy / service settings** are *stable* — they only move across a reboot or a feature update. Polling them every 30 s did nothing but burn cycles and hand every drifting auto-apply a fresh chance to fire a UAC prompt each tick. Instead they're checked at **startup**, on a **10-minute backstop** timer, and on the three OS events that coincide with a real change: `PowerModeChanged` (resume from sleep), `SessionSwitch` (unlock), and `DisplaySettingsChanged` (re-probe display support and re-check the volatile tier).

This keeps the watch responsive without subjecting the user to a secure-desktop UAC prompt every 30 s for a setting Windows keeps reverting. The [`AutoApplyCircuitBreaker`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/AutoApplyCircuitBreaker.cs) is the backstop for that case: after a few verify-then-revert loops it trips a setting to notify-only for a cooldown window.

Polling also makes the **paused** state trivial: skip the tick, do nothing. A pure subscription model would still wake the process when keys change during a game.

## Why pause during gameplay and benchmarks (and what counts)

The whole point is "stay out of the way." Three independent fullscreen detectors:

1. **Exclusive fullscreen / presentation mode** — `SHQueryUserNotificationState`. Catches DirectX exclusive fullscreen and Win11 UWP fullscreen apps.
2. **Borderless-fullscreen** — compares the foreground window's rect to `MONITORINFO.rcMonitor` (not `rcWork`). Edge-to-edge match → fullscreen-like. Maximized regular windows match `rcWork` and are correctly ignored.
3. **Known benchmarks** — process-name allowlist (3DMark, Cinebench, Geekbench, AIDA64, FurMark, Unigine Heaven/Valley/Superposition, OCCT, Prime95, y-cruncher, CrystalDiskMark, PCMark, PassMark).

Any of the three firing pauses the polling tick entirely. No drift checks, no notifications, no registry calls. There's also a manual pause from the tray menu.

The benchmark allowlist is hand-curated rather than heuristic because **false positives during benchmarks are unforgivable** — a registry-touching tool is exactly the kind of "background process" benchmark guides tell you to disable. Hand-curation lets us be deliberate about what we whitelist.

## Why a single-file self-contained .NET publish (~77 MB)

Three options were on the table:

| Approach | Binary size | Cost |
|---|---|---|
| Framework-dependent .NET 8 | ~3 MB | User must install .NET 8 runtime separately |
| Self-contained, multi-file | ~140 MB on disk | One folder with 100+ files; messy, makes spelunking harder |
| Self-contained, single-file (chosen) | ~77 MB on disk | Slightly slower cold start as the single-file extracts on first run |

The size cost is real but **predictable** — every release is the same ~77 MB regardless of what's inside. The trust cost of "go install the .NET 8 runtime first" is non-trivial: it asks the user to install a much larger system component just to run a tray app. Self-contained means GamerGuardian's binary is the only new code on the user's system.

Single-file specifically (vs. multi-file) makes "what is this?" easier to answer: one EXE, one set of hashes in [`SHA256SUMS.txt`](Security#reproducibility), no DLL substitution surface.

## Why per-user install (no admin required)

The Inno Setup script installs to `%LOCALAPPDATA%\Programs\GamerGuardian` with `PrivilegesRequired=lowest`. This was deliberate:

- No UAC at install — installer is just an EXE writing to user-writable paths.
- No HKLM keys created during install. The only HKLM activity is when the user *applies* a setting that needs HKLM — and those each prompt UAC explicitly.
- Uninstall is the same — runs without UAC and only removes user-scope state.

The downside: a per-machine install would let multiple Windows users share one binary. We've judged that's not worth the elevated install for a tray app.

## Why a separate `IMonitoredSetting` per setting

Each monitor is one ~30-line file — read raw, compute desired, yield a `DriftItem`. This is verbose compared to a generic "registry-key monitor" abstraction, but on purpose:

- **Auditable.** A reader can open one file and see the entire read+write logic for one setting. No abstraction layers to chase through.
- **Targetable.** When something breaks for a specific setting (e.g., the [v0.1.18 power-plan combo bug](https://github.com/carterscode/GamerGuardian/issues)), the blast radius is one file.
- **Diffable.** `git log` on one file tells you the full history of how that setting has been handled.

The genericized version of this exists too — [`WindowsServiceMonitor`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/WindowsServiceMonitor.cs) is registered N times from [`ServiceCatalog`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/ServiceCatalog.cs). That's because services genuinely follow one shape (start type + stop). The dozens of fixed settings genuinely don't.

## Why HKLM writes go through `reg.exe` / `sc.exe` and not `runas` of GamerGuardian itself

Two options for an HKLM write from a non-elevated process:
1. Re-launch GamerGuardian with `Verb=runas` and a "perform-write" CLI flag. The whole app runs elevated, performs the write, exits.
2. Spawn a tiny helper (`reg.exe`, `sc.exe`) with `Verb=runas` to do just the one write. The main app stays at medium IL.

We use option 2. Reasons:
- **Smaller surface in elevated context.** A bug in any monitor code can never run as admin because the monitor code never runs elevated.
- **Audit trail.** A user watching their UAC prompts sees `reg.exe` or `sc.exe` — Microsoft-signed, well-known. They don't see "another mystery program" asking for admin.
- **Easier to verify.** The exact `reg add` / `sc config` arguments are visible in the change log mechanism field. Run them yourself in an elevated cmd and you'll get the same result.

## Why no telemetry / phone-home, ever

Trivial to defend at the source level: there's no HTTP client in the codebase except [`UpdateService`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Services/UpdateService.cs), and it talks only to `api.github.com` (release metadata) and `github.com` (installer download from the configured Releases endpoint).

Crash reports go to a local file (`%TEMP%\gamerguardian_error.log`), not a server. Auto-update preference is a local checkbox with no remote opt-in tracking.

Trust signal: `netstat -bno` should show no GamerGuardian connections at all when sitting in the tray, except in the brief window after launch when it's hitting the GitHub Releases API.

## Why working-set trimming runs at all

WPF is fat. The visual tree of an idle Settings window is 60+ MB. Without explicit trimming, the working set stayed elevated long after the window was closed because the .NET runtime doesn't aggressively return memory to the OS by default.

The trimming code:
- After Settings window close: GC Gen 2 with LOH compaction, then `EmptyWorkingSet`
- Every 5 polling ticks (~2.5 minutes): GC Gen 2, `EmptyWorkingSet`
- `RetainVMGarbageCollection=false` in csproj — runtime returns memory aggressively

Net effect: ~135 MB peak after first Settings open → ~25 MB at idle within minutes. Verified empirically over hours of runtime.

## How DRR is monitored

DRR (Dynamic Refresh Rate) is a different mechanism from VRR. VRR is the `GraphicsDrivers\VRROptimizeEnable` registry flag; DRR is set per **display target** through the public CCD `SetDisplayConfig` path by toggling the boost-refresh-rate flag with `SDC_VIRTUAL_REFRESH_RATE_AWARE` — user-mode, no elevation (see [`DrrInterop`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Native/DrrInterop.cs)).

Support detection is the subtle part. Whether a target can run DRR is a static property of the panel + driver + OS, but probing it isn't free — it calls `SetDisplayConfig(SDC_VALIDATE)`, which on some GPU/driver combos re-evaluates the display pipeline and briefly stalls the mouse and keyboard. Running that on every 30 s drift poll caused a periodic input hitch, so the result is **cached per display** and the probe runs at most once per target — re-probed only when the display topology changes, via `ClearSupportCache()` on the `DisplaySettingsChanged` event. DRR lives on the Display tab alongside the other volatile display settings.
