<#
.SYNOPSIS
    Crops a 24bpp image to induce both C# (byte) and DjVuLibre C++ (pixel) padding.
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
$cropW = $bmp.Width
$cropH = $bmp.Height

# We want Width = 4k - 1 (e.g., 3, 7, 11, 15). 
# This forces GDI+ to add exactly 3 bytes of padding, which equals exactly 1 GPixel.
# This allows testing C# byte padding and C++ pixel padding simultaneously without misalignment crashes.
while ((($cropW + 1) % 4) -ne 0) {
    $cropW--
}

if ($cropW -ne $bmp.Width) {
    Write-Host "Cropping width from $($bmp.Width) to $cropW to induce 3-byte (1 GPixel) padding."
} else {
    Write-Host "Width $cropW already induces 3-byte padding."
}

$rect = [System.Drawing.Rectangle]::new(0, 0, $cropW, $cropH)
$croppedBmp = $bmp.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)

$data = $croppedBmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, $croppedBmp.PixelFormat)
$actualBytes = $cropW * 3
$paddingBytes = $data.Stride - $actualBytes
$paddingPixels = $paddingBytes / 3

Write-Host "New image: ${cropW}x${cropH}. Stride: $($data.Stride) bytes."
Write-Host "Padding: $paddingBytes bytes ($paddingPixels GPixels)."

if ($paddingBytes -ne 3) {
    Write-Error "Failed to induce exactly 3 bytes of padding!"
}

$croppedBmp.UnlockBits($data)
$croppedBmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)

$croppedBmp.Dispose()
$bmp.Dispose()
Write-Host "Done."
