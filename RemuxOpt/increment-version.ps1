param(
    [Parameter(Mandatory=$false)]
    [string]$CsprojPath = ".",
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

# Find the csproj file
if (Test-Path $CsprojPath -PathType Container) {
    $csprojFiles = Get-ChildItem -Path $CsprojPath -Filter "*.csproj"
    if ($csprojFiles.Count -eq 0) {
        Write-Error "No .csproj file found in directory: $CsprojPath"
        exit 1
    }
    elseif ($csprojFiles.Count -gt 1) {
        Write-Error "Multiple .csproj files found. Please specify the exact file path."
        exit 1
    }
    $CsprojPath = $csprojFiles[0].FullName
}
elseif (!(Test-Path $CsprojPath)) {
    Write-Error "File not found: $CsprojPath"
    exit 1
}

Write-Host "Processing: $CsprojPath"

# Read the csproj file
$content = Get-Content -Path $CsprojPath -Raw

# Define regex patterns for version elements
$patterns = @{
    "Version" = '<Version>(\d+)\.(\d+)\.(\d+)\.(\d+)</Version>'
    "AssemblyVersion" = '<AssemblyVersion>(\d+)\.(\d+)\.(\d+)\.(\d+)</AssemblyVersion>'
    "FileVersion" = '<FileVersion>(\d+)\.(\d+)\.(\d+)\.(\d+)</FileVersion>'
}

$changes = @()

foreach ($versionType in $patterns.Keys) {
    $pattern = $patterns[$versionType]
    
    if ($content -match $pattern) {
        $major = $matches[1]
        $minor = $matches[2]
        $build = $matches[3]
        $revision = [int]$matches[4]
        
        $newRevision = $revision + 1
        $oldVersion = "$major.$minor.$build.$revision"
        $newVersion = "$major.$minor.$build.$newRevision"
        
        $oldTag = "<$versionType>$oldVersion</$versionType>"
        $newTag = "<$versionType>$newVersion</$versionType>"
        
        $changes += @{
            Type = $versionType
            Old = $oldVersion
            New = $newVersion
            OldTag = $oldTag
            NewTag = $newTag
        }
        
        Write-Host "$versionType`: $oldVersion -> $newVersion"
    }
    else {
        Write-Warning "$versionType not found or doesn't match expected format"
    }
}

if ($changes.Count -eq 0) {
    Write-Error "No version elements found to update"
    exit 1
}

if ($WhatIf) {
    Write-Host "`nWhatIf mode - no changes will be made"
    Write-Host "The following changes would be applied:"
    foreach ($change in $changes) {
        Write-Host "  $($change.Type): $($change.Old) -> $($change.New)"
    }
}
else {
    # Apply changes
    $newContent = $content
    foreach ($change in $changes) {
        $newContent = $newContent -replace [regex]::Escape($change.OldTag), $change.NewTag
    }
    
    # Write back to file
    Set-Content -Path $CsprojPath -Value $newContent -NoNewline
    
    Write-Host "`nVersion incremented successfully!"
    Write-Host "Changes applied:"
    foreach ($change in $changes) {
        Write-Host "  $($change.Type): $($change.Old) -> $($change.New)"
    }
}