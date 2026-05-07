# Verification

GamerGuardian doesn't ask you to take its word for it. Six independent ways to confirm what it's doing:

## 1. Settings UI re-reads from the OS after Apply

The "Current" line under each setting card comes directly from the same Win32 / registry call any other tool would use. If that line changes after you click Apply, the change happened. If it doesn't, it didn't.

## 2. Apply Results window

The window that pops after every Apply (v0.1.18+) shows per setting:

- Before value, target value, and the **after** value (re-read from the OS post-apply)
- ✓ Verified or ✗ Failed
- The exact registry path or Win32 API used
- A copy-pasteable PowerShell command you can run yourself

## 3. External verification commands

Every setting reads/writes a documented Windows location.

| Setting | PowerShell |
|---|---|
| HAGS | `(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode).HwSchMode` |
| Memory Integrity | `(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name Enabled).Enabled` |
| Game Mode | `(Get-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled).AutoGameModeEnabled` |
| Game DVR | `(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled).GameDVR_Enabled` |
| Mouse precision | `(Get-ItemProperty 'HKCU:\Control Panel\Mouse' -Name MouseSpeed).MouseSpeed` |
| Fullscreen optimizations | `(Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode).GameDVR_FSEBehaviorMode` |
| VRR | `(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name VRROptimizeEnable).VRROptimizeEnable` |
| System Responsiveness | `(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name SystemResponsiveness).SystemResponsiveness` |
| Network Throttling | `(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name NetworkThrottlingIndex).NetworkThrottlingIndex` |
| USB Selective Suspend | `(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\USB' -Name DisableSelectiveSuspend).DisableSelectiveSuspend` |
| Games task profile | `Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'` |
| Power plan | `powercfg /getactivescheme` |
| Windows service start type | `sc qc <ServiceName>   # look for START_TYPE` |
| HDR / Refresh / Resolution | Settings → System → Display, or `dxdiag` |

## 4. The change log

`%APPDATA%\GamerGuardian\changes.log` — every applied change (manual or silent auto-apply) is appended with full detail of the actual registry write or API call. See [Logging](Logging) for the schema and worked examples.

## 5. The `--test` CLI flag

```powershell
GamerGuardian.exe --test
```

Writes every monitor's current readout to `%TEMP%\gamerguardian_selftest.txt` — same call paths the live UI uses, so the file is always in sync with what Settings shows. Useful for QA on weird hardware, for bug reports, or just to see exactly what the app reads.

## 6. The source

Every monitor is a single file under [`src/GamerGuardian/Monitors/`](https://github.com/carterscode/GamerGuardian/tree/main/src/GamerGuardian/Monitors) that does exactly one thing each. Read [`HagsMonitor.cs`](https://github.com/carterscode/GamerGuardian/blob/main/src/GamerGuardian/Monitors/HagsMonitor.cs), say, to see the full code that reads and writes HAGS — about 30 lines.

See [Source file reference](Source-file-reference) for a tour of the rest of the codebase.
