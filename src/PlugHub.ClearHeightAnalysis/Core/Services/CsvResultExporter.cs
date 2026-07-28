#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class CsvResultExporter
    {
        public static byte[] Export(AnalysisBatch batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            var text = new StringBuilder();
            text.Append("批次ID,分析时间UTC,项目,楼层,网格编号,列,行,中心X_mm,中心Y_mm,净高_mm,阈值_mm,差值_mm,状态,可信度,控制构件Key,控制构件名称,构件类别,来源模型,链接实例Key\n");
            IReadOnlyDictionary<string, ObstacleSnapshot> obstacles = batch.RunData.Obstacles.ToDictionary(item => item.Key, StringComparer.Ordinal);
            foreach (CellAnalysisResult result in batch.RunData.Summary.Results)
            {
                ObstacleSnapshot? obstacle = null;
                if (!string.IsNullOrWhiteSpace(result.ControllingObstacleKey)) obstacles.TryGetValue(result.ControllingObstacleKey!, out obstacle);
                GridCellData cell = result.Cell;
                AppendRow(text, batch.BatchId, batch.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture), batch.DocumentTitle,
                    cell.LevelName, cell.Number, cell.Column.ToString(CultureInfo.InvariantCulture), cell.Row.ToString(CultureInfo.InvariantCulture),
                    cell.CenterX.ToString("0.###", CultureInfo.InvariantCulture), cell.CenterY.ToString("0.###", CultureInfo.InvariantCulture),
                    Number(result.ClearHeightMillimeters), result.ThresholdMillimeters.ToString("0.###", CultureInfo.InvariantCulture),
                    Number(result.DifferenceMillimeters), result.Status.ToString(), result.Confidence?.ToString() ?? string.Empty,
                    obstacle?.Key ?? string.Empty, obstacle?.DisplayName ?? string.Empty, obstacle?.CategoryName ?? string.Empty,
                    obstacle?.SourceModelName ?? string.Empty, obstacle?.LinkInstanceKey ?? string.Empty);
            }
            byte[] content = new UTF8Encoding(false).GetBytes(text.ToString().Replace("\n", "\r\n"));
            byte[] preamble = new UTF8Encoding(true).GetPreamble();
            return preamble.Concat(content).ToArray();
        }

        private static string Number(double? value) => value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty;
        private static void AppendRow(StringBuilder builder, params string[] fields)
        {
            builder.Append(string.Join(",", fields.Select(Escape))).Append('\n');
        }
        private static string Escape(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
