# Clear Height Analysis V2 Delivery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist complete V2 analysis batches, generate lightweight and optional owner-facing outputs, switch the production command to V2, and leave the module ready for Revit 2020 real-machine verification.

**Architecture:** `AnalysisBatch` is the only durable source of truth. Pure Core services serialize batches, merge risk cells, estimate outputs, generate CSV and render an in-memory raster; Revit adapters only perform short, tagged transactions for storage and derived outputs. The default output is unfilled detail curves plus numbered text, while area color plans remain explicit opt-in output using pre-existing template assets.

**Tech Stack:** C# 8, .NET Framework 4.8, Revit 2020 API, WPF, xUnit on .NET 8, GZip/Base64/SHA-256, Extensible Storage.

---

## File structure

- `Core/Models/AnalysisBatch.cs`: durable batch identity, timestamps, run data, summary and output registry.
- `Core/Models/DerivedOutputRecord.cs`: output type, view/file metadata and element identifiers.
- `Core/Persistence/AnalysisBatchSerializer.cs`: versioned JSON DTO mapping, GZip/Base64 chunking and integrity validation.
- `Core/Models/RiskRegion.cs`, `Core/Services/RiskRegionBuilder.cs`: four-neighbour groups, boundary loops, holes and internal marker points.
- `Core/Models/OutputEstimate.cs`, `Core/Services/AreaOutputPlanner.cs`: merged/per-cell plans and preflight element counts.
- `Core/Services/CsvResultExporter.cs`, `Core/Services/RasterHeatmapRenderer.cs`: side-effect-free owner outputs.
- `Revit/AnalysisBatchRepository.cs`: DataStorage index/chunk persistence with short transactions.
- `Revit/OutputTagService.cs`, `Revit/DerivedOutputCleanupService.cs`: ownership tags and batch/output-scoped cleanup.
- `Revit/LightweightDrawingExporter.cs`: default detail-curve and TextNote output.
- `Revit/AreaColorPlanExporter.cs`: opt-in area plan based only on `PH_净高分析` and `PH_净高分析_色块`.
- `Revit/RasterHeatmapExporter.cs`, `Revit/CsvExporter.cs`: one-image Revit import and full CSV file output.
- `UI/V2ResultWindow.xaml(.cs)`: batch summary, filters, region/cell inspection and explicit output actions.
- `Revit/AnalysisWorkflowController.cs`: command orchestration; save first, then show the result manager.

### Task 1: Versioned batch and integrity-checked serializer

**Files:** create the batch/persistence Core files above and `tests/.../Core/AnalysisBatchSerializerTests.cs`.

- [ ] Write tests for deterministic round-trip of boundary, cells, obstacles, results and output registry; multiple chunks at a small test chunk size; rejection of missing/reordered chunks, hash mismatch, uncompressed-length mismatch and unsupported schema version.
- [ ] Run `dotnet test ... --filter FullyQualifiedName~AnalysisBatchSerializerTests`; verify RED from missing types.
- [ ] Implement `AnalysisBatch.CurrentSchemaVersion = 2`, generated batch ID, UTC timestamp and immutable collections. Implement explicit serializer DTOs so constructor-only Core models round-trip without relying on private reflection.
- [ ] Serialize UTF-8 JSON, record its byte length and SHA-256, GZip, Base64 and split into ordered chunks no larger than 256 KiB. During read, validate schema, sequence, chunk count, Base64/GZip, byte length and hash before DTO mapping.
- [ ] Run focused and full tests; commit `feat: persist versioned clear height batches`.

### Task 2: Risk-region topology

**Files:** create `RiskRegion.cs`, `RiskRegionBuilder.cs` and `RiskRegionBuilderTests.cs`.

- [ ] Write RED tests proving: same-status four-neighbours merge; diagonal cells do not; shared internal edges disappear; a ring of cells produces one outer loop and one hole; collinear vertices simplify; marker point lies inside the filled region; passed cells are excluded by default.
- [ ] Build regions from grid edge keys: BFS components by `(column,row,status)`, cancel undirected shared edges, trace directed loops, classify outer/hole loops, simplify collinear vertices and select the centre of a member cell that is inside the region.
- [ ] Preserve deterministic region numbers, member cell numbers, minimum clear height, worst confidence and distinct controlling obstacle keys.
- [ ] Run focused/full tests; commit `feat: merge clear height risk regions`.

### Task 3: Pure output planners, CSV and raster

**Files:** create `AreaOutputPlanner.cs`, `CsvResultExporter.cs`, `RasterHeatmapRenderer.cs` and focused tests.

- [ ] Write RED tests for merged/per-cell area and boundary-line estimates, complete RFC4180 CSV rows (including controlling element/source/confidence), one raster covering the grid bounds, transparent excluded pixels and stable risk colors.
- [ ] Implement planners without Revit types. Area plans expose loops, placement points and estimated area/boundary counts before any transaction.
- [ ] Implement UTF-8 BOM CSV with invariant numeric values and Chinese headers. Implement a minimal PNG encoder using built-in compression/CRC so production does not depend on GDI availability.
- [ ] Run focused/full tests; commit `feat: plan optional clear height outputs`.

### Task 4: DataStorage repository and output ownership tags

**Files:** create repository/tag/cleanup Revit files and extend `tests/Validate-Package.ps1`.

