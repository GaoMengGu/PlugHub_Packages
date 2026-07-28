# Clear Height Analysis Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `PlugHub.ClearHeightAnalysis`, a Revit 2020 PlugHub module that analyzes clear height by projecting structural, architectural, and MEP obstacle footprints onto configurable building-outline grids and rendering a heatmap plus result table.

**Architecture:** Keep Revit-independent grid and risk calculation in small pure C# classes so it can be unit-tested on Linux. Put Revit API integration behind adapters for outline extraction, linked-model transforms, obstacle collection, heatmap rendering, cleanup, and command orchestration. Package the feature as one PlugHub module with WPF settings/results windows and validation coverage in `tests/Validate-Package.ps1`.

**Tech Stack:** C# 8, .NET Framework 4.8, WPF, Revit API 2020, PlugHub.Contracts, PowerShell package validator, xUnit for pure algorithm tests.

---

## File Structure

- Create `src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj` for the Revit module, WPF UI, Revit API references, and PlugHub contract reference.
- Create `src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisModule.cs` for PlugHub module and feature metadata.
- Create `src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisCommand.cs` for command orchestration, settings window display, selection prompts, analysis transaction, and result window display.
- Create `src/PlugHub.ClearHeightAnalysis/Models/*.cs` for pure data types: `AnalysisSettings`, `AnalysisLevel`, `GridCell`, `Rect2d`, `ObstacleProjection`, `ClearHeightResult`, `RiskLevel`, and `SourceModelInfo`.
- Create `src/PlugHub.ClearHeightAnalysis/Services/*.cs` for pure or adapter services: `GridBuilder`, `ClearHeightCalculator`, `ObstacleProjectionMapper`, `BuildingOutlineProvider`, `ObstacleCollector`, `HeatmapRenderer`, `ResultCleanupService`, `ResultTagService`, and `UnitConversion`.
- Create `src/PlugHub.ClearHeightAnalysis/UI/ClearHeightAnalysisWindow.xaml` and `.xaml.cs` for analysis settings.
- Create `src/PlugHub.ClearHeightAnalysis/UI/AnalysisResultWindow.xaml` and `.xaml.cs` for low-height and unknown-grid results.
- Create `tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj` plus test files that compile linked pure classes from the module.
- Modify `packages.json`, `README.md`, `build.ps1`, `PlugHub_Packages.slnx`, and `tests/Validate-Package.ps1` to register and validate the new package.
- Create `icons/clear-height-analysis.png`, a 32x32 transparent PNG with all visible pixels using `#1A1A1A`.

---

### Task 1: Add Pure Algorithm Test Harness

**Files:**
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/GridBuilderTests.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/ClearHeightCalculatorTests.cs`

- [ ] **Step 1: Write failing grid and clear-height tests**

Create `tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>disable</Nullable>
    <LangVersion>8.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.8.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.1" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Models/AnalysisSettings.cs" Link="Models/AnalysisSettings.cs" />
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Models/ClearHeightResult.cs" Link="Models/ClearHeightResult.cs" />
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Models/GridCell.cs" Link="Models/GridCell.cs" />
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Models/ObstacleProjection.cs" Link="Models/ObstacleProjection.cs" />
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Models/Rect2d.cs" Link="Models/Rect2d.cs" />
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Models/RiskLevel.cs" Link="Models/RiskLevel.cs" />
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Services/ClearHeightCalculator.cs" Link="Services/ClearHeightCalculator.cs" />
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Services/GridBuilder.cs" Link="Services/GridBuilder.cs" />
    <Compile Include="../../src/PlugHub.ClearHeightAnalysis/Services/ObstacleProjectionMapper.cs" Link="Services/ObstacleProjectionMapper.cs" />
  </ItemGroup>
