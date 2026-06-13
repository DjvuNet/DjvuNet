<#
.SYNOPSIS
    Converts a 24bpp RGB image to a BGR image (or vice versa) by swapping the first and third bytes of every pixel.

.DESCRIPTION
    This script loads an image using System.Drawing, locks the bits in memory, and performs a fast byte-swap on the Red and Blue channels. It is useful for generating test artifacts for native libraries (like DjVuLibre) that expect a specific memory layout.

.PARAMETER InputPath
    The absolute or relative path to the input image file.

.PARAMETER OutputPath
    The absolute or relative path to save the converted image file.
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$InputPath,

    [Parameter(Mandatory=$true)]
    [string]$OutputPath
)

Add-Type -AssemblyName System.Drawing

$fullInputPath = (Resolve-Path $InputPath).Path
Write-Host "Loading image from: $fullInputPath"

$bmp = [System.Drawing.Bitmap]::new($fullInputPath)
$rect = [System.Drawing.Rectangle]::new(0, 0, $bmp.Width, $bmp.Height)

# We must lock in 24bpp format to ensure a 3-byte pixel size
$pixelFormat = [System.Drawing.Imaging.PixelFormat]::Format24bppRgb
$data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadWrite, $pixelFormat)

$totalBytes = $data.Stride * $bmp.Height
$buffer = [byte[]]::new($totalBytes)

[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buffer, 0, $totalBytes)

Write-Host "Swapping Red and Blue channels ($totalBytes bytes)..."

# Swap R and B (Bytes 0 and 2 of every 3-byte block)
for ($i = 0; $i -lt $totalBytes; $i += 3) {
    $temp = $buffer[$i]
    $buffer[$i] = $buffer[$i + 2]
    $buffer[$i + 2] = $temp
}

[System.Runtime.InteropServices.Marshal]::Copy($buffer, 0, $data.Scan0, $totalBytes)
$bmp.UnlockBits($data)

Write-Host "Saving converted image to: $OutputPath"
$bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Host "Done."