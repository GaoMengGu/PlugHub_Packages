param(
    [string]$Root = (Resolve-Path "$PSScriptRoot\..").Path
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure {
    param([string]$Message)
    $failures.Add($Message)
}

function Require-File {
    param([string]$RelativePath)
    $path = Join-Path $Root $RelativePath
    if (!(Test-Path -LiteralPath $path)) {
        Add-Failure "Missing file: $RelativePath"
    }
}

function Require-Text {
    param(
        [string]$RelativePath,
        [string]$Pattern,
        [string]$Description
    )

    $path = Join-Path $Root $RelativePath
    if (!(Test-Path -LiteralPath $path)) {
        Add-Failure "Missing file for text check: $RelativePath"
        return
    }

    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $path
    if ($text -notmatch [regex]::Escape($Pattern)) {
        Add-Failure "$Description was not found in $RelativePath"
    }
}

function Reject-Text {
    param(
        [string]$RelativePath,
        [string]$Pattern,
        [string]$Description
    )

    $path = Join-Path $Root $RelativePath
    if (!(Test-Path -LiteralPath $path)) {
        Add-Failure "Missing file for text check: $RelativePath"
        return
    }

    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $path
    if ($text -match [regex]::Escape($Pattern)) {
        Add-Failure "$Description was found in $RelativePath"
    }
}

function Test-JsonProperty {
    param(
        [object]$Value,
        [string]$Name
    )

    if ($null -eq $Value) {
        return $false
    }

    return @($Value.PSObject.Properties | ForEach-Object { $_.Name }) -contains $Name
}

function Reject-JsonProperty {
    param(
        [object]$Value,
        [string]$Name,
        [string]$Description
    )

    if (Test-JsonProperty $Value $Name) {
        Add-Failure "$Description must not define $Name"
    }
}

function Require-FeatureIcon {
    param(
        [object]$Feature,
        [string]$ExpectedPath
    )

    $iconPath = [string]$Feature.iconPath
    if ([string]::IsNullOrWhiteSpace($iconPath)) {
        Add-Failure "Feature $($Feature.id) must define iconPath"
        return
    }

    if ($iconPath.StartsWith("builtin:", [StringComparison]::OrdinalIgnoreCase)) {
        Add-Failure "Feature $($Feature.id) must use a packaged icon file, not a built-in icon"
        return
    }

    if ([IO.Path]::IsPathRooted($iconPath)) {
        Add-Failure "Feature $($Feature.id) iconPath must be package-relative"
        return
    }

    if ($iconPath -ne $ExpectedPath) {
        Add-Failure "Feature $($Feature.id) iconPath must be $ExpectedPath"
        return
    }

    if ([IO.Path]::GetExtension($iconPath) -ne ".png") {
        Add-Failure "Feature $($Feature.id) iconPath must point to a PNG file"
        return
    }

    $resolvedPath = Join-Path $Root $iconPath
    if (!(Test-Path -LiteralPath $resolvedPath)) {
        Add-Failure "Feature $($Feature.id) icon file is missing: $iconPath"
    }
}

$manifestPath = Join-Path $Root "package.json"
if (!(Test-Path -LiteralPath $manifestPath)) {
    Add-Failure "Missing package.json"
}
else {
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    if ($manifest.version -notmatch '^V\d+\.\d+\.\d+$') {
        Add-Failure "package.json version must match V<major>.<minor>.<patch>"
    }
    if (@($manifest.revitVersions) -notcontains "2020") {
        Add-Failure "package.json revitVersions must include 2020"
    }
    if ($manifest.frameworkVersionRange -ne ">=1.3.0") {
        Add-Failure "package.json frameworkVersionRange must be >=1.3.0"
    }

    foreach ($property in @("packageDirectories", "moduleSources", "repositories", "conflictPolicy")) {
        Reject-JsonProperty $manifest $property "package.json root"
    }

    foreach ($module in @($manifest.modules)) {
        foreach ($property in @("type", "name", "sourceId", "resolvedBaseDirectory", "dependsOn")) {
            Reject-JsonProperty $module $property "Module $($module.id)"
        }

        foreach ($feature in @($module.features)) {
            foreach ($property in @("name", "commandKey")) {
                Reject-JsonProperty $feature $property "Feature $($feature.id)"
            }
        }
    }

    $gridModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.grid-visibility" } | Select-Object -First 1
    if ($null -eq $gridModule) {
        Add-Failure "Missing grid visibility module in package.json"
    }
    else {
        if ($gridModule.assembly -ne "dist/PlugHub.GridVisibility.dll") {
            Add-Failure "Grid visibility module assembly must be dist/PlugHub.GridVisibility.dll"
        }

        $feature = $gridModule.features | Where-Object { $_.id -eq "plughub.modules.grid-visibility.toggle" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing grid visibility toggle feature in package.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u8f74\u7f51\u663e\u9690\u5207\u6362"')) {
                Add-Failure "Grid visibility feature displayName must match the manifest display name"
            }
            if ($feature.commandAssembly -ne "dist/PlugHub.GridVisibility.dll") {
                Add-Failure "Grid visibility feature commandAssembly must be dist/PlugHub.GridVisibility.dll"
            }
            if ($feature.commandType -ne "PlugHub.GridVisibility.ToggleGridVisibilityCommand") {
                Add-Failure "Grid visibility feature commandType must be PlugHub.GridVisibility.ToggleGridVisibilityCommand"
            }
            Require-FeatureIcon $feature "icons/grid-visibility.png"
        }
    }

    $levelModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.level-visibility" } | Select-Object -First 1
    if ($null -eq $levelModule) {
        Add-Failure "Missing level visibility module in package.json"
    }
    else {
        if ($levelModule.assembly -ne "dist/PlugHub.LevelVisibility.dll") {
            Add-Failure "Level visibility module assembly must be dist/PlugHub.LevelVisibility.dll"
        }

        $feature = $levelModule.features | Where-Object { $_.id -eq "plughub.modules.level-visibility.toggle" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing level visibility toggle feature in package.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u6807\u9ad8\u663e\u9690\u5207\u6362"')) {
                Add-Failure "Level visibility feature displayName must match the manifest display name"
            }
            if ($feature.commandAssembly -ne "dist/PlugHub.LevelVisibility.dll") {
                Add-Failure "Level visibility feature commandAssembly must be dist/PlugHub.LevelVisibility.dll"
            }
            if ($feature.commandType -ne "PlugHub.LevelVisibility.ToggleLevelVisibilityCommand") {
                Add-Failure "Level visibility feature commandType must be PlugHub.LevelVisibility.ToggleLevelVisibilityCommand"
            }
            Require-FeatureIcon $feature "icons/level-visibility.png"
        }
    }

    $ductModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.duct-preferred-junction" } | Select-Object -First 1
    if ($null -eq $ductModule) {
        Add-Failure "Missing duct preferred junction module in package.json"
    }
    else {
        $feature = $ductModule.features | Where-Object { $_.id -eq "plughub.modules.duct-preferred-junction.switch" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing duct preferred junction switch feature in package.json"
        }
        else {
            Require-FeatureIcon $feature "icons/duct-preferred-junction.png"
        }
    }

    $familyModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.family-material-parameters" } | Select-Object -First 1
    if ($null -eq $familyModule) {
        Add-Failure "Missing family material parameters module in package.json"
    }
    else {
        $feature = $familyModule.features | Where-Object { $_.id -eq "plughub.modules.family-material-parameters.batch-add-material" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing family material parameters feature in package.json"
        }
        else {
            Require-FeatureIcon $feature "icons/family-material-parameters.png"
        }
    }
}

