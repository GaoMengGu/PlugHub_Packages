#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using PlugHub.ClearHeightAnalysis.Revit;

namespace PlugHub.ClearHeightAnalysis.UI
{
    public partial class V2ResultWindow : Window
    {
        private readonly UIDocument _uiDocument;
        private readonly AnalysisBatchRepository _repository;
        private AnalysisBatch _batch;
        private bool _loading;

        public V2ResultWindow(UIDocument uiDocument, AnalysisBatchRepository repository, AnalysisBatch batch)
        {
            InitializeComponent();
            _uiDocument=uiDocument??throw new ArgumentNullException(nameof(uiDocument));
            _repository=repository??throw new ArgumentNullException(nameof(repository));
            _batch=batch??throw new ArgumentNullException(nameof(batch));
            StatusFilter.ItemsSource=FilterOption<CellStatus>.Create(Enum.GetValues(typeof(CellStatus)).Cast<CellStatus>());
            ConfidenceFilter.ItemsSource=FilterOption<GeometryConfidence>.Create(Enum.GetValues(typeof(GeometryConfidence)).Cast<GeometryConfidence>());
            StatusFilter.SelectedIndex=0; ConfidenceFilter.SelectedIndex=0;
            RefreshAll();
        }

        private Document Document => _uiDocument.Document;

        private void RefreshAll()
        {
            _loading=true;
            try
            {
                SummaryText.Text=$"批次 {_batch.BatchId.Substring(0,8)} · {_batch.CreatedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm} · {_batch.RunData.Cells.Count} 个网格 · {_batch.RunData.Summary.Results.Count(r=>r.Status!=CellStatus.Passed)} 个问题网格";
                int approximate=_batch.RunData.Summary.Results.Count(result=>result.Confidence==GeometryConfidence.CategoryApproximation||result.Confidence==GeometryConfidence.BoundingBoxFallback);
                ApproximationBanner.Visibility=approximate>0?System.Windows.Visibility.Visible:System.Windows.Visibility.Collapsed;
                ApproximationText.Text=$"近似结果提醒：{approximate} 个网格由类别近似或包围盒回退控制，请在实机模型中复核控制构件。";
                IReadOnlyList<RiskRegion> regions=RiskRegionBuilder.Build(_batch.RunData.Summary.Results);
                RegionList.ItemsSource=new[]{new RegionRow(null,"全部区域")}.Concat(regions.Select(region=>new RegionRow(region,$"{region.Number}  {StatusText(region.Status)}  {region.MemberCells.Count}格"))).ToList();
                RegionList.SelectedIndex=0;
                List<string> sources=_batch.RunData.Obstacles.Select(item=>item.SourceModelName).Distinct().OrderBy(x=>x).ToList(); sources.Insert(0,"全部");
                List<string> categories=_batch.RunData.Obstacles.Select(item=>item.CategoryName).Distinct().OrderBy(x=>x).ToList(); categories.Insert(0,"全部");
                SourceFilter.ItemsSource=sources; CategoryFilter.ItemsSource=categories; SourceFilter.SelectedIndex=0; CategoryFilter.SelectedIndex=0;
                OutputComboBox.ItemsSource=_batch.Outputs.Select(OutputRow.From).ToList(); OutputComboBox.SelectedIndex=_batch.Outputs.Count>0?0:-1;
                IReadOnlyList<StoredBatchInfo> batches=_repository.List(Document); BatchComboBox.ItemsSource=batches;
                BatchComboBox.SelectedItem=batches.FirstOrDefault(item=>item.BatchId==_batch.BatchId);
            }
            finally { _loading=false; }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if(_loading)return;
            var criteria=new ResultFilterCriteria
            {
                Status=(StatusFilter.SelectedItem as FilterOption<CellStatus>)?.Value,
                Confidence=(ConfidenceFilter.SelectedItem as FilterOption<GeometryConfidence>)?.Value,
                SourceModelName=SourceFilter.SelectedIndex>0?SourceFilter.SelectedItem as string:null,
                CategoryName=CategoryFilter.SelectedIndex>0?CategoryFilter.SelectedItem as string:null,
                MemberCellNumbers=(RegionList.SelectedItem as RegionRow)?.Region?.MemberCells.Select(cell=>cell.Number).ToList()
            };
            ResultsGrid.ItemsSource=ResultFilter.Apply(_batch.RunData.Summary.Results,_batch.RunData.Obstacles,criteria).Select(result=>ResultRow.From(result,_batch.RunData.Obstacles)).ToList();
        }

