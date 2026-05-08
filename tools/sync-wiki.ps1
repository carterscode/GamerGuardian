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

$srcDir = (Resolve-Path "$PSScriptRoot\..\docs\wiki").Path
Write-Host "Source: $srcDir"
Write-Host "Target: $RepoUrl"

if (Test-Path $WorkDir) { Remove-Item -Recurse -Force $WorkDir }

Write-Host "Cloning wiki repo..."
& git clone $RepoUrl $WorkDir
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Failed to clone $RepoUrl." -ForegroundColor Red
    Write-Host ""
    Write-Host "The most common cause is that the wiki has not been initialized yet."
    Write-Host "Open https://github.com/carterscode/GamerGuardian/wiki in your browser"
    Write-Host "and click Create the first page (any content). Then re-run this script."
    exit 1
}

Write-Host "Copying markdown files..."
Get-ChildItem -Path $srcDir -Filter "*.md" | ForEach-Object {
    $dest = Join-Path $WorkDir $_.Name
    Copy-Item $_.FullName $dest -Force
    Write-Host ("  -> {0}" -f $_.Name)
}

# Copy the user's git identity from the source repo to the wiki clone.
# Git in a fresh clone doesn't inherit per-repo identity from the parent
# directory's repo, so commits would otherwise fail with "Author identity unknown".
$userName = & git -C $srcDir config user.name
$userEmail = & git -C $srcDir config user.email
if (-not $userName -or -not $userEmail) {
    Write-Host "Warning: source repo has no user.name / user.email set." -ForegroundColor Yellow
    Write-Host "Falling back to global git config for the wiki commit."
}
else {
    & git -C $WorkDir config user.name $userName
    & git -C $WorkDir config user.email $userEmail
    Write-Host ("Wiki commits will use: {0} <{1}>" -f $userName, $userEmail)
}

Push-Location $WorkDir
try {
    $changes = & git status --porcelain
    if (-not $changes) {
        Write-Host "Wiki is already up to date - nothing to push."
        exit 0
    }

    & git add -A
    if ($LASTEXITCODE -ne 0) { throw "git add failed" }

    $srcSha = & git -C $srcDir rev-parse --short HEAD
    & git commit -m "sync wiki from docs/wiki/ at $srcSha"
    if ($LASTEXITCODE -ne 0) { throw "git commit failed (LASTEXITCODE=$LASTEXITCODE)" }

    Write-Host "Pushing..."
    & git push
    if ($LASTEXITCODE -ne 0) { throw "git push failed (LASTEXITCODE=$LASTEXITCODE)" }

    Write-Host ""
    Write-Host "Done. Browse the result at https://github.com/carterscode/GamerGuardian/wiki"
}
finally {
    Pop-Location
}
