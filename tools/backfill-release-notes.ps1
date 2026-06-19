<#
.SYNOPSIS
  Rewrite the notes on existing GitHub Releases from CHANGELOG.md.

.DESCRIPTION
  For every "## [x.y.z]" section in CHANGELOG.md that has a matching GitHub
  Release tag (vX.Y.Z), this sets the release body to the curated, user-facing
  section plus a "Full commit history" compare link. Releases without a section
  in CHANGELOG.md are left untouched. Idempotent -- safe to re-run.

  Going forward the release workflow (.github/workflows/release.yml) builds the
  same notes automatically; this script is for backfilling the history once.

.NOTES
  Requires the GitHub CLI authenticated for the repo. PowerShell 5.1 compatible.
#>

param(
    [string]$Gh = "gh",
    [string]$ChangelogPath = (Join-Path $PSScriptRoot "..\CHANGELOG.md"),
    [string]$Repo = "carterscode/GamerGuardian",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
# Read as UTF-8 explicitly: Windows PowerShell 5.1 otherwise assumes the ANSI
# code page for a BOM-less file and mangles non-ASCII (em dashes etc.).
$cl = Get-Content -Raw -Encoding UTF8 $ChangelogPath

# Every released version section (skip [Unreleased]).
$matches = [regex]::Matches(
    $cl,
    "(?ms)^##\s*\[(?<ver>\d+\.\d+\.\d+)\][^\n]*\n(?<body>.*?)(?=^##\s*\[|\z)")

if ($matches.Count -eq 0) {
    Write-Host "No versioned sections found in $ChangelogPath." -ForegroundColor Yellow
    exit 0
}

# Fetch existing release tags once (avoids per-tag stderr noise).
$existing = (& $Gh release list --repo $Repo --limit 500 --json tagName |
             ConvertFrom-Json).tagName

$updated = 0
$skipped = 0
foreach ($m in $matches) {
    $ver = $m.Groups['ver'].Value
    $section = $m.Groups['body'].Value.Trim()
    $tag = "v$ver"

    if ($existing -notcontains $tag) {
        Write-Host "skip $tag (no release)" -ForegroundColor DarkGray
        $skipped++
        continue
    }

    $body = "## What's new in $ver`n`n$section`n`n---`n" +
            "[Full commit history](https://github.com/$Repo/commits/$tag)"

    if ($DryRun) {
        Write-Host "would update $tag" -ForegroundColor Cyan
        $updated++
        continue
    }

    $tmp = New-TemporaryFile
    try {
        # UTF-8 without BOM, cross-edition: avoids a leading BOM glyph in the
        # release body and the 5.1 ANSI round-trip that corrupts em dashes.
        [System.IO.File]::WriteAllText($tmp, $body, (New-Object System.Text.UTF8Encoding($false)))
        & $Gh release edit $tag --repo $Repo --notes-file $tmp 1>$null
        if ($LASTEXITCODE -ne 0) { throw "gh release edit failed for $tag" }
        Write-Host "updated $tag" -ForegroundColor Green
        $updated++
    }
    finally {
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host ("Done. {0} release(s) {1}, {2} skipped." -f `
    $updated, ($(if ($DryRun) { 'would be updated' } else { 'updated' })), $skipped)