</Project>
```

Create `tests/PlugHub.ClearHeightAnalysis.Tests/GridBuilderTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Models;
using PlugHub.ClearHeightAnalysis.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests
{
    public sealed class GridBuilderTests
    {
        [Fact]
        public void BuildInsideRectangleCreatesExpectedOneMeterCells()
        {
            var boundary = new Rect2d(0, 0, 2000, 1000);

            List<GridCell> cells = GridBuilder.BuildInsideRectangle(boundary, 1000, "1F", 0).ToList();

            Assert.Equal(2, cells.Count);
            Assert.Equal("1F-001", cells[0].Number);
            Assert.Equal(new Rect2d(0, 0, 1000, 1000), cells[0].Bounds);
            Assert.Equal(new Rect2d(1000, 0, 2000, 1000), cells[1].Bounds);
        }

        [Fact]
        public void BuildInsideRectangleSkipsCellsOutsidePolygonMask()
        {
            var boundary = new Rect2d(0, 0, 2000, 2000);
            var mask = new List<Rect2d>
            {
                new Rect2d(0, 0, 1000, 2000)
            };

            List<GridCell> cells = GridBuilder.BuildInsideRectangles(boundary, mask, 1000, "1F", 0).ToList();

            Assert.Equal(2, cells.Count);
            Assert.All(cells, cell => Assert.True(cell.Bounds.MaxX <= 1000));
        }
    }
}
```

Create `tests/PlugHub.ClearHeightAnalysis.Tests/ClearHeightCalculatorTests.cs`:

```csharp
using System.Collections.Generic;
using PlugHub.ClearHeightAnalysis.Models;
using PlugHub.ClearHeightAnalysis.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests
{
    public sealed class ClearHeightCalculatorTests
    {
        [Fact]
        public void CalculateUsesLowestObstacleBottomAsWorstCaseClearHeight()
        {
            var cell = new GridCell("1F-001", new Rect2d(0, 0, 1000, 1000), "1F", 0);
            var projections = new List<ObstacleProjection>
            {
                new ObstacleProjection("beam-a", "梁A", "结构梁", "结构模型", false, new Rect2d(0, 0, 1000, 500), 3200),
                new ObstacleProjection("duct-b", "风管B", "风管", "机电模型", true, new Rect2d(500, 0, 1000, 1000), 2800)
            };
            var settings = new AnalysisSettings
            {
                ClearHeightThresholdMillimeters = 3000,
                FinishFloorOffsetMillimeters = 50
            };

            ClearHeightResult result = ClearHeightCalculator.Calculate(cell, projections, settings);

            Assert.Equal(2750, result.ClearHeightMillimeters);
            Assert.Equal(RiskLevel.Insufficient, result.RiskLevel);
            Assert.Equal("duct-b", result.ControllingObstacle.ElementKey);
        }

        [Fact]
        public void CalculateMarksCellUnknownWhenNoObstacleCoversIt()
        {
            var cell = new GridCell("1F-001", new Rect2d(0, 0, 1000, 1000), "1F", 0);
            var settings = new AnalysisSettings { ClearHeightThresholdMillimeters = 3000 };

            ClearHeightResult result = ClearHeightCalculator.Calculate(cell, new List<ObstacleProjection>(), settings);

            Assert.Equal(RiskLevel.Unknown, result.RiskLevel);
            Assert.Null(result.ClearHeightMillimeters);
        }
    }
}
```

- [ ] **Step 2: Run tests and verify they fail because production files do not exist**

Run:

```bash
dotnet test tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj
```

Expected: build fails with missing files under `src/PlugHub.ClearHeightAnalysis/Models` and `src/PlugHub.ClearHeightAnalysis/Services`.

- [ ] **Step 3: Commit the failing test harness**

```bash
git add tests/PlugHub.ClearHeightAnalysis.Tests
git commit -m "test: add clear height analysis algorithm tests"
```

---

### Task 2: Implement Pure Grid and Calculation Core

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Models/AnalysisSettings.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Models/ClearHeightResult.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Models/GridCell.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Models/ObstacleProjection.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Models/Rect2d.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Models/RiskLevel.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Services/ClearHeightCalculator.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Services/GridBuilder.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Services/ObstacleProjectionMapper.cs`

- [ ] **Step 1: Implement pure model classes**

Create `src/PlugHub.ClearHeightAnalysis/Models/Rect2d.cs`:

```csharp
using System;

namespace PlugHub.ClearHeightAnalysis.Models
{
    public readonly struct Rect2d : IEquatable<Rect2d>
    {
        public Rect2d(double minX, double minY, double maxX, double maxY)
        {
            if (maxX < minX || maxY < minY)
            {
                throw new ArgumentException("矩形最大坐标必须大于或等于最小坐标。");
            }

            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public double CenterX => (MinX + MaxX) / 2.0;
        public double CenterY => (MinY + MaxY) / 2.0;

        public bool Intersects(Rect2d other)
        {
            return MinX < other.MaxX && MaxX > other.MinX && MinY < other.MaxY && MaxY > other.MinY;
        }

        public bool ContainsCenterOf(Rect2d other)
        {
            return other.CenterX >= MinX && other.CenterX <= MaxX && other.CenterY >= MinY && other.CenterY <= MaxY;
        }

        public bool Equals(Rect2d other)
        {
            return MinX.Equals(other.MinX) && MinY.Equals(other.MinY) && MaxX.Equals(other.MaxX) && MaxY.Equals(other.MaxY);
        }

        public override bool Equals(object obj)
        {
            return obj is Rect2d other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = MinX.GetHashCode();
                hash = (hash * 397) ^ MinY.GetHashCode();
                hash = (hash * 397) ^ MaxX.GetHashCode();
                hash = (hash * 397) ^ MaxY.GetHashCode();
                return hash;
            }
        }
    }
}
```

Create the remaining model classes with these public members:

```csharp
namespace PlugHub.ClearHeightAnalysis.Models
{
    public enum RiskLevel
    {
        Unknown,
        Severe,
        Insufficient,
        Warning,
        Passed
    }
}
```

