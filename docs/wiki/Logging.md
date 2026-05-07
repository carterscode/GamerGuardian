# Logging

`%APPDATA%\GamerGuardian\changes.log` is the authoritative record of every registry write or API call GamerGuardian makes. It's plain text, append-only, and auto-rotates at ~1 MB to `changes.log.1`.

Open it from **Settings → General → Open change log**, or from the Apply Results window after any Apply.

## Format

Each entry is multi-line:

```
--------------------------------------------------------------------------------
[YYYY-MM-DD HH:MM:SS] [source] STATUS Description
  Mechanism : <registry path or API call>
  Before    : <raw before value>  (<friendly description>)
  Wrote     : <raw target value>  (<friendly description>)
  After     : <raw after value>  (<friendly description>)  <- verified | NOT VERIFIED
  Reboot    : required to take effect       (only for reboot-required settings)
  Verify    : <PowerShell snippet to read the same value yourself>
```

Fields:

| Field | Meaning |
|---|---|
| **timestamp** | Local time of the apply |
| **source** | `manual` (you clicked Apply) · `auto` (silent auto-apply during a poll tick) · `ui` (preference toggle in Settings) · `pause` (polling paused/resumed for fullscreen, benchmark, or manual pause) |
| **STATUS** | `OK` if the after-read matched the target; `FAILED` otherwise (UAC declined, registry locked, etc.) |
| **Mechanism** | The exact registry path + value type, or the Win32 / DLL function called for non-registry settings |
| **Before** | The raw bytes / number / string GamerGuardian read before the write, plus a parenthesized friendly description |
| **Wrote** | The raw value GamerGuardian asked the OS to set |
| **After** | What GamerGuardian saw when it re-read the value post-apply. Should equal **Wrote** for verified entries. |
| **Reboot** | Present only when the setting needs a Windows restart to actually take effect |
| **Verify** | A PowerShell one-liner you can paste into a fresh terminal to read the same value yourself |

## Worked examples

### Manual Apply that flipped USB Selective Suspend to gaming-optimized

```
--------------------------------------------------------------------------------
[2026-05-06 22:14:08] [manual] OK     USB Selective Suspend (global override)
  Mechanism : HKLM\SYSTEM\CurrentControlSet\Services\USB\DisableSelectiveSuspend (DWORD)
  Before    : 0  (Default)
  Wrote     : 1  (Disabled (gaming))
  After     : 1  (Disabled (gaming))  <- verified
  Reboot    : required to take effect
  Verify    : (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend).DisableSelectiveSuspend
```

### Silent auto-apply that fixed Game Mode after a Windows update reset it

```
--------------------------------------------------------------------------------
[2026-05-07 09:14:33] [auto  ] OK     Windows Game Mode
  Mechanism : HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled / AllowAutoGameMode
  Before    : AutoGameModeEnabled=0, AllowAutoGameMode=0  (Off)
  Wrote     : AutoGameModeEnabled=1, AllowAutoGameMode=1  (On)
  After     : AutoGameModeEnabled=1, AllowAutoGameMode=1  (On)  <- verified
  Verify    : (Get-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled).AutoGameModeEnabled
```

### Pause / resume cycle while a fullscreen game runs

```
[2026-05-06 19:42:01] [pause ] PAUSED  fullscreen (Cyberpunk2077)
[2026-05-06 21:18:14] [pause ] RESUMED was: fullscreen (Cyberpunk2077)
```

### Preference toggle in the Settings UI

```
[2026-05-06 22:42:15] [ui    ] PREF   Fullscreen optimizations (global)  |  Monitor: False -> True
[2026-05-06 22:42:16] [ui    ] PREF   Fullscreen optimizations (global)  |  AutoApply: False -> True
```

### Failed Apply where the user cancelled the UAC prompt

```
--------------------------------------------------------------------------------
[2026-05-07 09:18:02] [manual] FAILED Memory Integrity (Core Isolation) — requires reboot
  Mechanism : HKLM\SYSTEM\...\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity\Enabled (DWORD)
  Before    : 1  (On)
  Wrote     : 0  (Off)
  After     : 1  (On)  <- NOT VERIFIED
  Reboot    : required to take effect
  Verify    : (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled).Enabled
```

The `<- NOT VERIFIED` marker plus matching Before/After tells you the write didn't land. For HKLM-write settings, the most common cause is a UAC cancel.

## Error log

`%TEMP%\gamerguardian_error.log` captures unhandled exceptions. It's typically empty in normal use. If you hit an issue worth filing, attach this file to the GitHub issue.