        private void Filter_Changed(object sender,SelectionChangedEventArgs e)=>ApplyFilters();
        private void RegionList_SelectionChanged(object sender,SelectionChangedEventArgs e)=>ApplyFilters();

        private void BatchComboBox_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            if(_loading||!(BatchComboBox.SelectedItem is StoredBatchInfo info)||info.BatchId==_batch.BatchId)return;
            if(info.Status!=StoredBatchStatus.Available){MessageBox.Show(info.StatusMessage,"批次不可用",MessageBoxButton.OK,MessageBoxImage.Warning);return;}
            try{_batch=_repository.Load(Document,info.BatchId);RefreshAll();}catch(Exception ex){MessageBox.Show(ex.Message,"读取批次失败",MessageBoxButton.OK,MessageBoxImage.Error);}
        }

        private void Lightweight_Click(object sender,RoutedEventArgs e)=>CreateOutput(()=>new LightweightDrawingExporter().Export(Document,_uiDocument.ActiveView,_batch));
        private void MergedArea_Click(object sender,RoutedEventArgs e)=>CreateArea(AreaOutputMode.MergedRegions);
        private void CellArea_Click(object sender,RoutedEventArgs e)=>CreateArea(AreaOutputMode.PerCell);

        private void CreateArea(AreaOutputMode mode)
        {
            try
            {
                var exporter=new AreaColorPlanExporter(); AreaOutputPreflight preflight=exporter.Preflight(Document,_batch,mode);
                if(!preflight.SchemeAvailable){MessageBox.Show(preflight.Warning,"无法生成面积成果",MessageBoxButton.OK,MessageBoxImage.Warning);return;}
                string detail=$"预计创建 {preflight.Plan.EstimatedAreaCount} 个面积、{preflight.Plan.EstimatedBoundaryLineCount} 条边界线。";
                if(!string.IsNullOrWhiteSpace(preflight.Warning))detail+="\n\n"+preflight.Warning;
                if(preflight.ExceedsRecommendedSize)detail+="\n\n数量较大，建议改用合并模式或 PNG 总览。";
                if(MessageBox.Show(detail+"\n\n继续生成？","面积成果预检",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
                CreateOutput(()=>exporter.Export(Document,_batch,mode));
            }catch(Exception ex){MessageBox.Show(ex.Message,"面积成果生成失败",MessageBoxButton.OK,MessageBoxImage.Error);}
        }

        private void Png_Click(object sender,RoutedEventArgs e)
        {
            var dialog=new SaveFileDialog{Title="保存净高分析 PNG",Filter="PNG 图像|*.png",FileName="净高分析_"+_batch.BatchId.Substring(0,8)+".png"};
            if(dialog.ShowDialog(this)==true)CreateOutput(()=>new RasterHeatmapExporter().Export(Document,_uiDocument.ActiveView,_batch,dialog.FileName,false));
        }
        private void Csv_Click(object sender,RoutedEventArgs e)
        {
            var dialog=new SaveFileDialog{Title="导出净高分析 CSV",Filter="CSV 文件|*.csv",FileName="净高分析_"+_batch.BatchId.Substring(0,8)+".csv"};
            if(dialog.ShowDialog(this)==true)CreateOutput(()=>new CsvExporter().Export(_batch,dialog.FileName));
        }

        private void CreateOutput(Func<DerivedOutputRecord> create)
        {
            try
            {
                DerivedOutputRecord created=create();
                List<DerivedOutputRecord> old=_batch.Outputs.Where(item=>item.OutputType==created.OutputType).ToList();
                foreach(DerivedOutputRecord item in old) if(item.ElementIds.Count>0)new DerivedOutputCleanupService().Delete(Document,_batch.BatchId,item.OutputId);
                var outputs=_batch.Outputs.Except(old).Concat(new[]{created}).ToList();
                _batch=_batch.WithOutputs(outputs); _repository.Save(Document,_batch); RefreshAll();
            }
            catch(Exception ex){MessageBox.Show(ex.Message,"成果生成失败",MessageBoxButton.OK,MessageBoxImage.Error);}
        }

        private void DeleteOutput_Click(object sender,RoutedEventArgs e)
        {
            if(!(OutputComboBox.SelectedItem is OutputRow row))return;
            if(MessageBox.Show("只删除所选派生成果，分析批次数据会保留。","删除成果",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
            try
            {
                if(row.Record.ElementIds.Count>0)new DerivedOutputCleanupService().Delete(Document,_batch.BatchId,row.Record.OutputId);
                _batch=_batch.WithOutputs(_batch.Outputs.Where(item=>item.OutputId!=row.Record.OutputId)); _repository.Save(Document,_batch); RefreshAll();
            }catch(Exception ex){MessageBox.Show(ex.Message,"删除成果失败",MessageBoxButton.OK,MessageBoxImage.Error);}
        }

        private void DeleteBatch_Click(object sender,RoutedEventArgs e)
        {
            if(MessageBox.Show("将删除当前分析批次数据及其全部 Revit 派生成果。外部 CSV/PNG 文件不会自动删除。","删除分析批次",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
            try{new DerivedOutputCleanupService().Delete(Document,_batch.BatchId);_repository.DeleteBatchData(Document,_batch.BatchId);DialogResult=true;Close();}
            catch(Exception ex){MessageBox.Show(ex.Message,"删除批次失败",MessageBoxButton.OK,MessageBoxImage.Error);}
        }
        private void Close_Click(object sender,RoutedEventArgs e)=>Close();

        private static string StatusText(CellStatus status)=>LightweightDrawingExporter.StatusName(status);
        private sealed class FilterOption<T> where T:struct
        {
            public FilterOption(string name,T? value){Name=name;Value=value;} public string Name{get;} public T? Value{get;} public override string ToString()=>Name;
            public static IReadOnlyList<FilterOption<T>> Create(IEnumerable<T> values)=>new[]{new FilterOption<T>("全部",null)}.Concat(values.Select(value=>new FilterOption<T>(value.ToString(),value))).ToList();
        }
        private sealed class RegionRow { public RegionRow(RiskRegion? region,string name){Region=region;DisplayName=name;} public RiskRegion? Region{get;} public string DisplayName{get;} }
        private sealed class OutputRow
        {
            public OutputRow(DerivedOutputRecord record,string display){Record=record;DisplayName=display;} public DerivedOutputRecord Record{get;} public string DisplayName{get;}
            public static OutputRow From(DerivedOutputRecord record)=>new OutputRow(record,$"{record.OutputType}  {record.CreatedAtUtc.ToLocalTime():MM-dd HH:mm}");
        }
        private sealed class ResultRow
        {
            public string CellNumber{get;private set;}=string.Empty; public string Status{get;private set;}=string.Empty; public string Height{get;private set;}=string.Empty;
            public string Difference{get;private set;}=string.Empty; public string Confidence{get;private set;}=string.Empty; public string Obstacle{get;private set;}=string.Empty;
            public string Category{get;private set;}=string.Empty; public string Source{get;private set;}=string.Empty; public string ObstacleKey{get;private set;}=string.Empty;
            public static ResultRow From(CellAnalysisResult result,IEnumerable<ObstacleSnapshot> obstacles)
            {
                ObstacleSnapshot? obstacle=obstacles.FirstOrDefault(item=>item.Key==result.ControllingObstacleKey);
                return new ResultRow{CellNumber=result.Cell.Number,Status=StatusText(result.Status),Height=Format(result.ClearHeightMillimeters),Difference=Format(result.DifferenceMillimeters),
                    Confidence=result.Confidence?.ToString()??string.Empty,Obstacle=obstacle?.DisplayName??string.Empty,Category=obstacle?.CategoryName??string.Empty,
                    Source=obstacle?.SourceModelName??string.Empty,ObstacleKey=obstacle?.Key??string.Empty};
            }
            private static string Format(double? value)=>value?.ToString("0",CultureInfo.InvariantCulture)??string.Empty;
        }
    }
}
