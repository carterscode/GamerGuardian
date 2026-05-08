# OpenSSF Scorecard

[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/carterscode/GamerGuardian/badge)](https://scorecard.dev/viewer/?uri=github.com/carterscode/GamerGuardian)

The [OpenSSF Scorecard](https://github.com/ossf/scorecard) is an automated tool that evaluates open-source projects against ~18 security best-practice checks. It's run on this repo by [a scheduled GitHub Actions workflow](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/scorecard.yml) and the result is publicly browsable at [scorecard.dev](https://scorecard.dev/viewer/?uri=github.com/carterscode/GamerGuardian).

This page explains what each check measures, what GamerGuardian's current score is, and why — including the structural ceilings that a single-author Windows desktop project hits.

## How the score works

Each check yields a score in `[0, 10]` (or `-1` for "couldn't be assessed"). The overall score is a weighted average; weights vary by criticality (Token-Permissions, Vulnerabilities, etc. weigh more than nice-to-haves like Fuzzing).

For a small solo-author OSS Windows project, **the realistic ceiling is around 8.5–9.0/10**. Several checks are structurally unreachable for solo projects (Code-Review needs external approvers; Contributors needs multiple orgs; Fuzzing is N/A for GUI apps). That's worth keeping in mind when comparing scores against large multi-org projects like Kubernetes.

## Current state

As of the most recent scan (~7.0/10):

| Check | Score | What it means here |
|---|---:|---|
| [Token-Permissions](#token-permissions) | 10/10 | All workflows declare minimal `permissions:`; write scopes only at job level when needed |
| [Pinned-Dependencies](#pinned-dependencies) | 10/10 | All 21 GitHub Actions are pinned to commit SHAs; Dependabot keeps them updated |
| [Vulnerabilities](#vulnerabilities) | 10/10 | No known unpatched vulnerabilities; `dotnet list --vulnerable` gates every release |
| [SAST](#sast) | 10/10 | CodeQL on every PR + push to main + weekly cron |
| [CI-Tests](#ci-tests) | 10/10 | Build + xUnit tests run on every PR via required status checks |
| [Dangerous-Workflow](#dangerous-workflow) | 10/10 | No script-injection or untrusted-input patterns |
| [Security-Policy](#security-policy) | 10/10 | `SECURITY.md` at repo root with private-channel disclosure flow |
| [Binary-Artifacts](#binary-artifacts) | 10/10 | No checked-in EXEs, DLLs, or other binaries |
| [License](#license) | 10/10 | MIT, properly declared |
| [Dependency-Update-Tool](#dependency-update-tool) | 10/10 | Dependabot configured for NuGet + Actions |
| [CII-Best-Practices](#cii-best-practices) | 5/10 | OpenSSF Best Practices Badge in progress (Passing tier) |
| [Branch-Protection](#branch-protection) | -1 | Workflow's default token can't read protection rules; needs `SCORECARD_TOKEN` PAT |
| [Signed-Releases](#signed-releases) | 0/10 | Climbing — recent releases (v0.1.31+) ship SLSA Build Provenance; check looks at last 5 |
| [Maintained](#maintained) | 0/10 | Repo < 90 days old; auto-fixes on its own |
| [Packaging](#packaging) | -1 | Heuristic doesn't recognize Inno Setup / EXE distribution |
| [Code-Review](#code-review) | 0/10 | Solo dev; no external PR approvals |
| [Fuzzing](#fuzzing) | 0/10 | N/A for a Windows tray app — no untrusted parsing surface |
| [Contributors](#contributors) | 0/10 | Single contributor, single organization |

## Per-check explanations

### Token-Permissions

**What it measures:** every workflow declares an explicit `permissions:` block, ideally with default `read` and only specific jobs using `write`. The default `GITHUB_TOKEN` would otherwise have broad write permissions, which is a privilege-escalation risk if a workflow is ever exploited.

**This project:** workflows declare top-level `contents: read`. The release job adds `contents: write` (to create the release tag) and `id-token: write` + `attestations: write` (to sign SLSA provenance via Sigstore). Nothing more.

### Pinned-Dependencies

**What it measures:** every external GitHub Action is pinned to a 40-character commit SHA, not a mutable tag like `@v4` or `@main`. A compromised maintainer can't repoint a tag mid-flight.

**This project:** all 21 Action references are SHA-pinned with a trailing `# vX.Y.Z` comment for readability. Dependabot watches them and proposes updates when new versions ship.

### Vulnerabilities

**What it measures:** no known-vulnerable transitive dependency in the source tree. Scorecard cross-references against [OSV](https://osv.dev/) and other databases.

**This project:** `release.yml` runs `dotnet list package --vulnerable --include-transitive` as a release gate — if any vulnerable dep is detected, the release fails. Plus Dependabot opens PRs for vulnerability advisories within days of disclosure.

### SAST

**What it measures:** static analysis runs on every commit / PR with a security-focused query suite.

**This project:** [CodeQL](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/codeql.yml) runs the `security-and-quality` query suite on every push to main, every PR, and weekly via cron.

### CI-Tests

**What it measures:** PRs are gated on a passing CI run.

**This project:** branch protection on `main` requires `build`, `Analyze (csharp)`, and `Analyze (actions)` checks before merge. The `build` job runs `dotnet build` + `dotnet test` (40 xUnit tests).

### Dangerous-Workflow

**What it measures:** no GitHub Actions patterns that allow script injection (e.g., `${{ github.event.issue.title }}` directly inside a `run:` block — an attacker can put `; rm -rf /` in an issue title).

**This project:** no untrusted input flows directly into shell commands. The release version computation reads from git refs, not from PR titles or issue bodies.

### Security-Policy

**What it measures:** repo has a `SECURITY.md` with vulnerability-reporting instructions.

**This project:** [`SECURITY.md`](https://github.com/carterscode/GamerGuardian/blob/main/SECURITY.md) at repo root, points to GitHub Security Advisories for private reports, commits to a 48-hour acknowledgement SLA.

### Binary-Artifacts

**What it measures:** no committed binary blobs (EXEs, DLLs, JARs, etc.) — those make supply-chain auditing harder because users can't trace them back to source.

**This project:** `.gitignore` excludes `bin/`, `obj/`, `publish/`, `installer/Output/`, etc. Release artifacts are produced by CI from source on every release; nothing pre-built is checked in.

### License

**What it measures:** repo has a recognized open-source license file at the root.

**This project:** [MIT](https://github.com/carterscode/GamerGuardian/blob/main/LICENSE) in `LICENSE` at repo root.

### Dependency-Update-Tool

**What it measures:** something automated is keeping dependencies current.

**This project:** [`dependabot.yml`](https://github.com/carterscode/GamerGuardian/blob/main/.github/dependabot.yml) configures weekly NuGet + GitHub Actions update PRs.

### CII-Best-Practices

**What it measures:** project has earned the [OpenSSF Best Practices Badge](https://www.bestpractices.dev/). 5/10 = "in progress" (passing tier targeted), 7/10 = "passing", 10/10 = "silver" or higher.

**This project:** application submitted at https://www.bestpractices.dev/projects/12779. Most criteria already met; full status is on that page. Climbs to 7/10 once tier is achieved.

### Branch-Protection

**What it measures:** the default branch is protected (no force pushes, no deletes, required status checks, required PRs).

**This project:** [Branch protection is active](https://github.com/carterscode/GamerGuardian/settings/branches): no force pushes, no deletions, required status checks (`build`, `Analyze (csharp)`, `Analyze (actions)`), PR-required for any merge to `main`, admin enforcement on. Despite all this, Scorecard reports `-1` because its default `GITHUB_TOKEN` doesn't have admin-read scope, so it can't actually see the protection rules.

**Fix in flight:** the [scorecard.yml](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/scorecard.yml) workflow now reads from an optional `SCORECARD_TOKEN` secret. Once a fine-grained PAT with `Administration: Read-only` + `Metadata: Read-only` is added as that secret, Scorecard scores this check at 10/10.

### Signed-Releases

**What it measures:** at least 1 of the most recent 5 releases has cryptographic provenance (Sigstore signature, SLSA attestation, etc.).

**This project:** as of v0.1.31, every release ships [SLSA Build Provenance](https://slsa.dev/) signed via Sigstore OIDC and verifiable with `gh attestation verify`. The check returns 0 today because Scorecard's last scan looked at the most recent 5 releases when most were unsigned. As more signed releases age in, this score climbs and reaches 10/10 once every release in the most-recent-5 window has provenance (~v0.1.36+).

### Maintained

**What it measures:** repo is actively maintained (recent commits, recent issue activity, NOT recently created).

**This project:** repo is < 90 days old as of writing. Scorecard explicitly penalizes very-new repos because there's no track record yet. This auto-resolves around 2026-08-04 (90 days after creation).

### Packaging

**What it measures:** project publishes a versioned package somewhere automated (npm, Maven Central, Docker Hub, GitHub Packages, etc.).

**This project:** GamerGuardian publishes as a Windows installer + portable EXE via GitHub Releases. The Inno Setup-based EXE distribution model isn't recognized by Scorecard's heuristic, which looks for npm-publish / docker-push / Maven / Container Registry steps. **Structurally unreachable** without changing the distribution model. Could be addressed by also publishing to [winget](https://github.com/microsoft/winget-pkgs) or [Chocolatey](https://community.chocolatey.org/), which are recognized.

### Code-Review

**What it measures:** PRs are reviewed and approved by someone other than the author before merge.

**This project:** **structurally unreachable for a solo dev**. Branch protection requires a PR for every change to `main`, but doesn't require external approvals (because there are no other contributors to approve). Adding required reviewers would either mean the project can never merge or requires bringing in a second human as a constant reviewer. There's no clean fix without changing the project's solo-dev nature.

### Fuzzing

**What it measures:** project has fuzz testing on parsing / network / untrusted-input surfaces.

**This project:** **N/A for a Windows tray app**. No file format parsing of user-supplied data (config.json is read by the same machine that wrote it). No network input parsing (only HTTP GETs to GitHub Releases API). The benchmark detector and service controller take fixed string inputs from a hardcoded catalog. There's no fuzzable surface that a real attacker could reach. Score stays at 0/10 because the check is binary.

### Contributors

**What it measures:** at least 3 different organizations have contributed to the project recently.

**This project:** **structurally unreachable for a solo project**. Single contributor, single organization (a personal account counts as 1).

## Improving the score

Achievable wins (in order of effort vs. impact):

1. **Create `SCORECARD_TOKEN` PAT** (~5 min, +0.7 to overall): see [Branch-Protection](#branch-protection) above. Highest-impact single thing you can do.
2. **Wait for OpenSSF Best Practices badge approval** (already submitted): CII-Best-Practices climbs from 5 to 7+.
3. **Cut ~3 more releases** (passive — happens with normal development): Signed-Releases climbs from 0 to 10 as older unsigned releases age out of the most-recent-5 window.
4. **Wait until 2026-08-04** (passive): Maintained climbs from 0 to 10 once repo is > 90 days old.

Combined, these get the project to ~9.0/10. The remaining ~1 point is the structural-limit cluster (Code-Review, Fuzzing, Contributors, Packaging) — Scorecard's model doesn't reward solo Windows desktop projects there, but that's a reflection of the model's bias, not project security.

## See also

- [Security](Security) — full security posture (CI hardening, supply-chain controls, code signing roadmap)
- [Verifying your download](Verifying-your-download) — how individual users can verify a downloaded binary
- The current scan: https://scorecard.dev/viewer/?uri=github.com/carterscode/GamerGuardian
