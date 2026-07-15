# Clear Height Analysis V2 Revit Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert real Revit 2020 levels, host/link floor boundaries and classified host/link elements into the verified V2 core data model.

**Architecture:** Keep every `Autodesk.Revit.*` type under `Revit/`; convert API objects to immutable Core models at that boundary. Use testable numeric transform and selection models for rotation/mirror correctness, then compile-check Revit adapters against Autodesk.Revit.SDK 2020. The active command is switched only after persistence/output wiring in the next plan.

**Tech Stack:** C# 8, Revit 2020 API, .NET Framework 4.8, .NET 8 xUnit pure adapter tests.

---

### Task 1: Transform-safe 3D bounds

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Geometry/Point3d.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Geometry/Transform3d.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Geometry/Bounds3d.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/Transform3dTests.cs`

- [ ] Write tests proving all eight corners are required for a 45-degree rotation, and that mirrored transforms still produce ordered bounds.
- [ ] Run `dotnet test ... --filter FullyQualifiedName~Transform3dTests` and confirm missing-type failure.
- [ ] Implement affine point transformation and `Bounds3d.TransformAllCorners`.
- [ ] Run focused and full tests; commit `feat: transform linked bounds safely`.

Required test assertions:

```csharp
Bounds3d rotated = new Bounds3d(0, 0, 0, 10, 2, 3)
    .TransformAllCorners(Transform3d.RotationZ(Math.PI / 4));
Assert.Equal(-Math.Sqrt(2), rotated.MinX, 6);
Assert.Equal(5 * Math.Sqrt(2), rotated.MaxY, 6);

Bounds3d mirrored = bounds.TransformAllCorners(
    new Transform3d(-1, 0, 0, 100, 0, 1, 0, 0, 0, 0, 1, 0));
Assert.True(mirrored.MinX <= mirrored.MaxX);
```

### Task 2: Analysis request and real Revit choices

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/AnalysisLevelChoice.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/SourceModelChoice.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/AnalysisBoundaryMode.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/AnalysisRequest.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Services/AnalysisRequestValidator.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/AnalysisRequestValidatorTests.cs`
- Modify: `src/PlugHub.ClearHeightAnalysis/UI/ClearHeightAnalysisWindow.xaml`
- Modify: `src/PlugHub.ClearHeightAnalysis/UI/ClearHeightAnalysisWindow.xaml.cs`

- [ ] Write tests that reject a missing level, no source model, invalid grid/threshold/search height and automatic boundary without current model.
- [ ] Verify RED, then implement immutable choices and validator.
- [ ] Replace free-text level name/elevation with a `ComboBox` bound to supplied level choices.
- [ ] Add a checked source-model list containing the host and each loaded link instance separately.
- [ ] Add boundary mode choices `SelectFloor`, `AutomaticHostFloors`, `ActiveViewCrop`, `ManualRectangle`; do not describe crop/manual modes as building outlines.
- [ ] Keep category, minimum pipe diameter and output-independent analysis parameters.
- [ ] Build the module and commit `feat: collect typed clear height analysis request`.

`AnalysisRequest` must carry `LevelUniqueId`, selected source keys and all validated numeric settings; it must not carry a user-entered level elevation.

### Task 3: Revit analysis context and true floor boundaries

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Revit/RevitAnalysisContextBuilder.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Revit/RevitBoundaryProvider.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Revit/RevitCurveLoopConverter.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Services/BoundaryLoopClassifier.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/BoundaryLoopClassifierTests.cs`

- [ ] Write pure tests classifying the largest containing loop as an outer ring, contained opposite/any-orientation loops as holes, and disconnected loops as separate regions.
- [ ] Verify RED and implement loop containment classification.
- [ ] Build `AnalysisContext` from `Document.GetElement(levelUniqueId)` and selected `RevitLinkInstance.UniqueId` values; unloaded links are explicit validation failures.
- [ ] For selected or automatic host/link floors, use `HostObjectUtils.GetTopFaces`, resolve `PlanarFace`, call `GetEdgesAsCurveLoops`, tessellate curves, transform every point through the element/link transform and classify loops.
- [ ] Automatic mode filters host floors by their associated level ID; it never collects upper floors within search height.
- [ ] Crop/manual fallback creates explicitly rectangular boundary regions using all transformed crop-box corners.
- [ ] Release build and commit `feat: extract real Revit floor boundaries`.

### Task 4: Classified Revit obstacle snapshots

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Revit/RevitObstacleSnapshotCollector.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Revit/RevitObstacleSnapshotFactory.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Revit/RevitElementGeometry.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Services/ObstacleRuleFilter.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/ObstacleRuleFilterTests.cs`

- [ ] Write tests for current-floor exclusion, search-top exclusion, structure-column `Blocked`, and minimum pipe diameter filtering.
- [ ] Verify RED and implement pure rule filtering.
- [ ] Collect only enabled categories from host and selected loaded links. Apply an XY/Z outline filter before geometry extraction.
- [ ] Build keys as `host:{UniqueId}` and `link:{LinkInstance.UniqueId}:{LinkedElement.UniqueId}`.
- [ ] Floors/ceilings use horizontal bottom planar-face loops when available; linear framing/duct/tray/pipe use transformed location curve plus width/height/diameter to create an oriented rectangle and elevation plane.
- [ ] Fittings/accessories/complex families use transformed solid projection when available, otherwise eight-corner bounds with `BoundingBoxFallback`.
- [ ] Columns crossing the finished-floor tolerance become `Blocked`; other vertical elements are ignored in V2 first release.
- [ ] Apply actual pipe diameter filtering to pipe curves; fittings without a reliable diameter are retained and marked approximate.
- [ ] Release build and commit `feat: snapshot Revit clear height obstacles`.

### Task 5: Revit-side analysis runner and integration contract

**Files:**
- Create: `src/PlugHub.ClearHeightAnalysis/Revit/RevitAnalysisRunner.cs`
- Create: `src/PlugHub.ClearHeightAnalysis/Core/Models/AnalysisRunData.cs`
- Create: `tests/PlugHub.ClearHeightAnalysis.Tests/Core/AnalysisRunDataTests.cs`
- Modify: `tests/Validate-Package.ps1`

- [ ] Write tests that `AnalysisRunData` rejects mismatched context/settings/results and preserves candidate-visit diagnostics.
- [ ] Verify RED and implement immutable run data.
- [ ] Implement runner order: validate request, build context, extract boundary, build grid, collect snapshots, build one spatial index, calculate summary, return run data. It must not create Revit result elements.
- [ ] Add package checks requiring the V2 Revit adapter files, `GetTotalTransform`, `HostObjectUtils.GetTopFaces`, `GetEdgesAsCurveLoops`, link-instance-aware key format and absence of `HeatmapRenderer` from `RevitAnalysisRunner`.
- [ ] Run all tests, Release/NuGet build and package validation.
- [ ] Commit `feat: assemble V2 Revit analysis pipeline`.

## Plan self-review

- Covers V2 design sections 7.1 through 12 at the Revit-to-Core boundary.
- Does not switch the production command prematurely; persistence and outputs remain the next required plan.
- All numerical transform, loop classification and rule decisions have RED/GREEN tests; Revit API calls are verified by compile here and by the final Revit runtime checklist before delivery.
- No dependency is added, and the Core namespace remains free of Autodesk references.
