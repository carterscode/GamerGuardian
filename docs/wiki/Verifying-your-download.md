# Verifying your download

Every GamerGuardian release ships with three independent integrity mechanisms. Any one of them is sufficient on its own; together they make tampering with a downloaded binary practically impossible to hide.

You don't need all three to feel safe — most users are fine with the VirusTotal link. The SHA-256 and SLSA paths are for users who want cryptographic proof rather than antivirus consensus.

## 1. VirusTotal scan (easiest)

Every release runs the installer and the portable EXE through [VirusTotal](https://www.virustotal.com/) — that's ~70 antivirus engines plus reputation/sandbox heuristics. The per-engine results URL is appended to each release's notes automatically by the [release workflow](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/release.yml).

**To check:** open the release page (e.g., `https://github.com/carterscode/GamerGuardian/releases/tag/v0.1.35`), scroll to the *VirusTotal scans* section, click the link for the file you downloaded.

**What you'll see:** detection count out of total engines, plus per-engine output. A small number of false positives (1–3 of 70+) is normal for unsigned Windows installers — heuristic engines flag *any* unsigned executable that touches the registry.

## 2. SHA-256 checksum (proves byte-identical to what GitHub built)

Every release ships a `SHA256SUMS.txt` asset alongside the installer + portable. The hashes inside are calculated at build time by the release workflow.

**To check** (PowerShell):

```powershell
# Download both the installer and SHA256SUMS.txt from the release page.
Get-FileHash -Algorithm SHA256 GamerGuardian-Setup-x.y.z.exe
# Compare the output to the line in SHA256SUMS.txt.
```

The hashes match → your download is byte-identical to what was built and uploaded by GitHub Actions. They don't match → re-download (could be transit corruption) or report it.

You can also cross-reference GitHub's own digest of each release asset, which appears in the GitHub API and on the release page itself. If your SHA256SUMS, GitHub's API digest, and your local Get-FileHash all agree, you have very high confidence.

## 3. SLSA Build Provenance attestation (proves the binary came from this repo's CI)

Every release attaches an SLSA Build Provenance attestation, signed via Sigstore OIDC and published to GitHub's transparency log. It cryptographically ties the binary to:

- The exact source commit on `main` it was built from
- The exact GitHub Actions workflow file that built it
- The runner that produced it

**To check** (requires the [GitHub CLI](https://cli.github.com/)):

```powershell
gh attestation verify GamerGuardian-Setup-x.y.z.exe --owner carterscode
```

Output looks like:

```
Loaded digest sha256:<hash> for file://GamerGuardian-Setup-x.y.z.exe
Loaded 1 attestation from GitHub API
✓ Verification succeeded!

The following policy criteria were verified:
- SLSA Predicate Type: https://slsa.dev/provenance/v1
- Source Repository: https://github.com/carterscode/GamerGuardian
- Source Ref: refs/heads/main
- Builder ID: https://github.com/carterscode/GamerGuardian/.github/workflows/release.yml@refs/heads/main
```

If verification fails, the binary was either tampered with or built outside this repo's CI — either way, don't run it.

## Why three?

Each mechanism catches a different attacker:

| Mechanism | Detects | Doesn't detect |
|---|---|---|
| VirusTotal | Known malware signatures, behavior heuristics | Novel attacks, supply-chain compromises pre-AV-signature |
| SHA-256 checksum | Tampering after upload | An attacker who controls the release pipeline (could match SHA256SUMS to malicious binary) |
| SLSA provenance | An attacker who controls the release pipeline (Sigstore signature would mismatch) | A bug in the source code itself (provenance only attests to "this code was built faithfully") |

All three fail to detect a compromised maintainer who pushes malicious code through the normal flow — but that's what the public source review, [CodeQL static analysis](https://github.com/carterscode/GamerGuardian/security/code-scanning), and small per-monitor file size (~30 lines, easy to audit) are for.

## See also

- [Security](Security) — full security posture including CI hardening and supply-chain controls
- [Architecture rationale](Architecture-rationale) — why GamerGuardian is user-mode P/Invoke and not a kernel driver, what trust boundaries this implies
- [SECURITY.md](https://github.com/carterscode/GamerGuardian/blob/main/SECURITY.md) — disclosure policy if you find an issue
