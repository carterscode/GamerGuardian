# Logging

`%APPDATA%\GamerGuardian\changes.log` is the authoritative record of every registry write or API call GamerGuardian makes. It's plain text, append-only, and auto-rotates at ~1 MB to `changes.log.1`.

Open it from **Settings → General → Open change log**, or from the Apply Results window after any Apply.

## Format

Each record is tagged with a bracketed **line-type** so you can grep the log for exactly the kind of event you care about. An applied change is multi-line:

```
--------------------------------------------------------------------------------
[YYYY-MM-DD HH:MM:SS] [source    ] STATUS Description
  session      : <short id shared by every record in one Apply batch>
  settingId    : <machine identifier>
  location     : <registry path + value type, or API / DLL call>
  before       : <display value>  (<raw value>)
  desired      : <display value>  (<raw value>)
  after        : <display value>  (<raw value>)  <- verified | NOT VERIFIED
  applyCmd     : <PowerShell snippet that reproduces the change>
  verifyCmd    : <PowerShell snippet that reads the current value yourself>
  elapsedMs    : <apply duration in ms>
  reboot       : required to take effect       (only for reboot-required settings)
  trigger      : Windows externally reset this value; this entry is the corrective re-apply
  stickiness   : Windows has reverted this N time(s) this session
  error        : <exception message>            (only when Apply threw)
```

### Line types

Every record opens with a fixed-width bracketed tag. Grep for the tag in brackets to isolate one kind of event.

| Tag | Emitted when |
|---|---|
| `[SESSION   ]` | App start. A header block with app version, OS build, CLR, machine, user + elevation, PID, and config path. |
| `[SNAPSHOT  ]` | A one-line-per-setting baseline of every monitored setting's current vs. desired value. Written on session start and from the UI "Verify all" button. |
| `[APPLY-START]` / `[APPLY-END  ]` | Bracket an Apply batch. START carries `session`, `source`, and `count`; END carries `verified=N/total` and `totalMs`. |
| *(per-change record)* | One multi-line block per applied setting inside a batch, tagged with the `source` (`[manual    ]`, `[auto      ]`, `[auto-revert]`). |
| `[PREF-STAGE]` | A preference toggle (Monitor / AutoApply / Want) staged in the Settings UI draft — not yet applied. |
| `[PAUSE     ]` | Polling paused or resumed (fullscreen, benchmark, or manual pause). |
| `[MEM       ]` | A working-set / private-memory snapshot, written on each periodic memory trim. |
| `[EXTRESET  ]` | Windows (or another tool) reverted a value the app had previously applied and verified. |
| `[CIRCUIT   ]` | The auto-apply circuit breaker tripped: Windows kept reverting a setting, so re-applying it is suspended for a cooldown. |

Fields on a per-change record:

| Field | Meaning |
|---|---|
| **timestamp** | Local time of the apply |
| **source** | `manual` (you clicked Apply) · `auto` (silent auto-apply during a poll tick) · `auto-revert` (corrective re-apply after Windows externally reset the value) · `ui` / preset (preference toggle in Settings, emitted as a `[PREF-STAGE]` line) · `pause` (polling paused/resumed for fullscreen, benchmark, or manual pause) |
| **STATUS** | `OK` if the after-read matched the target; `FAILED` otherwise; `ERROR` if Apply threw |
| **session** | Short id shared by every record in one Apply batch, so you can group a batch with grep |
| **settingId** | The machine identifier for the setting |
| **location** | The exact registry path + value type, or the Win32 / DLL function called for non-registry settings |
| **before** | The value GamerGuardian read before the write — display form, with the raw value parenthesized when it differs |
| **desired** | The value GamerGuardian asked the OS to set |
| **after** | What GamerGuardian saw when it re-read the value post-apply. Should equal **desired** for verified entries. |
| **applyCmd** | A PowerShell one-liner that reproduces the change |
| **verifyCmd** | A PowerShell one-liner you can paste into a fresh terminal to read the same value yourself |
| **elapsedMs** | How long the apply took |
| **reboot** | Present only when the setting needs a Windows restart to actually take effect |
| **stickiness** | How many times Windows has reverted this same value this session |
| **error** | Present only when Apply threw (e.g. user denied UAC) |

## Worked examples

### Manual Apply that flipped USB Selective Suspend to gaming-optimized

```
--------------------------------------------------------------------------------
[2026-05-06 22:14:08] [APPLY-START] session=a3f1  source=manual  count=1
--------------------------------------------------------------------------------
[2026-05-06 22:14:08] [manual    ] OK     USB Selective Suspend (global override)
  session      : a3f1
  settingId    : usb-selective-suspend
  location     : HKLM\SYSTEM\CurrentControlSet\Services\USB\DisableSelectiveSuspend (DWORD)
  before       : Default  (0)
  desired      : Disabled (gaming)  (1)
  after        : Disabled (gaming)  (1)  <- verified
  applyCmd     : Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend -Value 1
  verifyCmd    : (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend).DisableSelectiveSuspend
  elapsedMs    : 412
  reboot       : required to take effect
--------------------------------------------------------------------------------
[2026-05-06 22:14:08] [APPLY-END  ] session=a3f1  verified=1/1  totalMs=412
```

