# Clear Height Analysis V2 Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the pure, testable V2 geometry and clear-height calculation core without changing the active Revit command yet.

**Architecture:** Add a Revit-independent `PlugHub.ClearHeightAnalysis.Core` namespace beside the legacy implementation. The core represents polygon boundaries, valid grid cells and typed obstacle snapshots, indexes obstacles into grid buckets, and calculates blocked/unknown/risk results without scanning the full obstacle collection per cell. Revit adapters, persistence and output exporters remain separate follow-up plans.

**Tech Stack:** C# 8, .NET Framework 4.8 production library, .NET 8 xUnit test host, no additional runtime NuGet dependencies.

---

## Scope boundary

This is plan 1 of 4 from the approved V2 design. It delivers a compiling and independently verified core but deliberately does not switch `ClearHeightAnalysisCommand` to V2. Follow-up plans cover:

1. Revit context, real floor boundaries, linked-model snapshots and UI integration.
2. `DataStorage` batch persistence, result management and lightweight vector output.
3. Optional area color plans, PNG/CSV output, migration cleanup and Revit 2020 runtime verification.

## File structure

Create production files under `src/PlugHub.ClearHeightAnalysis/Core/` so they contain no `Autodesk.Revit.*` references:

- `Geometry/Point2d.cs`: immutable XY point in millimetres.
- `Geometry/Segment2d.cs`: immutable line segment and normalized undirected edge key.
- `Geometry/PolygonLoop2d.cs`: validated closed polygon loop and bounds.
- `Geometry/PolygonMath.cs`: point containment, segment intersection, polygon/rectangle intersection and signed area.
- `Models/AnalysisBoundary.cs`: outer loops with optional holes.
- `Models/CoreAnalysisSettings.cs`: calculation-only validated settings.
- `Models/GridCellData.cs`: grid indices, bounds, centre and effective boundary coverage.
- `Models/ObstacleKind.cs`: `Overhead` and `Blocked` semantics.
- `Models/GeometryConfidence.cs`: exact/approximation/fallback confidence.
- `Models/ObstacleSnapshot.cs`: serializable pure obstacle geometry and elevation plane.
- `Models/CellStatus.cs`: blocked/unknown/risk status.
- `Models/CellAnalysisResult.cs`: immutable per-cell calculation result.
- `Services/GridDomainBuilder.cs`: polygon-aware grid creation.
- `Services/ObstacleSpatialIndex.cs`: grid bucket index.
- `Services/ClearHeightEngine.cs`: candidate refinement and worst-case calculation.

Create tests under `tests/PlugHub.ClearHeightAnalysis.Tests/Core/` and link all pure core source through the existing test project.

### Task 1: Geometry primitives and polygon predicates

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Geometry/Point2d.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Geometry/Segment2d.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Geometry/PolygonLoop2d.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Geometry/PolygonMath.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/PolygonMathTests.cs`
- Modify: `tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj`

- [ ] **Step 1: Link the pure V2 core into the test project**

Add this item after the existing linked legacy sources:

```xml
<Compile Include="../../src/PlugHub.ClearHeightAnalysis/Core/**/*.cs"
         LinkBase="Core" />
