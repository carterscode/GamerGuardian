<#
.SYNOPSIS
  Bulk-dismiss CodeQL alerts that are intentional patterns or false positives.

.DESCRIPTION
  Walks all open CodeQL alerts in the repo and dismisses each one based on
  its rule ID, with a category-specific reason and comment. Idempotent —
  re-running dismisses any new alerts in these same categories without
  affecting alerts in other (potentially actionable) categories.

  The intent is: keep the Security tab high-signal. Real findings get fixed
  in code (see commits referencing CodeQL findings); intentional patterns
  and informational notes get dismissed here once with a documented reason.

  To dismiss a new category, add an entry to $Categories. To stop dismissing
  one, remove its entry — the script will stop touching alerts of that rule
  ID, leaving any open ones alone.

.PARAMETER Repo
  GitHub repo in owner/name form. Defaults to carterscode/GamerGuardian.

.PARAMETER DryRun
  Print what would be dismissed without calling the API.

.NOTES
  Requires the gh CLI authenticated with repo write access (specifically the
  security_events: write permission, which the default token has for repo
  admins).

  Dismissed alerts can be re-opened via the GitHub Security tab or the API
  if a category later turns out to need fixing.
#>
[CmdletBinding()]
param(
    [string]$Repo = "carterscode/GamerGuardian",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$gh = "C:\Program Files\GitHub CLI\gh.exe"
if (-not (Test-Path $gh)) {
    $gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
    if (-not $gh) { throw "gh CLI not found on PATH or at default install location" }
}

# Map CodeQL rule ID -> { reason, comment }.
# `reason` must be one of the GitHub-allowed enum values:
#   "false positive", "won't fix", "used in tests"
$Categories = @{
    "cs/catch-of-all-exceptions" = @{
        reason  = "won't fix"
        comment = "Used in monitor poll loops and disposal paths to maintain availability when one of N independent monitors fails. Documented design — see docs/wiki/Architecture-rationale.md."
    }
    "cs/empty-catch-block" = @{
        reason  = "won't fix"
        comment = "Same intent as cs/catch-of-all-exceptions: per-monitor failure isolation in poll loop and best-effort disposal paths."
    }
    "cs/call-to-unmanaged-code" = @{
        reason  = "won't fix"
        comment = "Project intentionally uses Win32 P/Invoke for HDR/refresh/registry/service operations — informational only."
    }
    "cs/unmanaged-code" = @{
        reason  = "won't fix"
        comment = "Project intentionally uses Win32 P/Invoke. Informational alert only."
    }
    "cs/path-combine" = @{
        reason  = "false positive"
        comment = "All instances pass (directory, literal_filename) where the second arg is a string constant. Static analysis can't prove the second arg is relative; manually verified safe at every call site."
    }
    "cs/missed-using-statement" = @{
        reason  = "won't fix"
        comment = "BenchmarkDetector's try/finally disposes all enumerated processes including on early-return. Switching to per-item 'using' would leak Process handles when the matching process is found before the end of the array."
    }
    "cs/static-field-written-by-instance" = @{
        reason  = "won't fix"
        comment = "Single-instance mutex pattern; WPF App.OnStartup is the canonical owner of process-lifetime statics."
    }
    "cs/call-to-gc" = @{
        reason  = "won't fix"
        comment = "Explicit working-set trim after Settings window close. WPF visual tree retention requires manual GC + EmptyWorkingSet to return memory to OS — verified empirically over hours of runtime."
    }
}

Write-Host "Fetching open CodeQL alerts from $Repo..."
$rawJson = & $gh api "repos/$Repo/code-scanning/alerts?state=open&per_page=100" --paginate 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to fetch alerts. gh output:`n$rawJson"
    exit 1
}

$alerts = $rawJson | ConvertFrom-Json
Write-Host "Got $($alerts.Count) open alerts."
Write-Host ""

# Pre-flight summary so the user sees what's about to happen.
$grouped = $alerts | Group-Object { $_.rule.id } | Sort-Object Count -Descending
Write-Host "Open alerts by rule:"
foreach ($g in $grouped) {
    $action = if ($Categories.ContainsKey($g.Name)) { $Categories[$g.Name].reason } else { "skip (not in dismissal map)" }
    Write-Host ("  {0,-50} {1,4}  {2}" -f $g.Name, $g.Count, $action)
}
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN — no API calls. Re-run without -DryRun to dismiss."
    exit 0
}

$dismissed = 0
$skipped   = 0
$failed    = 0

foreach ($alert in $alerts) {
    $ruleId = $alert.rule.id
    if (-not $Categories.ContainsKey($ruleId)) {
        $skipped++
        continue
    }

    $cat = $Categories[$ruleId]
    $payload = @{
        state             = "dismissed"
        dismissed_reason  = $cat.reason
        dismissed_comment = $cat.comment
    } | ConvertTo-Json -Compress

    $tmp = New-TemporaryFile
    Set-Content -Path $tmp.FullName -Value $payload -Encoding utf8 -NoNewline

    try {
        $null = & $gh api -X PATCH "repos/$Repo/code-scanning/alerts/$($alert.number)" --input $tmp.FullName 2>&1
        if ($LASTEXITCODE -eq 0) {
            $dismissed++
            if ($dismissed % 25 -eq 0) {
                Write-Host "  ...$dismissed dismissed so far"
            }
        } else {
            Write-Warning "Failed to dismiss alert #$($alert.number) ($ruleId)"
            $failed++
        }
    }
    finally {
        Remove-Item $tmp.FullName -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "Done."
Write-Host "  Dismissed: $dismissed"
Write-Host "  Skipped (rule not in dismissal map, left untouched): $skipped"
if ($failed -gt 0) { Write-Host "  Failed:    $failed" -ForegroundColor Yellow }
