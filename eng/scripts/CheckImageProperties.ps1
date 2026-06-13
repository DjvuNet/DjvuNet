param([string]$ImagePath)
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap $ImagePath
$width = $bmp.Width
$height = $bmp.Height
$pixelFormat = $bmp.PixelFormat
Write-Host "Width: $width"
Write-Host "Height: $height"
Write-Host "PixelFormat: $pixelFormat"

$rect = New-Object System.Drawing.Rectangle 0, 0, $width, $height
$bmpData = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$stride = $bmpData.Stride
Write-Host "Stride (Format24bppRgb): $stride"
Write-Host "Expected Stride without padding: $($width * 3)"

$bmp.UnlockBits($bmpData)
$bmp.Dispose()
