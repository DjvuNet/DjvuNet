<#
.SYNOPSIS
    Multi-pass diagnostic script to find quoting errors by recursively resolving batch variables.
.DESCRIPTION
    cmd.exe has several "poison characters" that break command parsing if unquoted:
    Semicolon (;), Ampersand (&), Pipe (|), Less-than (<), Greater-than (>)

    Pass 1: Records all visible 'set' assignments without expanding them.
    Pass 2: Recursively resolves the variables to determine their final expanded values.
    Pass 3: Confirms quoting requirements based on whether poison characters are literal or inherited.
#>
param(
    [Parameter(Mandatory=$true)][string]$FilePath,
    [switch]$Fix
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -Path $FilePath -PathType Leaf)) {
    Write-Error "File not found: $FilePath"
}

$lines = Get-Content $FilePath
$env = @{}
$assignments = @()

# Define characters that break unquoted cmd.exe evaluation
$poisonPattern = '[;&|<>]'

# PASS 1: Record visible assignments
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    $trimmed = $line.Trim()
    
    if ($trimmed.StartsWith("REM", [System.StringComparison]::InvariantCultureIgnoreCase) -or 
        $trimmed.StartsWith("::") -or 
        $trimmed -eq "") {
        continue
    }
    
    if ($trimmed -match "(?i)^set\s+(.+)$") {
        $assignText = $matches[1]
        $isOuterQuoted = $false
        
        if ($assignText -match '^"([^"=]+)=(.*)"$') {
            $name = $matches[1]
            $rawValue = $matches[2]
            $isOuterQuoted = $true
        } elseif ($assignText -match '^([^"=]+)=(.*)$') {
            $name = $matches[1]
            $rawValue = $matches[2]
        } else {
            continue
        }
        
        $strippedRaw = $rawValue -replace '""[^""]*""', ''
        
        $assignments += [PSCustomObject]@{
            LineNumber = $i + 1
            LineIndex = $i
            Original = $line
            Name = $name
            RawValue = $rawValue
            StrippedRaw = $strippedRaw
            IsOuterQuoted = $isOuterQuoted
            HasLiteralPoison = ($rawValue -match $poisonPattern)
            HasUnprotectedLiteralPoison = ($strippedRaw -match $poisonPattern)
        }
        
        $env[$name] = $rawValue
    }
}

# PASS 2: Recursive Resolution
function Resolve-Value($val, $visited) {
    if ([string]::IsNullOrEmpty($val)) { return "" }
    
    $resolved = $val
    # Match %VAR% or !VAR!
    $pattern = '(?i)[%!](\w+)[%!]'
    $matches = [regex]::Matches($resolved, $pattern)
    
    foreach ($m in $matches) {
        $varName = $m.Groups[1].Value
        if ($visited -contains $varName) { continue }
        
        $childVal = ""
        if ($env.Contains($varName)) {
            $childVal = Resolve-Value -val $env[$varName] -visited ($visited + @($varName))
        }
        $resolved = $resolved.Replace($m.Value, $childVal)
    }
    return $resolved
}

# PASS 3: Detect Errors
$findings = @()

foreach ($assign in $assignments) {
    $resolved = Resolve-Value -val $assign.RawValue -visited @()
    $strippedResolved = $resolved -replace '""[^""]*""', ''
    
    $needsFix = $false
    $proposedFix = ""
    $errorReason = ""
    
    $hasUnprotectedPoisonInResolved = ($strippedResolved -match $poisonPattern)
    
    if ($assign.HasUnprotectedLiteralPoison) {
        # If the line ITSELF contains an unprotected literal poison character
        if ($assign.RawValue -match '(?i)-flp|/flp|/p:|-p:') {
            # It introduces an MSBuild flag. It MUST have literal inner quotes around the poison.
            # We fix by wrapping the entire value in """" if it wasn't already.
            if ($assign.RawValue -notmatch '^".*"$') {
                $needsFix = $true
                $errorReason = "MSBuild flag with special chars missing inner literal quotes"
                $leadingSpace = $assign.Original -replace "\S.*", ""
                $proposedFix = $leadingSpace + 'set "' + $assign.Name + '="' + $assign.RawValue + '""'
            }
        } elseif (-not $assign.IsOuterQuoted) {
            # Standard variable with literal poison char lacking outer quotes
            $needsFix = $true
            $errorReason = "Literal special char breaks unquoted SET command"
            $leadingSpace = $assign.Original -replace "\S.*", ""
            $proposedFix = "${leadingSpace}set ""$($assign.Name)=$($assign.RawValue)"""
        }
    } elseif ($hasUnprotectedPoisonInResolved -and -not $assign.IsOuterQuoted) {
        # The line inherits an unprotected poison character and lacks outer quotes.
        # If using %VAR%, cmd.exe expands it during parse, breaking SET if it contains poison chars.
        if ($assign.RawValue -match '%\w+%') {
            $needsFix = $true
            $errorReason = "Inherited special char via %VAR% breaks unquoted SET command"
            $leadingSpace = $assign.Original -replace "\S.*", ""
            $proposedFix = "${leadingSpace}set ""$($assign.Name)=$($assign.RawValue)"""
        }
    }
    
    if ($needsFix) {
        $findings += [PSCustomObject]@{
            LineNumber = $assign.LineNumber
            Name = $assign.Name
            Original = $assign.Original
            Resolved = $resolved
            Reason = $errorReason
            ProposedFix = $proposedFix
            LineIndex = $assign.LineIndex
        }
    }
}

if ($findings.Count -gt 0) {
    Write-Host "Found $($findings.Count) quoting issues:`n" -ForegroundColor Yellow
    foreach ($finding in $findings) {
        Write-Host "Line $($finding.LineNumber): $($finding.Reason)" -ForegroundColor Red
        Write-Host "  Original: $($finding.Original)" -ForegroundColor Cyan
        Write-Host "  Resolved: $($finding.Resolved)" -ForegroundColor DarkGray
        if ($finding.ProposedFix) {
            Write-Host "  Proposed: $($finding.ProposedFix)" -ForegroundColor Green
        }
        Write-Host ""
    }
    
    if ($Fix) {
        $allLines = Get-Content $FilePath
        foreach ($finding in $findings) {
            $allLines[$finding.LineIndex] = $finding.ProposedFix
        }
        
        $directory = [System.IO.Path]::GetDirectoryName($FilePath)
        $fileName = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
        $extension = [System.IO.Path]::GetExtension($FilePath)
        if ([string]::IsNullOrEmpty($directory)) { $directory = "." }
        $outPath = Join-Path $directory "$fileName-qfix$extension"
        
        Set-Content -Path $outPath -Value $allLines
        Write-Host "Changes saved to $outPath" -ForegroundColor Green
    } else {
        Write-Host "Run with -Fix switch to output corrections to a new file."
    }
} else {
    Write-Host "No quoting issues found." -ForegroundColor Green
}