```

- [ ] **Step 2: Write failing polygon tests**

Create `PolygonMathTests.cs` with these cases:

```csharp
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class PolygonMathTests
    {
        private static PolygonLoop2d Rectangle(double minX, double minY, double maxX, double maxY)
        {
            return new PolygonLoop2d(new[]
            {
                new Point2d(minX, minY),
                new Point2d(maxX, minY),
                new Point2d(maxX, maxY),
                new Point2d(minX, maxY)
            });
        }

        [Fact]
        public void ContainsPointHandlesInsideBoundaryAndOutside()
        {
            PolygonLoop2d loop = Rectangle(0, 0, 2000, 1000);

            Assert.True(PolygonMath.ContainsPoint(loop, new Point2d(500, 500)));
            Assert.True(PolygonMath.ContainsPoint(loop, new Point2d(0, 500)));
            Assert.False(PolygonMath.ContainsPoint(loop, new Point2d(2500, 500)));
        }

        [Fact]
        public void IntersectsRectangleDetectsAnEdgeCrossingWithoutContainingItsCenter()
        {
            var diagonal = new PolygonLoop2d(new[]
            {
                new Point2d(-100, 450), new Point2d(1100, 450),
                new Point2d(1100, 550), new Point2d(-100, 550)
            });

            Assert.True(PolygonMath.IntersectsRectangle(diagonal, 0, 0, 1000, 1000));
            Assert.False(PolygonMath.IntersectsRectangle(diagonal, 1200, 0, 2200, 1000));
        }

        [Fact]
        public void SignedAreaDistinguishesClockwiseAndCounterClockwiseLoops()
        {
            PolygonLoop2d ccw = Rectangle(0, 0, 1000, 1000);
            var cw = new PolygonLoop2d(new[]
            {
                new Point2d(0, 0), new Point2d(0, 1000),
                new Point2d(1000, 1000), new Point2d(1000, 0)
            });

            Assert.True(PolygonMath.SignedArea(ccw) > 0);
            Assert.True(PolygonMath.SignedArea(cw) < 0);
        }
    }
}
```

- [ ] **Step 3: Run the focused test and verify it fails**

Run:

```bash
/home/yilan/.dotnet/dotnet test tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj --filter FullyQualifiedName~PolygonMathTests
```

Expected: compilation fails because the `Core.Geometry` types do not exist.

- [ ] **Step 4: Implement immutable geometry primitives**

Implement `Point2d` with `X`, `Y`, value equality and invariant hash code. Implement `Segment2d` with `Start`, `End` and `Bounds`. Implement `PolygonLoop2d` so its constructor rejects null input, fewer than three distinct points and zero-area loops; do not repeat the first point at the end. Expose `IReadOnlyList<Point2d> Points` and computed `Rect2d Bounds`, reusing the existing pure `Models.Rect2d` type only if that does not introduce a legacy namespace dependency; otherwise define the bounds as four scalar properties on `PolygonLoop2d`.

Implement `PolygonMath` using:

```csharp
public static double SignedArea(PolygonLoop2d loop)
{
    double twiceArea = 0;
    for (int i = 0; i < loop.Points.Count; i++)
    {
        Point2d current = loop.Points[i];
        Point2d next = loop.Points[(i + 1) % loop.Points.Count];
        twiceArea += current.X * next.Y - next.X * current.Y;
    }
    return twiceArea / 2.0;
}
```

`ContainsPoint` must first recognize points on an edge within `0.001mm`, then use an even-odd ray crossing. `IntersectsRectangle` returns true when any polygon point lies in the rectangle, any rectangle corner lies in the polygon, or any polygon edge intersects a rectangle edge. Rectangle bounds use half-open grid ownership for indexing but geometric edge contact alone does not count as positive overlap.

- [ ] **Step 5: Run focused tests and the legacy suite**

Run:

```bash
/home/yilan/.dotnet/dotnet test tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj --filter FullyQualifiedName~PolygonMathTests
/home/yilan/.dotnet/dotnet test tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj
```

Expected: 3 new tests pass and the existing 6 tests remain green.

- [ ] **Step 6: Commit Task 1**

```bash
git add src/PlugHub.ClearHeightAnalysis/Core/Geometry tests/PlugHub.ClearHeightAnalysis.Tests
git commit -m "feat: add clear height core geometry"
```

### Task 2: Polygon-aware analysis boundary and grid domain

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/AnalysisBoundary.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/GridCellData.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Services/GridDomainBuilder.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/GridDomainBuilderTests.cs`

- [ ] **Step 1: Write failing boundary/grid tests**

Cover these exact behaviors:

