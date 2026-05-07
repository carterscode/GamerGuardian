# Security Policy

## Supported versions

Only the latest released version of GamerGuardian is supported. If you're on a Release older than the current `latest` tag, please upgrade before reporting an issue — most bugs are fixed in the most recent build.

| Version | Supported |
|---|---|
| Latest released | ✅ |
| Anything older | ❌ |
| Dev builds (workflow artifacts with `-dev.` in the version) | ❌ — these are uncurated test builds |

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security reports. Use one of these private channels instead:

1. **GitHub Security Advisories** (preferred): [Open a draft advisory](https://github.com/carterscode/GamerGuardian/security/advisories/new). This keeps the conversation private until a fix is ready.
2. **Email**: send to the address in the [GitHub profile](https://github.com/carterscode) bio.

Include:

- A description of the issue and its impact (what an attacker could do)
- Steps to reproduce, or a proof-of-concept
- The affected version (`Settings → version link → tooltip`)
- Your handle if you'd like credit

## What to expect

- **Acknowledgement within 48 hours** of report.
- A short triage note within ~5 business days describing severity and target fix window.
- For confirmed valid reports, a fix in the next Release with credit in the release notes (if you want it).

## Scope

In scope:

- The `GamerGuardian.exe` binary distributed via [Releases](https://github.com/carterscode/GamerGuardian/releases)
- The Inno Setup installer
- The auto-update flow (signature verification, download path, etc.)
- The CI/CD pipeline (`.github/workflows/`) as it relates to build integrity

Out of scope:

- Vulnerabilities in third-party dependencies — please report those upstream. We'll consume the fix once it's released.
- Issues that require physical access to a user's already-unlocked machine.
- Social engineering of repo maintainers.
- Theoretical attacks against unmaintained Windows versions (Win10 isn't supported yet).

## Hall of fame

Reporters who've contributed credited fixes will be listed here as they happen.

*(none yet)*
