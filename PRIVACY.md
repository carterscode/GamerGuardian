# Privacy Policy

**Effective: 2026-05-07**

GamerGuardian is a desktop tray app that collects no personal data and operates no first-party server. This document describes everything the app does that could conceivably be considered data handling.

## Summary

- GamerGuardian **does not collect, transmit, store, or sell any personal data** about you.
- It has **no telemetry**, **no analytics**, **no crash-reporting service**, and **no first-party backend**.
- All configuration and logs live **on your local machine only**, in your user profile.
- The **only outbound network requests** the app ever makes are to GitHub, for the auto-update check and (if you accept an update) the installer download.

## What gets stored locally

GamerGuardian writes the following to your machine. None of it leaves your machine.

| File / location | Contents | Purpose |
|---|---|---|
| `%APPDATA%\GamerGuardian\config.json` | Your monitor / desired-value / auto-apply preferences. No personal data. | Persists settings between launches. |
| `%APPDATA%\GamerGuardian\changes.log` | Append-only audit log of registry writes / API calls the app made. No personal data. | Lets you verify what the app changed. |
| `%TEMP%\gamerguardian_error.log` | Unhandled exception stack traces, if any. May incidentally contain Windows usernames if those appear in stack frames. | Local debugging; never transmitted. |
| `%TEMP%\GamerGuardian-Setup-x.y.z.exe` | Auto-update installer downloaded from GitHub. | Cleared automatically after one day. |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\GamerGuardian` | Path to the installed binary, when "Launch at startup" is enabled. | Standard Windows autostart mechanism. |

You can delete any of these at any time. Uninstalling the app removes everything except the `HKCU` autostart entry (which the uninstaller also removes).

## Network requests

GamerGuardian makes outbound network requests only when:

1. **Checking for updates on startup** — one HTTPS GET to `https://api.github.com/repos/carterscode/GamerGuardian/releases/latest`. This is GitHub's public Releases API. No headers identifying you are sent beyond a generic `User-Agent: GamerGuardian/1.0` and the standard TLS fingerprint your operating system attaches.
2. **Checking for updates manually** — same request, on demand from *Settings → Check now*.
3. **Downloading an update** — only after you click *Install* in the update prompt. The download is an HTTPS GET to `https://github.com/carterscode/GamerGuardian/releases/download/...`.

You can disable both checks by unticking *Check for updates on startup* in *Settings → General*. With that off and no manual click, GamerGuardian makes **zero** network requests.

When these requests do happen, GitHub records them per its own [Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-privacy-statement). GamerGuardian receives only the JSON response (release metadata) or the installer bytes; it does not log, transmit, or otherwise share that data with anyone.

## Third parties

GitHub is the only third party involved in the operation of GamerGuardian, and only for hosting the source code and release binaries. GitHub's privacy practices are governed by their [Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-privacy-statement). GamerGuardian does not have, and never has had, any other third-party integration — no analytics SDKs, no advertising networks, no error-reporting services, no CDN.

## What we do not do

- We do not have user accounts.
- We do not have a server.
- We do not run analytics.
- We do not fingerprint your device or your installation.
- We do not collect crash reports remotely.
- We do not embed advertising or tracking pixels.
- We do not share, sell, or rent any data to anyone, because we have nothing to share.

## Children's privacy

GamerGuardian does not knowingly collect any data from anyone, including children. There is no account creation, profile, or data submission of any kind in the app.

## Changes to this policy

If the way the app handles data ever changes, this document will be updated in the same commit as the code change. The git history of this file is the authoritative record of policy changes:

> https://github.com/carterscode/GamerGuardian/commits/main/PRIVACY.md

The "Effective" date at the top reflects the most recent material change.

## Contact

For privacy questions or concerns, open an issue at https://github.com/carterscode/GamerGuardian/issues, or contact the maintainer via the GitHub profile at https://github.com/carterscode.

For security disclosures, see [SECURITY.md](SECURITY.md).
