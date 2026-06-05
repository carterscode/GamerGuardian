# Settings & tabs guide

This page explains **every tab** in the GamerGuardian Settings window and **what each setting does and means**. For the deep per-setting reference (full rationale, per-scenario recommendations, risks, exact reversal, and a copy-pasteable verify command), see [SETTINGS-REFERENCE.md](https://github.com/carterscode/GamerGuardian/blob/main/docs/SETTINGS-REFERENCE.md), which is generated from the source catalog.

## The three controls on every monitored setting

Almost every setting exposes the same three controls, so they're worth understanding once:

| Control | What it means |
|---|---|
| **Monitor** | When checked, GamerGuardian watches this setting on every poll (default every 30 s) and reports when it has *drifted* away from your chosen value. Unchecked = ignored entirely. |
| **Want** (the desired value) | The value you want this setting to hold — shown as **Enabled / Disabled** for intuitive settings or **Gaming / Default** where the registry meaning is inverted (so "Gaming" always means the gaming-optimized state, even when that writes a `0` under the hood). |
| **Auto-apply silently** | When checked, GamerGuardian re-applies your desired value automatically the moment it detects drift — no prompt. Unchecked = it only notifies you, and you apply with a click. |

Other conventions:

- **Current** / **Default** labels under each setting show the live system value and the Windows default, so you can see at a glance whether you're already where you want to be.
- A yellow **reboot required** badge means the change only takes full effect after a restart.
- **Learn more** expanders carry the same per-setting explanation as the reference doc.
- Changes are **staged**: toggling preferences doesn't write to Windows until you click **Apply** (or **Save & close**). Everything is reversible, and every applied change is recorded in [`changes.log`](Logging).
- **HKCU** (per-user) settings apply directly; **HKLM** (machine-wide) settings prompt once for **UAC** elevation. Multi-value changes batch into a single prompt.

---

## General

App preferences and the one-click setup — not a monitored-setting tab.

- **One-click Recommended setup** — stages the gaming-optimized configuration across *every* tab at once (sets each setting's **Want**, turns **Monitor** on, and opts into **Auto-apply**). Idempotent: re-running it after an update only picks up newly added settings.
- **Launch at startup** — start GamerGuardian minimized to the tray when you sign in.
- **Polling interval** — how often (seconds) the drift check runs. Default 30 s.
- **Theme** — Light / Dark / System.
- **Check for updates on startup** — looks at GitHub Releases and offers a one-click update.
- **Change log** — quick access to `changes.log`, the record of everything the app has applied.

---

## Global gaming

System-wide gaming tweaks (HKCU/HKLM registry). Each is a monitored toggle.

| Setting | What it does / means |
|---|---|
| **Game Mode** | Windows prioritizes the running game and suppresses background work. Safe to leave on. |
| **Game DVR background recording** | Always-on rolling game capture (Win+Alt+G). Costs CPU/GPU; gaming-recommended **off**. GamerGuardian also locks the machine-wide `AllowGameDVR` policy so updates can't silently re-enable capture. |
| **Hardware-accelerated GPU Scheduling (HAGS)** | Lets the GPU manage its own command queue — lower latency on supported GPUs. *(Reboot)* |
| **Memory Integrity / VBS (Core Isolation)** | Hypervisor-enforced code integrity. Disabling recovers ~5–15% gaming performance at the cost of reduced malware protection — a deliberate security trade-off. *(Reboot)* |
| **System Responsiveness** | The CPU share Windows reserves for non-multimedia tasks (default 20%). Lowering to 10% frees CPU for games. *(Reboot)* |
| **USB Selective Suspend (global)** | Stops Windows suspending idle USB devices, eliminating first-input lag on mice/keyboards/headsets. *(Reboot)* |
| **Games multimedia task profile** | Boosts the MMCSS scheduling priority of processes registered as games. |
| **Mouse "Enhance pointer precision"** | Mouse acceleration. Most gamers want this **off** for 1:1 aim. |
| **Fullscreen optimizations (global)** | Borderless-windowed compositing wrapper for fullscreen apps. Off forces true exclusive fullscreen on titles that support it. |
| **Variable Refresh Rate (DirectX)** | Exposes G-Sync/FreeSync to DirectX games that lack their own toggle. **Not** the same as Dynamic Refresh Rate (see Display). |
| **Fast Startup (hybrid boot)** | Hybrid shutdown that skips a true cold boot. Off makes every shutdown clean and fixes stale driver/USB state. *(Reboot)* |
| **Visual effects (best performance)** | Disables Windows UI animations/effects for a snappier desktop. Applies fully after sign-out. |
| **Power plan** (card) | Picks the active Windows power scheme (Balanced / High Performance / etc.). High Performance keeps CPU clocks elevated. *(See also the CPU/Power tab for a CPU-tuned custom plan.)* |

---

## Privacy

Telemetry/privacy toggles Windows often re-enables after feature updates — the drift-guard re-asserts your choice.

| Setting | What it does / means |
|---|---|
| **Advertising ID** | A per-user identifier apps use to profile you for ads. Privacy-recommended **off**. Per-user, no elevation. |
| **Tailored experiences** | Lets Windows use your diagnostic data to personalize tips, ads, and recommendations. Off stops the personalization. Per-user. |
| **Cross-Device Platform (CDP)** | The "Continue experiences on this device" / shared-experiences subsystem (handoff, shared clipboard, nearby-device discovery). Off via machine policy if you don't use cross-device features. |
| **Activity History / Timeline** | Collection and publishing of your activity feed. Off via machine policy (three values set together). |

---

## Network

Network-latency tweaks. **Network Throttling** is a safe, well-established tweak; **Nagle** and **NIC power management** are contested — their benefit varies by hardware and can make some connections *worse*, so read each Learn more and revert if latency degrades.

| Setting | What it does / means |
|---|---|
| **Network Throttling** | Windows' MMCSS rate-limits network packets during multimedia tasks; disabling removes that pacing for steadier online-game netcode. Safe. |
| **Nagle's algorithm (TCP no-delay)** | Nagle batches small TCP packets; disabling (per active adapter) sends them immediately, which *can* lower latency. Contested — may hurt on Wi-Fi/congested links. *(Applied across all active adapters in one prompt.)* |
| **NIC power management** | Disables "Allow the computer to turn off this device to save power" so the network adapter never sleeps, avoiding wake-from-idle stalls. Higher idle power; worse on laptop battery. *(Reboot; all active adapters.)* |

---

## Windows services

A curated catalog of Windows services GamerGuardian can stop and set to **Manual** or **Disabled**, each monitored against your preference.

- **One-click presets** — "Gaming optimized" flips the safe-to-disable subset; "Default" restores them.
- **Per-service control** — Default / Manual / Disabled, with honest risk notes on each.
- Includes telemetry (`DiagTrack`), `MapsBroker`, `Fax`, `lfsvc` (Geolocation), `wisvc` (Windows Insider), Xbox services, `DoSvc` (Delivery Optimization, handled via Group Policy because Windows reverts the normal path), `iphlpsvc` (IP Helper), `RemoteAccess` / `RemoteRegistry` (drift-confirm), and more. Some (Print Spooler, Windows Search) are listed but opt-in because disabling them has real downsides.

---

## Windows AI

Policy-toggle disables for Windows/Microsoft AI features. `DesiredOn = true` keeps the Windows default; turning a row **off** writes the disable policy (reversible by deleting the same value).

- **Lockdown policies** — Copilot, Recall + AI data analysis, Click-to-Do (Snipping Tool AI), Edge Copilot/Hubs/GenAI, Notepad Rewrite + Paint AI, search-box AI suggestions + taskbar companion, Windows AI Actions (right-click rewrite/summarize), typing/input-insights data collection, and Microsoft 365 Copilot in Word/Excel/OneNote.
- **AI apps (UWP removal)** — optional one-way removal of AI UWP packages (`Microsoft.Copilot`, the AI Copilot provider, the AI Experience component), with an auto-apply opt-in in case Windows re-provisions them.

Stays in the safe "policy toggle + service disable + opt-in UWP removal" lane — inspired by [zoicware/RemoveWindowsAI](https://github.com/zoicware/RemoveWindowsAI).

---

## Display

Per-display settings, enumerated for each active monitor. Backed by the Windows DisplayConfig (CCD) API.

| Setting | What it does / means |
|---|---|
| **HDR** | Per-display HDR on/off. Windows often turns HDR off after sleep/driver updates — monitoring catches and restores it. |
| **Dynamic Refresh Rate (DRR)** | Win11 22H2+ feature that boosts the refresh rate between a low virtual rate and the panel's physical max based on content. Shows **"Not supported on this display"** on panels/drivers that lack it. Distinct from VRR. |
| **Refresh rate** | Maximum supported, or pinned to a specific Hz. Catches Windows silently dropping the rate. |
| **Resolution** | Optionally pin a specific resolution (opt-in). |

---

## CPU / Power

CPU detection and CPU-aware power tuning.

- **Detected CPU** — the model GamerGuardian detected and the tuning tier it maps to.
- **Power Throttling** — disables Windows' throttling of background/idle threads for sustained performance (desktop-recommended; leave Default on laptop battery). A registry setting, not a power-scheme change.
- **Gaming power plan** — builds a CPU-tuned custom power plan (a Balanced clone with aggressive boost and the right core-parking behavior for your CPU — e.g. single-CCD X3D vs. asymmetric dual-CCD X3D). Your existing Windows plans are never modified. See [CPU-aware power plans](CPU-Power-Plans).
- **Dual-CCD routing dependencies** — for asymmetric X3D chips (e.g. 9950X3D), surfaces the BIOS/service/Game Bar prerequisites the power plan alone can't set.

---

## Recommended BIOS

Guidance only (nothing is changed): a checklist of BIOS settings worth enabling for gaming (Resizable BAR / Smart Access Memory, XMP/EXPO memory profiles, the correct CPPC mode for X3D routing, etc.). These live in firmware, outside what a user-mode app can touch.

---

> **Source-of-truth note:** this page is mirrored from [`docs/wiki/`](https://github.com/carterscode/GamerGuardian/tree/main/docs/wiki). The per-setting deep reference [SETTINGS-REFERENCE.md](https://github.com/carterscode/GamerGuardian/blob/main/docs/SETTINGS-REFERENCE.md) is generated from `SettingDocsCatalog.cs`, so it never drifts from the code.
