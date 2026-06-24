<#
.SYNOPSIS
    Packages the DjvuNet custom build tools into cross-platform archives.

.DESCRIPTION
    This script prepares and packages the 'Tools/DjvuNet' directory into 'Tools.zip' 
    and 'Tools.tar.gz' archives. These archives contain the custom MSBuild tasks and 
    their native dependencies (like LibGit2Sharp binaries) required to bootstrap the 
    DjvuNet build process on clean environments.
    
    Before packaging, it cleans up obsolete files, wipes redundant native binaries 
    from the managed root, and verifies the cross-platform dependencies graph.

.PARAMETER RepoRoot
    The absolute or relative path to the root directory of the repository.

.EXAMPLE
    .\eng\scripts\PackageTools.ps1 -RepoRoot "."
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory=$true, HelpMessage="Path to the repository root directory")]
    [string]$RepoRoot,
    
    [Parameter(Mandatory=$true, HelpMessage="Build configuration (e.g. Debug or Release)")]
    [ValidateSet('Debug', 'Release', 'Checked')]
    [string]$Configuration
)

$ErrorActionPreference = "Stop"

# Ensure we are working with an absolute path and resolving separators
$WorkspaceRoot = (Resolve-Path -Path $RepoRoot).Path
$ToolsDir = "$WorkspaceRoot/Tools"
$OutputDir = "$WorkspaceRoot/build/artifacts/tools/$Configuration"

Write-Host "1. Killing stray dotnet processes..."
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "2. Cleaning up .bak files in Tools directory..."
if (-not (Test-Path $ToolsDir)) {
    Write-Warning "Tools directory not found at $ToolsDir. Nothing to package."
    exit 1
}
Get-ChildItem -Path $ToolsDir -Filter "*.bak" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host "3. Removing redundant native binaries from managed root..."
Write-Host "`n--- 3. CLEANUP & 4. VERIFICATION PHASE ---"

# --- Establish Dynamic Oracles ---
$GlobalJsonPath = Join-Path $RepoRoot "global.json"
$DepsPropsPath = Join-Path $RepoRoot "dependencies.props"
$TasksProjPath = Join-Path $RepoRoot "eng\tools\DjvuNet.Build.Tasks\DjvuNet.Build.Tasks.csproj"

$GlobalJson = Get-Content $GlobalJsonPath -Raw | ConvertFrom-Json
$SdkMajorVersion = $GlobalJson.sdk.version.Split('.')[0]
$CoreFwName = "net${SdkMajorVersion}.0"
$CoreAppNodeName = ".NETCoreApp,Version=v${SdkMajorVersion}.0"

[xml]$DepsProps = Get-Content $DepsPropsPath -Raw
$NativeBinVersionNode = $DepsProps.Project.PropertyGroup.LibGit2SharpNativeBinariesPackageVersion | Where-Object { $_ }
$NativeBinVersion = $NativeBinVersionNode.Trim()
$NativeBinariesKey = "LibGit2Sharp.NativeBinaries$NativeBinVersion"

[xml]$TasksProj = Get-Content $TasksProjPath -Raw
$AllTfms = $TasksProj.SelectNodes("//TargetFrameworks") | Select-Object -ExpandProperty '#text' -ErrorAction SilentlyContinue
$NetFxFwName = ($AllTfms -split ';' | Where-Object { $_ -match "^net4" }) | Select-Object -First 1

Write-Host "   Oracles Established:"
Write-Host "     - Core Framework:  $CoreFwName ($CoreAppNodeName)"
Write-Host "     - NetFx Fallback:  $NetFxFwName"
Write-Host "     - Native Binaries: $NativeBinariesKey"

$TargetDir = Join-Path $ToolsDir "DjvuNet"
if (-not (Test-Path $TargetDir)) {
    Write-Error "CRITICAL: Directory $TargetDir not found. Cannot verify managed root or perform native binary cleanup. Halting packaging process."
    exit 1
}
$FrameworkDirs = Get-ChildItem -Path $TargetDir -Directory

