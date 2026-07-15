using System;
using System.IO;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class RasterHeatmapExporter
    {
        public DerivedOutputRecord Export(Document document, View view, AnalysisBatch batch, string pngPath, bool includePassed)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (view == null || view.IsTemplate) throw new InvalidOperationException("请选择可放置图像的二维视图。");
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (string.IsNullOrWhiteSpace(pngPath)) throw new ArgumentException("PNG 路径不能为空。", nameof(pngPath));
            RasterHeatmap raster=RasterHeatmapRenderer.Render(batch.RunData.Summary.Results,includePassed);
            string fullPath=Path.GetFullPath(pngPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath,raster.ToPngBytes());
            string outputId=Guid.NewGuid().ToString("N");
            ImageType imageType;
            ImageInstance instance;
            using(var transaction=new Transaction(document,"导入净高分析 PNG 热力图"))
            {
                transaction.Start();
                using(var options=new ImageTypeOptions(fullPath,false)) imageType=ImageType.Create(document,options);
                var location=new XYZ(Services.UnitConversion.MillimetersToFeet((raster.MinX+raster.MaxX)/2),
                    Services.UnitConversion.MillimetersToFeet((raster.MinY+raster.MaxY)/2),view.GenLevel!=null?view.GenLevel.Elevation:view.Origin.Z);
                using(var placement=new ImagePlacementOptions(location,BoxPlacement.Center))
                    instance=ImageInstance.Create(document,view,imageType.Id,placement);
                instance.LockProportions=false;
                instance.Width=Services.UnitConversion.MillimetersToFeet(raster.MaxX-raster.MinX);
                instance.Height=Services.UnitConversion.MillimetersToFeet(raster.MaxY-raster.MinY);
                instance.DrawLayer=DrawLayer.Background;
                OutputTagService.Tag(imageType,batch.BatchId,outputId,DerivedOutputType.RasterHeatmap);
                OutputTagService.Tag(instance,batch.BatchId,outputId,DerivedOutputType.RasterHeatmap);
                transaction.Commit();
            }
            return new DerivedOutputRecord(outputId,DerivedOutputType.RasterHeatmap,DateTime.UtcNow,view.Id.IntegerValue,fullPath,
                new[]{imageType.Id.IntegerValue,instance.Id.IntegerValue});
        }
    }
}
