param(
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "RELEASE_NOTES.md",
    
    [Parameter(Mandatory=$false)]
    [string]$FromTag = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ToTag = "HEAD",
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeAll
)

# Function to get version from csproj if not provided
function Get-VersionFromCsproj {
    $csprojFiles = Get-ChildItem -Filter "*.csproj" -Recurse | Select-Object -First 1
    if ($csprojFiles) {
        $content = Get-Content $csprojFiles.FullName -Raw
        if ($content -match '<Version>([^<]+)</Version>') {
            return $matches[1]
        }
    }
    return "Unknown"
}

# Find git repository root
function Find-GitRoot {
    param([string]$StartPath = (Get-Location).Path)
    
    $currentPath = $StartPath
    while ($currentPath -and $currentPath -ne [System.IO.Path]::GetPathRoot($currentPath)) {
        if (Test-Path (Join-Path $currentPath ".git")) {
            return $currentPath
        }
        $currentPath = Split-Path $currentPath -Parent
    }
    return $null
}

# Find and change to git root directory
$gitRoot = Find-GitRoot
if ($null -eq $gitRoot) {
    Write-Warning "Not in a git repository - creating basic release notes from version info only"
    
    # Generate basic release notes without git
    if ([string]::IsNullOrEmpty($Version)) {
        $Version = Get-VersionFromCsproj
    }
    
    $basicNotes = @"
# Release Notes - Version $Version

**Release Date:** $(Get-Date -Format 'yyyy-MM-dd')

## Changes
- Version $Version release

---
*Release notes generated without git history*
"@
    
    Set-Content -Path $OutputPath -Value $basicNotes -Encoding UTF8
    Write-Host "Basic release notes generated: $OutputPath"
    exit 0
}

# Change to git root directory
Push-Location $gitRoot
Write-Host "Found git repository at: $gitRoot"

# Get version if not provided
if ([string]::IsNullOrEmpty($Version)) {
    $Version = Get-VersionFromCsproj
}

# Determine commit range
if ([string]::IsNullOrEmpty($FromTag)) {
    # Get the latest tag
    $latestTag = git describe --tags --abbrev=0 2>$null
    if ($LASTEXITCODE -eq 0 -and ![string]::IsNullOrEmpty($latestTag)) {
        $FromTag = $latestTag
        $commitRange = "$FromTag..$ToTag"
    } else {
        # No tags found, use all commits
        $commitRange = $ToTag
        $FromTag = "Initial commit"
    }
} else {
    $commitRange = "$FromTag..$ToTag"
}

Write-Host "Generating release notes for version $Version"
Write-Host "Commit range: $commitRange"

# Get commit messages
$commits = git log $commitRange --pretty=format:"%s|%h|%an|%ad" --date=short 2>$null

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrEmpty($commits)) {
    Write-Warning "No commits found in range $commitRange"
    exit 0
}

# Parse commits and categorize
$features = @()
$fixes = @()
$breaking = @()
$other = @()

foreach ($commit in $commits) {
    if ([string]::IsNullOrEmpty($commit)) { continue }
    
    $parts = $commit -split '\|'
    if ($parts.Length -lt 4) { continue }
    
    $message = $parts[0].Trim()
    $hash = $parts[1].Trim()
    $author = $parts[2].Trim()
    $date = $parts[3].Trim()
    
    # Skip merge commits unless IncludeAll is specified
    if (!$IncludeAll -and ($message -match "^Merge " -or $message -match "^Merge pull request")) {
        continue
    }
    
    $commitInfo = @{
        Message = $message
        Hash = $hash
        Author = $author
        Date = $date
    }
    
    # Categorize commits based on conventional commit format or keywords
    switch -Regex ($message) {
        "^feat(\(.+\))?!?:" { $features += $commitInfo; break }
        "^fix(\(.+\))?!?:" { $fixes += $commitInfo; break }
        "^BREAKING CHANGE|!:" { $breaking += $commitInfo; break }
        "^(build|chore|ci|docs|style|refactor|perf|test)(\(.+\))?:" { 
            if ($IncludeAll) { $other += $commitInfo }; break 
        }
        default { 
            # Check for common keywords
            if ($message -match "\b(add|new|feature)\b" -and $message -notmatch "\b(fix|bug)\b") {
                $features += $commitInfo
            } elseif ($message -match "\b(fix|bug|issue|resolve)\b") {
                $fixes += $commitInfo
            } elseif ($IncludeAll -or $message -match "\b(update|improve|enhance)\b") {
                $other += $commitInfo
            }
        }
    }
}

# Generate release notes content
$releaseNotes = @"
# Release Notes - Version $Version

**Release Date:** $(Get-Date -Format 'yyyy-MM-dd')

"@

if ($breaking.Count -gt 0) {
    $releaseNotes += @"

## 🚨 Breaking Changes
"@
    foreach ($commit in $breaking) {
        $cleanMessage = $commit.Message -replace "^[^:]+:\s*", ""
        $releaseNotes += "`n- $cleanMessage [$($commit.Hash)]"
    }
    $releaseNotes += "`n"
}

if ($features.Count -gt 0) {
    $releaseNotes += @"

## 🚀 New Features
"@
    foreach ($commit in $features) {
        $cleanMessage = $commit.Message -replace "^feat(\(.+\))?!?:\s*", ""
        $releaseNotes += "`n- $cleanMessage [$($commit.Hash)]"
    }
    $releaseNotes += "`n"
}

if ($fixes.Count -gt 0) {
    $releaseNotes += @"

## 🐛 Bug Fixes
"@
    foreach ($commit in $fixes) {
        $cleanMessage = $commit.Message -replace "^fix(\(.+\))?!?:\s*", ""
        $releaseNotes += "`n- $cleanMessage [$($commit.Hash)]"
    }
    $releaseNotes += "`n"
}

if ($other.Count -gt 0) {
    $releaseNotes += @"

## 🔧 Other Changes
"@
    foreach ($commit in $other) {
        $cleanMessage = $commit.Message -replace "^[^:]+:\s*", ""
        $releaseNotes += "`n- $cleanMessage [$($commit.Hash)]"
    }
    $releaseNotes += "`n"
}

# Add commit range info
$releaseNotes += @"

---
**Full Changelog:** $FromTag...$Version
**Commits in this release:** $($features.Count + $fixes.Count + $breaking.Count + $other.Count)
"@

# Write to file
Set-Content -Path $OutputPath -Value $releaseNotes -Encoding UTF8

Write-Host "Release notes generated: $OutputPath"
Write-Host "Statistics:"
Write-Host "  Breaking Changes: $($breaking.Count)"
Write-Host "  New Features: $($features.Count)"
Write-Host "  Bug Fixes: $($fixes.Count)"
Write-Host "  Other Changes: $($other.Count)"

# Return to original directory
Pop-Location