```csharp
namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class AnalysisSettings
    {
        public const double DefaultGridSizeMillimeters = 1000;
        public const double DefaultClearHeightThresholdMillimeters = 3000;
        public double GridSizeMillimeters { get; set; } = DefaultGridSizeMillimeters;
        public double ClearHeightThresholdMillimeters { get; set; } = DefaultClearHeightThresholdMillimeters;
        public double FinishFloorOffsetMillimeters { get; set; }
        public bool IncludeCeilings { get; set; } = true;
        public bool IncludeMep { get; set; } = true;
        public double MinimumPipeDiameterMillimeters { get; set; } = 50;
        public bool RenderOnlyProblemCells { get; set; } = true;
    }
}
```

```csharp
namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class GridCell
    {
        public GridCell(string number, Rect2d bounds, string levelName, double levelElevationMillimeters)
        {
            Number = number;
            Bounds = bounds;
            LevelName = levelName;
            LevelElevationMillimeters = levelElevationMillimeters;
        }

        public string Number { get; }
        public Rect2d Bounds { get; }
        public string LevelName { get; }
        public double LevelElevationMillimeters { get; }
    }
}
```

```csharp
namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class ObstacleProjection
    {
        public ObstacleProjection(string elementKey, string elementName, string categoryName, string sourceModelName, bool isFromLink, Rect2d bounds, double bottomElevationMillimeters)
        {
            ElementKey = elementKey;
            ElementName = elementName;
            CategoryName = categoryName;
            SourceModelName = sourceModelName;
            IsFromLink = isFromLink;
            Bounds = bounds;
            BottomElevationMillimeters = bottomElevationMillimeters;
        }

        public string ElementKey { get; }
        public string ElementName { get; }
        public string CategoryName { get; }
        public string SourceModelName { get; }
        public bool IsFromLink { get; }
        public Rect2d Bounds { get; }
        public double BottomElevationMillimeters { get; }
    }
}
```

```csharp
namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class ClearHeightResult
    {
        public ClearHeightResult(GridCell cell, double? clearHeightMillimeters, double thresholdMillimeters, RiskLevel riskLevel, ObstacleProjection controllingObstacle)
        {
            Cell = cell;
            ClearHeightMillimeters = clearHeightMillimeters;
            ThresholdMillimeters = thresholdMillimeters;
            RiskLevel = riskLevel;
            ControllingObstacle = controllingObstacle;
        }

        public GridCell Cell { get; }
        public double? ClearHeightMillimeters { get; }
        public double ThresholdMillimeters { get; }
        public double? DifferenceMillimeters => ClearHeightMillimeters.HasValue ? ClearHeightMillimeters.Value - ThresholdMillimeters : (double?)null;
        public RiskLevel RiskLevel { get; }
        public ObstacleProjection ControllingObstacle { get; }
    }
}
```

- [ ] **Step 2: Implement pure services**

Create `GridBuilder`, `ObstacleProjectionMapper`, and `ClearHeightCalculator`:

```csharp
using System;
using System.Collections.Generic;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class GridBuilder
    {
        public static IEnumerable<GridCell> BuildInsideRectangle(Rect2d boundary, double gridSizeMillimeters, string levelName, double levelElevationMillimeters)
        {
            return BuildInsideRectangles(boundary, new[] { boundary }, gridSizeMillimeters, levelName, levelElevationMillimeters);
        }

        public static IEnumerable<GridCell> BuildInsideRectangles(Rect2d boundary, IReadOnlyCollection<Rect2d> masks, double gridSizeMillimeters, string levelName, double levelElevationMillimeters)
        {
            if (gridSizeMillimeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridSizeMillimeters), "网格尺寸必须大于 0。");
            }

            int index = 1;
            for (double y = boundary.MinY; y < boundary.MaxY; y += gridSizeMillimeters)
            {
                for (double x = boundary.MinX; x < boundary.MaxX; x += gridSizeMillimeters)
                {
                    var cellBounds = new Rect2d(x, y, Math.Min(x + gridSizeMillimeters, boundary.MaxX), Math.Min(y + gridSizeMillimeters, boundary.MaxY));
                    if (!IsCellInsideAnyMask(cellBounds, masks))
                    {
                        continue;
                    }

                    yield return new GridCell(levelName + "-" + index.ToString("000"), cellBounds, levelName, levelElevationMillimeters);
                    index++;
                }
            }
        }

        private static bool IsCellInsideAnyMask(Rect2d cellBounds, IEnumerable<Rect2d> masks)
        {
            foreach (Rect2d mask in masks)
            {
                if (mask.ContainsCenterOf(cellBounds))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
```

```csharp
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class ObstacleProjectionMapper
    {
        public static IReadOnlyList<ObstacleProjection> FindCoveringObstacles(GridCell cell, IEnumerable<ObstacleProjection> obstacles)
        {
            return obstacles.Where(obstacle => obstacle.Bounds.Intersects(cell.Bounds)).ToList();
        }
    }
}
```

