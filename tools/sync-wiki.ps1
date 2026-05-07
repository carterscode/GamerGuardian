<#
.SYNOPSIS
  Push the markdown files in docs/wiki/ to the GitHub wiki repo.

.DESCRIPTION
  Wikis on GitHub are a separate Git repo at <repo>.wiki.git, but it
  doesn't exist until you create at least one page in the web UI.
  This script clones that repo, copies docs/wiki/*.md into it, commits,
  and pushes — making docs/wiki/ the source of truth and the wiki a
  rendered mirror.

.NOTES
  First-time setup:
    1. Go to https://github.com/carterscode/GamerGuardian/wiki
    2. Click "Create the first page" — any content is fine; this script
       overwrites it.
    3. Run this script.
#>

param(
    [string]$RepoUrl = "https://github.com/carterscode/GamerGuardian.wiki.git",
    [string]$WorkDir = (Join-Path $env:TEMP "gg-wiki-sync")
)

$ErrorActionPreference = "Stop"

$srcDir = Join-Path $PSScriptRoot ".." "docs" "wiki" | Resolve-Path
Write-Host "Source: $srcDir"
Write-Host "Target: $RepoUrl"

if (Test-Path $WorkDir) { Remove-Item -Recurse -Force $WorkDir }

Write-Host "Cloning wiki repo..."
git clone $RepoUrl $WorkDir 2>&1 | ForEach-Object { Write-Host "  $_" }
if ($LASTEXITCODE -ne 0) {
    Write-Error @"
Failed to clone $RepoUrl.

The most common cause is that the wiki hasn't been initialized yet.
Open https://github.com/carterscode/GamerGuardian/wiki in your browser
and click "Create the first page" (any content). Then re-run this script.
"@
    exit 1
}

Write-Host "Copying markdown files..."
Get-ChildItem -Path $srcDir -Filter "*.md" | ForEach-Object {
    $dest = Join-Path $WorkDir $_.Name
    Copy-Item $_.FullName $dest -Force
    Write-Host "  -> $($_.Name)"
}

Push-Location $WorkDir
try {
    $changes = git status --porcelain
    if (-not $changes) {
        Write-Host "Wiki is already up to date — nothing to push."
        return
    }

    git add -A | Out-Null
    git commit -m "sync wiki from docs/wiki/ at $(git -C $srcDir rev-parse --short HEAD)" | Out-Null
    Write-Host "Pushing..."
    git push | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
    Write-Host "Done. Browse the result at: https://github.com/carterscode/GamerGuardian/wiki"
}
finally {
    Pop-Location
}
