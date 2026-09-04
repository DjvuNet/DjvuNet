[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, Position=0, HelpMessage="Name of the test assembly (e.g., DjvuNet.Wavelet.Tests)")]
    [string]$TestAssemblyName,

    [Parameter(Position=1, HelpMessage="Build Configuration (Debug/Release)")]
    [string]$Configuration = "Release",

    [Parameter(Position=2, HelpMessage="Target Framework Moniker (e.g., net10.0)")]
    [string]$TargetFramework = "net10.0",

    [Parameter(Position=3, HelpMessage="Target Architecture (e.g., x64, arm64)")]
    [string]$TargetPlatform = "x64",

    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$xUnitArgs
)

$ErrorActionPreference = 'Stop'

# 1. Determine OS and Runtime Identifier (RID)
$osName = "Windows"
$rid = "win-$TargetPlatform"
if ($IsLinux) {
    $osName = "Linux"
    $rid = "linux-$TargetPlatform"
} elseif ($IsMacOS) {
    $osName = "OSX"
    $rid = "osx-$TargetPlatform"
}

# 2. Resolve the published executable path based on build-architecture-design.md
$repoRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$testExe = Join-Path $repoRoot "build\bin\$osName.$TargetPlatform.$Configuration\binaries\$TargetFramework\$rid\publish\$TestAssemblyName"
if ($IsWindows) {
    $testExe += ".exe"
}

if (-not (Test-Path $testExe)) {
    Write-Error "Test executable not found: $testExe`nPlease ensure you have built and published the tests."
}

# 3. Hardware Diagnostic Probe (via DjvuNet.Shared.Tests)
$publishDir = Split-Path $testExe
$sourceSharedDll = Join-Path $publishDir "DjvuNet.Shared.Tests.dll"

if (Test-Path $sourceSharedDll) {
    Write-Host "`n=======================================================" -ForegroundColor DarkGray
    Write-Host " HOST HARDWARE ISA PROBE" -ForegroundColor DarkGray
    Write-Host "=======================================================" -ForegroundColor DarkGray
    
    $tmpDir = Join-Path $publishDir "tmp"
    if (-not (Test-Path $tmpDir)) {
        New-Item -ItemType Directory -Path $tmpDir | Out-Null
    }
    
    $sharedDll = Join-Path $tmpDir "DjvuNet.Shared.Tests.dll"
    $shouldCopyShared = -not (Test-Path $sharedDll) -or ((Get-Item $sourceSharedDll).LastWriteTime -gt (Get-Item $sharedDll).LastWriteTime)
    if ($shouldCopyShared) {
        Copy-Item $sourceSharedDll $sharedDll -Force
    }

    $sourceAttrDll = Join-Path $publishDir "System.Attributes.dll"
    if (Test-Path $sourceAttrDll) {
        $attrDll = Join-Path $tmpDir "System.Attributes.dll"
        $shouldCopyAttr = -not (Test-Path $attrDll) -or ((Get-Item $sourceAttrDll).LastWriteTime -gt (Get-Item $attrDll).LastWriteTime)
        if ($shouldCopyAttr) {
            Copy-Item $sourceAttrDll $attrDll -Force
        }
    }

    Add-Type -Path $sharedDll
    
    $platEnum = if ($TargetPlatform -eq "arm64") { [DjvuNet.Shared.Tests.Platform]::Arm64 } else { [DjvuNet.Shared.Tests.Platform]::X64 }
    
    $info = [DjvuNet.Shared.Tests.HardwareIntrinsics]::GetFullInfo($platEnum)
    $vecSize = [DjvuNet.Shared.Tests.HardwareIntrinsics]::GetHardwareVectorSize()
    Write-Host "HardwareIntrinsics=$info $vecSize`n" -ForegroundColor Green
} else {
    $sharedDll = $null
    Write-Warning "DjvuNet.Shared.Tests.dll not found in $testExe directory. ISA Matrix may not evaluate correctly."
}

# 4. Dynamic Hardware Probe & Matrix Construction
$osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture

$hasAvx512v2 = $false
$hasAvx2 = $false
$hasAdvSimd = $false

if (Test-Path $sharedDll) {
    $hasAvx512v2 = [DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Avx512v2Supported
    $hasAvx2 = [DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Avx2Supported
    $hasAdvSimd = [DjvuNet.Shared.Tests.HardwareIntrinsics]::IsArmBaseSupported
}

$nativeTierName = "Scalar (Native)"
if ($osArch -eq 'X64') {
    if ($null -ne $sharedDll -and (Test-Path $sharedDll)) {
        if ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Avx10v2Supported) { $nativeTierName = "AVX10v2 (Native)" }
        elseif ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Avx10v1Supported) { $nativeTierName = "AVX10v1 (Native)" }
        elseif ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Avx512v3Supported) { $nativeTierName = "AVX512v3 (Native)" }
        elseif ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Avx512v2Supported) { $nativeTierName = "AVX512v2 (Native)" }
        elseif ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Avx512Supported) { $nativeTierName = "AVX512 (Native)" }
        elseif ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Avx2Supported) { $nativeTierName = "AVX2 (Native)" }
        elseif ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Sse42Supported) { $nativeTierName = "SSE4.2 (Native)" }
        elseif ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86BaseSupported) { $nativeTierName = "SSE2 (Native)" }
    }
} elseif ($osArch -eq 'Arm64') {
    if ($null -ne $sharedDll -and (Test-Path $sharedDll) -and [DjvuNet.Shared.Tests.HardwareIntrinsics]::IsArmBaseSupported) { 
        $nativeTierName = "AdvSimd (Native)" 
    }
}