```csharp
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class ClearHeightCalculator
    {
        public static ClearHeightResult Calculate(GridCell cell, IEnumerable<ObstacleProjection> coveringObstacles, AnalysisSettings settings)
        {
            ObstacleProjection controllingObstacle = coveringObstacles
                .OrderBy(obstacle => obstacle.BottomElevationMillimeters)
                .FirstOrDefault();

            if (controllingObstacle == null)
            {
                return new ClearHeightResult(cell, null, settings.ClearHeightThresholdMillimeters, RiskLevel.Unknown, null);
            }

            double clearHeight = controllingObstacle.BottomElevationMillimeters - cell.LevelElevationMillimeters - settings.FinishFloorOffsetMillimeters;
            RiskLevel riskLevel = Classify(clearHeight, settings.ClearHeightThresholdMillimeters);
            return new ClearHeightResult(cell, clearHeight, settings.ClearHeightThresholdMillimeters, riskLevel, controllingObstacle);
        }

        public static RiskLevel Classify(double clearHeightMillimeters, double thresholdMillimeters)
        {
            if (clearHeightMillimeters < thresholdMillimeters - 300)
            {
                return RiskLevel.Severe;
            }

            if (clearHeightMillimeters < thresholdMillimeters)
            {
                return RiskLevel.Insufficient;
            }

            if (clearHeightMillimeters < thresholdMillimeters + 300)
            {
                return RiskLevel.Warning;
            }

            return RiskLevel.Passed;
        }
    }
}
```

- [ ] **Step 3: Run algorithm tests and verify they pass**

Run:

```bash
dotnet test tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 4: Commit pure core**

```bash
git add src/PlugHub.ClearHeightAnalysis/Models src/PlugHub.ClearHeightAnalysis/Services tests/PlugHub.ClearHeightAnalysis.Tests
git commit -m "feat: add clear height grid calculation core"
```

---

### Task 3: Register PlugHub Module and Package Validation

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj`
- Create: `src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisModule.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisCommand.cs`
- Create: `icons/clear-height-analysis.png`
- Modify: `packages.json`
- Modify: `build.ps1`
- Modify: `PlugHub_Packages.slnx`
- Modify: `README.md`
- Modify: `tests/Validate-Package.ps1`

- [ ] **Step 1: Add validator expectations first**

Append validation checks for the new module in `tests/Validate-Package.ps1` near existing module-specific checks:

```powershell
$clearHeightModule = $manifest.modules | Where-Object { $_.id -eq "plughub.modules.clear-height-analysis" } | Select-Object -First 1
if ($null -eq $clearHeightModule) {
    Add-Failure "Missing clear height analysis module in packages.json"
}
else {
    if ($clearHeightModule.assembly -ne "dist/PlugHub.ClearHeightAnalysis.dll") {
        Add-Failure "Clear height analysis module assembly must be dist/PlugHub.ClearHeightAnalysis.dll"
    }

    $feature = $clearHeightModule.features | Where-Object { $_.id -eq "plughub.modules.clear-height-analysis.analyze" } | Select-Object -First 1
    if ($null -eq $feature) {
        Add-Failure "Missing clear height analysis feature in packages.json"
    }
    else {
        if ($feature.displayName -ne (ConvertFrom-Json '"\u51c0\u9ad8\u5206\u6790"')) {
            Add-Failure "Clear height analysis feature displayName must match the manifest display name"
        }
        if ($feature.commandType -ne "PlugHub.ClearHeightAnalysis.ClearHeightAnalysisCommand") {
            Add-Failure "Clear height analysis commandType must be PlugHub.ClearHeightAnalysis.ClearHeightAnalysisCommand"
        }
        Require-FeatureIcon $feature "icons/clear-height-analysis.png"
    }
}

Require-File "src\PlugHub.ClearHeightAnalysis\PlugHub.ClearHeightAnalysis.csproj"
Require-File "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisModule.cs"
Require-File "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisCommand.cs"
Require-Text "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisModule.cs" "土建工具" "Clear height module business category"
Require-Text "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisCommand.cs" "BuildingOutlineProvider" "Clear height outline provider usage"
Require-Text "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisCommand.cs" "ObstacleProjectionMapper" "Clear height projection mapper usage"
Require-Text "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisCommand.cs" "HeatmapRenderer" "Clear height heatmap renderer usage"
Require-Text "build.ps1" "src\PlugHub.ClearHeightAnalysis\PlugHub.ClearHeightAnalysis.csproj" "Clear height project build registration"
Require-Text "PlugHub_Packages.slnx" "src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj" "Clear height solution registration"
```

