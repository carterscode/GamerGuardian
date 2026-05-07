# Build from source

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) — only needed for the installer build

## Local debug build

```powershell
git clone https://github.com/carterscode/GamerGuardian.git
cd GamerGuardian
dotnet build
src\GamerGuardian\bin\Debug\net8.0-windows10.0.22000.0\GamerGuardian.exe --show-settings
```

`--show-settings` opens the Settings window even when a config already exists (otherwise the window only appears on first run).

`--test` writes every monitor's current readout to `%TEMP%\gamerguardian_selftest.txt` and exits — useful when triaging weird hardware.

## Local Release publish (single-file)

```powershell
dotnet publish src/GamerGuardian/GamerGuardian.csproj `
    -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=0.0.0 -o publish
```

This produces `publish/GamerGuardian.exe` (~77 MB) — runs standalone with no .NET runtime needed.

## Local installer build

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DAppVersion=0.0.0 installer\GamerGuardian.iss
```

Output: `installer\Output\GamerGuardian-Setup-0.0.0.exe` (~71 MB). The Inno script picks up the file from `publish/` produced by the previous step.

## CI / dev builds

Three workflows live in `.github/workflows/`:

| Workflow | Trigger | Output |
|---|---|---|
| [`build.yml`](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/build.yml) | PRs to `main` | `dotnet build` only — fast sanity check, no artifacts |
| [`dev-build.yml`](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/dev-build.yml) | Push to any non-`main` branch, or `workflow_dispatch` | Installer + portable EXE as workflow artifacts. **Not a Release** — invisible to the auto-updater |
| [`release.yml`](https://github.com/carterscode/GamerGuardian/blob/main/.github/workflows/release.yml) | Push to `main` (`src/**`, `installer/**`, or `release.yml`), tag push, `workflow_dispatch` | Public GitHub Release with the installer + portable EXE attached |

Dev builds are stamped `{nextStable}-dev.{sha7}` so the parsed semver sits above the current stable — the dev binary won't try to "upgrade" itself to stable mid-test. See [Security → Reproducibility](Security#reproducibility) for verifying CI-built artifacts match a local build.