$isaTiers = @()
$isaTiers += @{ Name = $nativeTierName; Envs = @{} }

if ($osArch -eq 'X64') {
    if ($hasAvx512v2) {
        $isaTiers += @{ Name = "AVX2 Fallback"; Envs = @{ "DOTNET_EnableAVX512" = "0" } }
    }
    if ($hasAvx2) {
        $sseTierName = "SSE Fallback"
        if ($null -ne $sharedDll -and (Test-Path $sharedDll)) {
            if ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86Sse42Supported) { $sseTierName = "SSE4.2 Fallback" }
            elseif ([DjvuNet.Shared.Tests.HardwareIntrinsics]::IsX86BaseSupported) { $sseTierName = "SSE2 Fallback" }
        }
        $isaTiers += @{ Name = $sseTierName; Envs = @{ "DOTNET_EnableAVX" = "0" } }
    }
} elseif ($osArch -eq 'Arm64') {
    if ($hasAdvSimd) {
        $isaTiers += @{ Name = "Scalar Fallback (No AdvSimd)"; Envs = @{ "DOTNET_EnableAdvSimd" = "0" } }
    }
}

# Unconditional final tier for all architectures
$isaTiers += @{ Name = "Scalar (No HW Intrinsics)"; Envs = @{ "DOTNET_EnableHWIntrinsic" = "0" } }

$hasErrors = $false
$tierResults = @()
$sw = [System.Diagnostics.Stopwatch]::StartNew()

foreach ($tier in $isaTiers) {
    Write-Host "`n=======================================================" -ForegroundColor Cyan
    Write-Host " RUNNING TIER: $($tier.Name)" -ForegroundColor Cyan
    Write-Host "=======================================================" -ForegroundColor Cyan

    # Temporarily apply environment variables, backing up originals if they exist
    $backupEnvs = @{}
    foreach ($key in $tier.Envs.Keys) {
        $backupEnvs[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
        [Environment]::SetEnvironmentVariable($key, $tier.Envs[$key], "Process")
    }

    # Execute xUnit v3 executable and capture output for summary
    Write-Host "Executing: $testExe $xUnitArgs`n" -ForegroundColor DarkGray
    
    $tmpLog = New-TemporaryFile
    try {
        & $testExe $xUnitArgs | Tee-Object -FilePath $tmpLog.FullName
        $exitCode = $LASTEXITCODE
        
        $summaryStats = ""
        Get-Content $tmpLog.FullName | ForEach-Object {
            if ($_ -match "Total:\s*\d+,\s*Errors:\s*\d+") {
                $summaryStats = $_ -replace '^.*?(Total:\s*\d+.*)$', '$1'
            }
        }
    } finally {
        Remove-Item $tmpLog.FullName -ErrorAction SilentlyContinue
    }
    
    if ($exitCode -ne 0) {
        $tierResults += @{ Name = $tier.Name; Status = "FAIL"; ExitCode = $exitCode; Stats = $summaryStats }
        $hasErrors = $true
    } else {
        $tierResults += @{ Name = $tier.Name; Status = "PASS"; ExitCode = 0; Stats = $summaryStats }
    }

    # Restore original environment variables
    foreach ($key in $tier.Envs.Keys) {
        [Environment]::SetEnvironmentVariable($key, $backupEnvs[$key], "Process")
    }
}

$sw.Stop()

Write-Host "`n=======================================================" -ForegroundColor Cyan
Write-Host " ISA Matrix Execution Summary: `n" -ForegroundColor Cyan
$cmdArgsStr = $xUnitArgs -join ' '
$fullCmdLine = "$TestAssemblyName $cmdArgsStr".Trim()
Write-Host " $fullCmdLine`n" -ForegroundColor Cyan

foreach ($res in $tierResults) {
    $paddedName = $res.Name.PadRight(35)
    if ($res.Status -eq "PASS") {
        Write-Host "[PASS] $paddedName $($res.Stats)"
    } else {
        Write-Host "[FAIL] " -NoNewline -ForegroundColor Red
        Write-Host "$paddedName $($res.Stats)  (Exit: $($res.ExitCode))"
    }
}

Write-Host "`n Total Execution Time: $([math]::Round($sw.Elapsed.TotalSeconds, 2))s" -ForegroundColor Cyan
Write-Host "=======================================================`n" -ForegroundColor Cyan

if ($hasErrors) {
    exit 1
}
