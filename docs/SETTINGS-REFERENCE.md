# GamerGuardian settings reference

This document is **generated from [`SettingDocsCatalog.cs`](../src/GamerGuardian/Services/SettingDocsCatalog.cs)**. Edit the catalog, then run `GamerGuardian.exe --gen-docs` to regenerate. A unit test asserts that the committed file matches the catalog so they can't drift.

Every setting here is managed via the Settings window. Toggle **Monitor** to have GamerGuardian watch the value; toggle **Auto-apply silently** to have it auto-correct on drift. Both default off, so nothing changes until you opt in.

## Contents

**Global gaming + display**

- [Active Windows power plan](#active-windows-power-plan) (`powerplan`)
- [CPU-optimized gaming power plan](#cpu-optimized-gaming-power-plan) (`cpuplan`)
- [Display refresh rate](#display-refresh-rate) (`refresh`)
- [Display resolution](#display-resolution) (`resolution`)
- [Dynamic Refresh Rate (DRR)](#dynamic-refresh-rate-drr) (`drr`)
- [Fast Startup (hybrid boot)](#fast-startup-hybrid-boot) (`faststartup`)
- [Fullscreen optimizations (global)](#fullscreen-optimizations-global) (`fso`)
- [Game DVR background recording](#game-dvr-background-recording) (`gamedvr`)
- [Games multimedia task profile](#games-multimedia-task-profile) (`gamestask`)
- [Hardware-accelerated GPU Scheduling](#hardware-accelerated-gpu-scheduling) (`hags`)
- [HDR (High Dynamic Range)](#hdr-high-dynamic-range) (`hdr`)
- [Memory Integrity / VBS (Core Isolation)](#memory-integrity---vbs-core-isolation) (`memintegrity`)
- [Mouse "Enhance pointer precision"](#mouse-enhance-pointer-precision) (`mouseaccel`)
- [Network Throttling](#network-throttling) (`netthrottle`)
- [Power Throttling](#power-throttling) (`powerthrottling`)
- [System Responsiveness](#system-responsiveness) (`sysresponse`)
- [USB Selective Suspend (global)](#usb-selective-suspend-global) (`usbsuspend`)
- [Variable Refresh Rate (DirectX)](#variable-refresh-rate-directx) (`vrr`)
- [Virtualization-Based Security (full stack)](#virtualization-based-security-full-stack) (`vbs`)
- [Visual effects (best performance)](#visual-effects-best-performance) (`visualfx`)
- [Windows Game Mode](#windows-game-mode) (`gamemode`)

**Privacy**

- [Activity History / Timeline](#activity-history---timeline) (`privacy.activityhistory`)
- [Advertising ID](#advertising-id) (`privacy.advertisingid`)
- [Cross-Device Platform (CDP)](#cross-device-platform-cdp) (`privacy.cdp`)
- [Inking & typing personalization](#inking--typing-personalization) (`privacy.inking`)
- [Online (cloud) speech recognition](#online-cloud-speech-recognition) (`privacy.speech`)
- [Tailored experiences](#tailored-experiences) (`privacy.tailoredexp`)

**Debloat (ads, nags & background bloat)**

- ["Finish setting up your device" nag](#finish-setting-up-your-device-nag) (`debloat.finishsetup`)
- [Edge startup boost & background mode](#edge-startup-boost--background-mode) (`debloat.edge`)
- [File Explorer ad banners](#file-explorer-ad-banners) (`debloat.explorerads`)
- [Lock screen tips, fun facts & ads](#lock-screen-tips-fun-facts--ads) (`debloat.spotlight`)
- [Start menu recommendations & recent files](#start-menu-recommendations--recent-files) (`debloat.startrecommend`)
- [Suggested content & silent app installs](#suggested-content--silent-app-installs) (`debloat.suggestedcontent`)
- [Widgets / News and interests](#widgets---news-and-interests) (`debloat.widgets`)
- [Windows feedback request popups](#windows-feedback-request-popups) (`debloat.feedback`)

**Network**

- [Nagle's algorithm (TCP no-delay)](#nagles-algorithm-tcp-no-delay) (`network.nagle`)
- [NIC power management](#nic-power-management) (`network.nicpower`)

**Windows AI policies**

- [Click-to-Do (Snipping Tool AI)](#click-to-do-snipping-tool-ai) (`ai.clicktodo`)
- [Microsoft 365 Copilot in Word / Excel / OneNote](#microsoft-365-copilot-in-word---excel---onenote) (`ai.office`)
- [Microsoft Edge Copilot / Hubs / GenAI](#microsoft-edge-copilot---hubs---genai) (`ai.edge`)
- [Notepad Rewrite + Paint AI features](#notepad-rewrite--paint-ai-features) (`ai.notepadpaint`)
- [Search box AI suggestions + taskbar companion](#search-box-ai-suggestions--taskbar-companion) (`ai.settingssearch`)
- [Typing / input insights data collection](#typing---input-insights-data-collection) (`ai.inputinsights`)
- [Windows AI Actions](#windows-ai-actions) (`ai.actions`)
- [Windows Copilot](#windows-copilot) (`ai.copilot`)
- [Windows Recall + AI data analysis](#windows-recall--ai-data-analysis) (`ai.recall`)

**Windows AI UWP packages**

- [Microsoft 365 Copilot (launcher app)](#microsoft-365-copilot-launcher-app) (`ai.app:Microsoft.MicrosoftOfficeHub`)
- [Microsoft Copilot (UWP)](#microsoft-copilot-uwp) (`ai.app:Microsoft.Copilot`)
- [Windows AI Copilot Provider](#windows-ai-copilot-provider) (`ai.app:Microsoft.Windows.Ai.Copilot.Provider`)
- [Windows AI Experience](#windows-ai-experience) (`ai.app:MicrosoftWindows.Client.AIX`)

**Windows services**

- [Agent Activation Runtime Service](#agent-activation-runtime-service) (`service:AarSvc`)
- [Connected User Experiences and Telemetry](#connected-user-experiences-and-telemetry) (`service:DiagTrack`)
- [Delivery Optimization](#delivery-optimization) (`service:DoSvc`)
- [Distributed Link Tracking Client](#distributed-link-tracking-client) (`service:TrkWks`)
- [Downloaded Maps Manager](#downloaded-maps-manager) (`service:MapsBroker`)
- [Kiosk Mode (Assigned Access)](#kiosk-mode-assigned-access) (`service:AssignedAccessManagerSvc`)
- [Parental Controls](#parental-controls) (`service:WpcMonSvc`)
- [Payments and NFC/SE Manager](#payments-and-nfc-se-manager) (`service:SEMgrSvc`)
- [Phone Service](#phone-service) (`service:PhoneSvc`)
- [Retail Demo Service](#retail-demo-service) (`service:RetailDemo`)
- [Routing and Remote Access](#routing-and-remote-access) (`service:RemoteAccess`)
- [Superfetch / SysMain](#superfetch---sysmain) (`service:SysMain`)
- [Windows AI Fabric Service](#windows-ai-fabric-service) (`service:WSAIFabricSvc`)
- [Windows Error Reporting Service](#windows-error-reporting-service) (`service:WerSvc`)
- [Windows Image Acquisition (WIA)](#windows-image-acquisition-wia) (`service:stisvc`)
- [Windows Insider Service](#windows-insider-service) (`service:wisvc`)
- [Windows Search](#windows-search) (`service:WSearch`)
- [Xbox Accessory Management](#xbox-accessory-management) (`service:XboxGipSvc`)
- [Xbox Live Auth Manager](#xbox-live-auth-manager) (`service:XblAuthManager`)
- [Xbox Live Game Save](#xbox-live-game-save) (`service:XblGameSave`)
- [Xbox Live Networking Service](#xbox-live-networking-service) (`service:XboxNetApiSvc`)

**Windows scheduled tasks**

- [Microsoft Compatibility Appraiser](#microsoft-compatibility-appraiser) (`task:\microsoft\windows\application experience\microsoft compatibility appraiser`)
- [Microsoft Compatibility Appraiser (Exp)](#microsoft-compatibility-appraiser-exp) (`task:\microsoft\windows\application experience\microsoft compatibility appraiser exp`)
- [PcaPatchDbTask](#pcapatchdbtask) (`task:\microsoft\windows\application experience\pcapatchdbtask`)
- [ProgramDataUpdater](#programdataupdater) (`task:\microsoft\windows\application experience\programdataupdater`)
- [StartupAppTask](#startupapptask) (`task:\microsoft\windows\application experience\startupapptask`)


---

## Global gaming + display

### Active Windows power plan

`powerplan` &nbsp; **Recommended:** CPU-aware -- the best-matching prebuilt for your CPU (Balanced on modern CPUs, whose boost algorithm beats a pegged High Performance plan), or build the custom optimized plan on the CPU / Power tab

**Why this is the recommendation.** Balanced (the default) lets the OS dynamically scale CPU clocks to save power, which costs you a few ms of latency at the start of any CPU-bound burst. High Performance / Ultimate Performance keeps CPU clocks pegged at the top of the curve for predictable response.

**What it does.** The active Windows power scheme. Controls CPU throttling thresholds, sleep timers, hard-drive spindown, USB selective suspend, and dozens of other power-related defaults.

**How it helps.** Eliminates CPU clock-ramp latency. First-frame and first-input responses feel snappier. Background tasks finish faster.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| High Performance / tuned plan (gaming) | CPU clocks stay pegged, so there's no ramp-up latency at the start of a CPU burst. | 10-30 W more idle draw, warmer components, more fan noise; worse battery on a laptop. |
| Balanced (default) | Lets modern CPUs' boost algorithms run (often better than a pegged plan) and saves power when idle. | A few ms of clock-ramp latency at the start of bursts on older CPUs. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS / Streaming | High Performance or a tuned custom plan |
| Casual single-player on a desktop | High Performance |
| Laptop on battery | Balanced (saves power) |
| Laptop plugged in | High Performance |
| Idle workstation | Balanced (drops back to power-saving when idle) |

**Risks.** Higher idle power draw -- typically 10-30 W on desktop, more on high-end. Components run a few degrees warmer. Fan noise slightly higher. On laptops on battery: noticeably worse battery life.

**Command line (PowerShell):**

```powershell
# Check the current value
powercfg /getactivescheme

# Apply the gaming-optimized value
powercfg /setactive (plan-guid)

# Reverse it (restore the Windows default)
powercfg /setactive SCHEME_BALANCED   # restore the Balanced plan
```

**Reversible via.** powercfg /setactive SCHEME_BALANCED (or pick another plan from Settings > System > Power).


### CPU-optimized gaming power plan

`cpuplan` &nbsp; **Recommended:** Build optimized for your CPU (or suggest Balanced)

**Why this is the recommendation.** The right gaming power plan is CPU-dependent. Single-CCD X3D wants core parking OFF; asymmetric dual-CCD X3D (e.g. 9950X3D) wants the frequency CCD PARKED so games stay on the cache CCD; symmetric and non-X3D parts want no parking. High Performance is wrong in both directions for these chips -- it pins clocks and disables the parking modern schedulers rely on. The optimized plan is always a Balanced clone (never a High Performance personality) with aggressive boost.

**What it does.** A GamerGuardian-authored power plan, built by cloning Balanced and writing a small set of processor overrides tuned for your detected CPU. The app detects the CPU at startup and offers either the best-matching prebuilt Windows plan or this custom optimized plan. The optimized recipe is tiered: an exact model match uses a precise recipe, a recognized family uses a family recipe, and an unknown CPU gets a safe generic tune (clearly labeled).

**How it helps.** Aggressive boost lets the CPU reach and hold its gaming clocks; correct parking keeps game threads on the right cores; faster ramp thresholds reduce clock-up latency. All without the heat/boost-headroom cost of High Performance.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Build the optimized plan (recommended) | A Balanced clone tuned to your CPU -- aggressive boost and the right core-parking -- without High Performance's heat/boost cost. | For asymmetric dual-CCD X3D it isn't enough alone; it relies on BIOS CPPC=Driver + the AMD V-Cache service the app can't set. |
| Keep Balanced / a stock plan | Zero setup, and fine on most modern CPUs whose own boost is already good. | Misses the per-CPU parking/boost tuning (notably for X3D chips). |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Single-CCD X3D (9850X3D / 9800X3D / 7800X3D) | Build optimized -- no parking, aggressive boost |
| Asymmetric dual-CCD X3D (9950X3D / 7950X3D) | Build optimized -- parks frequency CCD; also verify BIOS CPPC=Driver + AMD V-Cache service + Game Bar |
| Non-X3D / Intel hybrid | Build optimized (no parking / leave Thread Director) or suggest Balanced |
| Unknown CPU | Build optimized uses a labeled generic tune, or suggest the best prebuilt plan |

**Risks.** Low. The plan is additive -- your existing Windows plans are never modified or deleted, and you can switch back at any time. For asymmetric dual-CCD X3D the power plan alone is not sufficient: it depends on the AMD CCD-routing stack (BIOS CPPC=Driver, the 3D V-Cache Optimizer service, and Xbox Game Bar), which the app surfaces but cannot set.

**Command line (PowerShell):**

```powershell
# Check the current value
powercfg /getactivescheme; powercfg /query SCHEME_CURRENT SUB_PROCESSOR

# Apply the gaming-optimized value
powercfg -duplicatescheme SCHEME_BALANCED  # -> <new-guid>; powercfg -setacvalueindex <new-guid> SUB_PROCESSOR <setting-guid> <value>  (repeat per override; actual GUIDs/values in changes.log); powercfg -setactive <new-guid>

# Reverse it (restore the Windows default)
powercfg /setactive SCHEME_BALANCED   # switch back to Balanced (the custom plan can be deleted from the legacy Power control panel)
```

**Reversible via.** Switch the active plan back via Settings > System > Power, or 'powercfg /setactive SCHEME_BALANCED'. The GamerGuardian plan can be deleted from the legacy Power control panel if you no longer want it.


### Display refresh rate

`refresh` &nbsp; **Recommended:** Maximum supported

**Why this is the recommendation.** Higher refresh = lower input-to-photon latency and smoother motion. Windows sometimes silently drops the refresh rate after sleep, driver updates, or external display disconnects -- monitoring catches this.

**What it does.** Per-display refresh rate. GamerGuardian's recommended target is the display's maximum supported rate at the current resolution. Backed by ChangeDisplaySettingsEx (DEVMODE.dmDisplayFrequency).

**How it helps.** Keeps your display at its full rated refresh rate for both desktop and games (some games respect the desktop rate, some override).

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Maximum supported (recommended) | Lowest input-to-photon latency and smoothest motion; monitoring catches Windows silently dropping it. | On a laptop on battery a high rate costs real power. |
| A lower / fixed rate | Saves power on high-Hz panels (useful on battery). | Higher latency and less smooth motion than your panel can do. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Any monitor above 60 Hz | Maximum supported -- always |
| 60 Hz display | Doesn't matter; 60 is your max |
| Multi-monitor with mixed rates | Maximum per display |
| Power-saving / laptop on battery | Consider Fixed at a lower rate to save power -- the cost is real on high-Hz panels |

**Risks.** Very low. Some VRR displays produce eye-noticeable flicker at certain refresh rates in dark scenes -- if you see it, try the next rate down.

**Command line (PowerShell):**

```powershell
# Reverse it (restore the Windows default)
Settings > System > Display > Advanced display > Choose a refresh rate.
```

**Reversible via.** Settings > System > Display > Advanced display > Choose a refresh rate.


### Display resolution

`resolution` &nbsp; **Recommended:** Don't enforce unless you have a specific reason

**Why this is the recommendation.** Lets you pin a specific resolution per display. Useful for users who run games at the desktop resolution and want absolute stability against Windows occasionally changing it after driver updates.

**What it does.** Per-display resolution. Optional preference -- only enforced when the user explicitly pins a resolution. Backed by ChangeDisplaySettingsEx.

**How it helps.** Catches the case where Windows downgrades you to a lower resolution after a display reconnect or driver update.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Don't enforce (recommended) | Windows can switch resolution freely when you dock/undock or change monitors. | Without pinning, Windows could occasionally drop you to a lower resolution after a driver update. |
| Pin a resolution | Absolute stability -- the app re-asserts your chosen resolution after drift. | Fights legitimate display changes (docking a laptop, plugging in a different monitor). |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Single fixed-resolution setup | Pin to native resolution |
| Multiple display configurations (docked / undocked laptop) | Don't pin -- let Windows handle |
| Variable resolution gaming (different per game) | Don't pin |

**Risks.** Pinning can fight legitimate display changes (docking a laptop, plugging in a different monitor).

**Command line (PowerShell):**

```powershell
# Reverse it (restore the Windows default)
Uncheck 'Monitor this setting' for Resolution on the display tab.
```

**Reversible via.** Uncheck 'Monitor this setting' for Resolution on the display tab.


### Dynamic Refresh Rate (DRR)

`drr` &nbsp; **Recommended:** Personal preference -- Enabled saves power, Disabled is the most predictable

**Why this is the recommendation.** DRR is mostly a laptop power feature. Some gamers prefer a fixed maximum refresh for consistent latency and disable DRR; others keep it on for battery. It needs a VRR-capable panel and a recent driver, so the toggle only appears on displays that actually support it.

**What it does.** Per-display Win11 22H2+ feature that dynamically boosts the refresh rate between a low 'virtual' rate (e.g. 60 Hz for static content, saving power) and the panel's physical max (e.g. 120/144 Hz for scrolling/ink). Read/written via the DisplayConfig CCD API (the BOOST_REFRESH_RATE path flag) -- user-mode, no elevation. Distinct from VRR (G-Sync/FreeSync).

**How it helps.** Monitoring keeps DRR at your chosen state -- Windows can reset it after driver updates, sleep, or display reconnects. The drift-guard re-asserts your preference.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Enabled | Saves power by dropping to a low virtual refresh for static content and boosting to max for scrolling/ink. | Refresh isn't fixed, which a few gamers find less predictable. |
| Disabled | A fixed, predictable maximum refresh for consistent latency. | Loses the battery saving DRR gives on static content. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Laptop, wants battery savings | Enabled |
| Wants a fixed predictable refresh | Disabled |
| Display without DRR support | n/a -- the control is hidden |
| Desktop high-refresh gaming | Personal taste; many leave it off for consistency |

**Risks.** Very low -- DRR only engages on supported panels. Verify reflects the path flag, not a guarantee the boost engaged in every app (same honest limitation as VRR).

**Command line (PowerShell):**

```powershell
# Reverse it (restore the Windows default)
Settings > System > Display > Advanced display > 'Choose a refresh rate' > pick Dynamic / a fixed rate. GamerGuardian toggles the same DisplayConfig flag.
```

**Reversible via.** Settings > System > Display > Advanced display > 'Choose a refresh rate' > pick Dynamic / a fixed rate. GamerGuardian toggles the same DisplayConfig flag.


### Fast Startup (hybrid boot)

`faststartup` &nbsp; **Recommended:** Disabled (gaming)

**Why this is the recommendation.** Fast Startup means 'shutdown' isn't a true cold boot -- drivers and hardware can carry stale state across restarts, which occasionally causes USB/GPU/peripheral quirks. Turning it off makes every shutdown a clean boot.

**What it does.** Saves the kernel session to the hiberfile on shutdown so the next boot skips part of initialization. Driven by HKLM\...\Session Manager\Power\HiberbootEnabled=0 to disable (requires elevation and a reboot to take effect).

**How it helps.** Cleaner, more predictable boots; resolves a class of intermittent driver/peripheral issues that 'a real restart fixes'. Low drift -- mostly a set-once toggle.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (gaming, recommended) | Every shutdown becomes a true cold boot, clearing the stale driver/USB/GPU state that 'a real restart fixes'. | Boots are slightly slower. |
| Default (on) | Faster boots by restoring a saved kernel session. | 'Shutdown' isn't a clean boot, so driver/peripheral quirks can carry across restarts; also locks the disk for dual-boot. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Troubleshooting flaky USB/GPU state | Disabled (gaming) |
| Wants the fastest possible boot, no quirks | Default (leave on) |
| Dual-boot with another OS | Disabled (gaming) -- Fast Startup locks the disk |

**Risks.** Boots are slightly slower (a true cold boot). No stability risk -- this is the pre-Win8 default behavior.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power' -Name HiberbootEnabled -EA SilentlyContinue).HiberbootEnabled

# Apply the gaming-optimized value
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power' -Name HiberbootEnabled -Value 0 -Type DWord   # reboot required; reverse: set to 1

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power' -Name HiberbootEnabled -Value 1 -Type DWord   # re-enable Fast Startup
```

**Reversible via.** Set HiberbootEnabled = 1 in HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power (Control Panel > Power Options > Choose what the power buttons do > Turn on fast startup).


### Fullscreen optimizations (global)

`fso` &nbsp; **Recommended:** On (Windows default)

**Why this is the recommendation.** FSO is a quality-of-life feature -- faster alt-tab, working overlays, no display-mode-change flicker. But true FSE can be marginally faster (lower input latency) for some titles, which is why some pros disable it globally.

**What it does.** A Windows feature that runs games requesting true Fullscreen Exclusive in a borderless-windowed mode wrapped by the DWM compositor. This lets the OS draw overlays (Win+G, notifications) on top of the game without an alt-tab.

**How it helps.** Disabling globally forces true FSE where the game supports it. Saves 1-2 frames of latency on some titles by skipping the DWM compositor pass.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| On (default, recommended) | Faster alt-tab, working overlays (Discord/NVIDIA), and no display-mode flicker. | A couple frames of extra latency vs true exclusive fullscreen on some titles. |
| Off | Forces true exclusive fullscreen where supported, shaving 1-2 frames of latency. | Some games crash or render wrong, some overlays can't draw, and alt-tab is slower. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS chasing every ms | Off (forces true FSE) |
| Casual single-player | On (default; better QoL) |
| Streaming + game | On (FSE breaks some capture modes -- Display capture, Game capture with anti-cheat) |
| Productivity | Doesn't matter |

**Risks.** Some games crash or render incorrectly without FSO. Some overlays (Discord, NVIDIA App) can't draw over true FSE. Alt-tab is slower / triggers a mode switch.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode).GameDVR_FSEBehaviorMode

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode -Value 2 -Type DWord

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode -EA SilentlyContinue   # restore fullscreen optimizations (Windows default)
```

**Reversible via.** Delete GameDVR_FSEBehaviorMode and the related values from HKCU\System\GameConfigStore.


### Game DVR background recording

`gamedvr` &nbsp; **Recommended:** Off

**Why this is the recommendation.** Continuous encoding is a constant tax on framerate and GPU. On older systems it's noticeable (5-10%). On modern GPUs the cost is small but nonzero. Most serious players already use NVIDIA App / OBS for clips and don't need the OS buffer.

**What it does.** Windows Game Bar's continuous rolling-buffer recording of the active game. While enabled, the OS encodes and buffers game video so you can press Win+Alt+G to save the last X seconds. GamerGuardian covers the two per-user capture toggles AND the machine-wide AllowGameDVR policy -- the part Windows re-enables after feature updates -- so the lockdown holds.

**How it helps.** Frees the GPU's video encoder and removes a constant background overhead. Lets third-party capture tools claim the encoder exclusively (NVENC, AMD Re-Live, etc.).

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Frees the GPU's video encoder and removes constant background recording overhead. | You lose the Win+Alt+G 'save the last 30 seconds' clip shortcut. |
| On | Press Win+Alt+G any time to save a clip of what just happened. | Constant background encoding costs framerate (more on older GPUs) and ties up the encoder. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Off |
| Streaming + game | Off (use OBS / NVIDIA App for capture) |
| Casual single-player | Personal taste; leave On if you use Win+Alt+G clips |
| Productivity / not gaming | Off |

**Risks.** You lose the 'save last 30s' shortcut. Game Bar itself (overlay, FPS counter, performance widgets) still works.

**Command line (PowerShell):**

```powershell
# Check the current value
@{Capture=(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled -EA SilentlyContinue).GameDVR_Enabled; Policy=(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Name AllowGameDVR -EA SilentlyContinue).AllowGameDVR}

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled -Value 0 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR' -Name AppCaptureEnabled -Value 0 -Type DWord; $p='HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR'; New-Item $p -Force | Out-Null; Set-ItemProperty $p -Name AllowGameDVR -Value 0 -Type DWord   # reverse: set HKCU values to 1 and Remove-ItemProperty $p -Name AllowGameDVR

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled -Value 1 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR' -Name AppCaptureEnabled -Value 1 -Type DWord; Remove-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Name AllowGameDVR -EA SilentlyContinue   # re-enable Game DVR capture
```

**Reversible via.** Set HKCU\System\GameConfigStore\GameDVR_Enabled = 1 and HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR\AppCaptureEnabled = 1, and delete AllowGameDVR from HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR (the app does all three when you set it back to On).


### Games multimedia task profile

`gamestask` &nbsp; **Recommended:** Gaming (boosted)

**Why this is the recommendation.** The Games profile defaults aren't the most aggressive Windows can do. Boosting them (Priority=2, Scheduling Category=High, SFIO Priority=High) gives game threads a stronger claim on CPU and I/O during contention.

**What it does.** The Multimedia Class Scheduler Service (MMCSS) has named task profiles. The Games profile controls Priority, Scheduling Category, and SFIO Priority for processes that register against it. Most modern games register here when they call AvSetMmThreadCharacteristics("Games").

**How it helps.** More consistent frame pacing on busy systems. Better behavior when streaming/encoding alongside the game. Lower 1% lows under contention.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Gaming, boosted (recommended) | Gives game threads a stronger claim on CPU and I/O, smoothing frame pacing under load. | Background tasks are deprioritized a little further (not observable with any CPU headroom). |
| Default | Windows' shipped balance between games and everything else. | Game threads get a weaker claim during contention, so 1% lows can suffer on busy systems. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Gaming (boosted) |
| Casual single-player | Gaming |
| Streaming + game | Gaming (OBS uses its own multimedia profile; doesn't conflict) |
| Productivity | Default |

**Risks.** Very low. Background tasks deprioritized slightly further -- in practice not observable on a system with any CPU headroom.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'

# Apply the gaming-optimized value
# Apply the Games multimedia task profile (Priority/Scheduling Category/SFIO Priority). The app writes 4 DWORDs under HKLM\...\Tasks\Games -- see SettingDocs.MechanismFor for the path.

# Reverse it (restore the Windows default)
# Restore the Games MMCSS profile defaults under HKLM\...\Multimedia\SystemProfile\Tasks\Games (Priority=2, 'Scheduling Category'='High', 'SFIO Priority'='High' are Windows' own defaults; GamerGuardian restores them when you pick Default).
```

**Reversible via.** Restore default values for Priority / Scheduling Category / SFIO Priority under HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games.


### Hardware-accelerated GPU Scheduling

`hags` &nbsp; **Recommended:** On

**Why this is the recommendation.** On supported GPUs it reduces CPU overhead per frame and can lower input latency. Required for DLSS Frame Generation and some other features that rely on GPU-managed queues.

**What it does.** Lets the GPU's own scheduling processor own VRAM allocation and command submission instead of the CPU-side Windows display driver. Requires a supported GPU (NVIDIA Pascal+ / AMD Polaris+) and a reboot to switch.

**How it helps.** 1-5% framerate improvement in CPU-bound games. Smoother frame pacing under variable load. Enables modern GPU features that won't work without it.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| On (recommended) | 1-5% more FPS in CPU-bound games, and unlocks DLSS Frame Generation and other GPU-managed features. | Rare driver instability on first-gen HAGS GPUs; some pro render/ML/emulation apps prefer it off; needs a reboot. |
| Off | Safest for the few professional GPU workloads that prefer driver-side scheduling. | Leaves per-frame CPU scheduling overhead and disables features (like DLSS Frame Gen) that require GPU scheduling. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | On -- especially helpful for CPU-bound titles like CS2 / Valorant |
| Streaming + game | On |
| Casual single-player | On |
| Productivity / not gaming | On (Windows 11 default) |
| Professional GPU work (rendering, ML) | Off -- some workloads prefer driver-side scheduling |

**Risks.** Rare driver instability on first-generation HAGS-supported GPUs. Some professional/emulation apps prefer it off. Toggle requires a reboot.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode).HwSchMode

# Apply the gaming-optimized value
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode -Value 2 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode -Value 1 -Type DWord   # turn HAGS off; reboot
```

**Reversible via.** Set HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode = 1 and reboot.


### HDR (High Dynamic Range)

`hdr` &nbsp; **Recommended:** On (for HDR-capable displays where you watch HDR content)

**Why this is the recommendation.** HDR is genuinely better picture quality in supported games and movies -- but Windows is notorious for silently turning HDR off after sleep, driver updates, or display reconnects. Monitoring this catches the regression automatically.

**What it does.** Per-display HDR toggle. Enables 10-bit color depth, the wider Rec.2020 / DCI-P3 gamut, and PQ EOTF for HDR-capable monitors. Backed by the Windows DisplayConfig CCD API (the same API the OS Settings page uses).

**How it helps.** Keeps HDR enabled so games that detect it use HDR rendering paths. Catches silent OS regressions and auto-restores.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| On (recommended for HDR display + HDR content) | Genuinely better picture in HDR games/movies; monitoring auto-restores it after Windows silently turns it off. | Some games tone-map badly in HDR, and SDR desktop content can look worse than native SDR. |
| Off | Native SDR is often cleaner for desktop work and SDR-only content. | HDR games won't use their HDR rendering paths. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| HDR monitor, gaming/movies focus | On |
| HDR monitor, SDR-only content | Off (Windows SDR-in-HDR is often visually worse than native SDR) |
| SDR-only monitor | Doesn't matter; the toggle will be ignored |
| Multi-monitor with mixed HDR support | On for HDR displays only; per-display managed |

**Risks.** Some games look wrong in HDR (washed out, oversaturated) due to game-side tone mapping bugs -- a per-game preference. Windows SDR-in-HDR rendering is often visually worse than native SDR for desktop work.

**Command line (PowerShell):**

```powershell
# Reverse it (restore the Windows default)
Settings > System > Display > select the display > toggle HDR off.
```

**Reversible via.** Settings > System > Display > select the display > toggle HDR off.


### Memory Integrity / VBS (Core Isolation)

`memintegrity` &nbsp; **Recommended:** On (default)

**Why this is the recommendation.** Real security feature -- meaningfully reduces certain malware classes' ability to load kernel drivers. But the hypervisor's transitions cost CPU on every kernel call, which shows up as worse 1% lows in many games.

**What it does.** Hypervisor-Enforced Code Integrity. Runs the Windows kernel inside a Hyper-V-protected memory region so unsigned or compromised kernel drivers can't write to protected code. Part of the broader Virtualization-Based Security stack.

**How it helps.** Disabling can recover 5-15% framerate in CPU-bound games (especially 1% lows). On Ryzen, the win can be larger. Tradeoff is security: think hard before flipping this.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| On (recommended) | Keeps kernel-driver tamper protection on, and is required by some anti-cheat (Riot Vanguard). | Hypervisor transitions cost CPU -- typically 5-15% worse 1% lows in CPU-bound games. |
| Off | Recovers 5-15% framerate (especially 1% lows) in CPU-bound games. | Weakens kernel-driver malware protection and breaks games whose anti-cheat requires it (Valorant); needs a reboot. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS where every percent matters | Off (accept the security tradeoff knowingly) |
| Casual / mixed-use | On -- security beats the framerate |
| Productivity | On |
| Streaming + game | On -- the difference under stream encoding load is minor |
| Anti-cheat-protected games | On -- Vanguard, BattlEye, EAC may refuse to launch with it off |

**Risks.** Major: reduced kernel-driver protection. Some anti-cheat (Riot Vanguard especially) requires it on. Some kernel-mode hardware (cheap KVMs, old drivers) won't load with it on -- that's the tradeoff in the other direction.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled).Enabled

# Apply the gaming-optimized value
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled -Value 1 -Type DWord   # re-enable Memory Integrity; reboot
```

**Reversible via.** Set HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity\Enabled = 1 and reboot.


### Mouse "Enhance pointer precision"

`mouseaccel` &nbsp; **Recommended:** Off

**Why this is the recommendation.** Breaks 1:1 muscle memory between mouse and cursor. Every competitive FPS disables acceleration in-game; mismatching the OS-level setting means your desktop pointer behaves differently from your in-game crosshair.

**What it does.** A cursor acceleration curve applied to all mouse movement. Moving the mouse faster makes the cursor travel disproportionately further than the same distance moved slowly.

**How it helps.** Consistent 1:1 mouse-to-cursor mapping. Aim feels the same in-game and out of game. Easier to dial in pointer speed by DPI alone.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | 1:1 mouse-to-cursor movement that matches every competitive FPS's in-game feel. | The cursor feels slower at low DPI until you bump pointer speed or DPI. |
| On (default) | Acceleration helps cover large/high-res desktops with small movements. | Breaks 1:1 aim consistency between desktop and in-game. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Off |
| Casual gaming | Off (just because consistency helps) |
| Productivity / office work | Default On is fine; acceleration helps with quick navigation across large displays |
| Touchscreen / pen / tablet primary | Doesn't matter |

**Risks.** Cursor feels 'slower' at low DPI when you first turn it off. Counter: bump your Mouse pointer speed slider or your DPI.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Control Panel\Mouse' -Name MouseSpeed).MouseSpeed

# Apply the gaming-optimized value
# Enhance pointer precision OFF (per-user). Use SystemParametersInfo SPI_SETMOUSE in a small EXE; PowerShell can't call it cleanly.

# Reverse it (restore the Windows default)
# Re-check 'Enhance pointer precision' in Settings > Bluetooth & devices > Mouse > Additional mouse settings > Pointer Options (the OS uses SystemParametersInfo SPI_SETMOUSE, which PowerShell can't call cleanly).
```

**Reversible via.** Settings > Mouse > Additional mouse settings > Pointer Options > re-check 'Enhance pointer precision'.


### Network Throttling

`netthrottle` &nbsp; **Recommended:** Disabled (FFFFFFFF)

**Why this is the recommendation.** For online games, this throttling can introduce micro-stutter in netcode. Removing it lets netcode run at full rate.

**What it does.** Rate-limits outbound network packets during multimedia tasks to prevent network I/O from starving them. Default value 10 = throttled. FFFFFFFF (4294967295) = disabled.

**How it helps.** Smoother online experience in competitive games. Removes a known source of input-to-server latency variability.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Removes packet pacing that can add micro-stutter to online-game netcode. | Practically none -- in theory multimedia apps could see slightly less reliable timing on a saturated network. |
| Default (on) | Windows' shipped pacing protects multimedia playback under heavy network load. | Can introduce small, inconsistent latency in competitive online games. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive online (CS2, Valorant, Apex, etc.) | Disabled |
| Casual online | Disabled |
| Single-player offline | Doesn't matter |
| Streaming | Disabled (your encoder paces itself) |

**Risks.** Very low. In theory multimedia apps could see slightly less reliable timing if your network is saturated -- in practice not observable.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex).NetworkThrottlingIndex

# Apply the gaming-optimized value
Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex -Value 4294967295 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex -Value 10 -Type DWord   # restore the Windows default
```

**Reversible via.** Set HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\NetworkThrottlingIndex = 10.


### Power Throttling

`powerthrottling` &nbsp; **Recommended:** Disabled (gaming) on a desktop; Default on battery

**Why this is the recommendation.** On a desktop chasing sustained performance, throttling can clip background/helper threads a game relies on. Turning it off keeps all threads at full clock.

**What it does.** Windows Power Throttling reduces the clock/power of threads it considers background or idle to save energy. Disabled via HKLM\...\Power\PowerThrottling\PowerThrottlingOff=1 (requires elevation). Absence means the Windows default (throttling on). This is a registry setting, not a power-scheme change.

**How it helps.** More consistent performance for multi-threaded games and background helpers; no surprise downclocking under the OS's idle heuristics.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (gaming, recommended on desktop) | All threads stay at full clock -- no surprise downclocking of helper threads a game relies on. | Higher power/heat; on a laptop on battery it costs real runtime. |
| Default | Throttling saves real battery by clocking down idle/background threads. | Can clip background/helper threads a game uses, hurting consistency on a desktop. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Desktop / plugged-in gaming | Disabled (gaming) |
| Laptop on battery | Default -- throttling saves real battery |
| Streaming + game | Disabled (gaming) |

**Risks.** Higher power draw and heat, especially on laptops on battery. No stability risk.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling' -Name PowerThrottlingOff -EA SilentlyContinue).PowerThrottlingOff

# Apply the gaming-optimized value
$k='HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling'; New-Item $k -Force | Out-Null; Set-ItemProperty $k -Name PowerThrottlingOff -Value 1 -Type DWord   # reverse: Remove-ItemProperty $k -Name PowerThrottlingOff

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling' -Name PowerThrottlingOff -EA SilentlyContinue   # restore the Windows default
```

**Reversible via.** Delete PowerThrottlingOff from HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling to restore the Windows default.


### System Responsiveness

`sysresponse` &nbsp; **Recommended:** 10

**Why this is the recommendation.** Drops the reservation from 20% to 10% so games tagged as multimedia get more CPU during contention.

**What it does.** Registry knob that reserves a percentage of CPU time for non-multimedia tasks. Default value 20 = 20% reserved. Lower = more CPU available for multimedia tasks (which includes games registered via MMCSS).

**How it helps.** Tiny but measurable improvement on CPU-bound games. Most useful on lower-core-count CPUs where 20% is a lot of reserved time.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Gaming, 10 (recommended) | Frees ~10% more CPU for games tagged as multimedia, helping low-core-count CPUs most. | Effect is small, and the value only takes effect after a reboot. |
| Default, 20 | Windows' shipped balance; guaranteed headroom for background tasks. | Reserves 20% of CPU time away from games during contention. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | 10 (gaming) |
| Pro audio | 0 (audio guides usually recommend 0; gives the audio scheduler full priority) |
| Casual gaming | 10 or default |
| Productivity | 20 (default) |

**Risks.** Very low at 10. At 0, rare audio glitches under sustained CPU load. Reboot is required for the value to take effect.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness).SystemResponsiveness

# Apply the gaming-optimized value
Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness -Value 10 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness -Value 20 -Type DWord   # restore the Windows default; reboot
```

**Reversible via.** Set HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\SystemResponsiveness = 20.


### USB Selective Suspend (global)

`usbsuspend` &nbsp; **Recommended:** Disabled (for desktops)

**Why this is the recommendation.** For HID devices (gaming mice, keyboards, headsets), the wake-from-suspend introduces a noticeable first-input delay -- the cursor pauses for a moment, the first keystroke after a long idle is dropped, or a USB headset pops.

**What it does.** Windows power feature that suspends idle USB devices to save power. The device wakes when Windows touches it again. Applies per-device but flipping this global flag disables the default-suspend behavior.

**How it helps.** Eliminates the first-input lag on cold mouse/keyboard input. Removes random audio pops on cheap USB DACs/headsets that are sensitive to suspend cycles.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended for desktops) | Kills first-input lag and random USB audio pops by never suspending idle mice/keyboards/headsets. | Slightly higher idle power (1-3 W) -- measurably worse battery on a laptop. Reboot to apply. |
| Enabled (default) | Saves power by letting Windows sleep idle USB devices -- the right call on battery. | First mouse move or keypress after idle can drop or stutter; cheap USB DACs may pop. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Desktop gaming PC | Disabled |
| Laptop on battery | Enabled -- the power saving matters more than first-input lag |
| Laptop plugged in / docked | Disabled |
| USB audio interface (streaming / recording) | Disabled |

**Risks.** Slightly higher idle power draw (typically 1-3 W). Negligible heat. On laptops, observably faster battery drain.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend).DisableSelectiveSuspend

# Apply the gaming-optimized value
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend -Value 1 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend -Value 0 -Type DWord   # restore Windows-managed USB suspend; reboot
```

**Reversible via.** Set HKLM\SYSTEM\CurrentControlSet\Services\USB\DisableSelectiveSuspend = 0 and reboot.


### Variable Refresh Rate (DirectX)

`vrr` &nbsp; **Recommended:** On if you have VRR hardware

**Why this is the recommendation.** Allows VRR (G-Sync / FreeSync) to work in games that don't have a VRR / G-Sync toggle of their own.

**What it does.** Windows Settings > Display > Graphics > Variable Refresh Rate. Tells Windows to expose VRR to DirectX games even when the game doesn't explicitly request it. NOT the same as Dynamic Refresh Rate (DRR) in Advanced Display, which scales refresh based on content.

**How it helps.** Smooth frame delivery between the display's min and max refresh -- no tearing, no V-Sync input latency.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| On (recommended if you have VRR hardware) | Smooth, tear-free frame delivery (G-Sync/FreeSync) even in games without their own VRR toggle. | A few old driver+game combos can flicker -- fixable by turning off in-game V-Sync. |
| Off | Avoids the rare VRR flicker on problem displays. | Games without a VRR toggle won't get variable refresh, so you're back to tearing or V-Sync latency. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| VRR-capable display + supported GPU | On |
| Display without VRR | Doesn't matter -- no-op |
| Multi-monitor with one VRR display | On (Windows handles per-monitor) |
| Competitive FPS with V-Sync off as standard | On (still benefits from VRR-paced delivery up to the FPS cap) |

**Risks.** Very low. Some older driver+game combos can flicker -- if you see it, turn off in-game V-Sync, leave VRR on.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name VRROptimizeEnable).VRROptimizeEnable

# Apply the gaming-optimized value
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name VRROptimizeEnable -Value 1 -Type DWord

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name VRROptimizeEnable -EA SilentlyContinue   # restore the Windows default
```

**Reversible via.** Delete VRROptimizeEnable from HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers.


### Virtualization-Based Security (full stack)

`vbs` &nbsp; **Recommended:** On (default) -- only disable if you understand the tradeoff

**Why this is the recommendation.** Every VBS service pays the hypervisor transition cost on kernel calls. Disabling only Memory Integrity recovers most of it, but Credential Guard (default-on for domain-joined 22H2+ Enterprise/Education machines and Pro machines that previously ran it) and the other scenarios keep the hypervisor resident and keep re-enabling paths open. This is the 'I want it actually, durably off' switch.

**What it does.** The complete VBS disable -- a superset of the Memory Integrity toggle. VBS runs a Hyper-V micro-hypervisor under Windows to host security services: Memory Integrity (HVCI), Credential Guard, System Guard Secure Launch, kernel-mode Hardware-enforced Stack Protection, and (on 24H2+, community-reported rather than formally documented) a Windows Hello sign-in scenario. Disabling only Memory Integrity leaves VBS itself running if any other scenario is active. This toggle writes an explicit 0 to the DeviceGuard master switch, EVERY scenario subkey (including ones future Windows versions add), Credential Guard's LsaCfgFlags, and the Group Policy mirror keys -- explicit zeros, not deletions, because Microsoft documents that absent values get re-defaulted by feature updates while explicit zeros survive them. It also deletes the per-scenario re-enable metadata (WasEnabledBy / EnabledBootId / ChangedInBootCycle) wherever present -- the values Windows uses to restore HVCI after upgrades.

**How it helps.** 5-15% better framerate and 1% lows in CPU-bound games (Tom's Hardware: up to 10% average, up to 15% better 1% lows on a 13900K + RTX 4090; ~5% on post-2018 CPUs with MBEC, more on older CPUs). Registry-only: WSL2, Docker and Hyper-V keep working -- the hypervisor itself is untouched (the optional bcdedit hypervisorlaunchtype step that breaks them is deliberately NOT automated; see Risks).

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| On (recommended) | Keeps the full security stack (HVCI, Credential Guard, boot protection) and Valorant/Vanguard working. | Every VBS service keeps paying the hypervisor cost on kernel calls. |
| Off | Durably recovers 5-15% framerate/1% lows by zeroing every VBS scenario so updates can't silently re-enable them. | Disables kernel-driver, credential, and boot-time protections at once and breaks Valorant; reboot required, and a UEFI lock can keep it on. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS where every percent matters | Off -- accept the security tradeoff knowingly |
| Valorant / Riot Vanguard players | On -- Vanguard REQUIRES Memory Integrity since July 2024; this toggle breaks Valorant |
| Casual / mixed-use | On -- security beats the framerate |
| Work PC under corporate management (Intune/GPO) | On -- domain policy will fight the change; the drift monitor will show the tug-of-war |
| Dedicated gaming rig, no sensitive credentials | Off is a defensible choice |

**Risks.** Major: disables kernel-driver tamper protection, credential isolation (pass-the-hash defenses) and boot-time firmware protection in one move. Breaks Valorant (Vanguard requires HVCI). Windows Security shows Memory Integrity greyed out ('managed by your administrator') while disabled -- that's this app's policy keys closing the re-enable loophole; flipping the toggle back to Enabled removes them (do that BEFORE uninstalling GamerGuardian, or the grey-out persists until you delete the SOFTWARE\Policies\Microsoft\Windows\DeviceGuard values yourself). If VBS was enabled with UEFI lock (Locked=1 / LsaCfgFlags=1), firmware keeps VBS running after these writes: clearing it needs Microsoft's SecConfig.efi opt-out (mountvol the EFI partition, bcdedit a boot entry with 'loadoptions DISABLE-LSA-ISO' -- older DG_Readiness_Tool releases also passed DISABLE-VBS -- then reboot and confirm at the physical-presence prompt) -- the app detects and reports the lock but will not automate firmware surgery. Going further with 'bcdedit /set hypervisorlaunchtype off' is optional and NOT done by the app: it breaks WSL2, Docker, Windows Sandbox, Hyper-V and Windows Hello ESS in exchange for a further, smaller gain on top of the zeroed scenarios. Note: msinfo32 saying 'a hypervisor has been detected' does NOT mean VBS is on -- verify with the WMI command in the verify snippet (VirtualizationBasedSecurityStatus 0 = off).

**Command line (PowerShell):**

```powershell
# Check the current value
# Run elevated. Configured state (registry):
$dg='HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'; @{EVBS=(Get-ItemProperty $dg -Name EnableVirtualizationBasedSecurity -EA SilentlyContinue).EnableVirtualizationBasedSecurity; RPSF=(Get-ItemProperty $dg -Name RequirePlatformSecurityFeatures -EA SilentlyContinue).RequirePlatformSecurityFeatures; Mandatory=(Get-ItemProperty $dg -Name Mandatory -EA SilentlyContinue).Mandatory; LsaCfg=(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa' -Name LsaCfgFlags -EA SilentlyContinue).LsaCfgFlags; Policy=(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard' -EA SilentlyContinue | Select-Object EnableVirtualizationBasedSecurity,LsaCfgFlags,HypervisorEnforcedCodeIntegrity | Out-String).Trim(); Scenarios=(Get-ChildItem "$dg\Scenarios" -EA SilentlyContinue | ForEach-Object { "$($_.PSChildName)=$((Get-ItemProperty $_.PSPath -Name Enabled -EA SilentlyContinue).Enabled)" }) -join ', '}
# Runtime truth (0 = off/not enabled, 1 = configured but not running, 2 = running; changes only after a reboot):
(Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard).VirtualizationBasedSecurityStatus

# Apply the gaming-optimized value
# Disable the full VBS stack (explicit zeros survive feature updates; reboot required):
$dg='HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'; foreach($n in 'EnableVirtualizationBasedSecurity','RequirePlatformSecurityFeatures','Mandatory','HypervisorEnforcedCodeIntegrity'){ Set-ItemProperty $dg -Name $n -Value 0 -Type DWord }; foreach($s in 'HypervisorEnforcedCodeIntegrity','CredentialGuard','SystemGuard','KernelShadowStacks','WindowsHello'){ $k="$dg\Scenarios\$s"; New-Item $k -Force | Out-Null; Set-ItemProperty $k -Name Enabled -Value 0 -Type DWord; foreach($m in 'WasEnabledBy','EnabledBootId','ChangedInBootCycle'){ Remove-ItemProperty $k -Name $m -EA SilentlyContinue } }; Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa' -Name LsaCfgFlags -Value 0 -Type DWord; $p='HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard'; New-Item $p -Force | Out-Null; foreach($n in 'EnableVirtualizationBasedSecurity','LsaCfgFlags','HypervisorEnforcedCodeIntegrity'){ Set-ItemProperty $p -Name $n -Value 0 -Type DWord }

# Reverse it (restore the Windows default)
# Restore VBS to Windows defaults (removes only the explicit-disable zeros; reboot required):
$dg='HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'; Set-ItemProperty $dg -Name EnableVirtualizationBasedSecurity -Value 1 -Type DWord; foreach($n in 'RequirePlatformSecurityFeatures','Mandatory','HypervisorEnforcedCodeIntegrity'){ Remove-ItemProperty $dg -Name $n -EA SilentlyContinue }; $k="$dg\Scenarios\HypervisorEnforcedCodeIntegrity"; New-Item $k -Force | Out-Null; Set-ItemProperty $k -Name Enabled -Value 1 -Type DWord; Set-ItemProperty $k -Name WasEnabledBy -Value 2 -Type DWord; foreach($s in 'CredentialGuard','SystemGuard','KernelShadowStacks','WindowsHello'){ Remove-ItemProperty "$dg\Scenarios\$s" -Name Enabled -EA SilentlyContinue }; Remove-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa' -Name LsaCfgFlags -EA SilentlyContinue; $p='HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard'; foreach($n in 'EnableVirtualizationBasedSecurity','LsaCfgFlags','HypervisorEnforcedCodeIntegrity'){ Remove-ItemProperty $p -Name $n -EA SilentlyContinue }
```

**Reversible via.** Flip the toggle back to Enabled: the app sets EnableVirtualizationBasedSecurity = 1, restores Scenarios\HypervisorEnforcedCodeIntegrity Enabled = 1 + WasEnabledBy = 2 (un-greys the Windows Security toggle), and deletes the policy-mirror zeros and LsaCfgFlags = 0 so Windows defaults take over again. Reboot required.


### Visual effects (best performance)

`visualfx` &nbsp; **Recommended:** Best performance (gaming) for a snappy desktop; Default if you like the animations

**Why this is the recommendation.** Disabling desktop animations removes compositor work and makes window/menu interactions instant. The gain is mostly desktop snappiness rather than in-game FPS, but some users prefer the zero-animation feel.

**What it does.** The Windows UI animation/effects profile. 'Adjust for best performance' (VisualFXSetting=2) disables window animations, menu fades, smooth-scrolling, and shadows. GamerGuardian writes VisualFXSetting=2 plus the matching best-performance UserPreferencesMask; the per-effect changes finish applying on the next sign-out.

**How it helps.** Instant window/menu response, no animation delays, slightly less idle GPU compositor work.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Best performance (gaming) | Instant window/menu response and a touch less idle GPU compositor work. | Purely cosmetic loss -- the desktop looks flatter; full effect needs a sign-out. |
| Default | Keeps the Fluent animations and fades many people prefer. | Animations add a small delay to every window/menu interaction. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Wants the snappiest desktop | Best performance (gaming) |
| Likes Windows animations / fluent effects | Default |
| Low-end / integrated GPU | Best performance (gaming) |

**Risks.** Purely cosmetic -- the desktop looks flatter (no fades/animations). No stability or functionality impact. Full effect applies after sign-out.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects' -Name VisualFXSetting -EA SilentlyContinue).VisualFXSetting

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects' -Name VisualFXSetting -Value 2 -Type DWord; Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name UserPreferencesMask -Value ([byte[]](0x90,0x12,0x03,0x80,0x10,0,0,0)) -Type Binary   # reverse: VisualFXSetting=0; sign out to fully apply

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects' -Name VisualFXSetting -Value 0 -Type DWord   # 'Let Windows choose'; sign out to fully apply
```

**Reversible via.** Set VisualFXSetting = 0 in HKCU\...\Explorer\VisualEffects (System Properties > Performance > 'Let Windows choose' or 'Adjust for best appearance'). GamerGuardian sets it to 0 when you choose Default.


### Windows Game Mode

`gamemode` &nbsp; **Recommended:** On (Windows default)

**Why this is the recommendation.** Game Mode is essentially free on modern Windows -- it's been the default since 1809. The only reason to think about it is if a specific game shows stuttering that goes away when Game Mode is off (rare, but documented for some GPU+driver combos).

**What it does.** Windows 10/11 feature that tells the OS to prioritize the foreground app when it detects a game: CPU/GPU resources are biased toward the game, Windows Update reboots are deferred during gameplay, and background app push notifications are paused.

**How it helps.** Small but measurable input-latency reduction on systems with background work happening. Suppresses Windows Update mid-game reboots.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| On (recommended) | Slightly lower input latency and no Windows Update reboots mid-game, at zero cost. | On a few GPU/driver/game combos it can cause stutter or capture glitches. |
| Off | Rules Game Mode out as the cause if you're chasing a specific stutter. | You give up the small latency win and the mid-game update-reboot suppression. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | On -- no measurable downside; consistent frame pacing |
| Streaming + game | On -- but verify your encoder isn't being deprioritized (rare) |
| Casual single-player | On |
| Productivity / not gaming | Doesn't matter; Windows ignores Game Mode for non-game foreground apps |

**Risks.** Some users report frame-rate stuttering or capture glitches on specific GPU/driver/game combos. If you only see stuttering with Game Mode on, turn it off and re-test.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled).AutoGameModeEnabled

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled -Value 1 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled -Value 0 -Type DWord   # turn Game Mode off
```

**Reversible via.** Set HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled = 1 (or delete the value).

## Privacy

### Activity History / Timeline

`privacy.activityhistory` &nbsp; **Recommended:** Disabled (gaming)

**Why this is the recommendation.** Activity History records what you do across apps and (when signed in) uploads it. Most gamers don't use Timeline, and Windows can re-enable the feed after feature updates.

**What it does.** Collection and publishing of your activity feed (Timeline). Disabled via three HKLM policy values set to 0 together: EnableActivityFeed, PublishUserActivities, UploadUserActivities (requires elevation, one prompt). Absence means the Windows default (on).

**How it helps.** Stops the activity feed from collecting and publishing. Reasserted automatically after updates that turn it back on.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (gaming, recommended) | Stops the activity feed collecting and uploading what you do, reasserted after updates. | Timeline and cross-device activity resume stop working. |
| Default (on) | Timeline shows recent activities and can resume them across devices. | Records -- and, when signed in, uploads -- your cross-app activity. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Doesn't use Timeline | Disabled (gaming) |
| Uses Timeline / cross-device activity resume | Default (leave on) |
| Privacy-conscious | Disabled (gaming) |

**Risks.** Timeline stops showing your recent activities and cross-device resume won't work. No effect on app/game functionality.

**Command line (PowerShell):**

```powershell
# Check the current value
$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; @{Feed=(Get-ItemProperty $k -Name EnableActivityFeed -EA SilentlyContinue).EnableActivityFeed; Publish=(Get-ItemProperty $k -Name PublishUserActivities -EA SilentlyContinue).PublishUserActivities; Upload=(Get-ItemProperty $k -Name UploadUserActivities -EA SilentlyContinue).UploadUserActivities}

# Apply the gaming-optimized value
$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; foreach($n in 'EnableActivityFeed','PublishUserActivities','UploadUserActivities'){ Set-ItemProperty $k -Name $n -Value 0 -Type DWord }   # reverse: Remove-ItemProperty for each

# Reverse it (restore the Windows default)
$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; foreach($n in 'EnableActivityFeed','PublishUserActivities','UploadUserActivities'){ Remove-ItemProperty $k -Name $n -EA SilentlyContinue }   # restore the Windows default
```

**Reversible via.** Delete EnableActivityFeed, PublishUserActivities, and UploadUserActivities from HKLM\SOFTWARE\Policies\Microsoft\Windows\System to restore the Windows default.


### Advertising ID

`privacy.advertisingid` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** There's no gaming or functionality reason to keep the advertising ID on. Disabling it stops apps from correlating your activity under a stable ad identity.

**What it does.** A per-user identifier (HKCU\...\AdvertisingInfo\Enabled) that apps can read to build a cross-session advertising profile of you. Direct HKCU value -- no elevation needed.

**How it helps.** Apps fall back to requesting a fresh, non-correlatable ID (or none). No effect on app functionality.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Apps can't build a stable cross-session ad profile of you, with no functional downside. | Ads you see may be less 'relevant' (which is the point). |
| Enabled (default) | Personalized ads across apps. | Gives apps a persistent identifier to correlate your activity. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious | Disabled |
| Gaming setup | Disabled -- no downside |
| Doesn't care about ad targeting | Either; Disabled is the safe default |

**Risks.** None functional. Ads you see may be slightly less 'relevant' -- which is the point.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo' -Name Enabled -EA SilentlyContinue).Enabled

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo' -Name Enabled -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo' -Name Enabled -Value 1 -Type DWord   # re-enable the advertising ID
```

**Reversible via.** Set HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo\Enabled = 1 (Settings > Privacy & security > General > 'Let apps show me personalized ads').


### Cross-Device Platform (CDP)

`privacy.cdp` &nbsp; **Recommended:** Disabled (gaming) if you don't use cross-device features

**Why this is the recommendation.** CDP runs background discovery/sync that most desktop gamers don't use, and Windows re-enables it after feature updates -- exactly the drift the monitor re-asserts.

**What it does.** The 'Continue experiences on this device' / shared-experiences subsystem that lets nearby and account-linked devices hand off activities, share the clipboard, and discover each other. Disabled via the HKLM policy EnableCdp=0 (requires elevation). Absence of the value means the Windows default (CDP on).

**How it helps.** Stops the cross-device discovery/sync background activity. Reasserted automatically if a feature update turns it back on.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (gaming, recommended if unused) | Stops cross-device discovery/sync background activity, reasserted after updates. | Handoff, shared clipboard, and nearby-device discovery stop working (Phone Link integrations may be affected). |
| Default (on) | Cross-device handoff and shared clipboard work. | Background discovery/sync runs even if you never use those features. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Single desktop, no device handoff | Disabled (gaming) |
| Uses Phone Link / cross-device clipboard | Default (leave on) |
| Privacy-conscious | Disabled (gaming) |

**Risks.** Cross-device features (handoff, shared clipboard with phones/other PCs, nearby-device discovery) stop working. Phone Link's deeper integrations may be affected.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name EnableCdp -EA SilentlyContinue).EnableCdp

# Apply the gaming-optimized value
$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; Set-ItemProperty $k -Name EnableCdp -Value 0 -Type DWord   # reverse: Remove-ItemProperty $k -Name EnableCdp

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name EnableCdp -EA SilentlyContinue   # restore the Windows default (CDP on)
```

**Reversible via.** Delete EnableCdp from HKLM\SOFTWARE\Policies\Microsoft\Windows\System to restore the Windows default.


### Inking & typing personalization

`privacy.inking` &nbsp; **Recommended:** Disabled (privacy)

**Why this is the recommendation.** It's a data-collection feature; turning it off stops the harvesting. Autocorrect still works, just less personalized.

**What it does.** Windows building a personal dictionary from your handwriting samples and contact names to improve suggestions -- and uploading some of it. Covers the master AcceptedPrivacyPolicy opt-in plus implicit ink collection and contact harvesting. (The typing-text side is the separate 'Typing / input insights' toggle on the Windows AI tab.)

**How it helps.** Stops handwriting/contact data collection. Minimal day-to-day impact.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Stops Windows harvesting your handwriting samples and contact names. | Handwriting recognition and suggestions become less personalized. |
| Enabled | Better personalized handwriting recognition and word suggestions. | Builds and uploads a personal dictionary from your ink and contacts. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious | Disabled |
| Heavy pen / handwriting user who wants better recognition | Enabled |
| Typical keyboard user | Disabled |

**Risks.** Handwriting recognition and word suggestions become less personalized. No functional breakage.

**Command line (PowerShell):**

```powershell
# Check the current value
@{Accepted=(Get-ItemProperty 'HKCU:\Software\Microsoft\Personalization\Settings' -Name AcceptedPrivacyPolicy -EA SilentlyContinue).AcceptedPrivacyPolicy; Ink=(Get-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization' -Name RestrictImplicitInkCollection -EA SilentlyContinue).RestrictImplicitInkCollection}

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Personalization\Settings' -Name AcceptedPrivacyPolicy -Value 0 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization' -Name RestrictImplicitInkCollection -Value 1 -Type DWord; $t='HKCU:\Software\Microsoft\InputPersonalization\TrainedDataStore'; New-Item $t -Force | Out-Null; Set-ItemProperty $t -Name HarvestContacts -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\Personalization\Settings' -Name AcceptedPrivacyPolicy -Value 1 -Type DWord; Remove-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization' -Name RestrictImplicitInkCollection -EA SilentlyContinue; Remove-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization\TrainedDataStore' -Name HarvestContacts -EA SilentlyContinue   # re-enable inking & typing personalization
```

**Reversible via.** Settings > Privacy & security > Inking & typing personalization (or set AcceptedPrivacyPolicy = 1).


### Online (cloud) speech recognition

`privacy.speech` &nbsp; **Recommended:** Disabled (privacy)

**Why this is the recommendation.** It's a privacy trade-off: your audio leaves the machine. Offline recognition / Voice Access keeps working without it.

**What it does.** When enabled, Windows sends your voice audio to Microsoft's cloud for recognition (used by some dictation and voice features). Controlled by the per-user HasAccepted flag.

**How it helps.** Keeps voice audio on-device. No functional loss for offline voice typing and Voice Access.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Your voice audio never leaves the machine; offline speech and Voice Access still work. | Cloud-powered dictation loses accuracy or stops working. |
| Enabled | Most accurate cloud dictation and voice features. | Sends your voice audio to Microsoft's cloud. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious / don't use voice | Disabled |
| Use cloud dictation heavily | Enabled |
| Mixed use | Disabled -- offline recognition still works |

**Risks.** Cloud-powered voice features lose accuracy or stop working. Offline Windows speech / Voice Access is unaffected.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy' -Name HasAccepted -EA SilentlyContinue).HasAccepted

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy' -Name HasAccepted -Value 0 -Type DWord   # reverse: set to 1

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy' -Name HasAccepted -Value 1 -Type DWord   # re-enable cloud speech recognition
```

**Reversible via.** Settings > Privacy & security > Speech > Online speech recognition (or set HasAccepted = 1).


### Tailored experiences

`privacy.tailoredexp` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** Removes Microsoft's use of your diagnostic data to target suggestions and promotional content in the Start menu, Settings, and lock screen.

**What it does.** Lets Windows use your diagnostic data to personalize tips, ads, and recommendations (HKCU\...\Privacy\TailoredExperiencesWithDiagnosticDataEnabled). Direct HKCU value -- no elevation.

**How it helps.** Fewer suggested/promoted items surfaced by the OS. No effect on app or game functionality.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Windows stops mining your diagnostic data for targeted tips and promos. | You stop seeing personalized Windows tips and suggestions. |
| Enabled (default) | Personalized tips and recommendations in Start/Settings/lock screen. | Uses your diagnostic data to target suggestions and promotional content. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious | Disabled |
| Gaming setup | Disabled -- no downside |
| Likes Windows tips/suggestions | Enabled |

**Risks.** None functional. You stop seeing personalized Windows tips and suggestions.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy' -Name TailoredExperiencesWithDiagnosticDataEnabled -EA SilentlyContinue).TailoredExperiencesWithDiagnosticDataEnabled

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy' -Name TailoredExperiencesWithDiagnosticDataEnabled -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy' -Name TailoredExperiencesWithDiagnosticDataEnabled -Value 1 -Type DWord   # re-enable tailored experiences
```

**Reversible via.** Set HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy\TailoredExperiencesWithDiagnosticDataEnabled = 1 (Settings > Privacy & security > Diagnostics & feedback).

## Debloat (ads, nags & background bloat)

### "Finish setting up your device" nag

`debloat.finishsetup` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** It's a recurring nag screen, not a feature. Most users have already decided and don't want to be asked again.

**What it does.** The full-screen / notification SCOOBE prompts that nag you to set up OneDrive, a Microsoft account, or a Microsoft 365 subscription -- and resurface after feature updates. Controlled by UserProfileEngagement + a ContentDeliveryManager notification flag.

**How it helps.** Suppresses the post-update 'finish setup' interruption.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | No more post-update 'finish setting up your device' interruption. | You won't be prompted to finish optional account/OneDrive setup. |
| Enabled (default) | Windows reminds you to finish optional setup steps. | A recurring full-screen nag that resurfaces after feature updates. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Annoyed by the setup nag | Disabled |
| Want Windows' setup reminders | Enabled |
| Managed/clean setup | Disabled |

**Risks.** You won't be prompted to finish optional account/OneDrive setup. Some newer build variants add prompt types this doesn't fully cover.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement' -Name ScoobeSystemSettingEnabled -EA SilentlyContinue).ScoobeSystemSettingEnabled

# Apply the gaming-optimized value
$e='HKCU:\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement'; New-Item $e -Force | Out-Null; Set-ItemProperty $e -Name ScoobeSystemSettingEnabled -Value 0 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager' -Name SubscribedContent-310093Enabled -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement' -Name ScoobeSystemSettingEnabled -Value 1 -Type DWord; Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager' -Name SubscribedContent-310093Enabled -EA SilentlyContinue   # restore the finish-setup prompt
```

**Reversible via.** Settings > System > Notifications > 'Suggest ways to get the most out of Windows' (or set ScoobeSystemSettingEnabled = 1).


### Edge startup boost & background mode

`debloat.edge` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** On a machine where Edge isn't the daily browser, these keep 150-500 MB of Edge resident for no benefit.

**What it does.** Two Microsoft Edge behaviors: 'startup boost' keeps Edge processes resident from boot, and 'background mode' keeps it running after every window is closed. Set via HKLM Edge enterprise policies that survive Edge updates.

**How it helps.** Edge stops pre-launching at boot and exits when you close it, freeing idle RAM/CPU. Edge still opens on demand.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Edge stops pre-launching at boot and exits when closed, freeing 150-500 MB of idle RAM. | Edge cold-starts a little slower (WebView2 apps are unaffected; one UAC prompt). |
| Enabled (default) | Edge launches instantly and stays warm in the background. | Keeps Edge resident from boot for no benefit if it isn't your daily browser. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Edge isn't your main browser | Disabled |
| Edge is your daily driver and you want fast launches | Enabled |
| Minimal background processes | Disabled |

**Risks.** Edge cold-starts a little slower (no prelaunch). Does NOT block Edge or WebView2 -- apps that embed WebView2 keep working. Policy write needs one UAC prompt.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Edge' | Select-Object StartupBoostEnabled,BackgroundModeEnabled

# Apply the gaming-optimized value
$p='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; New-Item $p -Force | Out-Null; foreach($n in 'StartupBoostEnabled','BackgroundModeEnabled'){ Set-ItemProperty $p -Name $n -Value 0 -Type DWord }   # reverse: Remove-ItemProperty each

# Reverse it (restore the Windows default)
$p='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; foreach($n in 'StartupBoostEnabled','BackgroundModeEnabled'){ Remove-ItemProperty $p -Name $n -EA SilentlyContinue }   # restore Edge startup boost & background mode
```

**Reversible via.** Edge > Settings > System and performance (Startup boost / 'Continue running background extensions'), or delete the two Edge policy values.


### File Explorer ad banners

`debloat.explorerads` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** They're advertising inside the file manager. Disabling them is purely cosmetic with no downside.

**What it does.** The 'sync provider notifications' in File Explorer -- the OneDrive / Microsoft 365 upsell banners shown in the navigation pane and status bar. Single per-user Explorer\Advanced flag.

**How it helps.** Removes the promo banners from File Explorer.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Removes the OneDrive/Microsoft 365 upsell banners from File Explorer. | None -- genuine sync-status icons on files are unaffected. |
| Enabled (default) | Shows OneDrive/Office sync prompts in Explorer. | Advertising inside your file manager. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Dislike ads in Explorer | Disabled |
| Want OneDrive sync prompts | Enabled |

**Risks.** You won't see OneDrive/Office promotional banners. Genuine sync-status icons on files are unaffected.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name ShowSyncProviderNotifications -EA SilentlyContinue).ShowSyncProviderNotifications

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name ShowSyncProviderNotifications -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name ShowSyncProviderNotifications -Value 1 -Type DWord   # restore Explorer sync banners
```

**Reversible via.** File Explorer > View > Options > View tab > 'Show sync provider notifications' (or set ShowSyncProviderNotifications = 1).


### Lock screen tips, fun facts & ads

`debloat.spotlight` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** Many users find the lock-screen captions and tips intrusive or ad-like.

**What it does.** The Windows Spotlight overlay that shows 'fun facts', tips, and ad-like captions on the lock screen. Controlled by per-user ContentDeliveryManager flags.

**How it helps.** Removes the tips/ad overlay from the lock screen.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Removes the tips/fun-facts/ad overlay from the lock screen. | If your lock-screen background is Spotlight, also switch it to Picture/Slideshow for a full opt-out. |
| Enabled (default) | Rotating Spotlight images with fun facts and tips. | Shows ad-like captions and tips on your lock screen. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Dislike lock-screen tips/ads | Disabled |
| Enjoy the Spotlight facts | Enabled |
| Use a custom lock-screen image | Disabled |

**Risks.** Only suppresses the tips/ads overlay. If your lock-screen background is set to 'Windows Spotlight', switch it to Picture/Slideshow in Settings for a full opt-out.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager' | Select-Object RotatingLockScreenOverlayEnabled,'SubscribedContent-338387Enabled'

# Apply the gaming-optimized value
$k='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; foreach($n in 'RotatingLockScreenOverlayEnabled','SubscribedContent-338387Enabled'){ Set-ItemProperty $k -Name $n -Value 0 -Type DWord }

# Reverse it (restore the Windows default)
$k='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; foreach($n in 'RotatingLockScreenOverlayEnabled','SubscribedContent-338387Enabled'){ Remove-ItemProperty $k -Name $n -EA SilentlyContinue }   # restore lock-screen tips
```

**Reversible via.** Settings > Personalization > Lock screen (or delete the ContentDeliveryManager overlay values).


### Start menu recommendations & recent files

`debloat.startrecommend` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** The recommendations are often ads/suggestions, and the recent-files list is a privacy leak on a shared screen.

**What it does.** The Start menu 'Recommended' section: AI/Iris-driven app and web suggestions plus the list of recently opened files. Per-user Explorer\Advanced flags.

**How it helps.** Quiets the Recommended section and stops surfacing recently opened files in Start/jump lists.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Quiets Start 'Recommended' suggestions and hides recently opened files (a privacy win on a shared screen). | Recent files stop appearing in Start/jump lists; on Win11 Home the section can't be fully emptied. |
| Enabled (default) | Quick access to recent files and app/web suggestions in Start. | Suggestions are often ads, and recent files are visible to anyone at your screen. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy on a shared screen | Disabled |
| Rely on recent files in Start | Enabled |
| Minimal Start menu | Disabled |

**Risks.** Recently opened files stop appearing in Start and jump lists. On Windows 11 Home the Recommended section can't be fully emptied -- this removes the suggestions/recents that it can.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' | Select-Object Start_IrisRecommendations,Start_TrackDocs

# Apply the gaming-optimized value
$k='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; foreach($n in 'Start_IrisRecommendations','Start_TrackDocs'){ Set-ItemProperty $k -Name $n -Value 0 -Type DWord }

# Reverse it (restore the Windows default)
$k='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; foreach($n in 'Start_IrisRecommendations','Start_TrackDocs'){ Set-ItemProperty $k -Name $n -Value 1 -Type DWord }   # restore Start recommendations & recent files
```

**Reversible via.** Settings > Personalization > Start (toggles for recommendations and recently opened items), or delete the two Explorer\Advanced values.


### Suggested content & silent app installs

`debloat.suggestedcontent` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** These are ads and unsolicited installs, not features. They cost disk, clutter Start, and re-appear after major updates.

**What it does.** Windows 11's 'suggested content' machinery: silently installed promo apps (the Candy-Crush-style installs), Start-menu app suggestions, and 'tips, tricks & suggestions' cards. All live under the per-user ContentDeliveryManager key.

**How it helps.** Stops silent third-party app installs and removes Start/Settings suggestion cards.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | No silent promo-app installs and no Start/Settings suggestion cards. | You stop seeing Microsoft's app/feature suggestions; a feature update may re-enable some (Auto-apply holds it). |
| Enabled (default) | Microsoft surfaces app and feature suggestions. | Silently installs promo apps and clutters Start with ad-like cards. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who dislikes ads in the OS | Disabled |
| Want Microsoft's app suggestions | Enabled |
| Clean/minimal setup | Disabled |

**Risks.** You stop seeing Microsoft's app/feature suggestions. No functional impact. Windows may re-enable some after a feature update -- tick Auto-apply to hold it.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager' | Select-Object SilentInstalledAppsEnabled,OemPreInstalledAppsEnabled,PreInstalledAppsEnabled,SoftLandingEnabled

# Apply the gaming-optimized value
$k='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; foreach($n in 'SilentInstalledAppsEnabled','OemPreInstalledAppsEnabled','PreInstalledAppsEnabled','SubscribedContent-338388Enabled','SubscribedContent-338389Enabled','SubscribedContent-338393Enabled','SubscribedContent-353694Enabled','SubscribedContent-353696Enabled','SoftLandingEnabled'){ Set-ItemProperty $k -Name $n -Value 0 -Type DWord }   # reverse: Remove-ItemProperty each

# Reverse it (restore the Windows default)
$k='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; foreach($n in 'SilentInstalledAppsEnabled','OemPreInstalledAppsEnabled','PreInstalledAppsEnabled','SubscribedContent-338388Enabled','SubscribedContent-338389Enabled','SubscribedContent-338393Enabled','SubscribedContent-353694Enabled','SubscribedContent-353696Enabled','SoftLandingEnabled'){ Remove-ItemProperty $k -Name $n -EA SilentlyContinue }   # restore suggested content
```

**Reversible via.** Settings > Personalization > Start and > Privacy > General toggles (or delete the ContentDeliveryManager values GamerGuardian set to 0).


### Widgets / News and interests

`debloat.widgets` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** It's a background web feed many users never open; the panel and its updater consume RAM/CPU and bandwidth.

**What it does.** The Windows 11 Widgets board (the left-edge weather button) that opens a web-connected MSN feed and fetches data in the background. Disabled machine-wide via the HKLM Dsh policy plus the per-user taskbar button flag.

**How it helps.** Stops the Widgets process/feed and removes the taskbar button. Frees idle resources.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Stops the Widgets process/MSN feed and removes the taskbar button, freeing idle RAM/CPU/bandwidth. | The Widgets board and its button disappear (one UAC prompt to apply). |
| Enabled (default) | One-click weather/news board on the taskbar. | A background web feed that uses RAM/CPU/bandwidth even if you rarely open it. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Never use Widgets | Disabled |
| Use the weather/news board daily | Enabled |
| Latency-sensitive gaming | Disabled |

**Risks.** The Widgets board and its taskbar button disappear. The machine-wide policy write needs one UAC prompt.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Dsh' -Name AllowNewsAndInterests -EA SilentlyContinue).AllowNewsAndInterests

# Apply the gaming-optimized value
$p='HKLM:\SOFTWARE\Policies\Microsoft\Dsh'; New-Item $p -Force | Out-Null; Set-ItemProperty $p -Name AllowNewsAndInterests -Value 0 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name TaskbarDa -Value 0 -Type DWord   # reverse: Remove-ItemProperty $p -Name AllowNewsAndInterests

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Dsh' -Name AllowNewsAndInterests -EA SilentlyContinue; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name TaskbarDa -Value 1 -Type DWord   # restore Widgets
```

**Reversible via.** Settings > Personalization > Taskbar > Widgets (or delete the Dsh\AllowNewsAndInterests policy value).


### Windows feedback request popups

`debloat.feedback` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** On fresh installs these can fire frequently and interrupt you. Most users never want to be asked.

**What it does.** The periodic 'rate your experience' dialogs Windows pops. Controlled by the per-user Siuf\Rules\NumberOfSIUFInPeriod count (0 = never).

**How it helps.** Stops the periodic feedback-request dialogs.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disabled (recommended) | Windows stops popping 'rate your experience' dialogs (Feedback Hub still opens manually). | None -- your telemetry level is unaffected. |
| Enabled (default) | You can answer Microsoft's periodic feedback prompts. | Interrupting popups, sometimes frequent on fresh installs. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Don't want to be asked for feedback | Disabled |
| Windows Insider who submits feedback | Enabled |

**Risks.** Windows stops prompting for feedback. You can still open Feedback Hub manually any time. Telemetry level is unaffected.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKCU:\Software\Microsoft\Siuf\Rules' -Name NumberOfSIUFInPeriod -EA SilentlyContinue).NumberOfSIUFInPeriod

# Apply the gaming-optimized value
$k='HKCU:\Software\Microsoft\Siuf\Rules'; New-Item $k -Force | Out-Null; Set-ItemProperty $k -Name NumberOfSIUFInPeriod -Value 0 -Type DWord; Remove-ItemProperty $k -Name PeriodInNanoSeconds -EA SilentlyContinue

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKCU:\Software\Microsoft\Siuf\Rules' -Name NumberOfSIUFInPeriod -EA SilentlyContinue   # restore Windows' default feedback cadence
```

**Reversible via.** Settings > Privacy & security > Diagnostics & feedback > Feedback frequency (or delete NumberOfSIUFInPeriod).

## Network

### Nagle's algorithm (TCP no-delay)

`network.nagle` &nbsp; **Recommended:** Default unless you've measured a benefit -- this is a contested, per-hardware tweak

**Why this is the recommendation.** For latency-sensitive online games, batching can add a few ms of delay to small input/state packets. Turning Nagle off can shave that -- but the benefit is genuinely contested and per-hardware.

**What it does.** Nagle's algorithm batches small outgoing TCP packets to reduce overhead. Disabling it (TcpAckFrequency=1, TCPNoDelay=1 under each network adapter's interface key) sends small packets immediately. GamerGuardian asserts this on every active physical adapter in one elevation prompt; reversal deletes the values to restore the Windows default.

**How it helps.** Potentially lower, more consistent latency for small-packet game netcode. On many setups the difference is unmeasurable; on a few it helps.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Default (recommended unless you've measured a gain) | The safe choice -- no risk of making latency or throughput worse. | You might leave a few ms on the table on the rare setup where disabling helps. |
| Disabled | Sends small game packets immediately, which can lower latency on some setups. | Contested -- can increase bufferbloat latency or hurt throughput, especially on Wi-Fi/congested links; revert if worse. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive online shooter | Disabled (gaming) -- try it, measure, revert if worse |
| Stable connection, no latency issues | Default -- don't fix what isn't broken |
| Wi-Fi / high-latency link | Default -- more likely to hurt than help here |

**Risks.** Real: disabling Nagle can INCREASE bufferbloat-related latency or harm throughput on some links (especially Wi-Fi or congested connections). It is not a guaranteed win. Revert if your latency or stability gets worse.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces' | ForEach-Object { [pscustomobject]@{ If=$_.PSChildName; Ack=(Get-ItemProperty $_.PSPath -Name TcpAckFrequency -EA SilentlyContinue).TcpAckFrequency; NoDelay=(Get-ItemProperty $_.PSPath -Name TCPNoDelay -EA SilentlyContinue).TCPNoDelay } }

# Apply the gaming-optimized value
# Per adapter interface key (repeat for each active adapter GUID):
$if='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\<GUID>'; Set-ItemProperty $if -Name TcpAckFrequency -Value 1 -Type DWord; Set-ItemProperty $if -Name TCPNoDelay -Value 1 -Type DWord   # reverse: Remove-ItemProperty both per adapter

# Reverse it (restore the Windows default)
# Per active adapter interface key (repeat for each adapter GUID):
Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces' | ForEach-Object { Remove-ItemProperty $_.PSPath -Name TcpAckFrequency -EA SilentlyContinue; Remove-ItemProperty $_.PSPath -Name TCPNoDelay -EA SilentlyContinue }   # restore Nagle (the Windows default)
```

**Reversible via.** Delete TcpAckFrequency and TCPNoDelay from each HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID} (GamerGuardian does this across all adapters when you choose Default).


### NIC power management

`network.nicpower` &nbsp; **Recommended:** Default -- GamerGuardian treats this as a contested, per-hardware tweak. Desktops on wired Ethernet may gain from Disabled (no NIC wake stalls); test it and keep it only if your latency/hitching improves. Leave Default on a laptop on battery.

**Why this is the recommendation.** Letting Windows power down the NIC can cause brief stalls or micro-disconnects when it wakes -- noticeable as a hitch in online games. Keeping it powered avoids that.

**What it does.** The per-adapter 'Allow the computer to turn off this device to save power' setting (PnPCapabilities under the adapter's network-class instance). Disabling it keeps the NIC fully powered. GamerGuardian asserts this on every active physical adapter in one elevation prompt; reversal clears the bits to restore the default. A reboot (or adapter disable/enable) is needed for it to take effect.

**How it helps.** No NIC sleep/wake cycles, so no wake-from-idle network stalls. Most useful on desktops.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Default (recommended) | No battery cost and no per-hardware guesswork. | On a wired desktop you might miss a small win from preventing NIC wake stalls. |
| Disabled | NIC never sleeps, so no wake-from-idle network hitches (best on wired desktops). | Higher idle power, worse laptop battery, needs a reboot, and many adapters are unaffected either way. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Desktop online gaming | Disabled (gaming) |
| Laptop on battery | Default -- the NIC power saving matters more |
| Stable wired connection with no hitches | Personal taste; Default is fine |

**Risks.** Slightly higher idle power draw. On laptops on battery, measurably worse battery life. Contested per-hardware -- some adapters are unaffected either way. Needs a reboot to apply.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}' | ForEach-Object { $p=(Get-ItemProperty $_.PSPath -Name PnPCapabilities -EA SilentlyContinue).PnPCapabilities; if ($null -ne $p) { [pscustomobject]@{ Key=$_.PSChildName; PnPCapabilities=$p; PowerSaveDisabled=(($p -band 0x18) -eq 0x18) } } }

# Apply the gaming-optimized value
# Per network-class instance (match NetCfgInstanceId to the adapter GUID), reboot after:
$k='HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}\<NNNN>'; $v=[int](Get-ItemProperty $k -Name PnPCapabilities -EA SilentlyContinue).PnPCapabilities; Set-ItemProperty $k -Name PnPCapabilities -Value ($v -bor 0x18) -Type DWord   # reverse: -band (-bnot 0x18)

# Reverse it (restore the Windows default)
# Per network-class instance, then reboot (clears the 0x18 power-save bits):
$k='HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}\<NNNN>'; $v=[int](Get-ItemProperty $k -Name PnPCapabilities -EA SilentlyContinue).PnPCapabilities; Set-ItemProperty $k -Name PnPCapabilities -Value ($v -band (-bnot 0x18)) -Type DWord   # restore Windows-managed NIC power
```

**Reversible via.** Clear the 0x18 bits from PnPCapabilities under the adapter's class instance, or check 'Allow the computer to turn off this device' in Device Manager > the adapter > Power Management (GamerGuardian clears the bits across all adapters when you choose Default).

## Windows AI policies

### Click-to-Do (Snipping Tool AI)

`ai.clicktodo` &nbsp; **Recommended:** Off

**Why this is the recommendation.** AI actions hit Microsoft cloud services. Removes a feature most users don't use anyway.

**What it does.** An AI action layer in the Snipping Tool. After capturing a screenshot, an AI button appears offering 'summarize this,' 'rewrite,' 'search the web for this,' etc. Setting Off writes DisableClickToDo in both the HKLM WindowsAI policy and the per-user HKCU Shell\ClickToDo key.

**How it helps.** Standard Snipping Tool screenshot functionality is completely unaffected; only the AI actions panel is hidden.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Hides the Snipping Tool AI actions panel; normal screenshots are unaffected. | You lose the AI 'summarize/rewrite/search' actions on captures. |
| On (default) | AI actions appear after you take a screenshot. | Those actions call Microsoft cloud services. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who doesn't use Click-to-Do | Off |
| Active Click-to-Do user | On |
| Privacy-conscious | Off |

**Risks.** None. You lose the AI actions panel from screenshots.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name DisableClickToDo -EA SilentlyContinue).DisableClickToDo

# Apply the gaming-optimized value
Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name DisableClickToDo -Value 1 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\Shell\ClickToDo' -Name DisableClickToDo -Value 1 -Type DWord

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name DisableClickToDo -EA SilentlyContinue; Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\Shell\ClickToDo' -Name DisableClickToDo -EA SilentlyContinue   # re-enable Click-to-Do
```

**Reversible via.** Delete DisableClickToDo from HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI and HKCU\Software\Microsoft\Windows\Shell\ClickToDo.


### Microsoft 365 Copilot in Word / Excel / OneNote

`ai.office` &nbsp; **Recommended:** Off

**Why this is the recommendation.** Microsoft 365 Copilot is opt-in by license, but the UI affordances still show up in every Word document; disabling cleanly removes them. The training opt-out is a separate policy that prevents document text from being used to train Microsoft's models even if a user happens to invoke Copilot.

**What it does.** Disables the Copilot button + ribbon entries inside the desktop Word, Excel, and OneNote apps; also opts the machine out of Microsoft's AI model training on document contents (HKLM\Policies\office admin template).

**How it helps.** No Copilot ribbon. No suggestions panel. No accidental cloud calls. No document-text contribution to model training.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Removes the Copilot ribbon/buttons from Word/Excel/OneNote and opts out of training on your document text. | If you have a Copilot license you lose the in-app entry points. |
| On (default) | In-app Copilot in Word/Excel/OneNote (with a license). | Copilot affordances in every document and potential document-text use for training. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Office user who doesn't have a Copilot license | Off -- the buttons are just dead weight |
| Office user with Copilot license, occasional use | On (or selectively per app) |
| Privacy-conscious / regulated workflows | Off |
| Office not installed | Doesn't matter -- the toggle is a no-op |

**Risks.** If you do have a Copilot license and want to use it, you lose the in-app entry points. Reverse by deleting the keys.

**Command line (PowerShell):**

```powershell
# Check the current value
@{Word=(Get-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Word\Options' -Name EnableCopilot -EA SilentlyContinue).EnableCopilot; Excel=(Get-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Excel\Options' -Name EnableCopilot -EA SilentlyContinue).EnableCopilot; OneNote=(Get-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\OneNote\Options\Copilot' -Name CopilotEnabled -EA SilentlyContinue).CopilotEnabled; Training=(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general' -Name disabletraining -EA SilentlyContinue).disabletraining}

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Word\Options' -Name EnableCopilot -Value 0 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Excel\Options' -Name EnableCopilot -Value 0 -Type DWord; $on='HKCU:\Software\Microsoft\Office\16.0\OneNote\Options\Copilot'; New-Item $on -Force | Out-Null; Set-ItemProperty $on -Name CopilotEnabled -Value 0 -Type DWord; $tr='HKLM:\SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general'; New-Item $tr -Force | Out-Null; Set-ItemProperty $tr -Name disabletraining -Value 1 -Type DWord

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Word\Options' -Name EnableCopilot -EA SilentlyContinue; Remove-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\Excel\Options' -Name EnableCopilot -EA SilentlyContinue; Remove-ItemProperty 'HKCU:\Software\Microsoft\Office\16.0\OneNote\Options\Copilot' -Name CopilotEnabled -EA SilentlyContinue; Remove-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general' -Name disabletraining -EA SilentlyContinue   # restore Office Copilot
```

**Reversible via.** Delete EnableCopilot from HKCU\Software\Microsoft\Office\16.0\Word\Options and Excel\Options, CopilotEnabled from HKCU\...\OneNote\Options\Copilot, and disabletraining from HKLM\SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general.


### Microsoft Edge Copilot / Hubs / GenAI

`ai.edge` &nbsp; **Recommended:** Off

**Why this is the recommendation.** Hides the persistent Copilot icon, blocks page contents from leaving the browser for AI processing, and disables in-browser AI generation.

**What it does.** Three Edge enterprise policies flipped together: HubsSidebarEnabled (the always-present right-edge Copilot icon), CopilotPageContext (sending current-page contents to Copilot for processing), and GenAILocalFoundationalModelSettings (Edge's in-browser local generative AI).

**How it helps.** Cleaner Edge UI; less background AI activity in the browser; no accidental page-context shares with cloud AI.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Cleaner Edge UI, no page contents sent to Copilot, and no in-browser AI generation. | You lose Edge's built-in Copilot sidebar and AI features (normal browsing is unaffected). |
| On (default) | Edge Copilot sidebar, page-aware help, and in-browser generative AI. | Persistent Copilot icon and page-context sharing with cloud AI. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious | Off |
| Anyone who doesn't actively use Edge Copilot | Off |
| Active Edge Copilot user | On |
| Enterprise environments | Whatever your IT policy says |

**Risks.** You lose Edge's built-in Copilot sidebar and AI features. Standard browsing is unaffected.

**Command line (PowerShell):**

```powershell
# Check the current value
$k='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; @{Hubs=(Get-ItemProperty $k -Name HubsSidebarEnabled -EA SilentlyContinue).HubsSidebarEnabled; Ctx=(Get-ItemProperty $k -Name CopilotPageContext -EA SilentlyContinue).CopilotPageContext; Gen=(Get-ItemProperty $k -Name GenAILocalFoundationalModelSettings -EA SilentlyContinue).GenAILocalFoundationalModelSettings; Compose=(Get-ItemProperty $k -Name ComposeInlineEnabled -EA SilentlyContinue).ComposeInlineEnabled; Browse=(Get-ItemProperty $k -Name AllowBrowsingWithCopilot -EA SilentlyContinue).AllowBrowsingWithCopilot}

# Apply the gaming-optimized value
$k='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; New-Item $k -Force | Out-Null; Set-ItemProperty $k -Name HubsSidebarEnabled -Value 0 -Type DWord; Set-ItemProperty $k -Name CopilotPageContext -Value 0 -Type DWord; Set-ItemProperty $k -Name GenAILocalFoundationalModelSettings -Value 1 -Type DWord

# Reverse it (restore the Windows default)
$k='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; foreach($n in 'HubsSidebarEnabled','CopilotPageContext','GenAILocalFoundationalModelSettings','ComposeInlineEnabled','AllowBrowsingWithCopilot'){ Remove-ItemProperty $k -Name $n -EA SilentlyContinue }   # re-enable Edge Copilot
```

**Reversible via.** Delete HubsSidebarEnabled, CopilotPageContext, and GenAILocalFoundationalModelSettings from HKLM\SOFTWARE\Policies\Microsoft\Edge.


### Notepad Rewrite + Paint AI features

`ai.notepadpaint` &nbsp; **Recommended:** Off

**Why this is the recommendation.** These features bolt cloud AI onto otherwise simple apps. Users who don't use the AI features may prefer Notepad and Paint without the buttons. v0.1.39 added the targeting opt-out + HKLM policy so the disable holds across new Paint experiments rolling out under feature flags.

**What it does.** Per-user disable of Notepad Rewrite, Paint Cocreator, Paint Image Creator, and Paint Generative Erase. Plus a per-user opt-out of Paint's experiment-targeting service and the HKLM machine-wide Paint policy that stops Image Creator from offering itself before per-user toggle. Combined HKCU + HKLM writes.

**How it helps.** Notepad and Paint behave like classic versions; no AI action buttons; no cloud calls when you open a document or image; no opt-in prompts when MS rolls out new AI experiments.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Notepad and Paint behave like the classic apps -- no AI buttons, cloud calls, or opt-in prompts. | You lose Notepad Rewrite and Paint Cocreator/Image Creator/Generative Erase. |
| On (default) | AI writing and image tools built into Notepad and Paint. | Bolts cloud AI (and opt-in prompts) onto otherwise simple apps. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who doesn't use AI in Notepad / Paint | Off |
| Active user of Paint Cocreator / Image Creator | On |
| Privacy-conscious | Off |

**Risks.** None. AI features disappear from those two apps.

**Command line (PowerShell):**

```powershell
# Check the current value
$np='HKCU:\Software\Microsoft\Notepad'; $pt='HKCU:\Software\Microsoft\Windows\CurrentVersion\Paint'; @{Rewrite=(Get-ItemProperty $np -Name RewriteEnabled -EA SilentlyContinue).RewriteEnabled; Cocreator=(Get-ItemProperty $pt -Name DisableCocreator -EA SilentlyContinue).DisableCocreator}

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Notepad' -Name RewriteEnabled -Value 0 -Type DWord; $pt='HKCU:\Software\Microsoft\Windows\CurrentVersion\Paint'; New-Item $pt -Force | Out-Null; Set-ItemProperty $pt -Name DisableCocreator -Value 1 -Type DWord; Set-ItemProperty $pt -Name DisableImageCreator -Value 1 -Type DWord; Set-ItemProperty $pt -Name DisableGenerativeErase -Value 1 -Type DWord; $pv='HKCU:\Software\Microsoft\Windows\CurrentVersion\Applets\Paint\View'; New-Item $pv -Force | Out-Null; Set-ItemProperty $pv -Name IsSignedUpForTargetingService -Value 0 -Type DWord; $hk='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint'; New-Item $hk -Force | Out-Null; Set-ItemProperty $hk -Name DisableImageCreator -Value 1 -Type DWord

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKCU:\Software\Microsoft\Notepad' -Name RewriteEnabled -EA SilentlyContinue; $pt='HKCU:\Software\Microsoft\Windows\CurrentVersion\Paint'; foreach($n in 'DisableCocreator','DisableImageCreator','DisableGenerativeErase'){ Remove-ItemProperty $pt -Name $n -EA SilentlyContinue }; Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Applets\Paint\View' -Name IsSignedUpForTargetingService -EA SilentlyContinue; Remove-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint' -Name DisableImageCreator -EA SilentlyContinue   # restore Notepad/Paint AI
```

**Reversible via.** Delete the registry values under HKCU\Software\Microsoft\Notepad (RewriteEnabled), HKCU\Software\Microsoft\Windows\CurrentVersion\Paint (DisableCocreator, DisableImageCreator, DisableGenerativeErase), HKCU\Software\Microsoft\Windows\CurrentVersion\Applets\Paint\View (IsSignedUpForTargetingService), and HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint (DisableImageCreator).


### Search box AI suggestions + taskbar companion

`ai.settingssearch` &nbsp; **Recommended:** Off

**Why this is the recommendation.** The search box's AI suggestion layer calls Microsoft web endpoints to suggest answers as you type. The taskbar companion is a floating overlay some Windows 11 builds enable by default. Both are noise for users who use the search box for files and apps.

**What it does.** Three HKCU values: BingSearchEnabled=0 (the value Windows 11 actually honors for the AI/web suggestion layer in the search box -- this is the authoritative one), IsDynamicSearchBoxEnabled=0 (search highlights / the companion content), and the legacy DisableSearchBoxSuggestions=1 policy (best-effort -- unreliable on Win11). HKCU only -- no UAC.

**How it helps.** Search box returns local files / apps only -- no web suggestions, no Copilot answers inline, no taskbar companion widget. Indexing itself (Start menu, Explorer, Outlook) is untouched.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | The search box returns local files/apps only -- no web/Copilot suggestions or taskbar companion. | You lose inline web answers in the search box (indexing and search itself are unchanged). |
| On (default) | Web and Copilot answers suggested as you type in the search box. | Calls Microsoft web endpoints on your keystrokes; some builds add a floating companion. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who uses Windows Search for local files only | Off |
| Active user of search box web/Copilot suggestions | On |
| Privacy-conscious | Off |

**Risks.** You lose the web-suggestion layer and the taskbar companion. Search itself works exactly as before.

**Command line (PowerShell):**

```powershell
# Check the current value
@{Bing=(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name BingSearchEnabled -EA SilentlyContinue).BingSearchEnabled; Dynamic=(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\SearchSettings' -Name IsDynamicSearchBoxEnabled -EA SilentlyContinue).IsDynamicSearchBoxEnabled; Disable=(Get-ItemProperty 'HKCU:\SOFTWARE\Policies\Microsoft\Windows\Explorer' -Name DisableSearchBoxSuggestions -EA SilentlyContinue).DisableSearchBoxSuggestions}

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name BingSearchEnabled -Value 0 -Type DWord; $ss='HKCU:\Software\Microsoft\Windows\CurrentVersion\SearchSettings'; New-Item $ss -Force | Out-Null; Set-ItemProperty $ss -Name IsDynamicSearchBoxEnabled -Value 0 -Type DWord; $pe='HKCU:\SOFTWARE\Policies\Microsoft\Windows\Explorer'; New-Item $pe -Force | Out-Null; Set-ItemProperty $pe -Name DisableSearchBoxSuggestions -Value 1 -Type DWord

# Reverse it (restore the Windows default)
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name BingSearchEnabled -Value 1 -Type DWord; Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\SearchSettings' -Name IsDynamicSearchBoxEnabled -EA SilentlyContinue; Remove-ItemProperty 'HKCU:\SOFTWARE\Policies\Microsoft\Windows\Explorer' -Name DisableSearchBoxSuggestions -EA SilentlyContinue   # re-enable search-box web/AI suggestions
```

**Reversible via.** Delete BingSearchEnabled from HKCU\Software\Microsoft\Windows\CurrentVersion\Search, IsDynamicSearchBoxEnabled from HKCU\Software\Microsoft\Windows\CurrentVersion\SearchSettings, and DisableSearchBoxSuggestions from HKCU\SOFTWARE\Policies\Microsoft\Windows\Explorer (or set them back to 1 / 1 / absent).


### Typing / input insights data collection

`ai.inputinsights` &nbsp; **Recommended:** Off

**Why this is the recommendation.** By default, Windows builds a per-user typing model from text you've typed in apps. That data feeds personalized suggestions, autocorrect, and (in some Insider builds) AI features. Users who don't want their typing harvested can opt out at the OS level.

**What it does.** Two HKCU settings that disable Windows' typing-data and ink-data harvesting: RestrictImplicitTextCollection (blocks the OS from saving the plain text you type for personalized suggestions) and InsightsEnabled (the per-user master switch in the Input settings panel). HKCU only -- no UAC.

**How it helps.** Stops the OS from saving samples of what you type. Personalized typing suggestions degrade slightly (Windows falls back to the global suggestion model); everything else works as normal.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Windows stops saving samples of what you type for personalization. | Typing suggestions get slightly less personalized over time (autocorrect/spell-check unaffected). |
| On (default) | Personalized typing suggestions that improve as you type. | The OS saves a per-user model of the text you type. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious | Off |
| Anyone who doesn't notice typing suggestions getting better over time | Off |
| Active user of personalized typing suggestions on a touch keyboard | On |

**Risks.** Typing suggestions become slightly less personalized over time. No effect on autocorrect or basic spell-check.

**Command line (PowerShell):**

```powershell
# Check the current value
@{Restrict=(Get-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization' -Name RestrictImplicitTextCollection -EA SilentlyContinue).RestrictImplicitTextCollection; Insights=(Get-ItemProperty 'HKCU:\Software\Microsoft\input\Settings' -Name InsightsEnabled -EA SilentlyContinue).InsightsEnabled}

# Apply the gaming-optimized value
Set-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization' -Name RestrictImplicitTextCollection -Value 1 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\input\Settings' -Name InsightsEnabled -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKCU:\Software\Microsoft\InputPersonalization' -Name RestrictImplicitTextCollection -EA SilentlyContinue; Remove-ItemProperty 'HKCU:\Software\Microsoft\input\Settings' -Name InsightsEnabled -EA SilentlyContinue   # re-enable typing insights
```

**Reversible via.** Delete RestrictImplicitTextCollection from HKCU\Software\Microsoft\InputPersonalization and InsightsEnabled from HKCU\Software\Microsoft\input\Settings.


### Windows AI Actions

`ai.actions` &nbsp; **Recommended:** Off

**Why this is the recommendation.** AI Actions is a 24H2-era Windows feature that adds AI suggestions to right-click menus and similar surfaces. The FeatureManagement override is the documented kill switch (zoicware uses the same IDs).

**What it does.** Windows' shell-level AI Actions surface (right-click "rewrite with AI / summarize / search the web for this" on selected text, images, etc.). Toggled via the FeatureManagement override hive -- two numeric feature IDs (1853569164 and 4098520719) get EnabledState = 1 (force-disabled).

**How it helps.** Right-click menus, image picker dialogs, and other shell surfaces stop showing AI action options. No cloud calls when you right-click an image or selected text.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Right-click and image menus stop offering AI actions; the menus otherwise work normally. | You lose the 'rewrite/summarize/search the web for this' shell actions. |
| On (default) | AI actions available from right-click and image context menus. | Those actions send selected text/images to cloud AI. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who doesn't use AI right-click actions | Off |
| Active user of AI Actions | On |
| Privacy-conscious | Off |

**Risks.** You lose the AI options in right-click / image-context menus. The right-click menus themselves still work for everything else.

**Command line (PowerShell):**

```powershell
# Check the current value
$r='HKLM:\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8'; @{A=(Get-ItemProperty $r\1853569164 -Name EnabledState -EA SilentlyContinue).EnabledState; B=(Get-ItemProperty $r\4098520719 -Name EnabledState -EA SilentlyContinue).EnabledState}

# Apply the gaming-optimized value
$r='HKLM:\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8'; foreach($id in 1853569164, 4098520719) { New-Item -Path "$r\$id" -Force | Out-Null; Set-ItemProperty -Path "$r\$id" -Name EnabledState -Value 1 -Type DWord }

# Reverse it (restore the Windows default)
$r='HKLM:\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8'; foreach($id in 1853569164, 4098520719){ Remove-ItemProperty -Path "$r\$id" -Name EnabledState -EA SilentlyContinue }   # restore Windows AI Actions
```

**Reversible via.** Delete EnabledState from HKLM\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\1853569164 and 4098520719. Feature IDs may change in future Windows builds -- if you see new AI Actions surfaces after a Windows Update, GamerGuardian's existing overrides will still hold for these two but new feature IDs would need a new monitor entry.


### Windows Copilot

`ai.copilot` &nbsp; **Recommended:** Off (GamerGuardian default for users who specifically open this tab)

**Why this is the recommendation.** Copilot calls Microsoft cloud endpoints, runs background processes, and consumes resources when invoked. Some users prefer not to send page or document context to cloud AI services.

**What it does.** The system-wide Copilot taskbar button and the Win+C keyboard shortcut. Setting Off writes the TurnOffWindowsCopilot policy in both HKLM and HKCU and hides the taskbar button.

**How it helps.** Removes the always-present taskbar button so it can't be invoked accidentally; blocks Win+C from launching it; prevents the policy from being unset by routine Windows configuration changes.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Removes the taskbar button and Win+C, and stops background Copilot processes and cloud calls. | You lose quick access to Copilot unless you turn it back on. |
| On (default) | One-click and Win+C access to Windows Copilot. | Always-present button, background processes, and page/context sent to cloud AI. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious users | Off |
| Gaming setup | Off -- no benefit, removes one more background subsystem |
| Active Copilot user | On |
| Enterprise with separate compliance | Whatever your IT policy says |

**Risks.** None for performance. You lose access to Copilot if you change your mind -- toggle back on or delete the policy values to restore.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -EA SilentlyContinue).TurnOffWindowsCopilot

# Apply the gaming-optimized value
# Disable Windows Copilot:
Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -Value 1 -Type DWord; Set-ItemProperty 'HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -Value 1 -Type DWord; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name ShowCopilotButton -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Remove-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -EA SilentlyContinue; Remove-ItemProperty 'HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot' -Name TurnOffWindowsCopilot -EA SilentlyContinue   # re-enable Copilot
```

**Reversible via.** Delete TurnOffWindowsCopilot from HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot and HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot.


### Windows Recall + AI data analysis

`ai.recall` &nbsp; **Recommended:** Off

**Why this is the recommendation.** Two distinct concerns: (1) privacy -- continuous screen capture, even local-only, is a meaningful new surface; (2) performance -- the NPU and disk I/O have nonzero cost. The policy block stops new snapshotting; it does NOT delete existing snapshots.

**What it does.** Recall captures snapshots of your screen every few seconds and indexes them with on-device AI so you can later search 'what was that thing I had open last Tuesday.' Currently rolling out on Copilot+ PCs (Snapdragon X, recent Intel Core Ultra, AMD Ryzen AI). Setting Off writes AllowRecallEnablement=0 and DisableAIDataAnalysis=1 in the HKLM WindowsAI policy key.

**How it helps.** Stops Recall snapshotting at the policy level (Windows honors this without question, unlike a per-app toggle). Blocks the broader Windows AI Data Analysis surface that future features may opt into.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Off (recommended) | Stops Recall snapshotting your screen at the policy level (no NPU/disk cost, smaller privacy surface). | You lose Recall's 'find what I had open' search; existing snapshots aren't deleted by this toggle. |
| On (default on Copilot+ PCs) | Search your past screen activity with on-device AI. | Continuous screen capture plus NPU/disk cost, even though it's processed locally. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who doesn't actively want Recall | Off |
| Copilot+ PC user who specifically wants Recall | On (and also delete this app's policy block) |
| Privacy-conscious | Off |
| Gaming setup | Off |

**Risks.** None for security or stability. You lose Recall if you change your mind. Existing Recall snapshots are not deleted by this toggle -- to remove them, go to Settings > Privacy & security > Recall & snapshots > Delete all snapshots.

**Command line (PowerShell):**

```powershell
# Check the current value
$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI'; @{Allow=(Get-ItemProperty $k -Name AllowRecallEnablement -EA SilentlyContinue).AllowRecallEnablement; Disable=(Get-ItemProperty $k -Name DisableAIDataAnalysis -EA SilentlyContinue).DisableAIDataAnalysis; Snap=(Get-ItemProperty $k -Name TurnOffSavingSnapshots -EA SilentlyContinue).TurnOffSavingSnapshots}

# Apply the gaming-optimized value
$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI'; New-Item $k -Force | Out-Null; Set-ItemProperty $k -Name AllowRecallEnablement -Value 0 -Type DWord; Set-ItemProperty $k -Name DisableAIDataAnalysis -Value 1 -Type DWord

# Reverse it (restore the Windows default)
$k='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI'; foreach($n in 'AllowRecallEnablement','DisableAIDataAnalysis','TurnOffSavingSnapshots'){ Remove-ItemProperty $k -Name $n -EA SilentlyContinue }   # re-allow Recall
```

**Reversible via.** Delete AllowRecallEnablement and DisableAIDataAnalysis from HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI.

## Windows AI UWP packages

### Microsoft 365 Copilot (launcher app)

`ai.app:Microsoft.MicrosoftOfficeHub` &nbsp; **Recommended:** Remove (it does not affect installed Office apps)

**Why this is the recommendation.** If you don't use the launcher tile -- and most people open Word/Excel directly -- it's dead weight that re-pins itself to Start and nags about Copilot. Removing it reclaims the tile and the background app.

**What it does.** The standalone 'Microsoft 365 Copilot' Store app -- formerly the 'Office' / 'Microsoft 365' hub launcher (package Microsoft.MicrosoftOfficeHub). Microsoft renamed it and auto-pushed it onto Windows 11 machines in 2025, prompting a wave of 'why is this here' complaints. It's a thin web wrapper that promotes Copilot and the Office suite; it is NOT Word/Excel/PowerPoint themselves.

**How it helps.** Removes the launcher from Start and stops its Copilot promotion. Your actual Office programs keep working.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Remove | Removes the Microsoft 365 launcher tile and its Copilot promotion -- your actual Office apps keep working. | Windows Update/Store may re-provision it (Auto-apply re-removes); reinstall via the Store. |
| Keep | Keeps the hub tile for finding docs. | Re-pins itself to Start and nags about Copilot if you never use it. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Open Word/Excel directly, never use the hub | Remove |
| Use the Microsoft 365 launcher to find docs | Don't remove |
| Worried Windows Update re-pins it | Remove + tick Auto-apply |

**Risks.** The Microsoft 365 launcher tile disappears. Windows Update / Store may re-provision it after major updates -- the AutoApply tick re-removes it. Reinstall via the Microsoft Store ('Microsoft 365 Copilot').

**Command line (PowerShell):**

```powershell
# Check the current value
Get-AppxPackage -Name 'Microsoft.MicrosoftOfficeHub'   # empty output = removed

# Apply the gaming-optimized value
Get-AppxPackage -Name 'Microsoft.MicrosoftOfficeHub' | Remove-AppxPackage

# Reverse it (restore the Windows default)
(no command-line restore -- reinstall from the Microsoft Store, or wait for Windows Update to re-provision)
```

**Reversible via.** Install 'Microsoft 365 Copilot' from the Microsoft Store.


### Microsoft Copilot (UWP)

`ai.app:Microsoft.Copilot` &nbsp; **Recommended:** Remove (only after the system policy is set to Off)

**Why this is the recommendation.** If you've blocked Copilot via the system policy toggle above, the standalone app is dead weight. Removing it reclaims disk and removes the launcher entry.

**What it does.** The standalone Copilot UWP app that Windows installs alongside the system-wide Copilot integration. Hundreds of MB on disk.

**How it helps.** Reclaims disk space. No more Copilot app launcher in Start.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Remove (after the Copilot policy is Off) | Reclaims hundreds of MB and removes the Copilot launcher from Start. | Reinstalling needs the Microsoft Store; Windows Update may re-provision it (Auto-apply re-removes). |
| Keep | The Copilot app stays one click away. | Dead weight on disk if you've already blocked Copilot via policy. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Already disabled Copilot policy | Remove |
| Active Copilot user | Don't remove |
| Worried Windows Update might re-provision it | Remove + tick Auto-apply silently |

**Risks.** Reinstalling requires the Microsoft Store. Windows Update may re-provision the app after major updates -- the AutoApply tick handles that.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-AppxPackage -Name 'Microsoft.Copilot'   # empty output = removed

# Apply the gaming-optimized value
Get-AppxPackage -Name 'Microsoft.Copilot' | Remove-AppxPackage

# Reverse it (restore the Windows default)
(no command-line restore -- reinstall from the Microsoft Store, or wait for Windows Update to re-provision)
```

**Reversible via.** Install 'Microsoft Copilot' from the Microsoft Store.


### Windows AI Copilot Provider

`ai.app:Microsoft.Windows.Ai.Copilot.Provider` &nbsp; **Recommended:** Remove (only after the system policy is Off)

**Why this is the recommendation.** Pairs with the Copilot system policy block. With the policy off, the provider is unused.

**What it does.** Background provider package that backs the Windows AI Copilot surface (the in-OS Copilot integration, not the standalone app).

**How it helps.** Removes the background provider; small reduction in installed-app surface.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Remove (after the Copilot policy is Off) | Removes the unused background provider; smaller installed-app surface. | Re-provisioned by Windows Update; reinstall needs the Store. |
| Keep | Nothing to re-provision later. | Keeps a provider that does nothing once Copilot is blocked. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Already disabled Copilot policy | Remove |
| Active Copilot user | Don't remove |
| Privacy-conscious | Remove |

**Risks.** Re-provisioned by Windows Update; tick AutoApply to keep it removed. Reinstall requires the Microsoft Store.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-AppxPackage -Name 'Microsoft.Windows.Ai.Copilot.Provider'   # empty output = removed

# Apply the gaming-optimized value
Get-AppxPackage -Name 'Microsoft.Windows.Ai.Copilot.Provider' | Remove-AppxPackage

# Reverse it (restore the Windows default)
(no command-line restore -- reinstall from the Microsoft Store, or wait for Windows Update to re-provision)
```

**Reversible via.** Install via Microsoft Store or wait for Windows Update to re-provision.


### Windows AI Experience

`ai.app:MicrosoftWindows.Client.AIX` &nbsp; **Recommended:** Remove if you don't use Windows AI features

**Why this is the recommendation.** On non-Copilot+ PCs the component is often unused. On Copilot+ PCs, removing it deletes the AI settings UI.

**What it does.** AI Experience component shipped on Copilot+ PCs. Backs the AI settings panel and assorted shell AI integrations.

**How it helps.** Reclaims disk; removes the AI settings panel from Settings.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Remove (if you don't use Windows AI) | Reclaims disk and removes the AI settings panel. | The AI Settings UI disappears; re-provisioned by Windows Update. |
| Keep | Keeps the AI settings panel and shell AI integrations. | Unused weight on non-Copilot+ PCs. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Non-Copilot+ PC | Remove if you don't use any Windows AI |
| Copilot+ PC with Recall / Click-to-Do disabled | Remove |
| Active AI user on Copilot+ PC | Don't remove |

**Risks.** AI Settings panel disappears. Re-provisioned by Windows Update.

**Command line (PowerShell):**

```powershell
# Check the current value
Get-AppxPackage -Name 'MicrosoftWindows.Client.AIX'   # empty output = removed

# Apply the gaming-optimized value
Get-AppxPackage -Name 'MicrosoftWindows.Client.AIX' | Remove-AppxPackage

# Reverse it (restore the Windows default)
(no command-line restore -- reinstall from the Microsoft Store, or wait for Windows Update to re-provision)
```

**Reversible via.** Install via Microsoft Store or wait for Windows Update to re-provision.

## Windows services

### Agent Activation Runtime Service

`service:AarSvc` &nbsp; **Recommended:** Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc

**Why this is the recommendation.** Like WSAIFabricSvc, this service is paired with the Windows AI policy toggles. If you've disabled Copilot, Recall, etc. at the policy level, AarSvc has nothing useful to do; if any AI feature is still enabled, leave it on.

**What it does.** Per-user service that backs Windows AI agent activations -- the runtime Copilot voice, Cortana legacy hooks, and certain shell AI surfaces call into when they want to launch in the background.

**How it helps.** Removes a per-user service backing AI features you've already disabled. Pairs naturally with WSAIFabricSvc + the Windows AI policy toggles.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | Removes a per-user service backing AI features you've already disabled. Pairs naturally with WSAIFabricSvc + the Windows AI policy toggles. | Per-user services use a generated suffix on the actual service name (AarSvc_<hex>). GamerGuardian disables the template definition so every new per-user instance starts disabled, but existing user sessions may need a logoff/logon to pick up the change. If you re-enable any AI feature later, it will fail to launch until you re-enable this service. |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc |
| Streaming + game | Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc |
| Casual single-player | Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc |
| Productivity / mixed-use | Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc |

**Risks.** Per-user services use a generated suffix on the actual service name (AarSvc_<hex>). GamerGuardian disables the template definition so every new per-user instance starts disabled, but existing user sessions may need a logoff/logon to pick up the change. If you re-enable any AI feature later, it will fail to launch until you re-enable this service.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "AarSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "AarSvc"; sc.exe config "AarSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "AarSvc" start= auto; sc.exe start "AarSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name AarSvc -StartupType Manual


### Connected User Experiences and Telemetry

`service:DiagTrack` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** Constant background CPU + network for telemetry you didn't ask for. Disabling is safe on consumer Windows.

**What it does.** Collects diagnostic and usage data and sends it to Microsoft. Always-on background sender.

**How it helps.** Removes a constant low-level background sender. Small CPU and bandwidth saving.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes a constant low-level background sender. Small CPU and bandwidth saving. | Microsoft loses diagnostic data from your machine. Rare reports of Windows Update issues in unusual configurations; never observed on a desktop with a normal update cadence. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Microsoft loses diagnostic data from your machine. Rare reports of Windows Update issues in unusual configurations; never observed on a desktop with a normal update cadence.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "DiagTrack"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "DiagTrack"; sc.exe config "DiagTrack" start= demand

# Reverse it (restore the Windows default)
sc.exe config "DiagTrack" start= auto; sc.exe start "DiagTrack"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name DiagTrack -StartupType Automatic


### Delivery Optimization

`service:DoSvc` &nbsp; **Recommended:** Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc)

**Why this is the recommendation.** Background bandwidth use, both upload and download, that you didn't authorize per-update. Especially impactful on metered or asymmetric connections.

**What it does.** Peer-to-peer Windows Update downloads. Lets your PC download update bits from other LAN/Internet peers and lets your PC contribute uplink to other peers.

**How it helps.** Stops the bandwidth contribution entirely. Updates still install normally; they just come from Microsoft directly.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Stops the bandwidth contribution entirely. Updates still install normally; they just come from Microsoft directly. | Slightly slower update downloads on networks with many other Windows PCs. None observable on a single-PC household. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc) |
| Streaming + game | Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc) |
| Casual single-player | Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc) |
| Productivity / mixed-use | Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc) |

**Risks.** Slightly slower update downloads on networks with many other Windows PCs. None observable on a single-PC household.

**Command line (PowerShell):**

```powershell
# Check the current value
(Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization' -Name DODownloadMode -EA SilentlyContinue).DODownloadMode

# Apply the gaming-optimized value
New-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization' -Force | Out-Null; Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization' -Name DODownloadMode -Value 0 -Type DWord

# Reverse it (restore the Windows default)
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization' -Name DODownloadMode -Force   # restore the Windows default
```

**Reversible via.** Delete the DODownloadMode value from HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization.


### Distributed Link Tracking Client

`service:TrkWks` &nbsp; **Recommended:** Disabled (Manual if you rely on shortcut auto-repair across drives)

**Why this is the recommendation.** Runs automatically but is rarely exercised on a standalone home PC; most users never notice it being off.

**What it does.** Maintains links between NTFS files when their targets move across volumes or a domain (e.g. keeping a shortcut valid after the file moves).

**How it helps.** Removes a small always-on background service.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes a small always-on background service. | Shortcuts/links won't auto-repair if their target moves between volumes. Minor and rarely noticed. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled (Manual if you rely on shortcut auto-repair across drives) |
| Streaming + game | Disabled (Manual if you rely on shortcut auto-repair across drives) |
| Casual single-player | Disabled (Manual if you rely on shortcut auto-repair across drives) |
| Productivity / mixed-use | Disabled (Manual if you rely on shortcut auto-repair across drives) |

**Risks.** Shortcuts/links won't auto-repair if their target moves between volumes. Minor and rarely noticed.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "TrkWks"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "TrkWks"; sc.exe config "TrkWks" start= demand

# Reverse it (restore the Windows default)
sc.exe config "TrkWks" start= auto; sc.exe start "TrkWks"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name TrkWks -StartupType Automatic


### Downloaded Maps Manager

`service:MapsBroker` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** If you never use the Maps app, this service does nothing useful and downloads map data you'll never look at.

**What it does.** Background service that downloads and updates offline maps for the Windows Maps app.

**How it helps.** Cuts background disk I/O and reclaims a small amount of memory.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Cuts background disk I/O and reclaims a small amount of memory. | If you do open the Maps app later, offline map functionality won't work until you re-enable. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** If you do open the Maps app later, offline map functionality won't work until you re-enable.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "MapsBroker"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "MapsBroker"; sc.exe config "MapsBroker" start= demand

# Reverse it (restore the Windows default)
sc.exe config "MapsBroker" start= auto; sc.exe start "MapsBroker"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name MapsBroker -StartupType AutomaticDelayed


### Kiosk Mode (Assigned Access)

`service:AssignedAccessManagerSvc` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** A personal gaming PC is not a kiosk, so this service is unused.

**What it does.** Backs single-app 'kiosk' / assigned-access mode used on shared or public terminals.

**How it helps.** Removes an idle service that personal machines never use.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes an idle service that personal machines never use. | Only relevant if you actually configure Assigned Access / kiosk mode -- rare on a home PC. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Only relevant if you actually configure Assigned Access / kiosk mode -- rare on a home PC.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "AssignedAccessManagerSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "AssignedAccessManagerSvc"; sc.exe config "AssignedAccessManagerSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "AssignedAccessManagerSvc" start= auto; sc.exe start "AssignedAccessManagerSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name AssignedAccessManagerSvc -StartupType Manual


### Parental Controls

`service:WpcMonSvc` &nbsp; **Recommended:** Disabled (no Family Safety on this PC)

**Why this is the recommendation.** If you don't have child accounts or Family Safety configured on this PC, the service has nothing to enforce.

**What it does.** Enforces Microsoft Family Safety parental-control restrictions (time limits, content filters).

**How it helps.** Removes an idle service on machines with no parental controls.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes an idle service on machines with no parental controls. | If a child account on this PC relies on Family Safety enforcement, do NOT disable -- restrictions would stop applying. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled (no Family Safety on this PC) |
| Streaming + game | Disabled (no Family Safety on this PC) |
| Casual single-player | Disabled (no Family Safety on this PC) |
| Productivity / mixed-use | Disabled (no Family Safety on this PC) |

**Risks.** If a child account on this PC relies on Family Safety enforcement, do NOT disable -- restrictions would stop applying.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "WpcMonSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "WpcMonSvc"; sc.exe config "WpcMonSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "WpcMonSvc" start= auto; sc.exe start "WpcMonSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name WpcMonSvc -StartupType Manual


### Payments and NFC/SE Manager

`service:SEMgrSvc` &nbsp; **Recommended:** Disabled (if you have no NFC reader on this PC)

**Why this is the recommendation.** A gaming desktop almost never has NFC payment hardware, so this service has nothing to manage.

**What it does.** Manages tap-to-pay and the NFC secure element used for contactless payments.

**How it helps.** Removes an idle background service on machines without NFC.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes an idle background service on machines without NFC. | If you do use tap-to-pay / NFC on this machine (some laptops), leave it on -- payments and NFC apps will fail without it. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled (if you have no NFC reader on this PC) |
| Streaming + game | Disabled (if you have no NFC reader on this PC) |
| Casual single-player | Disabled (if you have no NFC reader on this PC) |
| Productivity / mixed-use | Disabled (if you have no NFC reader on this PC) |

**Risks.** If you do use tap-to-pay / NFC on this machine (some laptops), leave it on -- payments and NFC apps will fail without it.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "SEMgrSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "SEMgrSvc"; sc.exe config "SEMgrSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "SEMgrSvc" start= auto; sc.exe start "SEMgrSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name SEMgrSvc -StartupType Manual


### Phone Service

`service:PhoneSvc` &nbsp; **Recommended:** Disabled (no cellular hardware) / Manual otherwise

**Why this is the recommendation.** On a desktop with no cellular hardware this service is idle.

**What it does.** Manages the telephony/cellular device state for machines with a cellular modem or phone-calling integration.

**How it helps.** Removes an idle background service on non-cellular machines.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes an idle background service on non-cellular machines. | If you make calls through Windows or use a cellular modem, leave it on. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled (no cellular hardware) / Manual otherwise |
| Streaming + game | Disabled (no cellular hardware) / Manual otherwise |
| Casual single-player | Disabled (no cellular hardware) / Manual otherwise |
| Productivity / mixed-use | Disabled (no cellular hardware) / Manual otherwise |

**Risks.** If you make calls through Windows or use a cellular modem, leave it on.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "PhoneSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "PhoneSvc"; sc.exe config "PhoneSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "PhoneSvc" start= auto; sc.exe start "PhoneSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name PhoneSvc -StartupType Manual


### Retail Demo Service

`service:RetailDemo` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** Useless outside retail kiosks.

**What it does.** Supports the in-store retail demo mode for Windows.

**How it helps.** Removes a useless service from the running list.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes a useless service from the running list. | None. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** None.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "RetailDemo"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "RetailDemo"; sc.exe config "RetailDemo" start= demand

# Reverse it (restore the Windows default)
sc.exe config "RetailDemo" start= auto; sc.exe start "RetailDemo"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name RetailDemo -StartupType Manual


### Routing and Remote Access

`service:RemoteAccess` &nbsp; **Recommended:** Default (stays Disabled)

**Why this is the recommendation.** Already disabled on a default install -- this entry is a drift-guard so you can confirm nothing silently re-enables it.

**What it does.** Provides LAN/WAN routing and dial-up/VPN server functionality. Disabled by default on client Windows.

**How it helps.** No change on a default machine; catches an unexpected re-enable.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | No change on a default machine; catches an unexpected re-enable. | If you intentionally run the Windows routing / RRAS VPN server role (rare on a gaming desktop), leave it alone. |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (stays Disabled) |
| Streaming + game | Default (stays Disabled) |
| Casual single-player | Default (stays Disabled) |
| Productivity / mixed-use | Default (stays Disabled) |

**Risks.** If you intentionally run the Windows routing / RRAS VPN server role (rare on a gaming desktop), leave it alone.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "RemoteAccess"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "RemoteAccess"; sc.exe config "RemoteAccess" start= demand

# Reverse it (restore the Windows default)
sc.exe config "RemoteAccess" start= auto; sc.exe start "RemoteAccess"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name RemoteAccess -StartupType Disabled (its default), or Manual if you need it.


### Superfetch / SysMain

`service:SysMain` &nbsp; **Recommended:** Default (leave on -- current Microsoft guidance)

**Why this is the recommendation.** Hotly debated. On NVMe systems with abundant RAM, the cost is minor and the benefit is small -- Microsoft now recommends leaving it on. On slower drives or tight-RAM systems, the I/O cost can be more visible than the prefetch benefit.

**What it does.** Tracks app usage patterns and preloads code into RAM before you launch the app. On HDDs this provides large startup-time improvements; on NVMe SSDs the benefit is marginal.

**How it helps.** Slightly lower idle disk I/O.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | Slightly lower idle disk I/O. | Disabling can slow first-launch of frequently-used apps. On HDDs the slowdown is severe. |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (leave on -- current Microsoft guidance) |
| Streaming + game | Default (leave on -- current Microsoft guidance) |
| Casual single-player | Default (leave on -- current Microsoft guidance) |
| Productivity / mixed-use | Default (leave on -- current Microsoft guidance) |

**Risks.** Disabling can slow first-launch of frequently-used apps. On HDDs the slowdown is severe.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "SysMain"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "SysMain"; sc.exe config "SysMain" start= demand

# Reverse it (restore the Windows default)
sc.exe config "SysMain" start= auto; sc.exe start "SysMain"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name SysMain -StartupType Automatic


### Windows AI Fabric Service

`service:WSAIFabricSvc` &nbsp; **Recommended:** Default (Manual) -- only Disable if you've also flipped the AI policy toggles

**Why this is the recommendation.** If you've disabled the Windows AI policy toggles in the Windows AI tab, the AI features won't be invoked and the service is unused.

**What it does.** Backs the on-device AI runtime that Copilot+ features (Copilot, Recall, Click-to-Do) call into.

**How it helps.** Removes a process backing AI features you've already disabled. Pairs naturally with the policy toggles in the Windows AI tab.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | Removes a process backing AI features you've already disabled. Pairs naturally with the policy toggles in the Windows AI tab. | If you re-enable any AI feature later, it will fail to launch until you re-enable this service. |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) -- only Disable if you've also flipped the AI policy toggles |
| Streaming + game | Default (Manual) -- only Disable if you've also flipped the AI policy toggles |
| Casual single-player | Default (Manual) -- only Disable if you've also flipped the AI policy toggles |
| Productivity / mixed-use | Default (Manual) -- only Disable if you've also flipped the AI policy toggles |

**Risks.** If you re-enable any AI feature later, it will fail to launch until you re-enable this service.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "WSAIFabricSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "WSAIFabricSvc"; sc.exe config "WSAIFabricSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "WSAIFabricSvc" start= auto; sc.exe start "WSAIFabricSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name WSAIFabricSvc -StartupType Manual


### Windows Error Reporting Service

`service:WerSvc` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** If you don't send crash reports, the service has nothing useful to do.

**What it does.** Collects crash dumps and reports them to Microsoft.

**How it helps.** Removes background CPU spent on crash data collection.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes background CPU spent on crash data collection. | Crash dump collection stops. If you ever need to share a crash report with Microsoft support, re-enable first. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Crash dump collection stops. If you ever need to share a crash report with Microsoft support, re-enable first.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "WerSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "WerSvc"; sc.exe config "WerSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "WerSvc" start= auto; sc.exe start "WerSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name WerSvc -StartupType Manual


### Windows Image Acquisition (WIA)

`service:stisvc` &nbsp; **Recommended:** Disabled (no scanner/camera) / Manual otherwise

**Why this is the recommendation.** If you don't own a scanner or a WIA-class camera, nothing ever calls this service.

**What it does.** Provides image-acquisition services for scanners and digital still cameras.

**How it helps.** Removes an idle service on machines with no imaging hardware.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes an idle service on machines with no imaging hardware. | Scanning software and some camera-import flows will fail to acquire images with this disabled. Re-enable before scanning. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled (no scanner/camera) / Manual otherwise |
| Streaming + game | Disabled (no scanner/camera) / Manual otherwise |
| Casual single-player | Disabled (no scanner/camera) / Manual otherwise |
| Productivity / mixed-use | Disabled (no scanner/camera) / Manual otherwise |

**Risks.** Scanning software and some camera-import flows will fail to acquire images with this disabled. Re-enable before scanning.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "stisvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "stisvc"; sc.exe config "stisvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "stisvc" start= auto; sc.exe start "stisvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name stisvc -StartupType Manual


### Windows Insider Service

`service:wisvc` &nbsp; **Recommended:** Disabled (Manual if you run Insider builds)

**Why this is the recommendation.** If you're on the stable channel (the vast majority of users), this service has nothing to do. Disabling removes one more idle background service.

**What it does.** Backs the Windows Insider Program: preview-build enrollment, flighting configuration, and the diagnostic flow Insider builds use. Idle on a machine not enrolled in the Insider Program.

**How it helps.** Removes an idle service. No effect on stable Windows.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Removes an idle service. No effect on stable Windows. | If you are an Insider or plan to enroll, leave it on -- with it disabled, the Insider Program settings page won't enroll or flight new builds. Re-enable before joining. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled (Manual if you run Insider builds) |
| Streaming + game | Disabled (Manual if you run Insider builds) |
| Casual single-player | Disabled (Manual if you run Insider builds) |
| Productivity / mixed-use | Disabled (Manual if you run Insider builds) |

**Risks.** If you are an Insider or plan to enroll, leave it on -- with it disabled, the Insider Program settings page won't enroll or flight new builds. Re-enable before joining.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "wisvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "wisvc"; sc.exe config "wisvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "wisvc" start= auto; sc.exe start "wisvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name wisvc -StartupType Manual


### Windows Search

`service:WSearch` &nbsp; **Recommended:** Default (don't manage)

**Why this is the recommendation.** Indexing is heavy on slow disks and during initial scan. On a fast NVMe with SSD-friendly index location, the cost is minor.

**What it does.** Indexes file contents, properties, and Start-menu app names. Powers Start search, Explorer search, and Outlook search.

**How it helps.** Disabling stops indexing entirely. Start menu app search still works (uses a separate cache); file-content search degrades to slow scan.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | Disabling stops indexing entirely. Start menu app search still works (uses a separate cache); file-content search degrades to slow scan. | Major: Start search becomes much worse, Explorer search slows to a crawl, Outlook search may stop working entirely. Only disable on machines where you never search. |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (don't manage) |
| Streaming + game | Default (don't manage) |
| Casual single-player | Default (don't manage) |
| Productivity / mixed-use | Default (don't manage) |

**Risks.** Major: Start search becomes much worse, Explorer search slows to a crawl, Outlook search may stop working entirely. Only disable on machines where you never search.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "WSearch"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "WSearch"; sc.exe config "WSearch" start= demand

# Reverse it (restore the Windows default)
sc.exe config "WSearch" start= auto; sc.exe start "WSearch"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name WSearch -StartupType AutomaticDelayed


### Xbox Accessory Management

`service:XboxGipSvc` &nbsp; **Recommended:** Default (Manual -- don't manage)

**Why this is the recommendation.** If you don't use Xbox-branded controllers via the Xbox Accessories app, this service has nothing to do.

**What it does.** Backs Xbox-branded accessories (Xbox One controllers, Elite Series 2, etc.) for updates and configuration.

**How it helps.** Removes a constantly-running USB-watching service.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | Removes a constantly-running USB-watching service. | Xbox Accessories app won't be able to update controllers or change controller profiles. Game-pad input itself works regardless (handled by xinput). |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual -- don't manage) |
| Streaming + game | Default (Manual -- don't manage) |
| Casual single-player | Default (Manual -- don't manage) |
| Productivity / mixed-use | Default (Manual -- don't manage) |

**Risks.** Xbox Accessories app won't be able to update controllers or change controller profiles. Game-pad input itself works regardless (handled by xinput).

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "XboxGipSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "XboxGipSvc"; sc.exe config "XboxGipSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "XboxGipSvc" start= auto; sc.exe start "XboxGipSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name XboxGipSvc -StartupType Manual


### Xbox Live Auth Manager

`service:XblAuthManager` &nbsp; **Recommended:** Default (Manual)

**Why this is the recommendation.** If you don't use Microsoft Store games or Game Pass, this service is unused.

**What it does.** Authentication broker for Xbox Live. Required by Microsoft Store games, Game Pass, and the Xbox app.

**How it helps.** Removes a constantly-running auth-broker service.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | Removes a constantly-running auth-broker service. | Microsoft Store games and Game Pass titles will fail to launch (authentication error). |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) |
| Streaming + game | Default (Manual) |
| Casual single-player | Default (Manual) |
| Productivity / mixed-use | Default (Manual) |

**Risks.** Microsoft Store games and Game Pass titles will fail to launch (authentication error).

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "XblAuthManager"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "XblAuthManager"; sc.exe config "XblAuthManager" start= demand

# Reverse it (restore the Windows default)
sc.exe config "XblAuthManager" start= auto; sc.exe start "XblAuthManager"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name XblAuthManager -StartupType Manual


### Xbox Live Game Save

`service:XblGameSave` &nbsp; **Recommended:** Default (Manual)

**Why this is the recommendation.** If you don't use Microsoft Store games or Game Pass, this service is unused.

**What it does.** Cloud save sync for Microsoft Store / Game Pass titles.

**How it helps.** Removes a small background sync service.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | Removes a small background sync service. | Cloud saves stop syncing for affected titles. |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) |
| Streaming + game | Default (Manual) |
| Casual single-player | Default (Manual) |
| Productivity / mixed-use | Default (Manual) |

**Risks.** Cloud saves stop syncing for affected titles.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "XblGameSave"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "XblGameSave"; sc.exe config "XblGameSave" start= demand

# Reverse it (restore the Windows default)
sc.exe config "XblGameSave" start= auto; sc.exe start "XblGameSave"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name XblGameSave -StartupType Manual


### Xbox Live Networking Service

`service:XboxNetApiSvc` &nbsp; **Recommended:** Default (Manual)

**Why this is the recommendation.** If you don't use Microsoft Store games online, this service is unused.

**What it does.** Multiplayer and networking glue for Microsoft Store games.

**How it helps.** Removes a small background service.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it | Removes a small background service. | Microsoft Store multiplayer titles will fail to find lobbies or connect. |
| Leave it as Windows ships it (recommended) | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) |
| Streaming + game | Default (Manual) |
| Casual single-player | Default (Manual) |
| Productivity / mixed-use | Default (Manual) |

**Risks.** Microsoft Store multiplayer titles will fail to find lobbies or connect.

**Command line (PowerShell):**

```powershell
# Check the current value
sc qc "XboxNetApiSvc"   # look for START_TYPE

# Apply the gaming-optimized value
sc.exe stop "XboxNetApiSvc"; sc.exe config "XboxNetApiSvc" start= demand

# Reverse it (restore the Windows default)
sc.exe config "XboxNetApiSvc" start= auto; sc.exe start "XboxNetApiSvc"   # restore the Windows default (see Reversible via for this service's exact default start type)
```

**Reversible via.** Set-Service -Name XboxNetApiSvc -StartupType Manual

## Windows scheduled tasks

### Microsoft Compatibility Appraiser

`task:\microsoft\windows\application experience\microsoft compatibility appraiser` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** The appraiser scan is well documented for spiking CPU and disk to ~100% while it runs (sometimes at startup) -- an unpredictable hitch source mid-game. It runs on a daily trigger whether or not you ever upgrade Windows.

**What it does.** A Windows scheduled task that runs CompatTelRunner.exe to scan installed apps, drivers, and files, then writes compatibility + Windows-upgrade-readiness markers and sends telemetry to Microsoft. It's the engine behind the 'Microsoft Compatibility Telemetry' process you see in Task Manager.

**How it helps.** Disabling the task stops the periodic compatibility scan and its CPU/disk spike. Windows Update still works; you only lose pre-update compatibility checks and Win11 upgrade-readiness signals, which don't matter on a gaming PC.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Disabling the task stops the periodic compatibility scan and its CPU/disk spike. Windows Update still works; you only lose pre-update compatibility checks and Win11 upgrade-readiness signals, which don't matter on a gaming PC. | You give up automatic pre-update app-compatibility checks and Windows 11 upgrade-readiness data collection. A Windows feature update may re-enable the task; GamerGuardian's Auto-apply re-disables it. Re-enable any time with the reverse command. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** You give up automatic pre-update app-compatibility checks and Windows 11 upgrade-readiness data collection. A Windows feature update may re-enable the task; GamerGuardian's Auto-apply re-disables it. Re-enable any time with the reverse command.

**Command line (PowerShell):**

```powershell
# Check the current value
schtasks /Query /TN "\microsoft\windows\application experience\microsoft compatibility appraiser" /XML   # <Settings><Enabled>false</Enabled> = disabled

# Apply the gaming-optimized value
schtasks /Change /TN "\microsoft\windows\application experience\microsoft compatibility appraiser" /Disable

# Reverse it (restore the Windows default)
schtasks /Change /TN "\microsoft\windows\application experience\microsoft compatibility appraiser" /Enable   # re-enable the scheduled task
```

**Reversible via.** schtasks /Change /TN "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /Enable  (verify: schtasks /Query /TN "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /FO LIST)


### Microsoft Compatibility Appraiser (Exp)

`task:\microsoft\windows\application experience\microsoft compatibility appraiser exp` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** Same periodic CPU/disk scan cost as the main appraiser. Absent on many builds, in which case there is nothing to disable.

**What it does.** An experimental variant of the Compatibility Appraiser present on some newer Windows 11 builds. It performs the same compatibility/telemetry scan as the main appraiser task.

**How it helps.** Disabling it stops the experimental appraiser scan. No user-facing functionality is lost. If the task isn't present on your build, GamerGuardian simply reports it as not present.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Disabling it stops the experimental appraiser scan. No user-facing functionality is lost. If the task isn't present on your build, GamerGuardian simply reports it as not present. | Same as the main appraiser: only pre-update compatibility data collection is lost, and a feature update may re-enable it (Auto-apply re-disables). |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Same as the main appraiser: only pre-update compatibility data collection is lost, and a feature update may re-enable it (Auto-apply re-disables).

**Command line (PowerShell):**

```powershell
# Check the current value
schtasks /Query /TN "\microsoft\windows\application experience\microsoft compatibility appraiser exp" /XML   # <Settings><Enabled>false</Enabled> = disabled

# Apply the gaming-optimized value
schtasks /Change /TN "\microsoft\windows\application experience\microsoft compatibility appraiser exp" /Disable

# Reverse it (restore the Windows default)
schtasks /Change /TN "\microsoft\windows\application experience\microsoft compatibility appraiser exp" /Enable   # re-enable the scheduled task
```

**Reversible via.** schtasks /Change /TN "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp" /Enable  (verify: schtasks /Query /TN "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp" /FO LIST)


### PcaPatchDbTask

`task:\microsoft\windows\application experience\pcapatchdbtask` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** It contributes to the Application Experience scan load. The functional cost of disabling it is small: the PCA patch database stops refreshing.

**What it does.** A scheduled task that updates the Program Compatibility Assistant (PCA) patch database. Like StartupAppTask, it has a minor functional role rather than being pure telemetry.

**How it helps.** Disabling it stops PCA database refreshes and the associated background work. The Program Compatibility Assistant still runs; it just stops pulling new compatibility-shim data.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Disabling it stops PCA database refreshes and the associated background work. The Program Compatibility Assistant still runs; it just stops pulling new compatibility-shim data. | Minor: the Program Compatibility Assistant may apply fewer automatic app-compatibility shims for old software. A feature update may re-enable the task; Auto-apply re-disables it. Disable this one only if you want the whole Application Experience folder off. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Minor: the Program Compatibility Assistant may apply fewer automatic app-compatibility shims for old software. A feature update may re-enable the task; Auto-apply re-disables it. Disable this one only if you want the whole Application Experience folder off.

**Command line (PowerShell):**

```powershell
# Check the current value
schtasks /Query /TN "\microsoft\windows\application experience\pcapatchdbtask" /XML   # <Settings><Enabled>false</Enabled> = disabled

# Apply the gaming-optimized value
schtasks /Change /TN "\microsoft\windows\application experience\pcapatchdbtask" /Disable

# Reverse it (restore the Windows default)
schtasks /Change /TN "\microsoft\windows\application experience\pcapatchdbtask" /Enable   # re-enable the scheduled task
```

**Reversible via.** schtasks /Change /TN "\Microsoft\Windows\Application Experience\PcaPatchDbTask" /Enable  (verify: schtasks /Query /TN "\Microsoft\Windows\Application Experience\PcaPatchDbTask" /FO LIST)


### ProgramDataUpdater

`task:\microsoft\windows\application experience\programdataupdater` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** Pure background data collection with no user-facing function -- another contributor to the Application Experience scan load.

**What it does.** A scheduled task in the Application Experience pipeline that collects program-inventory data (which apps are installed and used) for compatibility telemetry.

**How it helps.** Disabling it stops the program-inventory telemetry collection and its background work. Nothing you interact with changes.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Disabling it stops the program-inventory telemetry collection and its background work. Nothing you interact with changes. | Microsoft loses program-inventory telemetry from your machine. A feature update may re-enable the task; Auto-apply re-disables it. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Microsoft loses program-inventory telemetry from your machine. A feature update may re-enable the task; Auto-apply re-disables it.

**Command line (PowerShell):**

```powershell
# Check the current value
schtasks /Query /TN "\microsoft\windows\application experience\programdataupdater" /XML   # <Settings><Enabled>false</Enabled> = disabled

# Apply the gaming-optimized value
schtasks /Change /TN "\microsoft\windows\application experience\programdataupdater" /Disable

# Reverse it (restore the Windows default)
schtasks /Change /TN "\microsoft\windows\application experience\programdataupdater" /Enable   # re-enable the scheduled task
```

**Reversible via.** schtasks /Change /TN "\Microsoft\Windows\Application Experience\ProgramDataUpdater" /Enable  (verify: schtasks /Query /TN "\Microsoft\Windows\Application Experience\ProgramDataUpdater" /FO LIST)


### StartupAppTask

`task:\microsoft\windows\application experience\startupapptask` &nbsp; **Recommended:** Disabled

**Why this is the recommendation.** It contributes to the periodic Application Experience scan. The functional cost of disabling it is small: Windows may not refresh startup-impact data shown in Task Manager's Startup tab.

**What it does.** A scheduled task that scans your startup apps for the Application Experience pipeline. Unlike the appraiser tasks, this one has a minor functional role (startup-app impact data), not pure telemetry.

**How it helps.** Disabling it stops the periodic startup-app scan. Your startup apps still launch normally; only the background scan and its data refresh stop.

**Pros & cons of each choice:**

| Choice | Pro | Con |
|---|---|---|
| Disable / remove it (recommended) | Disabling it stops the periodic startup-app scan. Your startup apps still launch normally; only the background scan and its data refresh stop. | Minor: Windows may show stale or missing 'startup impact' ratings for your startup apps. A feature update may re-enable the task; Auto-apply re-disables it. Disable this one only if you're comfortable with the whole Application Experience folder off. |
| Leave it as Windows ships it | The feature it backs keeps working exactly as before -- nothing to re-enable later. | Keeps the background work running, so you don't get the resource/idle saving above. |

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Minor: Windows may show stale or missing 'startup impact' ratings for your startup apps. A feature update may re-enable the task; Auto-apply re-disables it. Disable this one only if you're comfortable with the whole Application Experience folder off.

**Command line (PowerShell):**

```powershell
# Check the current value
schtasks /Query /TN "\microsoft\windows\application experience\startupapptask" /XML   # <Settings><Enabled>false</Enabled> = disabled

# Apply the gaming-optimized value
schtasks /Change /TN "\microsoft\windows\application experience\startupapptask" /Disable

# Reverse it (restore the Windows default)
schtasks /Change /TN "\microsoft\windows\application experience\startupapptask" /Enable   # re-enable the scheduled task
```

**Reversible via.** schtasks /Change /TN "\Microsoft\Windows\Application Experience\StartupAppTask" /Enable  (verify: schtasks /Query /TN "\Microsoft\Windows\Application Experience\StartupAppTask" /FO LIST)

