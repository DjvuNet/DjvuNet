#
# Get-Tools.ps1
#

param(
    [parameter(Mandatory=$true)]$ToolsRemotePath,
    [parameter(Mandatory=$true)]$ToolsLocalPath,
    [parameter(Mandatory=$true)]$ToolsPath,
    [parameter(Mandatory=$true)]$ToolsName
)

$retryCount = 0
$success = $false

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

do {
    try {
        Write-Output "Downloading $ToolsName from $ToolsRemotePath"
        (New-Object Net.WebClient).DownloadFile($ToolsRemotePath, $ToolsLocalPath)
        $success = $true
    } catch {
        if ($retryCount -ge 6) {
            Write-Output "Maximum of 5 retries exceeded. Aborting"
            throw
        }
        else {
            $retryCount++
            $retryTime = 5 * $retryCount
            Write-Output "Download failed. Retrying in $retryTime seconds"
            Start-Sleep -Seconds (5 * $retryCount)
        }
    }
} while ($success -eq $false)

Write-Output "Download of $ToolsName finished"


if ([System.IO.Directory]::Exists($ToolsPath)) {
    Write-Output "Directory $ToolsPath already exists - skipping $ToolsName extraction"
    exit
}
else {
    Add-Type -Assembly 'System.IO.Compression.FileSystem' -ErrorVariable AddTypeErrors
    if ($AddTypeErrors.Count -eq 0) {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($ToolsLocalPath, $ToolsPath)
        Write-Output "Extracted $ToolsName into $ToolsPath"
    }
    else {
        (New-Object -com shell.application).namespace($DotnetPath).CopyHere((new-object -com shell.application).namespace($DotnetLocalPath).Items(), 16)
        Write-Output "Extracted $ToolsName into $ToolsPath with explorer"
    }
}
