#requires -Version 5.1
<#
Captures screenshots of the running app for the README.
Builds Debug, launches with --show-settings, waits for the FluentWindow to render,
PrintWindow's it to a PNG. Repeats for any other named windows.

Run from repo root:
    pwsh ./tools/capture-screenshots.ps1
#>

param(
    [string]$OutDir = "$PSScriptRoot/../docs/screenshots",
    [string]$Exe = "$PSScriptRoot/../src/GamerGuardian/bin/Debug/net8.0-windows10.0.22000.0/GamerGuardian.exe",
    [int]$RenderWaitSeconds = 7
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class WinCap {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr d, uint flags);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool SetProcessDPIAware();
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out RECT pvAttr, int cbAttribute);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

[WinCap]::SetProcessDPIAware() | Out-Null

function Capture-Hwnd([IntPtr]$hwnd, [string]$outPath) {
    $rect = New-Object WinCap+RECT
    if (-not [WinCap]::GetWindowRect($hwnd, [ref]$rect)) {
        Write-Host "GetWindowRect failed for $hwnd"
        return $false
    }
    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    if ($w -le 1 -or $h -le 1) { return $false }

    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 32, 32, 32))
    $hdc = $g.GetHdc()
    $ok = [WinCap]::PrintWindow($hwnd, $hdc, 2)  # PW_RENDERFULLCONTENT
    $g.ReleaseHdc($hdc)

    $dir = Split-Path -Parent $outPath
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose(); $g.Dispose()
    "  saved: $outPath ({0}x{1})" -f $w, $h
    return $true
}

function Get-AppWindows([int]$processId) {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
    return @($wins)
}

# Cleanup any prior instance
Get-Process -Name 'GamerGuardian' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

if (-not (Test-Path $Exe)) {
    Write-Error "Build first: dotnet build -c Debug. Missing: $Exe"
    exit 1
}

Write-Host "Launching app with --show-settings..."
$p = Start-Process -FilePath $Exe -ArgumentList '--show-settings' -PassThru
Start-Sleep -Seconds $RenderWaitSeconds

$wins = Get-AppWindows $p.Id
if ($wins.Count -eq 0) {
    Write-Error "No windows found for pid $($p.Id)"
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

foreach ($w in $wins) {
    $title = $w.Current.Name
    $hwnd = [IntPtr]$w.Current.NativeWindowHandle
    if ($hwnd -eq [IntPtr]::Zero) { continue }

    if ($title -like '*Settings*') {
        Capture-Hwnd $hwnd (Join-Path $OutDir 'settings-window.png') | Out-Null
    }
}

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Host "Done."
