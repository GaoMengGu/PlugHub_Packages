param(
    [string]$Configuration = "Release",
    [string]$RevitApiDir = "",
    [switch]$UseRevitApiNuGet,
    [string]$RevitApiNuGetVersion = "",
    [string]$OutputDir = "$PSScriptRoot\dist"
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path $PSScriptRoot).Path
$Projects = @(
    (Join-Path $Root "src\\PlugHub.DuctPreferredJunction\\PlugHub.DuctPreferredJunction.csproj"),
    (Join-Path $Root "src\\PlugHub.FamilyMaterialParameters\\PlugHub.FamilyMaterialParameters.csproj"),
    (Join-Path $Root "src\\PlugHub.FamilyFileSaver\\PlugHub.FamilyFileSaver.csproj"),
    (Join-Path $Root "src\\PlugHub.GridVisibility\\PlugHub.GridVisibility.csproj"),
    (Join-Path $Root "src\\PlugHub.LevelVisibility\\PlugHub.LevelVisibility.csproj"),
    (Join-Path $Root "src\\PlugHub.MepTypeFilterVisibility\\PlugHub.MepTypeFilterVisibility.csproj"),
    (Join-Path $Root "src\\PlugHub.ReferencePlaneVisibility\\PlugHub.ReferencePlaneVisibility.csproj"),
    (Join-Path $Root "src\\PlugHub.AutoSave\\PlugHub.AutoSave.csproj")
)

function Test-RevitApiDir {
    param([string]$Path)
    return ![string]::IsNullOrWhiteSpace($Path) `
        -and (Test-Path (Join-Path $Path "RevitAPI.dll")) `
        -and (Test-Path (Join-Path $Path "RevitAPIUI.dll"))
}

$candidateApiDirs = @(
    $RevitApiDir,
    "D:\Program Files\Autodesk\Revit 2020",
    "D:\Program Files\Autodesk\Revit",
    "C:\Program Files\Autodesk\Revit 2020",
    "C:\Program Files\Autodesk\Revit"
)

if (!$UseRevitApiNuGet) {
    $resolvedApiDir = $candidateApiDirs | Where-Object { Test-RevitApiDir $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($resolvedApiDir)) {
        throw "Revit 2020 API DLLs were not found. Pass -RevitApiDir with a folder containing RevitAPI.dll and RevitAPIUI.dll, or pass -UseRevitApiNuGet for CI compile references."
    }

    $RevitApiDir = (Resolve-Path $resolvedApiDir).Path
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$OutputDir = (Resolve-Path $OutputDir).Path

# Ensure PlugHub/Contracts directory exists for ProjectReference resolution
# The revittool repo has the project under src/PlugHubContracts (flat name)
# but our .csproj files reference src/PlugHub/Contracts (nested path).
$contractsSrcDir = Join-Path $Root "revittool\src\PlugHubContracts"
$contractsDstDir = Join-Path $Root "revittool\src\PlugHub\Contracts"
if (Test-Path $contractsSrcDir) {
    if (!(Test-Path $contractsDstDir)) {
        # Create junction/symlink: PlugHub/Contracts -> PlugHubContracts
        cmd /c "mklink /D `"$contractsDstDir`" `"$contractsSrcDir`""
    }
}

# Ensure PlugHubContracts is built first (dependency for all plugin projects)
$contractsProject = Join-Path $Root "revittool\src\PlugHub\Contracts\PlugHubContracts.csproj"
if (Test-Path $contractsProject) {
    $contractsArgs = @("build", $contractsProject, "-c", $Configuration, "/p:RevitVersion=2020")
    if ($UseRevitApiNuGet) {
        $contractsArgs += "/p:RevitApiReferenceMode=NuGet"
        if (![string]::IsNullOrWhiteSpace($RevitApiNuGetVersion)) {
            $contractsArgs += "/p:RevitApiNuGetVersion=$RevitApiNuGetVersion"
        }
    }
    & dotnet $contractsArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for PlugHub.Contracts" }
}

foreach ($Project in $Projects) {
    $buildArguments = @(
        "build",
        $Project,
        "-c",
        $Configuration,
        "/p:RevitVersion=2020"
    )

    if ($UseRevitApiNuGet) {
        $buildArguments += "/p:RevitApiReferenceMode=NuGet"
        if (![string]::IsNullOrWhiteSpace($RevitApiNuGetVersion)) {
            $buildArguments += "/p:RevitApiNuGetVersion=$RevitApiNuGetVersion"
        }
    }
    else {
        $buildArguments += "/p:RevitApiReferenceMode=Installed"
        $buildArguments += "/p:RevitApiDir=$RevitApiDir"
    }

    & dotnet $buildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $Project with exit code $LASTEXITCODE."
    }

    $ProjectDirectory = Split-Path $Project -Parent
    $ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($Project)
    Copy-Item (Join-Path $ProjectDirectory "bin\$Configuration\$ProjectName.dll") $OutputDir -Force

    $Pdb = Join-Path $ProjectDirectory "bin\$Configuration\$ProjectName.pdb"
    if (Test-Path $Pdb) {
        Copy-Item $Pdb $OutputDir -Force
    }
}

Write-Host "PlugHub plugin package output: $OutputDir"