foreach ($FwDir in $FrameworkDirs) {
    $ManagedRoot = $FwDir.FullName
    $FwName = $FwDir.Name

    Write-Host "`n--- PROCESSING FRAMEWORK: $FwName ---"
    
    Write-Host "   Removing redundant native binaries from managed root to reduce archive size..."
    $BuildTasksDll = Join-Path $ManagedRoot "DjvuNet.Build.Tasks.dll"

    if (Test-Path $BuildTasksDll) {
        if ($FwName -eq $CoreFwName) {
            # Only inspect files at the root level of the managed directory
            $PotentialBinaries = Get-ChildItem -Path $ManagedRoot -File | Where-Object { $_.Extension -match "\.(dll|so|dylib)$" }
            foreach ($Bin in $PotentialBinaries) {
                try {
                    # If GetAssemblyName succeeds, it's a managed .NET assembly
                    $null = [System.Reflection.AssemblyName]::GetAssemblyName($Bin.FullName)
                } catch {
                    # If it throws, it's an unmanaged/native binary
                    Write-Host "   Deleting native binary from managed root: $($Bin.Name)"
                    Remove-Item $Bin.FullName -Force
                }
            }
        }
    } else {
        Write-Warning "   DjvuNet.Build.Tasks.dll not found in $FwName, skipping native binary cleanup."
    }

    $AllPassed = $true

    Write-Host "  Listing files in managed root (relative to Tools):"
    Get-ChildItem -Path $ManagedRoot -File | Select-Object @{Name="RelativePath";Expression={$_.FullName.Substring($ToolsDir.Length + 1)}} | Format-Table -HideTableHeaders
    
    Write-Host "  Verifying Assemblies in managed root:"
    $Assemblies = Get-ChildItem -Path $ManagedRoot -Filter "*.dll"
    foreach ($Asm in $Assemblies) {
        if ($FwName -ne $CoreFwName -and $Asm.Name -match "^git2-") {
            Write-Host "    [PASS] $($Asm.Name) - Permitted side-by-side native binary for legacy framework."
            continue
        }
        try {
            $AssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($Asm.FullName)
            Write-Host "    [PASS] $($Asm.Name) - Version: $($AssemblyName.Version)"
        } catch {
            Write-Host "    [FAIL] $($Asm.Name) is an unmanaged/native binary that escaped cleanup!"
            $AllPassed = $false
        }
    }
    
    if ($FwName -eq $CoreFwName) {
        Write-Host "  Verifying deps.json structure for $($FwName):"
        $DepsFiles = Get-ChildItem -Path $ManagedRoot -Filter "*.deps.json" -Recurse
        foreach ($Deps in $DepsFiles) {
            $Json = Get-Content $Deps.FullName -Raw | ConvertFrom-Json
            $AppNode = $Json.targets | Select-Object -Property $CoreAppNodeName
            if ($AppNode) {
                $LibGitNode = $AppNode."$CoreAppNodeName" | Select-Object -Property $NativeBinariesKey
                if ($LibGitNode -and $LibGitNode."$NativeBinariesKey") {
                    $Runtimes = $LibGitNode."$NativeBinariesKey".runtimeTargets.psobject.properties.name
                    Write-Host "    Found $NativeBinariesKey with $(@($Runtimes).Count) runtimes in $($Deps.Name)."
                    if (@($Runtimes).Count -gt 5) {
                        Write-Host "    [PASS] Cross-platform native dependencies are present."
                    } else {
                        Write-Host "    [FAIL] Missing cross-platform native dependencies in $($Deps.Name)! Found: $Runtimes"
                        $AllPassed = $false
                    }
                }
            }
        }
    }

    if (-not $AllPassed) {
        Write-Error "Verification failed for $FwName! Halting packaging process to prevent corrupted artifacts."
        exit 1
    }
}

Write-Host "`n5. Ensuring clean output directory..."
if (Test-Path $OutputDir) {
    Write-Host "   Deleting existing $OutputDir..."
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

Write-Host "6. Creating Tools.zip..."
$ZipPath = "$OutputDir/Tools.zip"
# Compress only the DjvuNet folder
Compress-Archive -Path "$ToolsDir/DjvuNet" -DestinationPath $ZipPath -Force

Write-Host "7. Creating Tools.tar.gz..."
$TarGzPath = "$OutputDir/Tools.tar.gz"
# Change directory to ensure the archive root is DjvuNet
Set-Location $WorkspaceRoot
tar -czf $TarGzPath -C "Tools" "DjvuNet"

$DepsDir = "$WorkspaceRoot/deps"
if (Test-Path $DepsDir) {
    Write-Host "8. Creating deps.zip..."
    $DepsZipPath = "$OutputDir/deps.zip"
    Compress-Archive -Path "$DepsDir/*" -DestinationPath $DepsZipPath -Force

    Write-Host "9. Creating deps.tar.gz..."
    $DepsTarGzPath = "$OutputDir/deps.tar.gz"
    tar -czf $DepsTarGzPath -C "deps" .
} else {
    Write-Warning "Dependencies directory not found at $DepsDir. Skipping deps archive generation."
}

Write-Host "`nPackaging complete. Archives located at:"
$NormalizedZipPath = [System.IO.Path]::GetFullPath($ZipPath)
$NormalizedTarGzPath = [System.IO.Path]::GetFullPath($TarGzPath)
Write-Host "  - $NormalizedZipPath"
Write-Host "  - $NormalizedTarGzPath"
if (Test-Path $DepsDir) {
    Write-Host "  - $([System.IO.Path]::GetFullPath($DepsZipPath))"
    Write-Host "  - $([System.IO.Path]::GetFullPath($DepsTarGzPath))"
}