```csharp
[Fact]
public void BuildExcludesCellsInsideAHole()
{
    AnalysisBoundary boundary = BoundaryFactory.Create(
        outer: Rectangle(0, 0, 3000, 3000),
        holes: new[] { Rectangle(1000, 1000, 2000, 2000) });

    var cells = GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0);

    Assert.Equal(8, cells.Count);
    Assert.DoesNotContain(cells, cell => cell.Column == 1 && cell.Row == 1);
}

[Fact]
public void BuildKeepsCellsIntersectingAConcaveBoundaryEdge()
{
    var lShape = new PolygonLoop2d(new[]
    {
        new Point2d(0, 0), new Point2d(2000, 0),
        new Point2d(2000, 1000), new Point2d(1000, 1000),
        new Point2d(1000, 2000), new Point2d(0, 2000)
    });

    var cells = GridDomainBuilder.Build(
        new AnalysisBoundary(new[] { new BoundaryRegion(lShape, new PolygonLoop2d[0]) }),
        1000, "level-1", "1F", 0);

    Assert.Equal(3, cells.Count);
}

[Fact]
public void BuildPreservesTwoDisconnectedRegions()
{
    AnalysisBoundary boundary = new AnalysisBoundary(new[]
    {
        new BoundaryRegion(Rectangle(0, 0, 1000, 1000), new PolygonLoop2d[0]),
        new BoundaryRegion(Rectangle(3000, 0, 4000, 1000), new PolygonLoop2d[0])
    });

    var cells = GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0);

    Assert.Equal(2, cells.Count);
}
```

Put shared test polygon construction in `tests/.../Core/TestGeometry.cs`; do not add a production-only factory for test convenience.

- [ ] **Step 2: Run tests and verify failure**

Run the test project filtered to `GridDomainBuilderTests`.

Expected: compilation fails because `AnalysisBoundary`, `BoundaryRegion`, `GridCellData` and `GridDomainBuilder` do not exist.

- [ ] **Step 3: Implement boundary and grid models**

`BoundaryRegion` contains one outer loop and read-only holes. Its constructor normalizes the outer loop to counter-clockwise and holes to clockwise. `AnalysisBoundary` requires at least one region and computes aggregate bounds.

`GridCellData` contains:

```csharp
public string Number { get; }
public int Column { get; }
public int Row { get; }
public double MinX { get; }
public double MinY { get; }
public double MaxX { get; }
public double MaxY { get; }
public string LevelUniqueId { get; }
public string LevelName { get; }
public double LevelElevationMillimeters { get; }
public IReadOnlyList<int> BoundaryRegionIndexes { get; }
```

`GridDomainBuilder.Build` anchors the grid at `floor(boundary.Min / gridSize) * gridSize`, evaluates each candidate rectangle against all outer loops and holes, and includes a cell when it has positive-area intersection with an outer loop and is not wholly inside a hole. Preserve the region indexes that intersect the cell. Number cells deterministically by increasing row then column as `1F-000001`.

- [ ] **Step 4: Run grid and full tests**

Expected: 3 grid-domain tests and all previous tests pass.

- [ ] **Step 5: Commit Task 2**

```bash
git add src/PlugHub.ClearHeightAnalysis/Core tests/PlugHub.ClearHeightAnalysis.Tests/Core
git commit -m "feat: build polygon-aware clear height grid"
```

### Task 3: Typed obstacle snapshots and spatial index

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/ObstacleKind.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/GeometryConfidence.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/ElevationPlane.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/ObstacleSnapshot.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Services/ObstacleSpatialIndex.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/ObstacleSpatialIndexTests.cs`

- [ ] **Step 1: Write failing index tests**

Create snapshots with rectangular footprints and assert:

```csharp
[Fact]
public void QueryReturnsOnlyObstaclesRegisteredForTheCell()
{
    ObstacleSnapshot near = Snapshot("near", 100, 100, 900, 900, 2800);
    ObstacleSnapshot far = Snapshot("far", 5000, 5000, 6000, 6000, 2600);
    var index = new ObstacleSpatialIndex(0, 0, 1000, new[] { near, far });
    var cell = new GridCellData("1F-000001", 0, 0, 0, 0, 1000, 1000,
        "level-1", "1F", 0, new[] { 0 });

    Assert.Equal(new[] { "near" }, index.Query(cell).Select(item => item.Key));
}