- [ ] Add validator RED checks for distinct immutable schema GUIDs, `DataStorage`, chunk sequence/hash validation calls, and cleanup filtering by batch ID plus output ID.
- [ ] Implement one index schema and one chunk schema. Index entries contain only searchable summaries/manifests; chunk DataStorage entities contain batch ID, sequence and payload.
- [ ] `Save`, `DeleteBatch` and output-registry updates each own one short transaction; `List` and `Load` are read-only. Corrupt/unsupported manifests return explicit status and remain deletable.
- [ ] Implement output tags with batch ID, output ID, output type and optional region ID. Cleanup deletes only matching tagged elements; legacy `ResultTagService` filled regions remain recognizable through an explicit compatibility cleanup path.
- [ ] Build Revit 2020 and run validator; commit `feat: store clear height batches in Revit`.

### Task 5: Lightweight default drawing

**Files:** create `LightweightDrawingExporter.cs` and tag/cleanup helpers.

- [ ] Add validator RED checks that the exporter uses `DetailCurve`/`TextNote`, never `FilledRegion`, and tags every created element.
- [ ] Preflight requires a detail-capable plan view. Create/reuse status line subcategories under Lines, create one curve per simplified region edge and one TextNote per region showing number and minimum height/blocked/unknown state.
- [ ] Regeneration first removes only the prior output ID inside one transaction group; failure rolls back and leaves batch data intact. Record created IDs in a `DerivedOutputRecord`.
- [ ] Build and validate; commit `feat: draw lightweight clear height results`.

### Task 6: Optional area color plan

**Files:** create `AreaColorPlanExporter.cs` and preflight result model.

- [ ] Add validator RED checks for exact asset names, merged/per-cell mode, preflight counts, explicit missing-template warning and absence of area-scheme creation/modification.
- [ ] Find the existing `PH_净高分析` area scheme and selected level; fail without changing the project when absent. Create a dedicated area plan, apply `PH_净高分析_色块` when present, otherwise return a visible warning.
- [ ] Create deduplicated boundary lines and Areas using internal points; stable `Name` values drive the template color rules. Tag the view, boundary lines and areas. Use a transaction group and roll back the whole area output on failure.
- [ ] Build and validate; commit `feat: add optional area color plan`.

### Task 7: PNG and CSV Revit outputs

**Files:** create `RasterHeatmapExporter.cs`, `CsvExporter.cs` and update output metadata.

- [ ] Add validator RED checks for exactly one `ImageType` and `ImageInstance` per raster output and CSV use of complete batch results.
- [ ] Generate PNG bytes before transactions, write to a user-selected path, then create one image type/instance in a supported plan view and tag both where supported. Store file path and placement/reload metadata; missing files can be regenerated from the batch.
- [ ] Export CSV atomically through a temporary file and replacement, without opening a Revit transaction.
- [ ] Revit 2020 build and validator; commit `feat: export clear height PNG and CSV`.

### Task 8: V2 result manager and workflow controller

**Files:** create `V2ResultWindow.xaml(.cs)`, `AnalysisWorkflowController.cs`; modify `V2AnalysisWindow` and `ClearHeightAnalysisCommand`.

- [ ] Before UI work, use the `ui-ux-pro-max` skill and keep keyboard-accessible labels, non-color-only status text and explicit warnings.
- [ ] The result window shows stored batches and supports status/confidence/model/category filters, region/member inspection, output generate/regenerate/delete actions, CSV export and approximation warnings.
- [ ] The controller builds real level/link choices, opens `V2AnalysisWindow`, runs `RevitAnalysisRunner`, creates and saves `AnalysisBatch` before any output, then opens the V2 result manager. Cancellation before save writes nothing.
- [ ] Switch `ClearHeightAnalysisCommand` to only instantiate the controller. Add validator checks proving the command contains no legacy calculator, limiter or heatmap renderer references.
- [ ] Build and validate; commit `feat: switch clear height analysis to V2 workflow`.

### Task 9: Legacy cleanup, documentation and real-machine package

**Files:** remove unused legacy model/service/UI files; modify project docs, package descriptor and validator; create `docs/testing/clear-height-v2-revit-2020-checklist.md`.

- [ ] Use `rg` to prove no production references remain to old window/calculator/grid/renderer/limiter. Remove them while retaining only the legacy tagged-filled-region cleanup reader.
- [ ] Update package text to describe data-driven batches, lightweight default output and optional area/PNG/CSV; remove validator requirements for `FilledRegion` rendering and silent 500-cell limits.
- [ ] Document Revit 2020 checks: host/link rotation/mirror, concave/hole boundaries, pipe filter, blocked columns, batch reopen/corruption behavior, default drawing print, area asset missing/present, PNG reload, CSV completeness and scoped cleanup.
- [ ] Run full tests, Revit 2020 Release/NuGet build and `pwsh tests/Validate-Package.ps1`; commit `refactor: retire legacy clear height pipeline`.

### Task 10: Full module review and branch integration

- [ ] Use `requesting-code-review`; review Core geometry, serialization, schema/versioning, Revit API validity, transactions, ownership cleanup, UI warnings and large-model performance. Fix findings through RED/GREEN tests where behavior changes.
- [ ] Use `verification-before-completion`; rerun all  tests, Release/NuGet Revit 2020 build, validator, `git diff --check`, forbidden-reference searches and worktree status.
- [ ] Use `finishing-a-development-branch`. Merge `clear-height-v2-core` into local `codex` without touching protected `main`, rerun the same verification on merged `codex`, then push `codex` only.

## Plan self-review

- Every V2 design requirement from batch persistence through production switch maps to Tasks 1–9.
- Default output contains no fill; area output remains explicit and template-dependent.
- Data deletion and derived-output deletion are separate operations.
- All pure behavior follows RED/GREEN/refactor. Revit-only behavior is compile-checked and statically validated, with an explicit Revit 2020 runtime checklist for the remaining environment boundary.
- No plan step writes package DLLs to `dist`; CI remains responsible for packaging outputs.
