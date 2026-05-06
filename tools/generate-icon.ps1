#requires -Version 5.1
<#
Generates the GamerGuardian app icon as a multi-resolution .ico file.
Re-run if the design changes; commit the resulting AppIcon.ico.

  pwsh ./tools/generate-icon.ps1
#>

param(
    [string]$OutputPath = "$PSScriptRoot/../src/GamerGuardian/Assets/AppIcon.ico"
)

Add-Type -AssemblyName System.Drawing

function New-LogoBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    $g.Clear([System.Drawing.Color]::Transparent)

    $bgColor = [System.Drawing.Color]::FromArgb(255, 24, 89, 168)
    $accentColor = [System.Drawing.Color]::FromArgb(255, 88, 166, 255)

    $r = [int]($size / 6)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, 2*$r, 2*$r, 180, 90)
    $path.AddArc($size - 2*$r - 1, 0, 2*$r, 2*$r, 270, 90)
    $path.AddArc($size - 2*$r - 1, $size - 2*$r - 1, 2*$r, 2*$r, 0, 90)
    $path.AddArc(0, $size - 2*$r - 1, 2*$r, 2*$r, 90, 90)
    $path.CloseFigure()

    $rectF = New-Object System.Drawing.RectangleF(0, 0, $size, $size)
    $gradBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rectF, $bgColor, $accentColor, 135.0)
    $g.FillPath($gradBrush, $path)
    $gradBrush.Dispose()

    $fontSize = [single]($size * 0.62)
    $font = New-Object System.Drawing.Font('Segoe UI', $fontSize, [System.Drawing.FontStyle]::Bold)
    $fg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center

    $textRect = New-Object System.Drawing.RectangleF(0, [single](-$size * 0.04), $size, $size)
    $g.DrawString('G', $font, $fg, $textRect, $sf)

    $font.Dispose(); $fg.Dispose(); $sf.Dispose(); $path.Dispose(); $g.Dispose()
    return $bmp
}

function Save-MultiResIco([string]$out, [int[]]$sizes) {
    $pngs = @()
    foreach ($s in $sizes) {
        $bmp = New-LogoBitmap $s
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngs += [pscustomobject]@{ Size = $s; Bytes = $ms.ToArray() }
        $bmp.Dispose(); $ms.Dispose()
    }

    $dir = Split-Path -Parent $out
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }

    $stream = [System.IO.File]::Create($out)
    try {
        $w = New-Object System.IO.BinaryWriter($stream)
        $w.Write([UInt16]0)
        $w.Write([UInt16]1)
        $w.Write([UInt16]$pngs.Count)

        $offset = 6 + $pngs.Count * 16
        foreach ($p in $pngs) {
            $sz = if ($p.Size -ge 256) { 0 } else { $p.Size }
            $w.Write([byte]$sz)
            $w.Write([byte]$sz)
            $w.Write([byte]0)
            $w.Write([byte]0)
            $w.Write([UInt16]1)
            $w.Write([UInt16]32)
            $w.Write([UInt32]$p.Bytes.Length)
            $w.Write([UInt32]$offset)
            $offset += $p.Bytes.Length
        }
        foreach ($p in $pngs) { $w.Write($p.Bytes) }
        $w.Flush()
    } finally {
        $stream.Close()
    }
}

Save-MultiResIco -out $OutputPath -sizes @(16, 24, 32, 48, 64, 128, 256)
"Wrote $OutputPath ({0:N0} bytes)" -f (Get-Item $OutputPath).Length

# README banner PNG
$pngPath = Join-Path (Split-Path $OutputPath) 'AppIcon-128.png'
$pngBmp = New-LogoBitmap 128
$pngBmp.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBmp.Dispose()
"Wrote $pngPath ({0:N0} bytes)" -f (Get-Item $pngPath).Length