- [ ] **Step 2: Run validator and verify it fails for missing module**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
```

Expected: validation fails with missing `PlugHub.ClearHeightAnalysis` files and manifest entries.

- [ ] **Step 3: Add project, module descriptor, command shell, icon, manifest, build, solution, and README entries**

Use the `PlugHub.ProjectAutoSave` project shape. The module descriptor must use:

```csharp
Id = "plughub.modules.clear-height-analysis",
Name = "土建工具",
Description = "按建筑外轮廓网格分析结构和机电构件控制下的项目净高。",
Tags = new[] { "civil", "architecture", "structural", "mep", "clear-height", "heatmap", "revit-api" },
CommandType = "PlugHub.ClearHeightAnalysis.ClearHeightAnalysisCommand"
```

The `packages.json` module entry must use:

```json
{
  "id": "plughub.modules.clear-height-analysis",
  "version": "V1.0.0",
  "author": "GAOMENGGU",
  "displayName": "土建工具",
  "description": "按建筑外轮廓网格分析结构和机电构件控制下的项目净高。",
  "assembly": "dist/PlugHub.ClearHeightAnalysis.dll",
  "category": "civil",
  "tags": ["civil", "architecture", "structural", "mep", "clear-height", "heatmap", "revit-api"],
  "features": [
    {
      "id": "plughub.modules.clear-height-analysis.analyze",
      "displayName": "净高分析",
      "description": "按可调网格投影结构和机电构件，生成净高热力图和低净高结果表。",
      "iconPath": "icons/clear-height-analysis.png",
      "commandType": "PlugHub.ClearHeightAnalysis.ClearHeightAnalysisCommand"
    }
  ]
}
```

Create `icons/clear-height-analysis.png` with a small script that writes an unfiltered 32x32 RGBA PNG and uses `#1A1A1A` for every visible pixel.

- [ ] **Step 4: Run validator and direct module build**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
dotnet build src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj -c Release /p:RevitVersion=2020 /p:RevitApiReferenceMode=NuGet
```

Expected: validator passes and module build exits 0.

- [ ] **Step 5: Commit package registration**

```bash
git add src/PlugHub.ClearHeightAnalysis icons/clear-height-analysis.png packages.json build.ps1 PlugHub_Packages.slnx README.md tests/Validate-Package.ps1
git commit -m "feat: register clear height analysis module"
```

---

### Task 4: Build Settings Window and Input Validation

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/UI/ClearHeightAnalysisWindow.xaml`
- Create: `src/PlugHub.ClearHeightAnalysis/UI/ClearHeightAnalysisWindow.xaml.cs`
- Modify: `src/PlugHub.ClearHeightAnalysis/Models/AnalysisSettings.cs`
- Modify: `tests/Validate-Package.ps1`

- [ ] **Step 1: Add validator checks for design-language and required controls**

Add checks:

```powershell
Require-File "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml"
Require-File "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml.cs"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml" "净高分析设置" "Clear height settings window title"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml" "#F5F5F5" "Clear height settings background color"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml" "#2B579A" "Clear height primary color"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml" "网格尺寸" "Clear height grid size input"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml" "净高阈值" "Clear height threshold input"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml" "包含机电" "Clear height MEP option"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml" "只显示问题网格" "Clear height render mode option"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\ClearHeightAnalysisWindow.xaml.cs" "TryCreateSettings" "Clear height settings validation method"
```

- [ ] **Step 2: Run validator and verify it fails for missing UI**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
```

Expected: validation fails with missing settings window checks.

- [ ] **Step 3: Implement WPF window**

Implement a 620px wide no-resize window using the same visual language as `AutoSaveSettingsWindow.xaml`: `#F5F5F5` background, white cards, `#2B579A` primary button, `Microsoft YaHei`, 13px body text. Include controls for level, outline source, grid size, finish-floor offset, clear-height threshold, search height, include ceilings, include MEP, minimum pipe diameter, and render-only-problem-cells.

Expose this method in `ClearHeightAnalysisWindow.xaml.cs`:

```csharp
public bool TryCreateSettings(out AnalysisSettings settings, out string validationMessage)
{
    settings = new AnalysisSettings();
    validationMessage = string.Empty;

    if (!double.TryParse(GridSizeTextBox.Text, out double gridSize) || gridSize < 100 || gridSize > 5000)
    {
        validationMessage = "网格尺寸需为 100-5000mm。";
        return false;
    }

    if (!double.TryParse(ClearHeightThresholdTextBox.Text, out double threshold) || threshold < 1000 || threshold > 10000)
    {
        validationMessage = "净高阈值需为 1000-10000mm。";
        return false;
    }

    if (!double.TryParse(FinishFloorOffsetTextBox.Text, out double finishOffset) || finishOffset < 0 || finishOffset > 1000)
    {
        validationMessage = "完成面偏移需为 0-1000mm。";
        return false;
    }

    settings.GridSizeMillimeters = gridSize;
    settings.ClearHeightThresholdMillimeters = threshold;
    settings.FinishFloorOffsetMillimeters = finishOffset;
    settings.IncludeCeilings = IncludeCeilingsCheckBox.IsChecked == true;
    settings.IncludeMep = IncludeMepCheckBox.IsChecked == true;
    settings.RenderOnlyProblemCells = RenderOnlyProblemCellsCheckBox.IsChecked == true;
    return true;
}
```

