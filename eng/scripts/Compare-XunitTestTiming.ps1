<#
.SYNOPSIS
    Parses two xUnit v3 XML test result files and compares execution times.

.DESCRIPTION
    This script reads two xUnit XML files (representing a 'long' and 'short' test run).
    It extracts individual test execution times and also aggregates parameterized 
    tests (e.g., [Theory] elements) into a "[THEORY TOTAL]" row.
    Times are converted to integer microseconds to avoid Excel floating-point 
    import bugs and time-format truncation.

.PARAMETER LongFile
    Path to the xUnit XML file representing the long execution time run.

.PARAMETER ShortFile
    Path to the xUnit XML file representing the short execution time run.

.PARAMETER OutputFile
    (Optional) Path to save the CSV output. If not provided, CSV is output to the console.
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$LongFile,

    [Parameter(Mandatory=$true)]
    [string]$ShortFile,

    [Parameter(Mandatory=$false)]
    [string]$OutputFile
)

function Get-TestTimes {
    param (
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Error "File not found: $FilePath"
        return @{}
    }

    $xml = New-Object System.Xml.XmlDocument
    $settings = New-Object System.Xml.XmlReaderSettings
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = [System.Xml.XmlReader]::Create((Resolve-Path $FilePath).ProviderPath, $settings)
    $xml.Load($reader)
    $reader.Close()
    $testTimes = @{}

    $testNodes = $xml.SelectNodes("//test")
    if ($null -eq $testNodes) { return $testTimes }

    $nameCounts = @{}

    foreach ($node in $testNodes) {
        $testName = $node.name
        $timeStr = $node.time

        if ([string]::IsNullOrWhiteSpace($testName) -or [string]::IsNullOrWhiteSpace($timeStr)) { continue }

        $nameCounts[$testName]++
        if ($nameCounts[$testName] -gt 1) {
            $testName = "${testName}_dup$($nameCounts[$node.name])"
        }

        $time = 0.0
        if (-not [double]::TryParse($timeStr, [System.Globalization.NumberStyles]::Any, [cultureinfo]::InvariantCulture, [ref]$time)) { continue }

        # Convert to exact integer microseconds to prevent float import issues in Excel
        $timeUs = [long][math]::Round($time * 1000000)

        # 1. Store individual run
        $testTimes[$testName] = $timeUs

        # 2. Aggregate for theories
        if ($testName -match '[\(\[]') {
            $baseName = ($testName -replace '[\(\[].*$', '').Trim() + ' [THEORY TOTAL]'
            if ($testTimes.ContainsKey($baseName)) {
                $testTimes[$baseName] += $timeUs
            } else {
                $testTimes[$baseName] = $timeUs
            }
        }
    }

    return $testTimes
}

Write-Host "Parsing long file: $LongFile" -ForegroundColor Cyan
$longTimes = Get-TestTimes -FilePath $LongFile

Write-Host "Parsing short file: $ShortFile" -ForegroundColor Cyan
$shortTimes = Get-TestTimes -FilePath $ShortFile

$allTests = @($longTimes.Keys) + @($shortTimes.Keys) | Select-Object -Unique | Sort-Object

$results = foreach ($test in $allTests) {
    $longTime = if ($longTimes.ContainsKey($test)) { $longTimes[$test] } else { [long]0 }
    $shortTime = if ($shortTimes.ContainsKey($test)) { $shortTimes[$test] } else { [long]0 }
    $diff = $longTime - $shortTime

    # Export using microseconds
    [PSCustomObject]@{
        "FQN Method Name"           = $test
        "Long Execution Time (us)"  = $longTime
        "Short Execution Time (us)" = $shortTime
        "Difference (us)"           = $diff
    }
}

# Sort by absolute difference descending
$results = $results | Sort-Object { [math]::Abs($_."Difference (us)") } -Descending

if ([string]::IsNullOrWhiteSpace($OutputFile)) {
    $results | ConvertTo-Csv -NoTypeInformation
} else {
    $results | Export-Csv -Path $OutputFile -NoTypeInformation -Encoding utf8BOM
    Write-Host "CSV output saved to: $OutputFile" -ForegroundColor Green
}

# Calculate total short run time excluding theory aggregates to avoid double counting
$totalShortTimeUs = 0
foreach ($key in $shortTimes.Keys) {
    if ($key -notmatch '\[THEORY TOTAL\]$') {
        $totalShortTimeUs += $shortTimes[$key]
    }
}

Write-Host ""
Write-Host "Top 10 Largest Execution Time Differences:" -ForegroundColor Cyan

$tableData = @()
$top10 = $results | Select-Object -First 10
foreach ($row in $top10) {
    $diff = $row."Difference (us)"
    $pct = 0.0
    if ($totalShortTimeUs -gt 0) {
        $pct = ($diff / $totalShortTimeUs) * 100
    }
    
    # Format with space as thousands separator
    $formattedDiff = $diff.ToString('#,0', [cultureinfo]::InvariantCulture).Replace(',', ' ')
    
    $tableData += [PSCustomObject]@{
        "% of Short Run"   = "{0:N3}%" -f $pct
        "Time Diff (us)"   = $formattedDiff
        "Method Name"      = $row.'FQN Method Name'
    }
}

$tableData | Format-Table -Wrap -AutoSize