### Silent auto-apply that fixed Game Mode after a Windows update reset it

```
--------------------------------------------------------------------------------
[2026-05-07 09:14:33] [auto      ] OK     Windows Game Mode
  session      : 91bc
  settingId    : game-mode
  location     : HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled / AllowAutoGameMode
  before       : Off  (AutoGameModeEnabled=0, AllowAutoGameMode=0)
  desired      : On  (AutoGameModeEnabled=1, AllowAutoGameMode=1)
  after        : On  (AutoGameModeEnabled=1, AllowAutoGameMode=1)  <- verified
  verifyCmd    : (Get-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled).AutoGameModeEnabled
  elapsedMs    : 38
```

### Pause / resume cycle while a fullscreen game runs

```
[2026-05-06 19:42:01] [PAUSE     ] PAUSED  fullscreen (Cyberpunk2077)
[2026-05-06 21:18:14] [PAUSE     ] RESUMED was: fullscreen (Cyberpunk2077)
```

The action is left-padded to 7 characters, so `PAUSED` and `RESUMED` line up.

### Preference toggle staged in the Settings UI

These are staged-only — the actual change isn't written until you click Apply or Save & close (which produces the multi-line `[APPLY-START]` … `[APPLY-END  ]` block above).

```
[2026-05-06 22:42:15] [PREF-STAGE] Fullscreen optimizations (global)  |  Monitor: False -> True
[2026-05-06 22:42:16] [PREF-STAGE] Fullscreen optimizations (global)  |  AutoApply: False -> True
```

### Session header written once per app start

```
--------------------------------------------------------------------------------
[2026-05-06 22:14:00] [SESSION   ] GamerGuardian v0.1.39
  OS         : Microsoft Windows NT 10.0.26200.0  (Win32NT)
  CLR        : .NET 8.0.6
  Machine    : DESKTOP-GAMER
  User       : Carter  (elevated: False)
  PID        : 18244
  ConfigPath : C:\Users\Carter\AppData\Roaming\GamerGuardian\config.json
```

### State snapshot of every monitored setting

```
--------------------------------------------------------------------------------
[2026-05-06 22:14:01] [SNAPSHOT  ] 2 monitored setting(s)
    OK  game-mode                       current=On  desired=On  (Windows Game Mode)
  DRIFT  usb-selective-suspend           current=Default  desired=Disabled (gaming)  (USB Selective Suspend)
  -- summary: 1 in sync, 1 drifting
```

### Windows externally reset a value the app had previously applied

`[EXTRESET  ]` is written when a setting GamerGuardian had applied and verified drifts again — i.e. Windows (or another tool) silently restored it. It records the value that was last applied, how long it held, the current value, and how many times the same setting has been reverted this session.

```
--------------------------------------------------------------------------------
[2026-05-07 11:02:48] [EXTRESET  ] Windows Game Mode
  settingId    : game-mode
  lastApplied  : On  (held for 1h12m)
  currentValue : Off
  stickiness   : Windows has reverted this setting 3 time(s) since app start
  next         : will silently restore
```

The `next` line tells you what happens on the next poll: `will silently restore` when Auto-apply is on for that setting, or `no auto-apply (notify only)` when it isn't.

### Auto-apply circuit breaker tripped

`[CIRCUIT   ]` is written when Windows keeps reverting a setting so fast that re-applying it every poll was interrupting input (e.g. spawning a UAC prompt or reconfiguring the display). GamerGuardian suspends auto-applying that one setting for a cooldown, then retries once.

```
--------------------------------------------------------------------------------
[2026-05-07 11:09:20] [CIRCUIT   ] auto-apply suspended: HDR / Auto HDR
  settingId    : auto-hdr
  reason       : Windows reverted this 3 time(s) in a row -- re-applying every poll was interrupting input
  cooldown     : not auto-applied for 15m0s; will retry once after that
  action       : leave it (notify-only), or untick Auto-apply for this setting in Settings
```

### Failed Apply where the user cancelled the UAC prompt

```
--------------------------------------------------------------------------------
[2026-05-07 09:18:02] [manual    ] FAILED Memory Integrity (Core Isolation) — requires reboot
  session      : c0de
  settingId    : memintegrity
  location     : HKLM\SYSTEM\...\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity\Enabled (DWORD)
  before       : On  (1)
  desired      : Off  (0)
  after        : On  (1)  <- NOT VERIFIED
  elapsedMs    : 1180
  reboot       : required to take effect
  verifyCmd    : (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled).Enabled
```

The `<- NOT VERIFIED` marker plus matching before/after tells you the write didn't land. For HKLM-write settings, the most common cause is a UAC cancel.

## Error log

`%TEMP%\gamerguardian_error.log` captures unhandled exceptions. It's typically empty in normal use. If you hit an issue worth filing, attach this file to the GitHub issue.