- [ ] **Step 4: Run validator and module build**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
dotnet build src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj -c Release /p:RevitVersion=2020 /p:RevitApiReferenceMode=NuGet
```

Expected: validator passes and build exits 0.

- [ ] **Step 5: Commit settings UI**

```bash
git add src/PlugHub.ClearHeightAnalysis/UI src/PlugHub.ClearHeightAnalysis/Models/AnalysisSettings.cs tests/Validate-Package.ps1
git commit -m "feat: add clear height analysis settings window"
```

---

### Task 5: Implement Outline, Link, and Obstacle Collection

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Models/AnalysisLevel.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Models/SourceModelInfo.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Services/BuildingOutlineProvider.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Services/ObstacleCollector.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Services/UnitConversion.cs`
- Modify: `src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisCommand.cs`
- Modify: `tests/Validate-Package.ps1`

- [ ] **Step 1: Add validator checks for final-version collection behavior**

Add checks:

```powershell
Require-File "src\PlugHub.ClearHeightAnalysis\Services\BuildingOutlineProvider.cs"
Require-File "src\PlugHub.ClearHeightAnalysis\Services\ObstacleCollector.cs"
Require-File "src\PlugHub.ClearHeightAnalysis\Services\UnitConversion.cs"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\BuildingOutlineProvider.cs" "OST_Floors" "Clear height floor outline category"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\BuildingOutlineProvider.cs" "PickObject" "Clear height manual outline selection"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\ObstacleCollector.cs" "RevitLinkInstance" "Clear height linked model support"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\ObstacleCollector.cs" "GetTotalTransform" "Clear height linked model coordinate transform"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\ObstacleCollector.cs" "OST_StructuralFraming" "Clear height structural beam category"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\ObstacleCollector.cs" "OST_DuctCurves" "Clear height duct category"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\ObstacleCollector.cs" "OST_PipeCurves" "Clear height pipe category"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\ObstacleCollector.cs" "OST_CableTray" "Clear height cable tray category"
Reject-Text "src\PlugHub.ClearHeightAnalysis\Services\ObstacleCollector.cs" "Ray" "Clear height must not rely on ray-only sampling"
```

- [ ] **Step 2: Run validator and verify it fails for missing services**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
```

Expected: validation fails on missing outline and obstacle services.

- [ ] **Step 3: Implement outline provider**

`BuildingOutlineProvider` must resolve boundary rectangles in millimeters from one of these sources in priority order selected by the user: selected floor, active view crop box, or all floors intersecting the chosen level. For each accepted floor, read `element.get_BoundingBox(null)` and transform min/max to a `Rect2d` through `UnitConversion.FeetToMillimeters`.

Public surface:

```csharp
public sealed class BuildingOutlineProvider
{
    public IReadOnlyList<Rect2d> GetOutlineMasks(UIDocument uiDocument, Document document, AnalysisSettings settings);
    public Rect2d GetBoundary(IReadOnlyList<Rect2d> masks);
}
```

If the user selects a non-floor element, return an empty list and let the command fail with `未能识别建筑外轮廓，请选择楼板或使用当前视图裁剪框。`.

- [ ] **Step 4: Implement obstacle collector with linked model transforms**

`ObstacleCollector` must collect bounding-box projections for current document and loaded links. It must include structure and MEP categories when enabled:

```csharp
private static readonly BuiltInCategory[] StructuralCategories =
{
    BuiltInCategory.OST_Floors,
    BuiltInCategory.OST_StructuralFraming,
    BuiltInCategory.OST_StructuralColumns,
    BuiltInCategory.OST_Ceilings
};

private static readonly BuiltInCategory[] MepCategories =
{
    BuiltInCategory.OST_DuctCurves,
    BuiltInCategory.OST_DuctFitting,
    BuiltInCategory.OST_DuctAccessory,
    BuiltInCategory.OST_CableTray,
    BuiltInCategory.OST_CableTrayFitting,
    BuiltInCategory.OST_PipeCurves,
    BuiltInCategory.OST_PipeFitting,
    BuiltInCategory.OST_PipeAccessory
};
```

For linked elements, use `RevitLinkInstance.GetLinkDocument()` and `RevitLinkInstance.GetTotalTransform()`; convert all bounding boxes into host coordinates before producing `ObstacleProjection`. The bottom elevation is transformed bounding-box `Min.Z` in millimeters.

- [ ] **Step 5: Run validator and module build**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
dotnet build src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj -c Release /p:RevitVersion=2020 /p:RevitApiReferenceMode=NuGet
```

Expected: validator passes and build exits 0.

- [ ] **Step 6: Commit outline and obstacle collection**

```bash
git add src/PlugHub.ClearHeightAnalysis/Models src/PlugHub.ClearHeightAnalysis/Services src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisCommand.cs tests/Validate-Package.ps1
git commit -m "feat: collect clear height outlines and obstacles"
```

---

