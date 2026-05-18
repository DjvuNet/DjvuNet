param(
    [Parameter(Mandatory=$true)]
    [string]$RunId,
    [Parameter(Mandatory=$false)]
    [string]$ArtifactName = "msbuild-logs"
)

$ErrorActionPreference = "Stop"

if (-not $env:GITHUB_DJVUNET_MCP_PAT) {
    Write-Error "GITHUB_DJVUNET_MCP_PAT environment variable is not set."
    exit 1
}

$repoOwner = "DjvuNet"
$repoName = "DjvuNet"
$headers = @{
    Authorization = "Bearer $($env:GITHUB_DJVUNET_MCP_PAT)"
    Accept = "application/vnd.github.v3+json"
}

# Traverse up from eng/scripts to the root, then to build/
$repoRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$buildDir = Join-Path $repoRoot "build"
$logsExtractDir = Join-Path $buildDir "ci_logs_$RunId"

if (-not (Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
}

if (-not (Test-Path $logsExtractDir)) {
    New-Item -ItemType Directory -Path $logsExtractDir | Out-Null
}

Write-Host "Fetching artifacts for Run ID $RunId..."
$artifactsUrl = "https://api.github.com/repos/$repoOwner/$repoName/actions/runs/$RunId/artifacts"

try {
    $response = Invoke-RestMethod -Uri $artifactsUrl -Headers $headers
} catch {
    Write-Error "Failed to fetch artifacts list: $_"
    exit 1
}

if ($response.total_count -eq 0) {
    Write-Error "No artifacts found for this run."
    exit 1
}

foreach ($artifact in $response.artifacts) {
    if ($artifact.name -match $ArtifactName) {
        Write-Host "Downloading artifact '$($artifact.name)' ($($artifact.size_in_bytes) bytes)..."
        $downloadUrl = $artifact.archive_download_url
        $zipFilePath = Join-Path $buildDir "$($artifact.name).zip"
        
        try {
            Invoke-RestMethod -Uri $downloadUrl -Headers $headers -OutFile $zipFilePath
            
            $extractPath = Join-Path $logsExtractDir $artifact.name
            New-Item -ItemType Directory -Path $extractPath -Force | Out-Null
            Expand-Archive -Path $zipFilePath -DestinationPath $extractPath -Force
            Remove-Item $zipFilePath -Force
            Write-Host "Extracted $($artifact.name) to $extractPath"
        } catch {
            Write-Warning "Failed to download or extract $($artifact.name): $_"
        }
    }
}

Write-Host "Successfully downloaded and extracted logs to $logsExtractDir"
