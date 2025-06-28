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

try {
    # Load XML document properly
    $xmlDoc = New-Object System.Xml.XmlDocument
    $xmlDoc.PreserveWhitespace = $true
    $xmlDoc.Load($CsprojPath)
    
    if ($null -eq $xmlDoc.DocumentElement -or $xmlDoc.DocumentElement.Name -ne "Project") {
        throw "Invalid project file - missing or invalid Project root element"
    }
    
    $changes = @()
    $versionElements = @('Version', 'AssemblyVersion', 'FileVersion')
    
    foreach ($versionType in $versionElements) {
        # Find the element in any PropertyGroup
        $versionNode = $xmlDoc.SelectSingleNode("//PropertyGroup/$versionType")
        
        if ($null -ne $versionNode -and $versionNode.InnerText -match '^\d+\.\d+\.\d+\.\d+$') {
            $versionParts = $versionNode.InnerText -split '\.'
            $major = [int]$versionParts[0]
            $minor = [int]$versionParts[1]
            $build = [int]$versionParts[2]
            $revision = [int]$versionParts[3]
            
            $newRevision = $revision + 1
            $oldVersion = "$major.$minor.$build.$revision"
            $newVersion = "$major.$minor.$build.$newRevision"
            
            $changes += @{
                Type = $versionType
                Old = $oldVersion
                New = $newVersion
                Node = $versionNode
            }
            
            Write-Host "$versionType`: $oldVersion -> $newVersion"
        }
        else {
            Write-Warning "$versionType not found or doesn't match expected format (x.x.x.x)"
        }
    }
    
    if ($changes.Count -eq 0) {
        Write-Warning "No version elements found to update"
        exit 0
    }
    
    if ($WhatIf) {
        Write-Host "`nWhatIf mode - no changes will be made"
        Write-Host "The following changes would be applied:"
        foreach ($change in $changes) {
            Write-Host "  $($change.Type): $($change.Old) -> $($change.New)"
        }
    }
    else {
        # Apply changes to XML nodes
        foreach ($change in $changes) {
            $change.Node.InnerText = $change.New
        }
        
        # Save the document with proper formatting
        $xmlSettings = New-Object System.Xml.XmlWriterSettings
        $xmlSettings.Indent = $true
        $xmlSettings.IndentChars = "  "
        $xmlSettings.NewLineChars = "`r`n"
        $xmlSettings.Encoding = [System.Text.UTF8Encoding]::new($false) # UTF-8 without BOM
        
        $xmlWriter = [System.Xml.XmlWriter]::Create($CsprojPath, $xmlSettings)
        $xmlDoc.Save($xmlWriter)
        $xmlWriter.Close()
        
        Write-Host "`nVersion incremented successfully!"
        Write-Host "Changes applied:"
        foreach ($change in $changes) {
            Write-Host "  $($change.Type): $($change.Old) -> $($change.New)"
        }
    }
}
catch {
    Write-Error "Error processing csproj file: $($_.Exception.Message)"
    exit 1
}