### Task 6: Render Heatmap and Cleanup Tagged Results

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Services/HeatmapRenderer.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Services/ResultCleanupService.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Services/ResultTagService.cs`
- Modify: `src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisCommand.cs`
- Modify: `tests/Validate-Package.ps1`

- [ ] **Step 1: Add validator checks for heatmap and cleanup**

Add checks:

```powershell
Require-File "src\PlugHub.ClearHeightAnalysis\Services\HeatmapRenderer.cs"
Require-File "src\PlugHub.ClearHeightAnalysis\Services\ResultCleanupService.cs"
Require-File "src\PlugHub.ClearHeightAnalysis\Services\ResultTagService.cs"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\HeatmapRenderer.cs" "FilledRegion" "Clear height heatmap filled region rendering"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\HeatmapRenderer.cs" "RiskLevel.Severe" "Clear height severe heatmap color"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\HeatmapRenderer.cs" "RiskLevel.Insufficient" "Clear height insufficient heatmap color"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\HeatmapRenderer.cs" "RiskLevel.Warning" "Clear height warning heatmap color"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\HeatmapRenderer.cs" "RiskLevel.Passed" "Clear height passed heatmap color"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\ResultTagService.cs" "Schema" "Clear height result tagging schema"
Require-Text "src\PlugHub.ClearHeightAnalysis\Services\ResultCleanupService.cs" "Delete" "Clear height cleanup deletes tagged results"
```

- [ ] **Step 2: Run validator and verify it fails for missing renderer**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
```

Expected: validation fails on missing heatmap and cleanup services.

- [ ] **Step 3: Implement heatmap renderer**

`HeatmapRenderer` must create one `FilledRegion` per rendered cell in the active plan view. Use `CurveLoop` rectangle boundaries at each grid cell bounds, convert millimeters to feet, and resolve one `FilledRegionType` per risk level. Colors:

```csharp
RiskLevel.Severe => new Color(220, 53, 69)
RiskLevel.Insufficient => new Color(245, 124, 0)
RiskLevel.Warning => new Color(251, 192, 45)
RiskLevel.Passed => new Color(76, 175, 80)
RiskLevel.Unknown => new Color(158, 158, 158)
```

Skip passed cells when `AnalysisSettings.RenderOnlyProblemCells` is true. Tag every generated region with a batch id through `ResultTagService`.

- [ ] **Step 4: Implement cleanup service**

`ResultCleanupService` must find and delete only elements tagged by `ResultTagService`. It must not delete user-created filled regions or unrelated PlugHub output. Public surface:

```csharp
public sealed class ResultCleanupService
{
    public int DeleteExistingResults(Document document);
}
```

- [ ] **Step 5: Run validator and module build**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
dotnet build src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj -c Release /p:RevitVersion=2020 /p:RevitApiReferenceMode=NuGet
```

Expected: validator passes and build exits 0.

- [ ] **Step 6: Commit heatmap and cleanup**

```bash
git add src/PlugHub.ClearHeightAnalysis/Services src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisCommand.cs tests/Validate-Package.ps1
git commit -m "feat: render clear height heatmap results"
```

---

### Task 7: Add Result Table and Command Orchestration

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/UI/AnalysisResultWindow.xaml`
- Create: `src/PlugHub.ClearHeightAnalysis/UI/AnalysisResultWindow.xaml.cs`
- Modify: `src/PlugHub.ClearHeightAnalysis/ClearHeightAnalysisCommand.cs`
- Modify: `tests/Validate-Package.ps1`

- [ ] **Step 1: Add validator checks for results and orchestration**

Add checks:

```powershell
Require-File "src\PlugHub.ClearHeightAnalysis\UI\AnalysisResultWindow.xaml"
Require-File "src\PlugHub.ClearHeightAnalysis\UI\AnalysisResultWindow.xaml.cs"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\AnalysisResultWindow.xaml" "净高分析结果" "Clear height result window title"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\AnalysisResultWindow.xaml" "控制构件" "Clear height controlling obstacle column"
Require-Text "src\PlugHub.ClearHeightAnalysis\UI\AnalysisResultWindow.xaml" "来源模型" "Clear height source model column"
Require-Text "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisCommand.cs" "ResultCleanupService" "Clear height cleanup service usage"
Require-Text "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisCommand.cs" "ClearHeightCalculator.Calculate" "Clear height calculation usage"
Require-Text "src\PlugHub.ClearHeightAnalysis\ClearHeightAnalysisCommand.cs" "AnalysisResultWindow" "Clear height result window usage"
```

