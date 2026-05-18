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
    [string]$RepoRoot
)

$ErrorActionPreference = "Stop"

# Ensure we are working with an absolute path and resolving separators
$WorkspaceRoot = (Resolve-Path -Path $RepoRoot).Path
$ToolsDir = "$WorkspaceRoot/Tools"
$OutputDir = "$WorkspaceRoot/build/artifacts/tools"

Write-Host "1. Killing stray dotnet processes..."
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "2. Cleaning up .bak files in Tools directory..."
if (-not (Test-Path $ToolsDir)) {
    Write-Warning "Tools directory not found at $ToolsDir. Nothing to package."
    exit 1
}
Get-ChildItem -Path $ToolsDir -Filter "*.bak" -Recurse | Remove-Item -Force

Write-Host "3. Removing redundant native binaries from managed root..."
$TaskAssembly = Get-ChildItem -Path $ToolsDir -Filter "DjvuNet.Build.Tasks.dll" -Recurse | Select-Object -First 1
if ($TaskAssembly) {
    $ManagedRoot = $TaskAssembly.Directory.FullName
    Write-Host "   Found managed root at: $ManagedRoot"
    
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
} else {
    Write-Warning "   DjvuNet.Build.Tasks.dll not found, skipping native binary cleanup."
}

Write-Host "`n--- 4. VERIFICATION PHASE ---"
function Verify-ToolsDirectory {
    param([string]$Path)
    $AllPassed = $true

    Write-Host "  Listing files in managed root (relative to Tools):"
    Get-ChildItem -Path $ManagedRoot -File | Select-Object @{Name="RelativePath";Expression={$_.FullName.Substring($Path.Length + 1)}} | Format-Table -HideTableHeaders
    
    Write-Host "  Verifying Assemblies in managed root:"
    $Assemblies = Get-ChildItem -Path $ManagedRoot -Filter "*.dll"
    foreach ($Asm in $Assemblies) {
        try {
            $AssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($Asm.FullName)
            Write-Host "    [PASS] $($Asm.Name) - Version: $($AssemblyName.Version)"
        } catch {
            Write-Host "    [FAIL] $($Asm.Name) is an unmanaged/native binary that escaped cleanup!"
            $AllPassed = $false
        }
    }
    
    Write-Host "  Verifying deps.json structure:"
    $DepsFiles = Get-ChildItem -Path $Path -Filter "*.deps.json" -Recurse
    foreach ($Deps in $DepsFiles) {
        $Json = Get-Content $Deps.FullName -Raw | ConvertFrom-Json
        $AppNode = $Json.targets | Select-Object -Property ".NETCoreApp,Version=v10.0"
        if ($AppNode) {
            $LibGitNode = $AppNode.".NETCoreApp,Version=v10.0" | Select-Object -Property "LibGit2Sharp.NativeBinaries/2.0.323"
            if ($LibGitNode -and $LibGitNode."LibGit2Sharp.NativeBinaries/2.0.323") {
                $Runtimes = $LibGitNode."LibGit2Sharp.NativeBinaries/2.0.323".runtimeTargets.psobject.properties.name
                Write-Host "    Found LibGit2Sharp.NativeBinaries with $(@($Runtimes).Count) runtimes in $($Deps.Name)."
                if (@($Runtimes).Count -gt 5) {
                    Write-Host "    [PASS] Cross-platform native dependencies are present."
                } else {
                    Write-Host "    [FAIL] Missing cross-platform native dependencies in $($Deps.Name)! Found: $Runtimes"
                    $AllPassed = $false
                }
            }
        }
    }

    if (-not $AllPassed) {
        Write-Error "Verification failed! Halting packaging process to prevent corrupted artifacts."
        exit 1
    }
}

Verify-ToolsDirectory -Path $ToolsDir

Write-Host "`n5. Ensuring clean output directory..."
if (Test-Path $OutputDir) {
    Write-Host "   Deleting existing $OutputDir..."
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

Write-Host "6. Creating Tools.zip..."
$ZipPath = "$OutputDir/Tools.zip"
# Compress the contents of the Tools directory
Compress-Archive -Path "$ToolsDir/*" -DestinationPath $ZipPath

Write-Host "7. Creating Tools.tar.gz..."
$TarGzPath = "$OutputDir/Tools.tar.gz"
# Change directory to ensure the archive root is DjvuNet
Set-Location $WorkspaceRoot
tar -czf $TarGzPath -C "Tools" "DjvuNet"

Write-Host "`nPackaging complete. Archives located at:"
$NormalizedZipPath = [System.IO.Path]::GetFullPath($ZipPath)
$NormalizedTarGzPath = [System.IO.Path]::GetFullPath($TarGzPath)
Write-Host "  - $NormalizedZipPath"
Write-Host "  - $NormalizedTarGzPath"
