# GamerGuardian Wiki

A Windows 11 tray app that watches gaming-related display and system settings, alerts you (or auto-fixes) when they drift from your preferences, and stays out of the way during gameplay and benchmarks.

## Getting started

- **[Installation](Installation)** — download, install, uninstall
- **[Verifying your download](Verifying-your-download)** — three independent integrity checks (VirusTotal, SHA-256, SLSA provenance) anyone can run before installing
- **[Verification](Verification)** — how to confirm GamerGuardian is doing what it says at runtime
- **[File locations](File-locations)** — where config, logs, and the installed binary live
- **[Logging](Logging)** — `changes.log` schema with worked examples

## Build & contribute

- **[Build from source](Build-from-source)** — local debug, local release, local installer
- **[Source file reference](Source-file-reference)** — what every `.cs` file does
- **[Architecture rationale](Architecture-rationale)** — why GamerGuardian looks the way it does (no kernel driver, polling, single-file publish, etc.)

## Trust & security

- **[Security](Security)** — how the build pipeline keeps the app trustworthy, and how you can verify it yourself

> **Source of truth note:** these pages are mirrored from [`docs/wiki/`](https://github.com/carterscode/GamerGuardian/tree/main/docs/wiki) in the main repo. PRs that touch wiki content go to that directory; a sync workflow pushes them here.
