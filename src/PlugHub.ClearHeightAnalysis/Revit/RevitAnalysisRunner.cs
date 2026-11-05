using System;
using System.Collections.Generic;
using System.Threading;
using Autodesk.Revit.UI;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using PlugHub.ClearHeightAnalysis.Core.Persistence;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class RevitAnalysisRunner
    {
        public RevitAnalysisPreparation Prepare(UIDocument uiDocument, AnalysisRequest request)
        {
            if (uiDocument == null) throw new ArgumentNullException(nameof(uiDocument));
            RevitAnalysisContext context = RevitAnalysisContextBuilder.Build(uiDocument.Document, request);
            var settings = new CoreAnalysisSettings(
                Services.UnitConversion.FeetToMillimeters(context.Level.Elevation),
                request.FinishFloorOffsetMillimeters,
                request.SearchHeightMillimeters,
                request.ClearHeightThresholdMillimeters);
            AnalysisBoundary boundary = new RevitBoundaryProvider().GetBoundary(uiDocument, context);
            AnalysisScaleEstimate scale = AnalysisScalePolicy.Estimate(boundary, request.GridSizeMillimeters);
            return new RevitAnalysisPreparation(request,boundary,settings,context,scale);
        }

        public RevitAnalysisSnapshot Capture(RevitAnalysisPreparation preparation)
        {
            if(preparation==null)throw new ArgumentNullException(nameof(preparation));
            IReadOnlyList<ObstacleSnapshot> obstacles=new RevitObstacleSnapshotCollector().Collect(
                preparation.Context,preparation.Boundary,preparation.Settings);
            return new RevitAnalysisSnapshot(preparation.Request,preparation.Boundary,obstacles,preparation.Settings,
                preparation.Context.Level.UniqueId,preparation.Context.Level.Name,preparation.Scale);
        }

        public AnalysisRunData Compute(RevitAnalysisSnapshot snapshot,
            CancellationToken cancellationToken, IProgress<AnalysisProgress> progress)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            AnalysisRequest request = snapshot.Request;
            AnalysisBoundary boundary = snapshot.Boundary;
            IReadOnlyList<GridCellData> cells = GridDomainBuilder.Build(
                boundary,
                request.GridSizeMillimeters,
                snapshot.LevelUniqueId,
                snapshot.LevelName,
                snapshot.Settings.LevelElevationMillimeters,
                cancellationToken, progress);
            if (cells.Count == 0)
                throw new InvalidOperationException("分析范围内没有生成有效网格。");

            double originX = cells[0].MinX - cells[0].Column * request.GridSizeMillimeters;
            double originY = cells[0].MinY - cells[0].Row * request.GridSizeMillimeters;
            CoreAnalysisSummary summary = new ClearHeightBatchAnalyzer(
                originX, originY, request.GridSizeMillimeters, snapshot.Obstacles, cancellationToken, progress).Analyze(
                    cells, snapshot.Settings, cancellationToken, progress);
            return new AnalysisRunData(request, boundary, cells, snapshot.Obstacles, snapshot.Settings, summary);
        }
    }

    public sealed class RevitAnalysisPreparation
    {
        public RevitAnalysisPreparation(AnalysisRequest request,AnalysisBoundary boundary,CoreAnalysisSettings settings,
            RevitAnalysisContext context,AnalysisScaleEstimate scale)
        {Request=request;Boundary=boundary;Settings=settings;Context=context;Scale=scale;}
        public AnalysisRequest Request{get;}public AnalysisBoundary Boundary{get;}public CoreAnalysisSettings Settings{get;}
        public RevitAnalysisContext Context{get;}public AnalysisScaleEstimate Scale{get;}
    }

    public sealed class PreparedAnalysisBatch
    {
        public PreparedAnalysisBatch(AnalysisBatch batch,AnalysisBatchEnvelope envelope,IReadOnlyList<RiskRegion> riskRegions)
        {Batch=batch??throw new ArgumentNullException(nameof(batch));Envelope=envelope??throw new ArgumentNullException(nameof(envelope));RiskRegions=riskRegions??throw new ArgumentNullException(nameof(riskRegions));}
        public AnalysisBatch Batch{get;}public AnalysisBatchEnvelope Envelope{get;}public IReadOnlyList<RiskRegion> RiskRegions{get;}
    }

    public sealed class RevitAnalysisSnapshot
    {
        public RevitAnalysisSnapshot(AnalysisRequest request,AnalysisBoundary boundary,IReadOnlyList<ObstacleSnapshot> obstacles,
            CoreAnalysisSettings settings,string levelUniqueId,string levelName,AnalysisScaleEstimate scale)
        {Request=request;Boundary=boundary;Obstacles=obstacles;Settings=settings;LevelUniqueId=levelUniqueId;LevelName=levelName;Scale=scale;}
        public AnalysisRequest Request{get;} public AnalysisBoundary Boundary{get;} public IReadOnlyList<ObstacleSnapshot> Obstacles{get;}
        public CoreAnalysisSettings Settings{get;} public string LevelUniqueId{get;} public string LevelName{get;} public AnalysisScaleEstimate Scale{get;}
    }
}
