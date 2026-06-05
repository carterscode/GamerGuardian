# GamerGuardian settings reference

This document is **generated from [`SettingDocsCatalog.cs`](../src/GamerGuardian/Services/SettingDocsCatalog.cs)**. Edit the catalog, then run `GamerGuardian.exe --gen-docs` to regenerate. A unit test asserts that the committed file matches the catalog so they can't drift.

Every setting here is managed via the Settings window. Toggle **Monitor** to have GamerGuardian watch the value; toggle **Auto-apply silently** to have it auto-correct on drift. Both default off, so nothing changes until you opt in.

## Contents

**Global gaming + display**

- [Active Windows power plan](#active-windows-power-plan) (`powerplan`)
- [Display refresh rate](#display-refresh-rate) (`refresh`)
- [Display resolution](#display-resolution) (`resolution`)
- [Fullscreen optimizations (global)](#fullscreen-optimizations-global) (`fso`)
- [Game DVR background recording](#game-dvr-background-recording) (`gamedvr`)
- [Games multimedia task profile](#games-multimedia-task-profile) (`gamestask`)
- [Hardware-accelerated GPU Scheduling](#hardware-accelerated-gpu-scheduling) (`hags`)
- [HDR (High Dynamic Range)](#hdr-high-dynamic-range) (`hdr`)
- [Memory Integrity / VBS (Core Isolation)](#memory-integrity---vbs-core-isolation) (`memintegrity`)
- [Mouse "Enhance pointer precision"](#mouse-enhance-pointer-precision) (`mouseaccel`)
- [Network Throttling](#network-throttling) (`netthrottle`)
- [System Responsiveness](#system-responsiveness) (`sysresponse`)
- [USB Selective Suspend (global)](#usb-selective-suspend-global) (`usbsuspend`)
- [Variable Refresh Rate (DirectX)](#variable-refresh-rate-directx) (`vrr`)
- [Windows Game Mode](#windows-game-mode) (`gamemode`)

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

- [Microsoft Copilot (UWP)](#microsoft-copilot-uwp) (`ai.app:Microsoft.Copilot`)
- [Windows AI Copilot Provider](#windows-ai-copilot-provider) (`ai.app:Microsoft.Windows.Ai.Copilot.Provider`)
- [Windows AI Experience](#windows-ai-experience) (`ai.app:MicrosoftWindows.Client.AIX`)

**Windows services**

- [Agent Activation Runtime Service](#agent-activation-runtime-service) (`service:AarSvc`)
- [Connected User Experiences and Telemetry](#connected-user-experiences-and-telemetry) (`service:DiagTrack`)
- [Delivery Optimization](#delivery-optimization) (`service:DoSvc`)
- [Downloaded Maps Manager](#downloaded-maps-manager) (`service:MapsBroker`)
- [Retail Demo Service](#retail-demo-service) (`service:RetailDemo`)
- [Superfetch / SysMain](#superfetch---sysmain) (`service:SysMain`)
- [Windows AI Fabric Service](#windows-ai-fabric-service) (`service:WSAIFabricSvc`)
- [Windows Error Reporting Service](#windows-error-reporting-service) (`service:WerSvc`)
- [Windows Search](#windows-search) (`service:WSearch`)
- [Xbox Accessory Management](#xbox-accessory-management) (`service:XboxGipSvc`)
- [Xbox Live Auth Manager](#xbox-live-auth-manager) (`service:XblAuthManager`)
- [Xbox Live Game Save](#xbox-live-game-save) (`service:XblGameSave`)
- [Xbox Live Networking Service](#xbox-live-networking-service) (`service:XboxNetApiSvc`)


---

## Global gaming + display

### Active Windows power plan

`powerplan` &nbsp; **Recommended:** High Performance

**What it does.** The active Windows power scheme. Controls CPU throttling thresholds, sleep timers, hard-drive spindown, USB selective suspend, and dozens of other power-related defaults.

**Why you'd change it.** Balanced (the default) lets the OS dynamically scale CPU clocks to save power, which costs you a few ms of latency at the start of any CPU-bound burst. High Performance / Ultimate Performance keeps CPU clocks pegged at the top of the curve for predictable response.

**How it helps.** Eliminates CPU clock-ramp latency. First-frame and first-input responses feel snappier. Background tasks finish faster.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS / Streaming | High Performance or a tuned custom plan |
| Casual single-player on a desktop | High Performance |
| Laptop on battery | Balanced (saves power) |
| Laptop plugged in | High Performance |
| Idle workstation | Balanced (drops back to power-saving when idle) |

**Risks.** Higher idle power draw -- typically 10-30 W on desktop, more on high-end. Components run a few degrees warmer. Fan noise slightly higher. On laptops on battery: noticeably worse battery life.

**Reversible via.** powercfg /setactive SCHEME_BALANCED (or pick another plan from Settings > System > Power).


### Display refresh rate

`refresh` &nbsp; **Recommended:** Maximum supported

**What it does.** Per-display refresh rate. GamerGuardian's recommended target is the display's maximum supported rate at the current resolution. Backed by ChangeDisplaySettingsEx (DEVMODE.dmDisplayFrequency).

**Why you'd change it.** Higher refresh = lower input-to-photon latency and smoother motion. Windows sometimes silently drops the refresh rate after sleep, driver updates, or external display disconnects -- monitoring catches this.

**How it helps.** Keeps your display at its full rated refresh rate for both desktop and games (some games respect the desktop rate, some override).

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Any monitor above 60 Hz | Maximum supported -- always |
| 60 Hz display | Doesn't matter; 60 is your max |
| Multi-monitor with mixed rates | Maximum per display |
| Power-saving / laptop on battery | Consider Fixed at a lower rate to save power -- the cost is real on high-Hz panels |

**Risks.** Very low. Some VRR displays produce eye-noticeable flicker at certain refresh rates in dark scenes -- if you see it, try the next rate down.

**Reversible via.** Settings > System > Display > Advanced display > Choose a refresh rate.


### Display resolution

`resolution` &nbsp; **Recommended:** Don't enforce unless you have a specific reason

**What it does.** Per-display resolution. Optional preference -- only enforced when the user explicitly pins a resolution. Backed by ChangeDisplaySettingsEx.

**Why you'd change it.** Lets you pin a specific resolution per display. Useful for users who run games at the desktop resolution and want absolute stability against Windows occasionally changing it after driver updates.

**How it helps.** Catches the case where Windows downgrades you to a lower resolution after a display reconnect or driver update.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Single fixed-resolution setup | Pin to native resolution |
| Multiple display configurations (docked / undocked laptop) | Don't pin -- let Windows handle |
| Variable resolution gaming (different per game) | Don't pin |

**Risks.** Pinning can fight legitimate display changes (docking a laptop, plugging in a different monitor).

**Reversible via.** Uncheck 'Monitor this setting' for Resolution on the display tab.


### Fullscreen optimizations (global)

`fso` &nbsp; **Recommended:** On (Windows default)

**What it does.** A Windows feature that runs games requesting true Fullscreen Exclusive in a borderless-windowed mode wrapped by the DWM compositor. This lets the OS draw overlays (Win+G, notifications) on top of the game without an alt-tab.

**Why you'd change it.** FSO is a quality-of-life feature -- faster alt-tab, working overlays, no display-mode-change flicker. But true FSE can be marginally faster (lower input latency) for some titles, which is why some pros disable it globally.

**How it helps.** Disabling globally forces true FSE where the game supports it. Saves 1-2 frames of latency on some titles by skipping the DWM compositor pass.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS chasing every ms | Off (forces true FSE) |
| Casual single-player | On (default; better QoL) |
| Streaming + game | On (FSE breaks some capture modes -- Display capture, Game capture with anti-cheat) |
| Productivity | Doesn't matter |

**Risks.** Some games crash or render incorrectly without FSO. Some overlays (Discord, NVIDIA App) can't draw over true FSE. Alt-tab is slower / triggers a mode switch.

**Reversible via.** Delete GameDVR_FSEBehaviorMode and the related values from HKCU\System\GameConfigStore.


### Game DVR background recording

`gamedvr` &nbsp; **Recommended:** Off

**What it does.** Windows Game Bar's continuous rolling-buffer recording of the active game. While enabled, the OS encodes and buffers game video so you can press Win+Alt+G to save the last X seconds.

**Why you'd change it.** Continuous encoding is a constant tax on framerate and GPU. On older systems it's noticeable (5-10%). On modern GPUs the cost is small but nonzero. Most serious players already use NVIDIA App / OBS for clips and don't need the OS buffer.

**How it helps.** Frees the GPU's video encoder and removes a constant background overhead. Lets third-party capture tools claim the encoder exclusively (NVENC, AMD Re-Live, etc.).

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Off |
| Streaming + game | Off (use OBS / NVIDIA App for capture) |
| Casual single-player | Personal taste; leave On if you use Win+Alt+G clips |
| Productivity / not gaming | Off |

**Risks.** You lose the 'save last 30s' shortcut. Game Bar itself (overlay, FPS counter, performance widgets) still works.

**Reversible via.** Set HKCU\System\GameConfigStore\GameDVR_Enabled = 1 and HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR\AppCaptureEnabled = 1.


### Games multimedia task profile

`gamestask` &nbsp; **Recommended:** Gaming (boosted)

**What it does.** The Multimedia Class Scheduler Service (MMCSS) has named task profiles. The Games profile controls Priority, Scheduling Category, and SFIO Priority for processes that register against it. Most modern games register here when they call AvSetMmThreadCharacteristics("Games").

**Why you'd change it.** The Games profile defaults aren't the most aggressive Windows can do. Boosting them (Priority=2, Scheduling Category=High, SFIO Priority=High) gives game threads a stronger claim on CPU and I/O during contention.

**How it helps.** More consistent frame pacing on busy systems. Better behavior when streaming/encoding alongside the game. Lower 1% lows under contention.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Gaming (boosted) |
| Casual single-player | Gaming |
| Streaming + game | Gaming (OBS uses its own multimedia profile; doesn't conflict) |
| Productivity | Default |

**Risks.** Very low. Background tasks deprioritized slightly further -- in practice not observable on a system with any CPU headroom.

**Reversible via.** Restore default values for Priority / Scheduling Category / SFIO Priority under HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games.


### Hardware-accelerated GPU Scheduling

`hags` &nbsp; **Recommended:** On

**What it does.** Lets the GPU's own scheduling processor own VRAM allocation and command submission instead of the CPU-side Windows display driver. Requires a supported GPU (NVIDIA Pascal+ / AMD Polaris+) and a reboot to switch.

**Why you'd change it.** On supported GPUs it reduces CPU overhead per frame and can lower input latency. Required for DLSS Frame Generation and some other features that rely on GPU-managed queues.

**How it helps.** 1-5% framerate improvement in CPU-bound games. Smoother frame pacing under variable load. Enables modern GPU features that won't work without it.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | On -- especially helpful for CPU-bound titles like CS2 / Valorant |
| Streaming + game | On |
| Casual single-player | On |
| Productivity / not gaming | On (Windows 11 default) |
| Professional GPU work (rendering, ML) | Off -- some workloads prefer driver-side scheduling |

**Risks.** Rare driver instability on first-generation HAGS-supported GPUs. Some professional/emulation apps prefer it off. Toggle requires a reboot.

**Reversible via.** Set HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode = 1 and reboot.


### HDR (High Dynamic Range)

`hdr` &nbsp; **Recommended:** On (for HDR-capable displays where you watch HDR content)

**What it does.** Per-display HDR toggle. Enables 10-bit color depth, the wider Rec.2020 / DCI-P3 gamut, and PQ EOTF for HDR-capable monitors. Backed by the Windows DisplayConfig CCD API (the same API the OS Settings page uses).

**Why you'd change it.** HDR is genuinely better picture quality in supported games and movies -- but Windows is notorious for silently turning HDR off after sleep, driver updates, or display reconnects. Monitoring this catches the regression automatically.

**How it helps.** Keeps HDR enabled so games that detect it use HDR rendering paths. Catches silent OS regressions and auto-restores.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| HDR monitor, gaming/movies focus | On |
| HDR monitor, SDR-only content | Off (Windows SDR-in-HDR is often visually worse than native SDR) |
| SDR-only monitor | Doesn't matter; the toggle will be ignored |
| Multi-monitor with mixed HDR support | On for HDR displays only; per-display managed |

**Risks.** Some games look wrong in HDR (washed out, oversaturated) due to game-side tone mapping bugs -- a per-game preference. Windows SDR-in-HDR rendering is often visually worse than native SDR for desktop work.

**Reversible via.** Settings > System > Display > select the display > toggle HDR off.


### Memory Integrity / VBS (Core Isolation)

`memintegrity` &nbsp; **Recommended:** On (default)

**What it does.** Hypervisor-Enforced Code Integrity. Runs the Windows kernel inside a Hyper-V-protected memory region so unsigned or compromised kernel drivers can't write to protected code. Part of the broader Virtualization-Based Security stack.

**Why you'd change it.** Real security feature -- meaningfully reduces certain malware classes' ability to load kernel drivers. But the hypervisor's transitions cost CPU on every kernel call, which shows up as worse 1% lows in many games.

**How it helps.** Disabling can recover 5-15% framerate in CPU-bound games (especially 1% lows). On Ryzen, the win can be larger. Tradeoff is security: think hard before flipping this.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS where every percent matters | Off (accept the security tradeoff knowingly) |
| Casual / mixed-use | On -- security beats the framerate |
| Productivity | On |
| Streaming + game | On -- the difference under stream encoding load is minor |
| Anti-cheat-protected games | On -- Vanguard, BattlEye, EAC may refuse to launch with it off |

**Risks.** Major: reduced kernel-driver protection. Some anti-cheat (Riot Vanguard especially) requires it on. Some kernel-mode hardware (cheap KVMs, old drivers) won't load with it on -- that's the tradeoff in the other direction.

**Reversible via.** Set HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity\Enabled = 1 and reboot.


### Mouse "Enhance pointer precision"

`mouseaccel` &nbsp; **Recommended:** Off

**What it does.** A cursor acceleration curve applied to all mouse movement. Moving the mouse faster makes the cursor travel disproportionately further than the same distance moved slowly.

**Why you'd change it.** Breaks 1:1 muscle memory between mouse and cursor. Every competitive FPS disables acceleration in-game; mismatching the OS-level setting means your desktop pointer behaves differently from your in-game crosshair.

**How it helps.** Consistent 1:1 mouse-to-cursor mapping. Aim feels the same in-game and out of game. Easier to dial in pointer speed by DPI alone.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Off |
| Casual gaming | Off (just because consistency helps) |
| Productivity / office work | Default On is fine; acceleration helps with quick navigation across large displays |
| Touchscreen / pen / tablet primary | Doesn't matter |

**Risks.** Cursor feels 'slower' at low DPI when you first turn it off. Counter: bump your Mouse pointer speed slider or your DPI.

**Reversible via.** Settings > Mouse > Additional mouse settings > Pointer Options > re-check 'Enhance pointer precision'.


### Network Throttling

`netthrottle` &nbsp; **Recommended:** Disabled (FFFFFFFF)

**What it does.** Rate-limits outbound network packets during multimedia tasks to prevent network I/O from starving them. Default value 10 = throttled. FFFFFFFF (4294967295) = disabled.

**Why you'd change it.** For online games, this throttling can introduce micro-stutter in netcode. Removing it lets netcode run at full rate.

**How it helps.** Smoother online experience in competitive games. Removes a known source of input-to-server latency variability.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive online (CS2, Valorant, Apex, etc.) | Disabled |
| Casual online | Disabled |
| Single-player offline | Doesn't matter |
| Streaming | Disabled (your encoder paces itself) |

**Risks.** Very low. In theory multimedia apps could see slightly less reliable timing if your network is saturated -- in practice not observable.

**Reversible via.** Set HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\NetworkThrottlingIndex = 10.


### System Responsiveness

`sysresponse` &nbsp; **Recommended:** 10

**What it does.** Registry knob that reserves a percentage of CPU time for non-multimedia tasks. Default value 20 = 20% reserved. Lower = more CPU available for multimedia tasks (which includes games registered via MMCSS).

**Why you'd change it.** Drops the reservation from 20% to 10% so games tagged as multimedia get more CPU during contention.

**How it helps.** Tiny but measurable improvement on CPU-bound games. Most useful on lower-core-count CPUs where 20% is a lot of reserved time.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | 10 (gaming) |
| Pro audio | 0 (audio guides usually recommend 0; gives the audio scheduler full priority) |
| Casual gaming | 10 or default |
| Productivity | 20 (default) |

**Risks.** Very low at 10. At 0, rare audio glitches under sustained CPU load. Reboot is required for the value to take effect.

**Reversible via.** Set HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\SystemResponsiveness = 20.


### USB Selective Suspend (global)

`usbsuspend` &nbsp; **Recommended:** Disabled (for desktops)

**What it does.** Windows power feature that suspends idle USB devices to save power. The device wakes when Windows touches it again. Applies per-device but flipping this global flag disables the default-suspend behavior.

**Why you'd change it.** For HID devices (gaming mice, keyboards, headsets), the wake-from-suspend introduces a noticeable first-input delay -- the cursor pauses for a moment, the first keystroke after a long idle is dropped, or a USB headset pops.

**How it helps.** Eliminates the first-input lag on cold mouse/keyboard input. Removes random audio pops on cheap USB DACs/headsets that are sensitive to suspend cycles.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Desktop gaming PC | Disabled |
| Laptop on battery | Enabled -- the power saving matters more than first-input lag |
| Laptop plugged in / docked | Disabled |
| USB audio interface (streaming / recording) | Disabled |

**Risks.** Slightly higher idle power draw (typically 1-3 W). Negligible heat. On laptops, observably faster battery drain.

**Reversible via.** Set HKLM\SYSTEM\CurrentControlSet\Services\USB\DisableSelectiveSuspend = 0 and reboot.


### Variable Refresh Rate (DirectX)

`vrr` &nbsp; **Recommended:** On if you have VRR hardware

**What it does.** Windows Settings > Display > Graphics > Variable Refresh Rate. Tells Windows to expose VRR to DirectX games even when the game doesn't explicitly request it. NOT the same as Dynamic Refresh Rate (DRR) in Advanced Display, which scales refresh based on content.

**Why you'd change it.** Allows VRR (G-Sync / FreeSync) to work in games that don't have a VRR / G-Sync toggle of their own.

**How it helps.** Smooth frame delivery between the display's min and max refresh -- no tearing, no V-Sync input latency.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| VRR-capable display + supported GPU | On |
| Display without VRR | Doesn't matter -- no-op |
| Multi-monitor with one VRR display | On (Windows handles per-monitor) |
| Competitive FPS with V-Sync off as standard | On (still benefits from VRR-paced delivery up to the FPS cap) |

**Risks.** Very low. Some older driver+game combos can flicker -- if you see it, turn off in-game V-Sync, leave VRR on.

**Reversible via.** Delete VRROptimizeEnable from HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers.


### Windows Game Mode

`gamemode` &nbsp; **Recommended:** On (Windows default)

**What it does.** Windows 10/11 feature that tells the OS to prioritize the foreground app when it detects a game: CPU/GPU resources are biased toward the game, Windows Update reboots are deferred during gameplay, and background app push notifications are paused.

**Why you'd change it.** Game Mode is essentially free on modern Windows -- it's been the default since 1809. The only reason to think about it is if a specific game shows stuttering that goes away when Game Mode is off (rare, but documented for some GPU+driver combos).

**How it helps.** Small but measurable input-latency reduction on systems with background work happening. Suppresses Windows Update mid-game reboots.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | On -- no measurable downside; consistent frame pacing |
| Streaming + game | On -- but verify your encoder isn't being deprioritized (rare) |
| Casual single-player | On |
| Productivity / not gaming | Doesn't matter; Windows ignores Game Mode for non-game foreground apps |

**Risks.** Some users report frame-rate stuttering or capture glitches on specific GPU/driver/game combos. If you only see stuttering with Game Mode on, turn it off and re-test.

**Reversible via.** Set HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled = 1 (or delete the value).

## Windows AI policies

### Click-to-Do (Snipping Tool AI)

`ai.clicktodo` &nbsp; **Recommended:** Off

**What it does.** An AI action layer in the Snipping Tool. After capturing a screenshot, an AI button appears offering 'summarize this,' 'rewrite,' 'search the web for this,' etc. Setting Off writes DisableClickToDo in both the HKLM WindowsAI policy and the per-user HKCU Shell\ClickToDo key.

**Why you'd change it.** AI actions hit Microsoft cloud services. Removes a feature most users don't use anyway.

**How it helps.** Standard Snipping Tool screenshot functionality is completely unaffected; only the AI actions panel is hidden.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who doesn't use Click-to-Do | Off |
| Active Click-to-Do user | On |
| Privacy-conscious | Off |

**Risks.** None. You lose the AI actions panel from screenshots.

**Reversible via.** Delete DisableClickToDo from HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI and HKCU\Software\Microsoft\Windows\Shell\ClickToDo.


### Microsoft 365 Copilot in Word / Excel / OneNote

`ai.office` &nbsp; **Recommended:** Off

**What it does.** Disables the Copilot button + ribbon entries inside the desktop Word, Excel, and OneNote apps; also opts the machine out of Microsoft's AI model training on document contents (HKLM\Policies\office admin template).

**Why you'd change it.** Microsoft 365 Copilot is opt-in by license, but the UI affordances still show up in every Word document; disabling cleanly removes them. The training opt-out is a separate policy that prevents document text from being used to train Microsoft's models even if a user happens to invoke Copilot.

**How it helps.** No Copilot ribbon. No suggestions panel. No accidental cloud calls. No document-text contribution to model training.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Office user who doesn't have a Copilot license | Off -- the buttons are just dead weight |
| Office user with Copilot license, occasional use | On (or selectively per app) |
| Privacy-conscious / regulated workflows | Off |
| Office not installed | Doesn't matter -- the toggle is a no-op |

**Risks.** If you do have a Copilot license and want to use it, you lose the in-app entry points. Reverse by deleting the keys.

**Reversible via.** Delete EnableCopilot from HKCU\Software\Microsoft\Office\16.0\Word\Options and Excel\Options, CopilotEnabled from HKCU\...\OneNote\Options\Copilot, and disabletraining from HKLM\SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general.


### Microsoft Edge Copilot / Hubs / GenAI

`ai.edge` &nbsp; **Recommended:** Off

**What it does.** Three Edge enterprise policies flipped together: HubsSidebarEnabled (the always-present right-edge Copilot icon), CopilotPageContext (sending current-page contents to Copilot for processing), and GenAILocalFoundationalModelSettings (Edge's in-browser local generative AI).

**Why you'd change it.** Hides the persistent Copilot icon, blocks page contents from leaving the browser for AI processing, and disables in-browser AI generation.

**How it helps.** Cleaner Edge UI; less background AI activity in the browser; no accidental page-context shares with cloud AI.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious | Off |
| Anyone who doesn't actively use Edge Copilot | Off |
| Active Edge Copilot user | On |
| Enterprise environments | Whatever your IT policy says |

**Risks.** You lose Edge's built-in Copilot sidebar and AI features. Standard browsing is unaffected.

**Reversible via.** Delete HubsSidebarEnabled, CopilotPageContext, and GenAILocalFoundationalModelSettings from HKLM\SOFTWARE\Policies\Microsoft\Edge.


### Notepad Rewrite + Paint AI features

`ai.notepadpaint` &nbsp; **Recommended:** Off

**What it does.** Per-user disable of Notepad Rewrite, Paint Cocreator, Paint Image Creator, and Paint Generative Erase. Plus a per-user opt-out of Paint's experiment-targeting service and the HKLM machine-wide Paint policy that stops Image Creator from offering itself before per-user toggle. Combined HKCU + HKLM writes.

**Why you'd change it.** These features bolt cloud AI onto otherwise simple apps. Users who don't use the AI features may prefer Notepad and Paint without the buttons. v0.1.39 added the targeting opt-out + HKLM policy so the disable holds across new Paint experiments rolling out under feature flags.

**How it helps.** Notepad and Paint behave like classic versions; no AI action buttons; no cloud calls when you open a document or image; no opt-in prompts when MS rolls out new AI experiments.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who doesn't use AI in Notepad / Paint | Off |
| Active user of Paint Cocreator / Image Creator | On |
| Privacy-conscious | Off |

**Risks.** None. AI features disappear from those two apps.

**Reversible via.** Delete the registry values under HKCU\Software\Microsoft\Notepad (RewriteEnabled), HKCU\Software\Microsoft\Windows\CurrentVersion\Paint (DisableCocreator, DisableImageCreator, DisableGenerativeErase), HKCU\Software\Microsoft\Windows\CurrentVersion\Applets\Paint\View (IsSignedUpForTargetingService), and HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint (DisableImageCreator).


### Search box AI suggestions + taskbar companion

`ai.settingssearch` &nbsp; **Recommended:** Off

**What it does.** Three HKCU values: BingSearchEnabled=0 (the value Windows 11 actually honors for the AI/web suggestion layer in the search box -- this is the authoritative one), IsDynamicSearchBoxEnabled=0 (search highlights / the companion content), and the legacy DisableSearchBoxSuggestions=1 policy (best-effort -- unreliable on Win11). HKCU only -- no UAC.

**Why you'd change it.** The search box's AI suggestion layer calls Microsoft web endpoints to suggest answers as you type. The taskbar companion is a floating overlay some Windows 11 builds enable by default. Both are noise for users who use the search box for files and apps.

**How it helps.** Search box returns local files / apps only -- no web suggestions, no Copilot answers inline, no taskbar companion widget. Indexing itself (Start menu, Explorer, Outlook) is untouched.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who uses Windows Search for local files only | Off |
| Active user of search box web/Copilot suggestions | On |
| Privacy-conscious | Off |

**Risks.** You lose the web-suggestion layer and the taskbar companion. Search itself works exactly as before.

**Reversible via.** Delete BingSearchEnabled from HKCU\Software\Microsoft\Windows\CurrentVersion\Search, IsDynamicSearchBoxEnabled from HKCU\Software\Microsoft\Windows\CurrentVersion\SearchSettings, and DisableSearchBoxSuggestions from HKCU\SOFTWARE\Policies\Microsoft\Windows\Explorer (or set them back to 1 / 1 / absent).


### Typing / input insights data collection

`ai.inputinsights` &nbsp; **Recommended:** Off

**What it does.** Two HKCU settings that disable Windows' typing-data and ink-data harvesting: RestrictImplicitTextCollection (blocks the OS from saving the plain text you type for personalized suggestions) and InsightsEnabled (the per-user master switch in the Input settings panel). HKCU only -- no UAC.

**Why you'd change it.** By default, Windows builds a per-user typing model from text you've typed in apps. That data feeds personalized suggestions, autocorrect, and (in some Insider builds) AI features. Users who don't want their typing harvested can opt out at the OS level.

**How it helps.** Stops the OS from saving samples of what you type. Personalized typing suggestions degrade slightly (Windows falls back to the global suggestion model); everything else works as normal.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious | Off |
| Anyone who doesn't notice typing suggestions getting better over time | Off |
| Active user of personalized typing suggestions on a touch keyboard | On |

**Risks.** Typing suggestions become slightly less personalized over time. No effect on autocorrect or basic spell-check.

**Reversible via.** Delete RestrictImplicitTextCollection from HKCU\Software\Microsoft\InputPersonalization and InsightsEnabled from HKCU\Software\Microsoft\input\Settings.


### Windows AI Actions

`ai.actions` &nbsp; **Recommended:** Off

**What it does.** Windows' shell-level AI Actions surface (right-click "rewrite with AI / summarize / search the web for this" on selected text, images, etc.). Toggled via the FeatureManagement override hive -- two numeric feature IDs (1853569164 and 4098520719) get EnabledState = 1 (force-disabled).

**Why you'd change it.** AI Actions is a 24H2-era Windows feature that adds AI suggestions to right-click menus and similar surfaces. The FeatureManagement override is the documented kill switch (zoicware uses the same IDs).

**How it helps.** Right-click menus, image picker dialogs, and other shell surfaces stop showing AI action options. No cloud calls when you right-click an image or selected text.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who doesn't use AI right-click actions | Off |
| Active user of AI Actions | On |
| Privacy-conscious | Off |

**Risks.** You lose the AI options in right-click / image-context menus. The right-click menus themselves still work for everything else.

**Reversible via.** Delete EnabledState from HKLM\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\1853569164 and 4098520719. Feature IDs may change in future Windows builds -- if you see new AI Actions surfaces after a Windows Update, GamerGuardian's existing overrides will still hold for these two but new feature IDs would need a new monitor entry.


### Windows Copilot

`ai.copilot` &nbsp; **Recommended:** Off (GamerGuardian default for users who specifically open this tab)

**What it does.** The system-wide Copilot taskbar button and the Win+C keyboard shortcut. Setting Off writes the TurnOffWindowsCopilot policy in both HKLM and HKCU and hides the taskbar button.

**Why you'd change it.** Copilot calls Microsoft cloud endpoints, runs background processes, and consumes resources when invoked. Some users prefer not to send page or document context to cloud AI services.

**How it helps.** Removes the always-present taskbar button so it can't be invoked accidentally; blocks Win+C from launching it; prevents the policy from being unset by routine Windows configuration changes.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Privacy-conscious users | Off |
| Gaming setup | Off -- no benefit, removes one more background subsystem |
| Active Copilot user | On |
| Enterprise with separate compliance | Whatever your IT policy says |

**Risks.** None for performance. You lose access to Copilot if you change your mind -- toggle back on or delete the policy values to restore.

**Reversible via.** Delete TurnOffWindowsCopilot from HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot and HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot.


### Windows Recall + AI data analysis

`ai.recall` &nbsp; **Recommended:** Off

**What it does.** Recall captures snapshots of your screen every few seconds and indexes them with on-device AI so you can later search 'what was that thing I had open last Tuesday.' Currently rolling out on Copilot+ PCs (Snapdragon X, recent Intel Core Ultra, AMD Ryzen AI). Setting Off writes AllowRecallEnablement=0 and DisableAIDataAnalysis=1 in the HKLM WindowsAI policy key.

**Why you'd change it.** Two distinct concerns: (1) privacy -- continuous screen capture, even local-only, is a meaningful new surface; (2) performance -- the NPU and disk I/O have nonzero cost. The policy block stops new snapshotting; it does NOT delete existing snapshots.

**How it helps.** Stops Recall snapshotting at the policy level (Windows honors this without question, unlike a per-app toggle). Blocks the broader Windows AI Data Analysis surface that future features may opt into.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Anyone who doesn't actively want Recall | Off |
| Copilot+ PC user who specifically wants Recall | On (and also delete this app's policy block) |
| Privacy-conscious | Off |
| Gaming setup | Off |

**Risks.** None for security or stability. You lose Recall if you change your mind. Existing Recall snapshots are not deleted by this toggle -- to remove them, go to Settings > Privacy & security > Recall & snapshots > Delete all snapshots.

**Reversible via.** Delete AllowRecallEnablement and DisableAIDataAnalysis from HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI.

## Windows AI UWP packages

### Microsoft Copilot (UWP)

`ai.app:Microsoft.Copilot` &nbsp; **Recommended:** Remove (only after the system policy is set to Off)

**What it does.** The standalone Copilot UWP app that Windows installs alongside the system-wide Copilot integration. Hundreds of MB on disk.

**Why you'd change it.** If you've blocked Copilot via the system policy toggle above, the standalone app is dead weight. Removing it reclaims disk and removes the launcher entry.

**How it helps.** Reclaims disk space. No more Copilot app launcher in Start.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Already disabled Copilot policy | Remove |
| Active Copilot user | Don't remove |
| Worried Windows Update might re-provision it | Remove + tick Auto-apply silently |

**Risks.** Reinstalling requires the Microsoft Store. Windows Update may re-provision the app after major updates -- the AutoApply tick handles that.

**Reversible via.** Install 'Microsoft Copilot' from the Microsoft Store.


### Windows AI Copilot Provider

`ai.app:Microsoft.Windows.Ai.Copilot.Provider` &nbsp; **Recommended:** Remove (only after the system policy is Off)

**What it does.** Background provider package that backs the Windows AI Copilot surface (the in-OS Copilot integration, not the standalone app).

**Why you'd change it.** Pairs with the Copilot system policy block. With the policy off, the provider is unused.

**How it helps.** Removes the background provider; small reduction in installed-app surface.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Already disabled Copilot policy | Remove |
| Active Copilot user | Don't remove |
| Privacy-conscious | Remove |

**Risks.** Re-provisioned by Windows Update; tick AutoApply to keep it removed. Reinstall requires the Microsoft Store.

**Reversible via.** Install via Microsoft Store or wait for Windows Update to re-provision.


### Windows AI Experience

`ai.app:MicrosoftWindows.Client.AIX` &nbsp; **Recommended:** Remove if you don't use Windows AI features

**What it does.** AI Experience component shipped on Copilot+ PCs. Backs the AI settings panel and assorted shell AI integrations.

**Why you'd change it.** On non-Copilot+ PCs the component is often unused. On Copilot+ PCs, removing it deletes the AI settings UI.

**How it helps.** Reclaims disk; removes the AI settings panel from Settings.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Non-Copilot+ PC | Remove if you don't use any Windows AI |
| Copilot+ PC with Recall / Click-to-Do disabled | Remove |
| Active AI user on Copilot+ PC | Don't remove |

**Risks.** AI Settings panel disappears. Re-provisioned by Windows Update.

**Reversible via.** Install via Microsoft Store or wait for Windows Update to re-provision.

## Windows services

### Agent Activation Runtime Service

`service:AarSvc` &nbsp; **Recommended:** Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc

**What it does.** Per-user service that backs Windows AI agent activations -- the runtime Copilot voice, Cortana legacy hooks, and certain shell AI surfaces call into when they want to launch in the background.

**Why you'd change it.** Like WSAIFabricSvc, this service is paired with the Windows AI policy toggles. If you've disabled Copilot, Recall, etc. at the policy level, AarSvc has nothing useful to do; if any AI feature is still enabled, leave it on.

**How it helps.** Removes a per-user service backing AI features you've already disabled. Pairs naturally with WSAIFabricSvc + the Windows AI policy toggles.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc |
| Streaming + game | Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc |
| Casual single-player | Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc |
| Productivity / mixed-use | Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc |

**Risks.** Per-user services use a generated suffix on the actual service name (AarSvc_<hex>). GamerGuardian disables the template definition so every new per-user instance starts disabled, but existing user sessions may need a logoff/logon to pick up the change. If you re-enable any AI feature later, it will fail to launch until you re-enable this service.

**Reversible via.** Set-Service -Name AarSvc -StartupType Manual


### Connected User Experiences and Telemetry

`service:DiagTrack` &nbsp; **Recommended:** Disabled

**What it does.** Collects diagnostic and usage data and sends it to Microsoft. Always-on background sender.

**Why you'd change it.** Constant background CPU + network for telemetry you didn't ask for. Disabling is safe on consumer Windows.

**How it helps.** Removes a constant low-level background sender. Small CPU and bandwidth saving.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Microsoft loses diagnostic data from your machine. Rare reports of Windows Update issues in unusual configurations; never observed on a desktop with a normal update cadence.

**Reversible via.** Set-Service -Name DiagTrack -StartupType Automatic


### Delivery Optimization

`service:DoSvc` &nbsp; **Recommended:** Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc)

**What it does.** Peer-to-peer Windows Update downloads. Lets your PC download update bits from other LAN/Internet peers and lets your PC contribute uplink to other peers.

**Why you'd change it.** Background bandwidth use, both upload and download, that you didn't authorize per-update. Especially impactful on metered or asymmetric connections.

**How it helps.** Stops the bandwidth contribution entirely. Updates still install normally; they just come from Microsoft directly.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc) |
| Streaming + game | Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc) |
| Casual single-player | Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc) |
| Productivity / mixed-use | Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc) |

**Risks.** Slightly slower update downloads on networks with many other Windows PCs. None observable on a single-PC household.

**Reversible via.** Delete the DODownloadMode value from HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization.


### Downloaded Maps Manager

`service:MapsBroker` &nbsp; **Recommended:** Disabled

**What it does.** Background service that downloads and updates offline maps for the Windows Maps app.

**Why you'd change it.** If you never use the Maps app, this service does nothing useful and downloads map data you'll never look at.

**How it helps.** Cuts background disk I/O and reclaims a small amount of memory.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** If you do open the Maps app later, offline map functionality won't work until you re-enable.

**Reversible via.** Set-Service -Name MapsBroker -StartupType AutomaticDelayed


### Retail Demo Service

`service:RetailDemo` &nbsp; **Recommended:** Disabled

**What it does.** Supports the in-store retail demo mode for Windows.

**Why you'd change it.** Useless outside retail kiosks.

**How it helps.** Removes a useless service from the running list.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** None.

**Reversible via.** Set-Service -Name RetailDemo -StartupType Manual


### Superfetch / SysMain

`service:SysMain` &nbsp; **Recommended:** Default (leave on -- current Microsoft guidance)

**What it does.** Tracks app usage patterns and preloads code into RAM before you launch the app. On HDDs this provides large startup-time improvements; on NVMe SSDs the benefit is marginal.

**Why you'd change it.** Hotly debated. On NVMe systems with abundant RAM, the cost is minor and the benefit is small -- Microsoft now recommends leaving it on. On slower drives or tight-RAM systems, the I/O cost can be more visible than the prefetch benefit.

**How it helps.** Slightly lower idle disk I/O.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (leave on -- current Microsoft guidance) |
| Streaming + game | Default (leave on -- current Microsoft guidance) |
| Casual single-player | Default (leave on -- current Microsoft guidance) |
| Productivity / mixed-use | Default (leave on -- current Microsoft guidance) |

**Risks.** Disabling can slow first-launch of frequently-used apps. On HDDs the slowdown is severe.

**Reversible via.** Set-Service -Name SysMain -StartupType Automatic


### Windows AI Fabric Service

`service:WSAIFabricSvc` &nbsp; **Recommended:** Default (Manual) -- only Disable if you've also flipped the AI policy toggles

**What it does.** Backs the on-device AI runtime that Copilot+ features (Copilot, Recall, Click-to-Do) call into.

**Why you'd change it.** If you've disabled the Windows AI policy toggles in the Windows AI tab, the AI features won't be invoked and the service is unused.

**How it helps.** Removes a process backing AI features you've already disabled. Pairs naturally with the policy toggles in the Windows AI tab.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) -- only Disable if you've also flipped the AI policy toggles |
| Streaming + game | Default (Manual) -- only Disable if you've also flipped the AI policy toggles |
| Casual single-player | Default (Manual) -- only Disable if you've also flipped the AI policy toggles |
| Productivity / mixed-use | Default (Manual) -- only Disable if you've also flipped the AI policy toggles |

**Risks.** If you re-enable any AI feature later, it will fail to launch until you re-enable this service.

**Reversible via.** Set-Service -Name WSAIFabricSvc -StartupType Manual


### Windows Error Reporting Service

`service:WerSvc` &nbsp; **Recommended:** Disabled

**What it does.** Collects crash dumps and reports them to Microsoft.

**Why you'd change it.** If you don't send crash reports, the service has nothing useful to do.

**How it helps.** Removes background CPU spent on crash data collection.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Disabled |
| Streaming + game | Disabled |
| Casual single-player | Disabled |
| Productivity / mixed-use | Disabled |

**Risks.** Crash dump collection stops. If you ever need to share a crash report with Microsoft support, re-enable first.

**Reversible via.** Set-Service -Name WerSvc -StartupType Manual


### Windows Search

`service:WSearch` &nbsp; **Recommended:** Default (don't manage)

**What it does.** Indexes file contents, properties, and Start-menu app names. Powers Start search, Explorer search, and Outlook search.

**Why you'd change it.** Indexing is heavy on slow disks and during initial scan. On a fast NVMe with SSD-friendly index location, the cost is minor.

**How it helps.** Disabling stops indexing entirely. Start menu app search still works (uses a separate cache); file-content search degrades to slow scan.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (don't manage) |
| Streaming + game | Default (don't manage) |
| Casual single-player | Default (don't manage) |
| Productivity / mixed-use | Default (don't manage) |

**Risks.** Major: Start search becomes much worse, Explorer search slows to a crawl, Outlook search may stop working entirely. Only disable on machines where you never search.

**Reversible via.** Set-Service -Name WSearch -StartupType AutomaticDelayed


### Xbox Accessory Management

`service:XboxGipSvc` &nbsp; **Recommended:** Default (Manual -- don't manage)

**What it does.** Backs Xbox-branded accessories (Xbox One controllers, Elite Series 2, etc.) for updates and configuration.

**Why you'd change it.** If you don't use Xbox-branded controllers via the Xbox Accessories app, this service has nothing to do.

**How it helps.** Removes a constantly-running USB-watching service.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual -- don't manage) |
| Streaming + game | Default (Manual -- don't manage) |
| Casual single-player | Default (Manual -- don't manage) |
| Productivity / mixed-use | Default (Manual -- don't manage) |

**Risks.** Xbox Accessories app won't be able to update controllers or change controller profiles. Game-pad input itself works regardless (handled by xinput).

**Reversible via.** Set-Service -Name XboxGipSvc -StartupType Manual


### Xbox Live Auth Manager

`service:XblAuthManager` &nbsp; **Recommended:** Default (Manual)

**What it does.** Authentication broker for Xbox Live. Required by Microsoft Store games, Game Pass, and the Xbox app.

**Why you'd change it.** If you don't use Microsoft Store games or Game Pass, this service is unused.

**How it helps.** Removes a constantly-running auth-broker service.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) |
| Streaming + game | Default (Manual) |
| Casual single-player | Default (Manual) |
| Productivity / mixed-use | Default (Manual) |

**Risks.** Microsoft Store games and Game Pass titles will fail to launch (authentication error).

**Reversible via.** Set-Service -Name XblAuthManager -StartupType Manual


### Xbox Live Game Save

`service:XblGameSave` &nbsp; **Recommended:** Default (Manual)

**What it does.** Cloud save sync for Microsoft Store / Game Pass titles.

**Why you'd change it.** If you don't use Microsoft Store games or Game Pass, this service is unused.

**How it helps.** Removes a small background sync service.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) |
| Streaming + game | Default (Manual) |
| Casual single-player | Default (Manual) |
| Productivity / mixed-use | Default (Manual) |

**Risks.** Cloud saves stop syncing for affected titles.

**Reversible via.** Set-Service -Name XblGameSave -StartupType Manual


### Xbox Live Networking Service

`service:XboxNetApiSvc` &nbsp; **Recommended:** Default (Manual)

**What it does.** Multiplayer and networking glue for Microsoft Store games.

**Why you'd change it.** If you don't use Microsoft Store games online, this service is unused.

**How it helps.** Removes a small background service.

**Per-scenario recommendation:**

| Scenario | Setting |
|---|---|
| Competitive FPS | Default (Manual) |
| Streaming + game | Default (Manual) |
| Casual single-player | Default (Manual) |
| Productivity / mixed-use | Default (Manual) |

**Risks.** Microsoft Store multiplayer titles will fail to find lobbies or connect.

**Reversible via.** Set-Service -Name XboxNetApiSvc -StartupType Manual

