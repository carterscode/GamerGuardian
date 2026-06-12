# feat: complete Virtualization-Based Security (VBS) disable — plan

Status: **implemented** (this PR)

## Goal

A single toggle that disables the *entire* VBS stack — not just Memory Integrity — and
keeps it disabled against every known re-enablement vector, with honest detection of the
cases registry writes cannot fix (UEFI lock).

## Why the existing Memory Integrity toggle is not enough

`memintegrity` flips one value (`DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity\Enabled`).
VBS itself keeps running via any other enabled scenario:

- **Credential Guard** (`Lsa\LsaCfgFlags`, `Scenarios\CredentialGuard`) — enabled by default
  on domain-joined Windows 11 22H2+ Enterprise/Education machines (and Pro machines that
  previously ran it).
- **System Guard Secure Launch** (`Scenarios\SystemGuard`).
- **Kernel-mode Hardware-enforced Stack Protection** (`Scenarios\KernelShadowStacks`).
- **Windows Hello** (`Scenarios\WindowsHello`, 24H2+) — community-confirmed to keep VBS
  alive even when the master switch is 0.
- **Group Policy mirrors** under `SOFTWARE\Policies\Microsoft\Windows\DeviceGuard`
  override everything at every gpupdate.

## What "disabled completely" writes (one UAC prompt)

All writes are explicit `REG_DWORD 0` — **never deletes** — because Microsoft documents
that absent values are re-defaulted on feature updates ("Deleting these registry settings
may not disable Credential Guard. They must be set to a value of 0"), while explicit
zeros set before an upgrade survive it.

| Key | Values set to 0 |
|---|---|
| `HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard` | `EnableVirtualizationBasedSecurity`, `RequirePlatformSecurityFeatures`, `Mandatory`, `HypervisorEnforcedCodeIntegrity` |
| `...\DeviceGuard\Scenarios\<each>` | `Enabled` for HypervisorEnforcedCodeIntegrity, CredentialGuard, SystemGuard, KernelShadowStacks, WindowsHello **plus every other scenario subkey discovered at runtime** |
| `HKLM\SYSTEM\CurrentControlSet\Control\Lsa` | `LsaCfgFlags` |
| `HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard` | `EnableVirtualizationBasedSecurity`, `LsaCfgFlags`, `HypervisorEnforcedCodeIntegrity` |

Plus deletes (only when present) of the upgrade re-enable metadata `WasEnabledBy`,
`EnabledBootId`, `ChangedInBootCycle` from **every** scenario subkey. Presence of
`WasEnabledBy`/`EnabledBootId` counts as drift (they re-arm upgrade re-enablement);
`ChangedInBootCycle` is boot-cycle bookkeeping Windows may rewrite on its own, so it
is cleaned up during an apply but never treated as drift by itself.

Writing the Policies mirror intentionally greys out the Windows Security "Memory
integrity" toggle ("managed by your administrator") — that closes the Windows-Security-app
re-enable vector. Re-enabling through GamerGuardian removes the policy values again.

## Re-enable (DesiredOn = true)

Removes every explicit-disable marker (only where it currently holds the disable value, so
a real domain GPO of 1/2 is never fought), sets `EnableVirtualizationBasedSecurity = 1`,
and restores `Scenarios\HypervisorEnforcedCodeIntegrity\Enabled = 1` + `WasEnabledBy = 2`
(the "enabled by user" marker that keeps the Windows Security UI un-greyed).

## Interaction with the Memory Integrity toggle

VBS-off is a strict superset of Memory-Integrity-off, so the two monitors would fight over
the HVCI key (30s poll, alternating writes, UAC spam). Rules:

- `MemoryIntegrityMonitor.CheckDrift` yields nothing while VBS is monitored with
  DesiredOn=false — the VBS monitor owns the HVCI key in that state.
- VBS re-enable skips the HVCI restore when Memory Integrity is monitored with
  DesiredOn=false — the user's standalone HVCI-off choice wins.

## Deliberately out of scope

- **`bcdedit /set hypervisorlaunchtype off`** — repo precedent excludes bcdedit
  (docs/feat-gaming-optimize-plan.md), it breaks WSL2 / Docker / Windows Sandbox /
  Hyper-V / Windows Hello ESS, and it is not required: with every scenario zeroed, VBS
  stops at next reboot. Documented in Learn More as an optional manual step.
- **UEFI-locked VBS/Credential Guard** (`Locked=1` or `LsaCfgFlags=1`): clearing the
  firmware variable requires mounting the EFI partition, a bcdedit boot-sequence entry
  (`loadoptions DISABLE-LSA-ISO`; older DG_Readiness_Tool releases also passed
  `DISABLE-VBS`, which current Microsoft docs no longer list) and a *physical-presence*
  confirmation at boot — far too dangerous to automate from a tray app. The monitor
  detects the lock, flags it in the drift description, and Learn More carries
  Microsoft's documented opt-out procedure.
- **Windows Security "memory integrity is off" nag suppression** (`HvciKeyDismissed`) —
  cosmetic, and hiding security nags is against the app's transparency stance.

## Detection / verification

Drift compares **configured registry state only** — VBS can never stop without a reboot,
so comparing runtime state would make apply-verification impossible and would retry
forever. `RequiresReboot=true` drives the existing reboot prompt flow. The Learn More /
verify snippet gives users the authoritative runtime check:
`(Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard).VirtualizationBasedSecurityStatus`
(0 = off) — msinfo32's "a hypervisor has been detected" is *not* a VBS indicator.

## Known limitation

If a third-party tool deleted every DeviceGuard value outright, the monitor reads that as
"not explicitly disabled" (Current: On) even though VBS may not be running until the next
feature update re-defaults it. Registry-only detection cannot distinguish this; the verify
snippet (WMI) shows the truth.

## Touched files

- `Monitors/VbsMonitor.cs` (new) — snapshot record + pure compliance/ops functions
  (headless-testable) + CheckDrift/ReadCurrent/Apply.
- `Monitors/MemoryIntegrityMonitor.cs` — defer rule.
- `Services/ElevatedRegistry.cs` — `ApplyHklmBatch` / `BuildHklmBatch` (mixed add+delete,
  single elevation).
- `Models/AppConfig.cs` — `Global.Vbs` pref.
- `App.xaml.cs` — registration.
- `UI/SettingsWindow.xaml.cs` — Global gaming row + SyncIfUnmonitored.
- `Services/SettingDocs.cs`, `Services/SettingDocsCatalog.cs` — mechanism / apply /
  verify / Learn More.
- `Services/RecommendedPreset.cs` — doc comment (excluded from preset, like Memory
  Integrity: Vanguard requires HVCI since July 2024).
- Tests: `VbsMonitorTests` (new), `ElevatedRegistryTests`, `SettingDocsTests`,
  `SettingDocsCatalogTests`.
- Docs: README table, `docs/wiki/Settings-and-tabs.md`, regenerated
  `docs/SETTINGS-REFERENCE.md`.