Require-File "src\PlugHub.GridVisibility\PlugHub.GridVisibility.csproj"
Require-File "src\PlugHub.GridVisibility\GridVisibilityModule.cs"
Require-File "src\PlugHub.GridVisibility\ToggleGridVisibilityCommand.cs"
Require-Text "src\PlugHub.GridVisibility\ToggleGridVisibilityCommand.cs" "BuiltInCategory.OST_Grids" "Grid category API"
Require-Text "src\PlugHub.GridVisibility\ToggleGridVisibilityCommand.cs" "GetCategoryHidden" "Current grid visibility read"
Require-Text "src\PlugHub.GridVisibility\ToggleGridVisibilityCommand.cs" "SetCategoryHidden" "Grid visibility toggle write"
Reject-Text "src\PlugHub.GridVisibility\ToggleGridVisibilityCommand.cs" "TaskDialog.Show" "Grid visibility success popup"
Require-Text "build.ps1" "src\PlugHub.GridVisibility\PlugHub.GridVisibility.csproj" "Grid visibility project build registration"
Require-Text "PlugHub_Packages.slnx" "src/PlugHub.GridVisibility/PlugHub.GridVisibility.csproj" "Grid visibility solution registration"

Require-File "src\PlugHub.LevelVisibility\PlugHub.LevelVisibility.csproj"
Require-File "src\PlugHub.LevelVisibility\LevelVisibilityModule.cs"
Require-File "src\PlugHub.LevelVisibility\ToggleLevelVisibilityCommand.cs"
Require-Text "src\PlugHub.LevelVisibility\ToggleLevelVisibilityCommand.cs" "BuiltInCategory.OST_Levels" "Level category API"
Require-Text "src\PlugHub.LevelVisibility\ToggleLevelVisibilityCommand.cs" "GetCategoryHidden" "Current level visibility read"
Require-Text "src\PlugHub.LevelVisibility\ToggleLevelVisibilityCommand.cs" "SetCategoryHidden" "Level visibility toggle write"
Reject-Text "src\PlugHub.LevelVisibility\ToggleLevelVisibilityCommand.cs" "TaskDialog.Show" "Level visibility success popup"
Require-Text "build.ps1" "src\PlugHub.LevelVisibility\PlugHub.LevelVisibility.csproj" "Level visibility project build registration"
Require-Text "PlugHub_Packages.slnx" "src/PlugHub.LevelVisibility/PlugHub.LevelVisibility.csproj" "Level visibility solution registration"
Reject-Text "package.json" "builtin:" "Built-in icon reference"
Reject-Text "package.json" "Tee/Tap" "Duct preferred junction old Tee/Tap wording"

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "ERROR: $_" }
    throw "Package validation failed with $($failures.Count) failure(s)."
}

Write-Host "Package validation passed."
