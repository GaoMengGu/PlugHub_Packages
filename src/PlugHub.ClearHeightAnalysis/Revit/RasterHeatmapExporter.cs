using System;
using System.IO;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class RasterHeatmapExporter
    {
        public void Validate(View view)
        {
            if(view==null||view.IsTemplate)throw new InvalidOperationException("请选择平面视图放置净高 PNG。");
            ResultViewPolicy.EnsureSupported(view.ViewType.ToString());
        }
        public DerivedOutputRecord Export(Document document, View view, AnalysisBatch batch, string pngPath, bool includePassed)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            Validate(view);
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (string.IsNullOrWhiteSpace(pngPath)) throw new ArgumentException("PNG 路径不能为空。", nameof(pngPath));
            string fullPath=Path.GetFullPath(pngPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            RasterHeatmap raster=RasterHeatmapRenderer.Render(batch.RunData.Summary.Results,includePassed);
            File.WriteAllBytes(fullPath,raster.ToPngBytes());
            using(var transaction=new Transaction(document,"导入净高分析 PNG 热力图"))
            {
                transaction.Start();
                DerivedOutputRecord result=Build(document,view,batch,fullPath,fullPath,raster,Guid.NewGuid().ToString("N"));
                transaction.Commit();
                return result;
            }
        }

        public DerivedOutputRecord Build(Document document,View view,AnalysisBatch batch,string imageSourcePath,
            string registeredPath,RasterHeatmap raster,string outputId)
        {
            if(document==null)throw new ArgumentNullException(nameof(document));
            Validate(view);
            if(batch==null)throw new ArgumentNullException(nameof(batch));
            if(raster==null)throw new ArgumentNullException(nameof(raster));
            if(string.IsNullOrWhiteSpace(imageSourcePath)||string.IsNullOrWhiteSpace(registeredPath))throw new ArgumentException("PNG 路径不能为空。");
            if(string.IsNullOrWhiteSpace(outputId))throw new ArgumentException("成果标识不能为空。",nameof(outputId));
            ImageType imageType;
            ImageInstance instance;
                using(var options=new ImageTypeOptions(imageSourcePath,false)) imageType=ImageType.Create(document,options);
                var location=new XYZ(Services.UnitConversion.MillimetersToFeet((raster.MinX+raster.MaxX)/2),
                    Services.UnitConversion.MillimetersToFeet((raster.MinY+raster.MaxY)/2),view.GenLevel!=null?view.GenLevel.Elevation:view.Origin.Z);
                using(var placement=new ImagePlacementOptions(location,BoxPlacement.Center))
                    instance=ImageInstance.Create(document,view,imageType.Id,placement);
                instance.LockProportions=false;
                double physicalWidth=Services.UnitConversion.MillimetersToFeet(raster.MaxX-raster.MinX);
                instance.Width=physicalWidth;
                instance.Height=physicalWidth*raster.Height/raster.Width;
                instance.DrawLayer=DrawLayer.Background;
                OutputTagService.Tag(imageType,batch.BatchId,outputId,DerivedOutputType.RasterHeatmap);
                OutputTagService.Tag(instance,batch.BatchId,outputId,DerivedOutputType.RasterHeatmap);
            return new DerivedOutputRecord(outputId,DerivedOutputType.RasterHeatmap,DateTime.UtcNow,view.Id.IntegerValue,registeredPath,
                new[]{imageType.Id.IntegerValue,instance.Id.IntegerValue});
        }
    }
}
