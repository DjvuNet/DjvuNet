#
# Get-Tools.ps1
#

param(
    [parameter(Mandatory=$true)]$ToolsRemotePath,
    [parameter(Mandatory=$true)]$ToolsLocalPath,
    [parameter(Mandatory=$true)]$ToolsPath,
    [parameter(Mandatory=$true)]$ToolsName,
    [parameter(Mandatory=$true)]$ToolsSemaphoreFileName,
    [parameter(Mandatory=$true)]$MessagePrefix
)

$ErrorActionPreference = "Stop"
$MessagePrefix = $MessagePrefix.Trim()
$retryCount = 0
$success = $false
$semaphoreFile = [System.IO.Path]::Combine($ToolsPath, $ToolsSemaphoreFileName)
$semaphoreFile = [System.IO.Path]::GetFullPath($semaphoreFile)

if ([System.IO.Directory]::Exists($ToolsPath)) {
    if ([System.IO.File]::Exists($semaphoreFile)) {
        Write-Output "$MessagePrefix $ToolsName already restored"
        exit
    }
    else {
        Write-Output "$MessagePrefix Not found sempahore file: $semaphoreFile"
        [System.IO.Directory]::Delete($ToolsPath, $true)
        Write-Output "$MessagePrefix Deleted $ToolsPath directory"
    }
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

do {
    try {
        Write-Output "$MessagePrefix Downloading $ToolsName from $ToolsRemotePath"
        (New-Object Net.WebClient).DownloadFile($ToolsRemotePath, $ToolsLocalPath)
        $success = $true
    } catch {
        if ($retryCount -ge 6) {
            Write-Output "$MessagePrefix Maximum of 5 retries exceeded. Aborting"
            throw
        }
        else {
            $retryCount++
            $retryTime = 5 * $retryCount
            Write-Output "$MessagePrefix Download failed. Retrying in $retryTime seconds"
            Start-Sleep -Seconds (5 * $retryCount)
        }
    }
} while ($success -eq $false)

Write-Output "$MessagePrefix Download of $ToolsName finished"


Add-Type -Assembly 'System.IO.Compression.FileSystem' -ErrorVariable AddTypeErrors
if ($AddTypeErrors.Count -eq 0) {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ToolsLocalPath, $ToolsPath)
    Write-Output "$MessagePrefix Extracted $ToolsName into $ToolsPath"
}
else {
    (New-Object -com shell.application).namespace($DotnetPath).CopyHere((new-object -com shell.application).namespace($DotnetLocalPath).Items(), 16)
    Write-Output "$MessagePrefix Extracted $ToolsName into $ToolsPath with explorer"
}

[System.IO.File]::WriteAllText($semaphoreFile, "")
Write-Host "$MessagePrefix Created $ToolsName semaphore $semaphoreFile"
