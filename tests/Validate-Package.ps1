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

function Test-VersionTag {
    param([string]$Value)

    return ![string]::IsNullOrWhiteSpace($Value) -and $Value -match '^V\d+\.\d+\.\d+$'
}

function ConvertFrom-BigEndianUInt32 {
    param(
        [byte[]]$Bytes,
        [int]$Offset
    )

    return (([uint32]$Bytes[$Offset] -shl 24) -bor ([uint32]$Bytes[$Offset + 1] -shl 16) -bor ([uint32]$Bytes[$Offset + 2] -shl 8) -bor [uint32]$Bytes[$Offset + 3])
}

function Require-RevitRibbonPng {
    param([string]$RelativePath)

    $path = Join-Path $Root $RelativePath
    if (!(Test-Path -LiteralPath $path)) {
        Add-Failure "Icon file is missing: $RelativePath"
        return
    }

    $bytes = [IO.File]::ReadAllBytes($path)
    $signature = [byte[]](0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
    if ($bytes.Length -lt 33) {
        Add-Failure "Icon $RelativePath is too small to be a valid PNG"
        return
    }

    for ($i = 0; $i -lt $signature.Length; $i++) {
        if ($bytes[$i] -ne $signature[$i]) {
            Add-Failure "Icon $RelativePath must be a PNG file"
            return
        }
    }

    $chunkType = [Text.Encoding]::ASCII.GetString($bytes, 12, 4)
    if ($chunkType -ne "IHDR") {
        Add-Failure "Icon $RelativePath must start with a PNG IHDR chunk"
        return
    }

    $width = ConvertFrom-BigEndianUInt32 $bytes 16
    $height = ConvertFrom-BigEndianUInt32 $bytes 20
    $bitDepth = [int]$bytes[24]
    $colorType = [int]$bytes[25]

    if ($width -ne 32 -or $height -ne 32) {
        Add-Failure "Icon $RelativePath must be exactly 32x32 px"
    }

    if ($bitDepth -ne 8 -or $colorType -ne 6) {
        Add-Failure "Icon $RelativePath must be an 8-bit RGBA PNG with transparency support"
    }
}

function Test-ClassificationMatch {
    param(
        [System.Collections.Generic.List[string]]$Values,
        [string[]]$Tokens
    )

    foreach ($value in $Values) {
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        foreach ($token in $Tokens) {
            if (![string]::IsNullOrWhiteSpace($token) -and $value.IndexOf($token, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                return $true
            }
        }
    }

    return $false
}

function Get-ExpectedModuleDisplayName {
    param([object]$Module)

    $familyDisplayName = ConvertFrom-Json '"\u65cf\u5de5\u5177"'
    $civilDisplayName = ConvertFrom-Json '"\u571f\u5efa\u5de5\u5177"'
    $mepDisplayName = ConvertFrom-Json '"\u673a\u7535\u5de5\u5177"'
    $drawingDisplayName = ConvertFrom-Json '"\u51fa\u56fe\u5de5\u5177"'
    $coordinationDisplayName = ConvertFrom-Json '"\u7ba1\u7efc\u5de5\u5177"'
    $viewDisplayName = ConvertFrom-Json '"\u89c6\u56fe\u5de5\u5177"'
    $miscDisplayName = ConvertFrom-Json '"\u5c0f\u5de5\u5177"'

    $values = New-Object System.Collections.Generic.List[string]
    foreach ($value in @($Module.category, $Module.id, $Module.description)) {
        if (![string]::IsNullOrWhiteSpace([string]$value)) {
            $values.Add([string]$value)
        }
    }

    foreach ($tag in @($Module.tags)) {
        if (![string]::IsNullOrWhiteSpace([string]$tag)) {
            $values.Add([string]$tag)
        }
    }

    if (Test-ClassificationMatch $values @("family", (ConvertFrom-Json '"\u65cf"'))) {
        return $familyDisplayName
    }

    if (Test-ClassificationMatch $values @("civil", "architecture", "structural", "structure", (ConvertFrom-Json '"\u571f\u5efa"'))) {
        return $civilDisplayName
    }

    if (Test-ClassificationMatch $values @("coordination", "pipeline-coordination", "pipe-coordination", "duct-coordination", "clash", (ConvertFrom-Json '"\u7ba1\u7efc"'), (ConvertFrom-Json '"\u7ba1\u7ebf\u7efc\u5408"'))) {
        return $coordinationDisplayName
    }

    if (Test-ClassificationMatch $values @("drawing", "annotation", "documentation", "sheet", "tag", "dimension", (ConvertFrom-Json '"\u51fa\u56fe"'), (ConvertFrom-Json '"\u6ce8\u91ca"'))) {
        return $drawingDisplayName
    }

    if (Test-ClassificationMatch $values @("mep", "mechanical", "electrical", "plumbing", "duct", "pipe", (ConvertFrom-Json '"\u673a\u7535"'))) {
        return $mepDisplayName
    }

    if (Test-ClassificationMatch $values @("view", "visibility", (ConvertFrom-Json '"\u89c6\u56fe"'))) {
        return $viewDisplayName
    }

    return $miscDisplayName
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
        return
    }

    Require-RevitRibbonPng $iconPath
}

$manifestPath = Join-Path $Root "packages.json"
if (!(Test-Path -LiteralPath $manifestPath)) {
    Add-Failure "Missing packages.json"
}
else {
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    if (!(Test-VersionTag $manifest.indexVersion)) {
        Add-Failure "packages.json indexVersion must match V<major>.<minor>.<patch>"
    }
    if (@($manifest.revitVersions) -notcontains "2020") {
        Add-Failure "packages.json revitVersions must include 2020"
    }
    if ($manifest.frameworkVersionRange -ne ">=1.3.0") {
        Add-Failure "packages.json frameworkVersionRange must be >=1.3.0"
    }

    foreach ($property in @("version", "packageDirectories", "moduleSources", "repositories", "conflictPolicy")) {
        Reject-JsonProperty $manifest $property "packages.json root"
    }

    foreach ($module in @($manifest.modules)) {
        if (!(Test-VersionTag $module.version)) {
            Add-Failure "Module $($module.id) version must match V<major>.<minor>.<patch>"
        }

        if ($module.author -ne "GAOMENGGU") {
            Add-Failure "Module $($module.id) author must be GAOMENGGU"
        }

        if ([string]::IsNullOrWhiteSpace([string]$module.category)) {
            Add-Failure "Module $($module.id) must define category"
        }

        $expectedDisplayName = Get-ExpectedModuleDisplayName $module
        if ($module.displayName -ne $expectedDisplayName) {
            Add-Failure "Module $($module.id) displayName must be $expectedDisplayName for its category/tags"
        }

        foreach ($property in @("type", "name", "sourceId", "resolvedBaseDirectory", "dependsOn", "enabled", "visible", "order")) {
            Reject-JsonProperty $module $property "Module $($module.id)"
        }

        foreach ($feature in @($module.features)) {
            foreach ($property in @("name", "commandKey", "version", "category", "group", "tags", "order", "defaultState", "buttonSize", "commandAssembly")) {
                Reject-JsonProperty $feature $property "Feature $($feature.id)"
            }
        }
    }

    $gridModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.grid-visibility" } | Select-Object -First 1
    if ($null -eq $gridModule) {
        Add-Failure "Missing grid visibility module in packages.json"
    }
    else {
        if ($gridModule.assembly -ne "dist/PlugHub.GridVisibility.dll") {
            Add-Failure "Grid visibility module assembly must be dist/PlugHub.GridVisibility.dll"
        }

        $feature = $gridModule.features | Where-Object { $_.id -eq "plughub.modules.grid-visibility.toggle" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing grid visibility toggle feature in packages.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u8f74\u7f51\u663e\u9690"')) {
                Add-Failure "Grid visibility feature displayName must match the manifest display name"
            }
            if ($feature.commandType -ne "PlugHub.GridVisibility.ToggleGridVisibilityCommand") {
                Add-Failure "Grid visibility feature commandType must be PlugHub.GridVisibility.ToggleGridVisibilityCommand"
            }
            Require-FeatureIcon $feature "icons/grid-visibility.png"
        }
    }

    $levelModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.level-visibility" } | Select-Object -First 1
    if ($null -eq $levelModule) {
        Add-Failure "Missing level visibility module in packages.json"
    }
    else {
        if ($levelModule.assembly -ne "dist/PlugHub.LevelVisibility.dll") {
            Add-Failure "Level visibility module assembly must be dist/PlugHub.LevelVisibility.dll"
        }

        $feature = $levelModule.features | Where-Object { $_.id -eq "plughub.modules.level-visibility.toggle" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing level visibility toggle feature in packages.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u6807\u9ad8\u663e\u9690"')) {
                Add-Failure "Level visibility feature displayName must match the manifest display name"
            }
            if ($feature.commandType -ne "PlugHub.LevelVisibility.ToggleLevelVisibilityCommand") {
                Add-Failure "Level visibility feature commandType must be PlugHub.LevelVisibility.ToggleLevelVisibilityCommand"
            }
            Require-FeatureIcon $feature "icons/level-visibility.png"
        }
    }

    $referencePlaneModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.reference-plane-visibility" } | Select-Object -First 1
    if ($null -eq $referencePlaneModule) {
        Add-Failure "Missing reference plane visibility module in packages.json"
    }
    else {
        if ($referencePlaneModule.assembly -ne "dist/PlugHub.ReferencePlaneVisibility.dll") {
            Add-Failure "Reference plane visibility module assembly must be dist/PlugHub.ReferencePlaneVisibility.dll"
        }

        $feature = $referencePlaneModule.features | Where-Object { $_.id -eq "plughub.modules.reference-plane-visibility.toggle" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing reference plane visibility toggle feature in packages.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u53c2\u7167\u5e73\u9762\u663e\u9690"')) {
                Add-Failure "Reference plane visibility feature displayName must match the manifest display name"
            }
            if ($feature.commandType -ne "PlugHub.ReferencePlaneVisibility.ToggleReferencePlaneVisibilityCommand") {
                Add-Failure "Reference plane visibility feature commandType must be PlugHub.ReferencePlaneVisibility.ToggleReferencePlaneVisibilityCommand"
            }
            Require-FeatureIcon $feature "icons/reference-plane-visibility.png"
        }
    }

    $ductModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.duct-preferred-junction" } | Select-Object -First 1
    if ($null -eq $ductModule) {
        Add-Failure "Missing duct preferred junction module in packages.json"
    }
    else {
        $feature = $ductModule.features | Where-Object { $_.id -eq "plughub.modules.duct-preferred-junction.switch" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing duct preferred junction switch feature in packages.json"
        }
        else {
            Require-FeatureIcon $feature "icons/duct-preferred-junction.png"
        }
    }

    $mepTypeFilterVisibilityModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.mep-type-filter-visibility" } | Select-Object -First 1
    if ($null -eq $mepTypeFilterVisibilityModule) {
        Add-Failure "Missing MEP type filter visibility module in packages.json"
    }
    else {
        if ($mepTypeFilterVisibilityModule.assembly -ne "dist/PlugHub.MepTypeFilterVisibility.dll") {
            Add-Failure "MEP type filter visibility module assembly must be dist/PlugHub.MepTypeFilterVisibility.dll"
        }

        $feature = $mepTypeFilterVisibilityModule.features | Where-Object { $_.id -eq "plughub.modules.mep-type-filter-visibility.apply" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing MEP type filter visibility feature in packages.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u673a\u7535\u8fc7\u6ee4"')) {
                Add-Failure "MEP type filter visibility feature displayName must match the manifest display name"
            }
            if ($feature.commandType -ne "PlugHub.MepTypeFilterVisibility.ApplyMepTypeFilterVisibilityCommand") {
                Add-Failure "MEP type filter visibility commandType must be PlugHub.MepTypeFilterVisibility.ApplyMepTypeFilterVisibilityCommand"
            }
            Require-FeatureIcon $feature "icons/mep-type-filter-visibility.png"
        }
    }

    $familyModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.family-material-parameters" } | Select-Object -First 1
    if ($null -eq $familyModule) {
        Add-Failure "Missing family material parameters module in packages.json"
    }
    else {
        $feature = $familyModule.features | Where-Object { $_.id -eq "plughub.modules.family-material-parameters.batch-add-material" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing family material parameters feature in packages.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u6279\u91cf\u6750\u8d28"')) {
                Add-Failure "Family material parameters feature displayName must match the manifest display name"
            }
            Require-FeatureIcon $feature "icons/family-material-parameters.png"
        }
    }

    $familyFileSaverModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.family-file-saver" } | Select-Object -First 1
    if ($null -eq $familyFileSaverModule) {
        Add-Failure "Missing family file saver module in packages.json"
    }
    else {
        if ($familyFileSaverModule.assembly -ne "dist/PlugHub.FamilyFileSaver.dll") {
            Add-Failure "Family file saver module assembly must be dist/PlugHub.FamilyFileSaver.dll"
        }

        $feature = $familyFileSaverModule.features | Where-Object { $_.id -eq "plughub.modules.family-file-saver.save" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing family file saver feature in packages.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u6279\u91cf\u4fdd\u5b58"')) {
                Add-Failure "Family file saver feature displayName must match the manifest display name"
            }
            if ($feature.commandType -ne "PlugHub.FamilyFileSaver.SaveFamilyFilesCommand") {
                Add-Failure "Family file saver commandType must be PlugHub.FamilyFileSaver.SaveFamilyFilesCommand"
            }
            Require-FeatureIcon $feature "icons/family-file-saver.png"
        }

        $autoSaveFeature = $familyFileSaverModule.features | Where-Object { $_.id -eq "plughub.modules.family-file-saver.auto-save-settings" } | Select-Object -First 1
        if ($null -ne $autoSaveFeature) {
            Add-Failure "Family file saver module must not contain project auto-save settings feature"
        }
    }

    $projectAutoSaveModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.project-auto-save" } | Select-Object -First 1
    if ($null -eq $projectAutoSaveModule) {
        Add-Failure "Missing project auto-save module in packages.json"
    }
    else {
        if ($projectAutoSaveModule.assembly -ne "dist/PlugHub.ProjectAutoSave.dll") {
            Add-Failure "Project auto-save module assembly must be dist/PlugHub.ProjectAutoSave.dll"
        }

        $feature = $projectAutoSaveModule.features | Where-Object { $_.id -eq "plughub.modules.project-auto-save.settings" } | Select-Object -First 1
        if ($null -eq $feature) {
            Add-Failure "Missing project auto-save settings feature in packages.json"
        }
        else {
            if ($feature.displayName -ne (ConvertFrom-Json '"\u81ea\u52a8\u4fdd\u5b58"')) {
                Add-Failure "Project auto-save feature displayName must match the manifest display name"
            }
            if ($feature.commandType -ne "PlugHub.ProjectAutoSave.ShowAutoSaveSettingsCommand") {
                Add-Failure "Project auto-save commandType must be PlugHub.ProjectAutoSave.ShowAutoSaveSettingsCommand"
            }
            Require-FeatureIcon $feature "icons/project-auto-save.png"
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

Require-File "src\PlugHub.ReferencePlaneVisibility\PlugHub.ReferencePlaneVisibility.csproj"
Require-File "src\PlugHub.ReferencePlaneVisibility\ReferencePlaneVisibilityModule.cs"
Require-File "src\PlugHub.ReferencePlaneVisibility\ToggleReferencePlaneVisibilityCommand.cs"
Require-Text "src\PlugHub.ReferencePlaneVisibility\ToggleReferencePlaneVisibilityCommand.cs" "BuiltInCategory.OST_CLines" "Reference plane category API"
Require-Text "src\PlugHub.ReferencePlaneVisibility\ToggleReferencePlaneVisibilityCommand.cs" "GetCategoryHidden" "Current reference plane visibility read"
Require-Text "src\PlugHub.ReferencePlaneVisibility\ToggleReferencePlaneVisibilityCommand.cs" "SetCategoryHidden" "Reference plane visibility toggle write"
Reject-Text "src\PlugHub.ReferencePlaneVisibility\ToggleReferencePlaneVisibilityCommand.cs" "TaskDialog.Show" "Reference plane visibility success popup"
Require-Text "build.ps1" "src\PlugHub.ReferencePlaneVisibility\PlugHub.ReferencePlaneVisibility.csproj" "Reference plane visibility project build registration"
Require-Text "PlugHub_Packages.slnx" "src/PlugHub.ReferencePlaneVisibility/PlugHub.ReferencePlaneVisibility.csproj" "Reference plane visibility solution registration"

Require-File "src\PlugHub.FamilyFileSaver\PlugHub.FamilyFileSaver.csproj"
Require-File "src\PlugHub.FamilyFileSaver\FamilyFileSaverModule.cs"
Require-File "src\PlugHub.FamilyFileSaver\FamilyItem.cs"
Require-File "src\PlugHub.FamilyFileSaver\FamilySelectionWindow.xaml"
Require-File "src\PlugHub.FamilyFileSaver\FamilySelectionWindow.xaml.cs"
Require-File "src\PlugHub.FamilyFileSaver\SaveFamilyFilesCommand.cs"
Require-Text "src\PlugHub.FamilyFileSaver\SaveFamilyFilesCommand.cs" "EditFamily" "Family edit API"
Require-Text "src\PlugHub.FamilyFileSaver\SaveFamilyFilesCommand.cs" "SaveAs" "Family save API"
Require-Text "src\PlugHub.FamilyFileSaver\SaveFamilyFilesCommand.cs" "FolderBrowserDialog" "Family save destination selector"
Require-Text "src\PlugHub.FamilyFileSaver\SaveFamilyFilesCommand.cs" "DialogBoxShowing" "Family saver background dialog suppression"
Require-Text "src\PlugHub.FamilyFileSaver\SaveFamilyFilesCommand.cs" "OverrideResult" "Family saver dialog auto-dismiss result"
Reject-Text "src\PlugHub.FamilyFileSaver\FamilyFileSaverModule.cs" "ShowAutoSaveSettingsCommand" "Family saver project auto-save command registration"
Require-Text "src\PlugHub.FamilyFileSaver\FamilySelectionWindow.xaml.cs" "_selectedFamilyIds" "Family saver persistent selection state"
Require-Text "src\PlugHub.FamilyFileSaver\FamilySelectionWindow.xaml.cs" "CaptureCurrentSelections" "Family saver filter selection persistence"
Require-Text "build.ps1" "src\PlugHub.FamilyFileSaver\PlugHub.FamilyFileSaver.csproj" "Family file saver project build registration"
Require-Text "PlugHub_Packages.slnx" "src/PlugHub.FamilyFileSaver/PlugHub.FamilyFileSaver.csproj" "Family file saver solution registration"

Require-File "src\PlugHub.ProjectAutoSave\PlugHub.ProjectAutoSave.csproj"
Require-File "src\PlugHub.ProjectAutoSave\ProjectAutoSaveModule.cs"
Require-File "src\PlugHub.ProjectAutoSave\AutoSaveSettings.cs"
Require-File "src\PlugHub.ProjectAutoSave\AutoSaveSettingsWindow.xaml"
Require-File "src\PlugHub.ProjectAutoSave\AutoSaveSettingsWindow.xaml.cs"
Require-File "src\PlugHub.ProjectAutoSave\AutoSaveService.cs"
Require-File "src\PlugHub.ProjectAutoSave\ShowAutoSaveSettingsCommand.cs"
Require-Text "src\PlugHub.ProjectAutoSave\AutoSaveSettings.cs" "DefaultIntervalMinutes = 10" "Project auto-save default interval"
Require-Text "src\PlugHub.ProjectAutoSave\AutoSaveSettings.cs" "ShowNotification" "Project auto-save notification toggle"
Require-Text "src\PlugHub.ProjectAutoSave\AutoSaveSettingsWindow.xaml" "自动保存设置" "Project auto-save settings title"
Require-Text "src\PlugHub.ProjectAutoSave\AutoSaveSettingsWindow.xaml" "自定义保存" "Project auto-save custom switch"
Require-Text "src\PlugHub.ProjectAutoSave\AutoSaveSettingsWindow.xaml" "分钟间隔" "Project auto-save interval input"
Require-Text "src\PlugHub.ProjectAutoSave\AutoSaveService.cs" "Idling" "Project auto-save Revit idling hook"
Require-Text "src\PlugHub.ProjectAutoSave\AutoSaveService.cs" "SaveDocument" "Project auto-save document write path"
Require-Text "src\PlugHub.ProjectAutoSave\ShowAutoSaveSettingsCommand.cs" "AutoSaveSettingsWindow" "Project auto-save settings window command"
Require-Text "src\PlugHub.ProjectAutoSave\ShowAutoSaveSettingsCommand.cs" "ApplySettings" "Project auto-save settings service application"
Require-Text "src\PlugHub.ProjectAutoSave\ProjectAutoSaveModule.cs" "ShowAutoSaveSettingsCommand" "Project auto-save settings command registration"
Require-Text "build.ps1" "src\PlugHub.ProjectAutoSave\PlugHub.ProjectAutoSave.csproj" "Project auto-save project build registration"
Require-Text "PlugHub_Packages.slnx" "src/PlugHub.ProjectAutoSave/PlugHub.ProjectAutoSave.csproj" "Project auto-save solution registration"

Require-File "src\PlugHub.MepTypeFilterVisibility\PlugHub.MepTypeFilterVisibility.csproj"
Require-File "src\PlugHub.MepTypeFilterVisibility\MepTypeFilterVisibilityModule.cs"
Require-File "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "PickElementsByRectangle" "MEP type filter rectangle selection prompt"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_DuctCurves" "MEP type filter duct category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_DuctFitting" "MEP type filter duct fitting category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_DuctAccessory" "MEP type filter duct accessory category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_DuctTerminal" "MEP type filter duct terminal category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_PipeCurves" "MEP type filter pipe category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_PipeFitting" "MEP type filter pipe fitting category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_PipeAccessory" "MEP type filter pipe accessory category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_CableTray" "MEP type filter cable tray category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInCategory.OST_CableTrayFitting" "MEP type filter cable tray fitting category"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM" "MEP type filter duct system type rule"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM" "MEP type filter pipe system type rule"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInParameter.RBS_CTC_SERVICE_TYPE" "MEP type filter cable tray service type rule"
Reject-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "BuiltInParameter.RBS_CABLETRAYCONDUIT_SYSTEM_TYPE" "MEP type filter cable tray system type rule"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "ParameterFilterElement.Create" "MEP type filter creation API"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "SetFilterVisibility" "MEP type filter view visibility API"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "ShouldRestoreAllFilterVisibility" "MEP type filter restore-all toggle check"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "RestoreAllFilterVisibility" "MEP type filter restore-all visibility path"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "GetFilterVisibility" "MEP type filter current visibility read"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "FindParameterFilterByTypeName" "MEP type filter prefixed filter name lookup"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "IsFilterNameMatch" "MEP type filter name suffix match helper"
Require-Text "src\PlugHub.MepTypeFilterVisibility\ApplyMepTypeFilterVisibilityCommand.cs" "EndsWith(typeFilterName, StringComparison.Ordinal)" "MEP type filter prefixed name suffix comparison"
Require-Text "build.ps1" "src\PlugHub.MepTypeFilterVisibility\PlugHub.MepTypeFilterVisibility.csproj" "MEP type filter visibility project build registration"
Require-Text "PlugHub_Packages.slnx" "src/PlugHub.MepTypeFilterVisibility/PlugHub.MepTypeFilterVisibility.csproj" "MEP type filter visibility solution registration"
Reject-Text "packages.json" "builtin:" "Built-in icon reference"
Reject-Text "packages.json" "Tee/Tap" "Duct preferred junction old Tee/Tap wording"
Require-Text ".github\workflows\build-package.yml" '$indexVersionPattern = [regex]::new(' "Root indexVersion replacement regex instance"
Require-Text ".github\workflows\build-package.yml" '$manifestText = $indexVersionPattern.Replace($manifestText, (' "Root indexVersion replacement count-limited call"
Require-Text ".github\workflows\build-package.yml" 'refs/heads/codex' "Codex branch workflow registration"
Require-Text ".github\workflows\build-package.yml" 'refs/heads/hermes' "Hermes branch workflow registration"
Require-Text ".github\workflows\build-package.yml" 'if: github.actor != ''github-actions[bot]'' && github.event_name == ''push'' && contains(fromJson(''["refs/heads/codex","refs/heads/hermes"]''), github.ref)' "Package output commit branch allow-list"
Reject-Text ".github\workflows\build-package.yml" 'if: github.actor != ''github-actions[bot]'' && ((github.event_name == ''push'' && contains(fromJson(''["refs/heads/main","refs/heads/codex","refs/heads/hermes"]''), github.ref)) || (github.event_name == ''workflow_dispatch'' && github.ref == ''refs/heads/main''))' "Package output commit main branch allow-list"
Reject-Text ".github\workflows\build-package.yml" "(github.event_name == 'workflow_dispatch' && github.ref == 'refs/heads/main')" "Package output commit manual main write-back condition"
Require-Text ".github\workflows\build-package.yml" 'origin/$githubRefName:refs/heads/$githubRefName' "Gitee working branch mirror refspec"
Require-Text ".github\workflows\build-package.yml" 'Sync Gitee release asset' "Gitee release asset sync step"

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "ERROR: $_" }
    throw "Package validation failed with $($failures.Count) failure(s)."
}

Write-Host "Package validation passed."
