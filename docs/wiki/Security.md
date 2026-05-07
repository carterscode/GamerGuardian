# Security

GamerGuardian is a small, single-author project that asks for permission to write to your system registry. Trust is reasonable to question. Here's what's in place to make that trust verifiable rather than assumed.

## What the app does and doesn't do

**Does:**
- Reads documented Windows registry / Win32 settings (full mechanism list in [Logging](Logging))
- Writes those same settings when you click Apply or when auto-apply fires
- Spawns `reg.exe` and `sc.exe` with `Verb=runas` for HKLM writes — these prompt UAC, which is the standard Windows mechanism for a non-admin process to make a privileged change
- Hits the GitHub Releases API on startup (auto-update check), and downloads the installer to `%TEMP%` if you accept

**Does not:**
- Install kernel drivers
- Inject into other processes
- Open inbound network sockets (no listener, no IPC server)
- Send telemetry of any kind to GamerGuardian-controlled servers (the only outbound network traffic is to `api.github.com` for the update check and to `github.com` for the installer download)
- Modify Windows files in `C:\Windows\` or System32
- Touch user files outside `%APPDATA%\GamerGuardian\` and `%TEMP%\gamerguardian_*`
- Bundle any third-party DLLs other than what `dotnet publish --self-contained` produces (the .NET runtime + WPF + WPF-UI)

## How CI keeps the build trustworthy

Every push triggers a stack of checks. Click any badge in the [README](https://github.com/carterscode/GamerGuardian) to see live status.

| Check | What it does | Where |
|---|---|---|
| **CodeQL** | GitHub's SAST analyzer — scans every PR + the main branch for code-injection, deserialization, path-traversal, and other classic vuln patterns. Findings are gated on the Security tab. | [Security → Code scanning](https://github.com/carterscode/GamerGuardian/security/code-scanning) |
| **Dependabot** | Watches NuGet + Actions for vulnerable / outdated dependencies. Auto-opens PRs with the upgrade. | [Security → Dependabot](https://github.com/carterscode/GamerGuardian/security/dependabot) |
| **OpenSSF Scorecard** | Automated scoring against [OpenSSF best practices](https://github.com/ossf/scorecard) — branch protection, signed commits, pinned actions, vulnerable deps, etc. Score visible publicly. | [scorecard.yml](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/scorecard.yml) |
| **Secret scanning** | GitHub's built-in scan for accidentally-committed secrets. Always-on for public repos. | [Security → Secret scanning](https://github.com/carterscode/GamerGuardian/security/secret-scanning) |
| **`dotnet list package --vulnerable`** | Run as part of `release.yml` — fails the build if any transitive dependency has a known CVE. | [release.yml](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/release.yml) |

## Reproducibility

Every Release attaches:

- The installer `GamerGuardian-Setup-x.y.z.exe`
- The portable `GamerGuardian.exe`
- A `SHA256SUMS.txt` file with hashes of both

To verify a Release matches what CI built:

```powershell
# Download both the installer and SHA256SUMS.txt from the Release page, then:
Get-FileHash -Algorithm SHA256 GamerGuardian-Setup-x.y.z.exe
# Compare the output to the line in SHA256SUMS.txt.
```

To verify a Release matches what the source code would produce, build locally with the same `-p:Version` value and compare hashes — see [Build from source](Build-from-source). Note that `dotnet publish` output is mostly but not fully deterministic; the embedded build timestamp will differ. The portable EXE's payload (the actual code section) does match between local and CI builds at the same source commit.

## Reporting a vulnerability

If you've found a security issue, please **do not** open a public GitHub issue. Email **security@example.com** (or the address listed in [SECURITY.md](https://github.com/carterscode/GamerGuardian/blob/main/SECURITY.md)) with:

- A description of the issue and its impact
- Steps to reproduce
- Your name / handle for credit (optional)

Expect a first response within 48 hours. Severity-driven fix cadence — critical issues land before any other work.

## Code signing (roadmap)

GamerGuardian is **not** code-signed today, which is why Windows SmartScreen warns on first install. Code signing via the [SignPath Foundation](https://signpath.org/) (free for qualifying OSS projects — [apply here](https://signpath.org/apply)) is on the roadmap. Once enabled, the warning goes away and the app's identity is cryptographically tied to this repo. No timeline yet.

In the meantime, the SHA-256 verification above gives you a way to confirm what you downloaded matches what CI built.

## What you can do to spot-check

1. **Read the source.** The codebase is intentionally small — every file is documented in [Source file reference](Source-file-reference). Each monitor is ~30 lines.
2. **Run `--test`.** `GamerGuardian.exe --test` writes every read GamerGuardian does to `%TEMP%\gamerguardian_selftest.txt`. No writes happen — pure read pass.
3. **Watch network activity.** Run `netstat -bno` or use Wireshark while GamerGuardian is in tray. The only outbound connections you should see are to `api.github.com` on startup (or when you click Check now).
4. **Watch process activity.** Process Monitor (procmon) with a filter on `Process Name = GamerGuardian.exe` will show every file/registry/network operation.
5. **Cross-check the change log.** Every applied change in `changes.log` includes the exact registry path. Run the `Verify:` PowerShell line in a fresh terminal to confirm independently.
