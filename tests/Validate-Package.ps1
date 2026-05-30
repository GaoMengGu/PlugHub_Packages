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

    $text = Get-Content -Raw -LiteralPath $path
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

    $text = Get-Content -Raw -LiteralPath $path
    if ($text -match [regex]::Escape($Pattern)) {
        Add-Failure "$Description was found in $RelativePath"
    }
}

$manifestPath = Join-Path $Root "package.json"
if (!(Test-Path -LiteralPath $manifestPath)) {
    Add-Failure "Missing package.json"
}
else {
    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    $gridModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.grid-visibility" } | Select-Object -First 1
    if ($null -eq $gridModule) {
        Add-Failure "Missing grid visibility module in package.json"
    }
    else {
        if ($gridModule.assembly -ne "dist/PlugHub.GridVisibility.dll") {
            Add-Failure "Grid visibility module assembly must be dist/PlugHub.GridVisibility.dll"
        }
        if ($gridModule.type -ne "PlugHub.GridVisibility.GridVisibilityModule") {
            Add-Failure "Grid visibility module type must be PlugHub.GridVisibility.GridVisibilityModule"
        }

        $feature = $gridModule.features | Where-Object { $_.id -eq "plughub.modules.grid-visibility.toggle" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing grid visibility toggle feature in package.json"
        }
        else {
            if ($feature.displayName -ne "轴网显隐切换") {
                Add-Failure "Grid visibility feature displayName must be 轴网显隐切换"
            }
            if ($feature.commandAssembly -ne "dist/PlugHub.GridVisibility.dll") {
                Add-Failure "Grid visibility feature commandAssembly must be dist/PlugHub.GridVisibility.dll"
            }
            if ($feature.commandType -ne "PlugHub.GridVisibility.ToggleGridVisibilityCommand") {
                Add-Failure "Grid visibility feature commandType must be PlugHub.GridVisibility.ToggleGridVisibilityCommand"
            }
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
        if ($levelModule.type -ne "PlugHub.LevelVisibility.LevelVisibilityModule") {
            Add-Failure "Level visibility module type must be PlugHub.LevelVisibility.LevelVisibilityModule"
        }

        $feature = $levelModule.features | Where-Object { $_.id -eq "plughub.modules.level-visibility.toggle" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing level visibility toggle feature in package.json"
        }
        else {
            if ($feature.displayName -ne "标高显隐切换") {
                Add-Failure "Level visibility feature displayName must be 标高显隐切换"
            }
            if ($feature.commandAssembly -ne "dist/PlugHub.LevelVisibility.dll") {
                Add-Failure "Level visibility feature commandAssembly must be dist/PlugHub.LevelVisibility.dll"
            }
            if ($feature.commandType -ne "PlugHub.LevelVisibility.ToggleLevelVisibilityCommand") {
                Add-Failure "Level visibility feature commandType must be PlugHub.LevelVisibility.ToggleLevelVisibilityCommand"
            }
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

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "ERROR: $_" }
    throw "Package validation failed with $($failures.Count) failure(s)."
}

Write-Host "Package validation passed."
