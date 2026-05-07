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
3. A row in `SettingsWindow.xaml.cs` `LoadGlobals` (or the equivalent for your tab).
4. A `MechanismFor` and `VerifyCommandFor` entry in `src/GamerGuardian/Services/SettingDocs.cs`.
5. **A test** in `tests/GamerGuardian.Tests/` (see *Tests* below).

If the new monitor writes to `HKLM`, route it through `ElevatedRegistry` so it shares the existing UAC-prompt behavior.

## Adding a new Windows service to the catalog

For the `Windows services` tab, just append to `ServiceCatalog.All` in `src/GamerGuardian/Services/ServiceCatalog.cs`. No code change required elsewhere — `WindowsServiceMonitor` is registered once per catalog entry by `App.xaml.cs`.

If the service is one Windows actively protects (re-enables via `WaaSMedicSvc` etc.), set `RecommendedTarget: ServiceTargetState.Manual` rather than `Disabled`, or omit it from the catalog entirely. See `docs/wiki/Architecture-rationale.md` for the WU-protection background.

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