[Fact]
public void IndexRegistersAnObstacleCrossingMultipleCellsInEachBucket()
{
    ObstacleSnapshot crossing = Snapshot("crossing", 500, 100, 2500, 900, 3000);
    var index = new ObstacleSpatialIndex(0, 0, 1000, new[] { crossing });

    Assert.Single(index.Query(CellAtColumn(0)));
    Assert.Single(index.Query(CellAtColumn(1)));
    Assert.Single(index.Query(CellAtColumn(2)));
    Assert.Empty(index.Query(CellAtColumn(3)));
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: missing obstacle core types.

- [ ] **Step 3: Implement snapshot models**

Define:

```csharp
public enum ObstacleKind { Overhead, Blocked }
public enum GeometryConfidence { Exact, CategoryApproximation, BoundingBoxFallback }

public readonly struct ElevationPlane
{
    public ElevationPlane(double a, double b, double c)
    {
        A = a; B = b; C = c;
    }
    public double A { get; }
    public double B { get; }
    public double C { get; }
    public double At(double x, double y) => A * x + B * y + C;
    public static ElevationPlane Constant(double elevation) => new ElevationPlane(0, 0, elevation);
}
```

`ObstacleSnapshot` stores key, display name, category, source model, optional link instance key, kind, footprint loop, bounds derived from the footprint, bottom plane, bottom/top Z and confidence. Reject blank keys, invalid vertical ranges and `Blocked` obstacles that do not cross the analysis elevation later supplied to the engine.

- [ ] **Step 4: Implement the bucket index**

`ObstacleSpatialIndex` calculates inclusive grid-column and row ranges from obstacle bounds using a `0.001mm` inward tolerance at maximum edges, stores each obstacle once per bucket, and returns an empty immutable collection for missing buckets. Construction is `O(obstacle bucket coverage)`; query is `O(bucket candidates)`.

- [ ] **Step 5: Run index and full tests**

Expected: 2 index tests and all previous tests pass.

- [ ] **Step 6: Commit Task 3**

```bash
git add src/PlugHub.ClearHeightAnalysis/Core tests/PlugHub.ClearHeightAnalysis.Tests/Core
git commit -m "feat: index typed clear height obstacles"
```

### Task 4: Clear-height engine with blocked, unknown and confidence semantics

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/CoreAnalysisSettings.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/CellStatus.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/CellAnalysisResult.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Services/ClearHeightEngine.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/ClearHeightEngineTests.cs`

- [ ] **Step 1: Write failing engine tests**

Cover all product states:

```csharp
[Fact]
public void CalculateUsesTheLowestActuallyOverlappingObstacle()
{
    GridCellData cell = Cell(0, 0);
    ObstacleSnapshot beam = Overhead("beam", Rect(0, 0, 1000, 400), 3200,
        GeometryConfidence.Exact);
    ObstacleSnapshot duct = Overhead("duct", Rect(500, 0, 1000, 1000), 2800,
        GeometryConfidence.CategoryApproximation);

    CellAnalysisResult result = ClearHeightEngine.Calculate(
        cell, new[] { beam, duct }, Settings(finish: 50, threshold: 3000));

    Assert.Equal(2750, result.ClearHeightMillimeters);
    Assert.Equal(CellStatus.Insufficient, result.Status);
    Assert.Equal("duct", result.ControllingObstacleKey);
    Assert.Equal(GeometryConfidence.CategoryApproximation, result.Confidence);
}

[Fact]
public void CalculateReturnsUnknownWhenNoOverheadObstacleIntersects()
{
    CellAnalysisResult result = ClearHeightEngine.Calculate(
        Cell(0, 0), new ObstacleSnapshot[0], Settings(0, 3000));

    Assert.Equal(CellStatus.Unknown, result.Status);
    Assert.Null(result.ClearHeightMillimeters);
}

[Fact]
public void CalculateReturnsBlockedBeforeEvaluatingOverheadClearance()
{
    ObstacleSnapshot column = Blocked("column", Rect(200, 200, 400, 400), -500, 4000);
    ObstacleSnapshot slab = Overhead("slab", Rect(0, 0, 1000, 1000), 3200,
        GeometryConfidence.Exact);

    CellAnalysisResult result = ClearHeightEngine.Calculate(
        Cell(0, 0), new[] { slab, column }, Settings(0, 3000));

    Assert.Equal(CellStatus.Blocked, result.Status);
    Assert.Equal("column", result.ControllingObstacleKey);
}

[Theory]
[InlineData(2699, CellStatus.Severe)]
[InlineData(2700, CellStatus.Insufficient)]
[InlineData(2999, CellStatus.Insufficient)]
[InlineData(3000, CellStatus.Warning)]
[InlineData(3299, CellStatus.Warning)]
[InlineData(3300, CellStatus.Passed)]
public void ClassifyUsesTheApprovedThreeHundredMillimetreBand(
    double clearHeight, CellStatus expected)
{
    Assert.Equal(expected, ClearHeightEngine.Classify(clearHeight, 3000));
}
```

Add a test in which an obstacle's axis-aligned bounds touch the cell but its footprint does not overlap; it must not control the result. Add a sloped-plane test proving the engine samples all overlap polygon vertices and takes the minimum plane elevation.

- [ ] **Step 2: Run tests and verify failure**

Expected: missing settings, result and engine types.

- [ ] **Step 3: Implement calculation-only settings and results**

`CoreAnalysisSettings` requires level elevation, finish offset, search height and threshold. Reject non-positive search height/threshold and finish offsets outside `0..1000mm`.

`CellStatus` values are `Unknown`, `Blocked`, `Severe`, `Insufficient`, `Warning`, `Passed`.

`CellAnalysisResult` stores the cell, nullable clear height, threshold, status, nullable controlling obstacle key and confidence. Unknown has no confidence; blocked inherits the blocking obstacle confidence.

- [ ] **Step 4: Implement precise candidate evaluation**

For every candidate:

1. Reject it when polygon/rectangle positive overlap is false.
2. For `Blocked`, require `BottomElevationMillimeters <= finishedFloor + 100` and `TopElevationMillimeters > finishedFloor + 100`; return the worst-confidence overlapping blocker deterministically by key.
3. For `Overhead`, reject `TopElevationMillimeters <= finishedFloor + 100` and bottom elevations above `level + searchHeight`.
4. Clip the obstacle footprint to the cell rectangle with Sutherland-Hodgman clipping against left, right, bottom and top edges.
5. Evaluate `BottomPlane.At` at every clipped vertex and take the minimum.
6. Select the lowest elevation, breaking ties by obstacle key.
7. Subtract finished floor and classify with the fixed 300mm band.

Add `PolygonMath.ClipToRectangle` in Task 1's geometry service as part of this step and cover it through the sloped-plane test.

- [ ] **Step 5: Run engine and full tests**

Expected: engine state, non-overlap and sloped-plane tests pass; legacy suite remains green.

- [ ] **Step 6: Commit Task 4**

```bash
git add src/PlugHub.ClearHeightAnalysis/Core tests/PlugHub.ClearHeightAnalysis.Tests/Core
git commit -m "feat: calculate indexed clear height results"
```

### Task 5: Batch core orchestration and performance regression guard

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/CoreAnalysisSummary.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Services/ClearHeightBatchAnalyzer.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/ClearHeightBatchAnalyzerTests.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/CorePerformanceTests.cs`

- [ ] **Step 1: Write failing batch tests**

Test deterministic ordering and summary counts:

```csharp
[Fact]
public void AnalyzeReturnsRowMajorResultsAndStatusCounts()
{
    IReadOnlyList<GridCellData> cells = new[] { CellAt(1, 0), CellAt(0, 0) };
    ObstacleSnapshot low = Overhead("low", Rect(0, 0, 1000, 1000), 2500);
    var analyzer = new ClearHeightBatchAnalyzer(0, 0, 1000, new[] { low });

    CoreAnalysisSummary summary = analyzer.Analyze(cells, Settings(0, 3000));

    Assert.Equal(new[] { 0, 1 }, summary.Results.Select(item => item.Cell.Column));
    Assert.Equal(1, summary.Count(CellStatus.Severe));
    Assert.Equal(1, summary.Count(CellStatus.Unknown));
}
```

- [ ] **Step 2: Write an index-usage regression test**

Create 2,000 grid cells and 50,000 one-cell obstacle snapshots spread over a larger domain. Instrument `ObstacleSpatialIndex` with an internal `CandidateVisitCount` incremented by batch analysis, exposed to the test assembly through a read-only property. Assert candidate visits remain below `cells.Count * 100`, which fails if the implementation regresses to scanning every obstacle for every cell. Do not assert a fixed wall-clock duration.

- [ ] **Step 3: Run tests and verify failure**

Expected: missing batch analyzer and summary types.

- [ ] **Step 4: Implement batch orchestration**

`ClearHeightBatchAnalyzer` constructs one `ObstacleSpatialIndex`, sorts input cells by row then column, queries one bucket per cell and delegates to `ClearHeightEngine`. `CoreAnalysisSummary` exposes immutable results, per-status counts and total candidate visits. It contains no Revit objects and performs no persistence.

- [ ] **Step 5: Run the complete core and legacy test suite**

Run:

```bash
/home/yilan/.dotnet/dotnet test tests/PlugHub.ClearHeightAnalysis.Tests/PlugHub.ClearHeightAnalysis.Tests.csproj
```

Expected: all old and new tests pass with zero failures.

- [ ] **Step 6: Build the production module in Release/NuGet mode**

Run:

```bash
/home/yilan/.dotnet/dotnet build src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj -c Release /p:RevitVersion=2020 /p:RevitApiReferenceMode=NuGet
```

Expected: build succeeds. Existing Revit SDK `MSB3246` warnings may remain; no new compile errors or warnings from V2 core are accepted.

- [ ] **Step 7: Run package validation**

Run:

```bash
pwsh -NoLogo -NoProfile -File tests/Validate-Package.ps1
```

Expected: `Package validation passed.`

- [ ] **Step 8: Commit Task 5**

```bash
git add src/PlugHub.ClearHeightAnalysis/Core tests/PlugHub.ClearHeightAnalysis.Tests
git commit -m "test: guard clear height core performance"
```

## Plan self-review

- Spec coverage: this plan covers the pure portions of V2 sections 9, 10, 11, 12 and the calculation half of section 23. Revit extraction, persistence and output are explicitly assigned to later plans rather than left implicit.
- Placeholder scan: no `TBD`, `TODO`, “similar to” or unspecified error-handling steps remain.
- Type consistency: all tests and tasks use `Point2d`, `PolygonLoop2d`, `AnalysisBoundary`, `GridCellData`, `ObstacleSnapshot`, `ObstacleSpatialIndex`, `CoreAnalysisSettings`, `CellAnalysisResult`, `ClearHeightEngine` and `ClearHeightBatchAnalyzer` consistently.
- Compatibility: the first phase is additive and does not switch or delete the legacy command, so each task produces a buildable intermediate state.