- [ ] **Step 2: Run validator and verify it fails for missing result UI**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
```

Expected: validation fails on missing result window and command usage.

- [ ] **Step 3: Implement result table**

The result window must display one row per `RiskLevel.Severe`, `RiskLevel.Insufficient`, `RiskLevel.Warning`, and `RiskLevel.Unknown` result. Columns: level, grid number, center X, center Y, clear height, threshold, difference, risk level, controlling element name, category, source model, element key, and link flag.

Use `DataGrid` with `IsReadOnly="True"`, `AutoGenerateColumns="False"`, and white-card styling matching other WPF windows.

- [ ] **Step 4: Wire the command end-to-end**

`ClearHeightAnalysisCommand.Execute` must:

1. Get `UIApplication`, `UIDocument`, `Document`, and active view.
2. Show `ClearHeightAnalysisWindow` owned by Revit main window.
3. Build outline masks with `BuildingOutlineProvider`.
4. Generate grids with `GridBuilder`.
5. Collect obstacles from current and linked models with `ObstacleCollector`.
6. For each grid, find covering obstacles with `ObstacleProjectionMapper.FindCoveringObstacles`.
7. Calculate each result with `ClearHeightCalculator.Calculate`.
8. Open one transaction named `生成净高分析热力图`.
9. Delete existing tagged results with `ResultCleanupService`.
10. Render new heatmap with `HeatmapRenderer`.
11. Show `AnalysisResultWindow` with results.

Fail with a clear `message` when there is no document, no active view, no outline, zero grid cells, or zero obstacles.

- [ ] **Step 5: Run validator, tests, and module build**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
dotnet test tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj
dotnet build src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj -c Release /p:RevitVersion=2020 /p:RevitApiReferenceMode=NuGet
```

Expected: validator passes, tests pass, and build exits 0.

- [ ] **Step 6: Commit command orchestration and result table**

```bash
git add src/PlugHub.ClearHeightAnalysis tests/Validate-Package.ps1
git commit -m "feat: wire clear height analysis workflow"
```

---

### Task 8: Package DLL, Validate Release Artifacts, and Push Branch

**Files:**
- Modify: `dist/PlugHub.ClearHeightAnalysis.dll`
- Inspect: `dist/*.dll`

- [ ] **Step 1: Run full package build**

Run:

```bash
pwsh ./build.ps1 -UseRevitApiNuGet
```

Expected: command exits 0 and creates `dist/PlugHub.ClearHeightAnalysis.dll`.

- [ ] **Step 2: Revert unrelated dist binaries if the build rewrites them**

Run:

```bash
git status --short dist
```

If existing module DLLs changed only because the full build rewrote them, revert unrelated DLLs and keep only `dist/PlugHub.ClearHeightAnalysis.dll`:

```bash
git checkout -- dist/PlugHub.DuctPreferredJunction.dll dist/PlugHub.FamilyFileSaver.dll dist/PlugHub.FamilyMaterialParameters.dll dist/PlugHub.GridVisibility.dll dist/PlugHub.LevelVisibility.dll dist/PlugHub.MepTypeFilterVisibility.dll dist/PlugHub.ProjectAutoSave.dll dist/PlugHub.ReferencePlaneVisibility.dll
```

- [ ] **Step 3: Run final local verification**

Run:

```bash
pwsh ./tests/Validate-Package.ps1
dotnet test tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj
dotnet build src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj -c Release /p:RevitVersion=2020 /p:RevitApiReferenceMode=NuGet
git status --short --branch
```

Expected: validator passes, tests pass, module build exits 0, and only intended files are modified.

- [ ] **Step 4: Commit package output**

```bash
git add dist/PlugHub.ClearHeightAnalysis.dll
git commit -m "build: add clear height analysis package output"
```

- [ ] **Step 5: Push `codex` branch**

```bash
git push origin HEAD:codex
```

Expected: push succeeds and updates the existing `codex -> main` pull request if one is open for the branch.

---

## Revit Runtime Verification Checklist

Run these checks on Windows with Revit 2020 after Linux build validation:

- [ ] `净高分析` button appears under `土建工具` with a `#1A1A1A` monochrome icon.
- [ ] Settings window opens without clipping action buttons at 100% and 125% display scaling.
- [ ] Selected floor or active view crop generates grid cells inside the building outline.
- [ ] A structure link with non-identity transform affects the correct host-model grid cells.
- [ ] Beams, slabs, ducts, pipes, and cable trays that cross only a grid edge still control that grid.
- [ ] Each grid reports the lowest controlling obstacle, not average clear height.
- [ ] Heatmap colors match severe, insufficient, warning, passed, and unknown states.
- [ ] Re-running analysis deletes only prior tagged heatmap regions from this plugin.
- [ ] Result table lists source model, linked flag, category, element key, and controlling element name.

---

## Plan Self-Review

- Spec coverage: The plan covers module registration, WPF settings, no-room outline strategy, configurable 1000mm default grid, linked structural/MEP models, projection-based obstacle coverage, minimum clear height calculation, heatmap rendering, result table, and cleanup.
- Test strategy: Pure grid/calculation behavior is unit-tested first; Revit API integration is covered by package validation, compile validation, and explicit Windows/Revit runtime checks.
- Scope control: The first implemented version is still the final projection-based architecture; exact polygon holes and formal report export are excluded by the design document and are not required in this implementation plan.
