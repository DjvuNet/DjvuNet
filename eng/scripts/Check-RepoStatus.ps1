<#
.SYNOPSIS
    Checks the status of a local Git repository and its corresponding GitHub remote.

.DESCRIPTION
    This script provides a comprehensive summary of a repository's state. It checks local Git 
    status, recent commits, and dynamically filters tags based on a time window. It also queries 
    the GitHub API using the GitHub CLI (gh) to retrieve recent remote commits on default and 
    current branches, open/closed issues (with closing reasons), and open/closed/merged pull 
    requests within the specified timespan.
    
    Crucially, it checks if the given Path is actually the root of a Git repository. If it is 
    merely a sub-folder of another repository (e.g. an extracted snapshot tarball), it skips
    local git commands to prevent duplicating the parent repository's status.

.PARAMETER Path
    The local file system path to the Git/snapshot directory.

.PARAMETER GhRepo
    The GitHub repository identifier in the format "owner/repo" (e.g., "DjvuNet/DjvuNet").

.PARAMETER Since
    A string parseable as a System.TimeSpan (e.g., "14.00:00:00" for 14 days) or an integer representing days. 
    Defaults to 14 days.
#>
param (
    [string]$Path,
    [string]$GhRepo,
    [string]$Since = "14"
)

if (-not (Test-Path $Path)) {
    Write-Host "Path $Path does not exist. Skipping."
    return
}

$timeSpan = $null
if ([int]::TryParse($Since, [ref]$null)) {
    $timeSpan = [TimeSpan]::FromDays([int]$Since)
} else {
    $timeSpan = [TimeSpan]::Parse($Since)
}
$sinceDateObj = (Get-Date).Subtract($timeSpan)
$sinceDate = $sinceDateObj.ToString("yyyy-MM-dd")
$sinceIso = $sinceDateObj.ToString("yyyy-MM-ddTHH:mm:ssZ")

Push-Location $Path
Write-Host "================================================================"
Write-Host "Checking repository: $GhRepo at $(Get-Location)"
Write-Host "Checking data since: $sinceDate"
Write-Host "================================================================"

# Determine if this directory is the root of its own git repo
$isGitRoot = $false
$gitTopLevel = (git rev-parse --show-toplevel 2>$null)
if ($gitTopLevel) {
    # Check if we are at the top level of the git repository using --show-cdup.
    # It returns an empty string if we are at the root, and a relative path if in a subdirectory.
    # This correctly handles junctions, symlinks, and worktrees where path string comparison fails.
    $cdup = (git rev-parse --show-cdup 2>$null)
    if ([string]::IsNullOrWhiteSpace($cdup)) {
        $isGitRoot = $true
    }
}

if ($isGitRoot) {
    Write-Host "`n--- Git Status ---"
    git status

    Write-Host "`n--- Git Diff HEAD (Summary) ---"
    git diff --stat HEAD
    
    # Always dump full diff to a file to prevent terminal flooding while preserving data
    git diff HEAD > repo_status_diff.patch
    Write-Host "  -> Full diff dumped to: repo_status_diff.patch"

    Write-Host "`n--- Recent Commits (Local) ---"
    git log -n 3

    Write-Host "`n--- Recent Tags (Since $sinceDate) ---"
    $tags = git for-each-ref --sort=-creatordate --format="%(creatordate:iso8601)|%(refname:short)" refs/tags
    $foundTags = $false
    foreach ($tag in $tags) {
        if ([string]::IsNullOrWhiteSpace($tag)) { continue }
        $parts = $tag -split '\|'
        $tagDate = [DateTime]::Parse($parts[0])
        if ($tagDate -ge $sinceDateObj) {
            Write-Host $parts[1]
            $foundTags = $true
        } else {
            break # Tags are sorted descending, so we can stop searching early
        }
    }
    if (-not $foundTags) { Write-Host "No tags found since $sinceDate" }
} else {
    Write-Host "`n--- Local Git Status ---"
    Write-Host "Directory is not a Git repository root (likely an extracted snapshot). Skipping local git checks to avoid duplication."
}

if ($GhRepo) {
    $currentBranch = $null
    if ($isGitRoot) {
        $currentBranch = (git rev-parse --abbrev-ref HEAD 2>$null).Trim()
    }
    
    $defaultBranch = (gh repo view $GhRepo --json defaultBranchRef --jq .defaultBranchRef.name 2>$null).Trim()
    if (-not $defaultBranch) { $defaultBranch = "master" }

    Write-Host "`n--- Recent Remote Commits: Default Branch ($defaultBranch) (Since $sinceDate) ---"
    gh api "/repos/$GhRepo/commits?sha=$defaultBranch&since=$sinceIso" 2>$null | ConvertFrom-Json | ForEach-Object { Write-Host ([string]$_.commit.message).Split("`n")[0].Trim() }

    if ($currentBranch -and $currentBranch -ne $defaultBranch) {
        Write-Host "`n--- Recent Remote Commits: Current Branch ($currentBranch) (Since $sinceDate) ---"
        gh api "/repos/$GhRepo/commits?sha=$currentBranch&since=$sinceIso" 2>$null | ConvertFrom-Json | ForEach-Object { Write-Host ([string]$_.commit.message).Split("`n")[0].Trim() }
    }

    Write-Host "`n--- Open Issues (Since $sinceDate) ---"
    gh issue list --repo $GhRepo --state open --search "updated:>=$sinceDate"

    Write-Host "`n--- Closed Issues (Since $sinceDate) ---"
    gh issue list --repo $GhRepo --state closed --search "updated:>=$sinceDate" --json number,title,stateReason --template '{{range .}}{{tablerow .number .title .stateReason}}{{end}}'

    Write-Host "`n--- Open PRs (Since $sinceDate) ---"
    gh pr list --repo $GhRepo --state open --search "updated:>=$sinceDate"

    Write-Host "`n--- Closed/Merged PRs (Since $sinceDate) ---"
    gh pr list --repo $GhRepo --state closed --search "updated:>=$sinceDate" --json number,title,state --template '{{range .}}{{tablerow .number .title .state}}{{end}}'
}

Pop-Location