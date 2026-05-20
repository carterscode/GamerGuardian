# Contributing to GamerGuardian

Thanks for your interest. This document describes how to set up, contribute, and what reviewers look for.

## Quick start for contributors

```powershell
git clone https://github.com/carterscode/GamerGuardian.git
cd GamerGuardian
dotnet build
dotnet test
src\GamerGuardian\bin\Debug\net8.0-windows10.0.22000.0\GamerGuardian.exe --show-settings
```

For the installer build and CI workflow details, see [docs/wiki/Build-from-source.md](https://github.com/carterscode/GamerGuardian/blob/main/docs/wiki/Build-from-source.md).

## Branching and pull requests

`main` is protected — you cannot push to it directly. The flow:

1. Branch off `main`: `git checkout -b feat/something-descriptive`. Use `feat/`, `fix/`, `chore/`, `ci/`, `docs/` prefixes.
2. Commit your changes with descriptive messages (see *Commit messages* below).
3. Push the branch: `git push -u origin feat/something-descriptive`.
4. Open a pull request: `gh pr create --base main`.
5. CI runs automatically — `build`, `Analyze (csharp)`, `Analyze (actions)`. All three must pass before merge.
6. Self-merge once green: `gh pr merge --merge --delete-branch` (no required approvals for the solo-dev workflow).

## Commit messages

Conventional Commits format. The first line is `<type>: <imperative summary>` under 72 chars.

Common types:
- `feat:` — new functionality (new monitor, UI feature, CLI flag)
- `fix:` — bug fix
- `chore:` — maintenance, refactors with no behavior change
- `ci:` — workflow / build pipeline changes
- `docs:` — wiki, README, comments
- `perf:` — performance improvements
- `ui:` — UI/UX changes

Multi-line bodies are encouraged for non-trivial changes — explain *why*, not *what*. Example:

```
fix(services): stop UAC spam when Windows reverts a service change

Symptom: enabling auto-apply on a service Windows refuses to actually
disable (DoSvc / Delivery Optimization is the trigger case) caused a
UAC prompt every 30 s forever.

MonitorService now backs off auto-apply for a setting whose verify
failed for 15 minutes. Drift still surfaces as a notification.
```

## Code style

- Follow the existing patterns. The codebase is small and consistent.
- `<Nullable>enable</Nullable>` is on. Don't introduce `?` types if you can avoid them.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is on. Build warnings break CI.
- Default to no comments. Only comment the *why* when the *what* is obvious from the code. See examples in `Monitors/HagsMonitor.cs` for the conventional level of commenting.
- C# expression-bodied members and pattern-matching are encouraged where they read naturally.
- Don't introduce abstractions speculatively. Three similar lines is better than a premature framework.

## Adding a new monitor

The canonical example is `src/GamerGuardian/Monitors/HagsMonitor.cs` — about 30 lines.

A new monitor needs:

1. A class implementing `IMonitoredSetting` in `src/GamerGuardian/Monitors/`.
2. Registration in `App.xaml.cs` in the `_allMonitors` array.
3. (If it's a global toggle / Windows AI policy:) a `ToggleSettingPref` field in `Models/AppConfig.cs` -> `GlobalPreferences` -- defaulting to gaming-recommended `DesiredOn` with `Monitor = false`.
4. A row in `SettingsWindow.xaml.cs` `LoadGlobals` / `LoadWindowsAi` / `LoadServices` (whichever tab fits), passing the new `settingId`.
5. `MechanismFor`, `VerifyCommandFor`, and `ApplyCommandFor` entries in `src/GamerGuardian/Services/SettingDocs.cs`. These are what `changes.log` and the in-app Learn more expander surface.
6. A rich `SettingDetails` entry in `src/GamerGuardian/Services/SettingDocsCatalog.cs` -- What / Why / How-it-helps / per-scenario recommendation / Recommended / Risks / ReversibleVia. This drives the in-app Learn more text and `docs/SETTINGS-REFERENCE.md`.
7. (If it belongs in the gaming-recommended preset:) a `SetToggle` / equivalent call in `Services/RecommendedPreset.cs` so the General-tab one-click preset picks up the new setting.
8. **A test** in `tests/GamerGuardian.Tests/` (see *Tests* below).
9. Regenerate the settings reference: `GamerGuardian.exe --gen-docs docs/SETTINGS-REFERENCE.md` and commit the regenerated file. A unit test asserts the committed doc matches the catalog, so CI will fail otherwise.

If the new monitor writes to `HKLM`, route it through `ElevatedRegistry` so it shares the existing UAC-prompt behavior.

If the setting's `ReadCurrent` checks multiple registry values that may individually be missing or controlled by Windows (not just by us), prefer a lenient OR-of-disable-signals semantic over strict AND. See `CopilotMonitor` / `SettingsSearchAiMonitor` for the pattern -- strict AND can verify-fail forever if any one value never persists.

## Adding a new Windows service to the catalog

For the `Windows services` tab, just append to `ServiceCatalog.All` in `src/GamerGuardian/Services/ServiceCatalog.cs`. No code change required elsewhere — `WindowsServiceMonitor` is registered once per catalog entry by `App.xaml.cs`.

If the service is one Windows actively protects (re-enables via `WaaSMedicSvc` etc.), set `RecommendedTarget: ServiceTargetState.Manual` rather than `Disabled`, or omit it from the catalog entirely. See `docs/wiki/Architecture-rationale.md` for the WU-protection background.

Also add a rich `SvcRec` entry in `SettingDocsCatalog.Services` (keyed by service name) so the new service gets a populated Learn more expander; without it, the in-app expander is hidden and the entry is missing from the regenerated `SETTINGS-REFERENCE.md`.

## Adding a Windows AI UWP package to the removal catalog

`Services/WindowsAiAppCatalog.cs` is the curated list of UWP packages users can choose to one-way-remove (via the Windows AI tab). Append a new `WindowsAiAppDefinition` and that's it -- `WindowsAiAppMonitor` is registered once per catalog entry by `App.xaml.cs`, and the UI surfaces the row automatically. Add a SettingDocsCatalog entry under `AiApps` (keyed by package name) so it shows up in the Learn more + reference doc.

UWP removal is one-way; CheckDrift only flags `installed -> remove`, never the reverse. The preset deliberately does **not** opt users into UWP removal (irreversible without the Store).

## Tests

We use xUnit. The test project lives at `tests/GamerGuardian.Tests/`.

Run all tests:

```powershell
dotnet test
```

CI runs the same on every PR.

### Test policy

When you add or change behavior:

- **Pure logic** (catalogs, mappings, parsers, lookup tables) — add a unit test covering the new behavior.
- **Native API wrappers** (anything in `Native/` or `WindowsServiceController`) — add a "doesn't throw on bad input" test if practical. Full coverage isn't expected since these wrap Windows APIs that aren't easily mockable.
- **UI** — manual verification on a dev-build artifact is the current standard. UI test automation is on the roadmap.
- **Bug fixes** — add a regression test if the bug is reproducible from a unit test.

The general rule: it's fine to merge without a test if the change can't be reasonably unit-tested (a UI tweak, a workflow change, a doc update). It's not fine to merge without a test if the change touches a class that *is* unit-tested already.

## Reporting issues and requesting features

- **Bug reports / feature requests:** [GitHub Issues](https://github.com/carterscode/GamerGuardian/issues). Search first; include `--test` output and your `changes.log` if relevant.
- **Security vulnerabilities:** see [SECURITY.md](SECURITY.md). **Do not** open a public issue.
- **Questions:** also fine in Issues; tag with `question`.

## What reviewers look for

- The change is scoped to one concern.
- New behavior has a test if reasonably testable.
- No new compiler warnings.
- Commit messages explain *why*.
- No secrets in the diff (GitHub push protection will catch most, but double-check).
- Touched files have consistent style with the surrounding code.
- For new dependencies: justified, well-maintained, license-compatible (MIT-friendly).

## License

By contributing you agree your contributions are licensed under the [MIT License](LICENSE), the same license the project uses.
