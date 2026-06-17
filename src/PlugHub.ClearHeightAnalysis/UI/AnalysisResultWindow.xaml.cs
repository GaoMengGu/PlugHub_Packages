using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.UI
{
    public partial class AnalysisResultWindow : Window
    {
        public AnalysisResultWindow(IEnumerable<ClearHeightResult> results)
        {
            InitializeComponent();
            List<ResultRow> rows = results
                .Where(result => result.RiskLevel != RiskLevel.Passed)
                .Select(ResultRow.FromResult)
                .ToList();
            ResultsGrid.ItemsSource = rows;
            SummaryText.Text = "共列出 " + rows.Count.ToString(CultureInfo.InvariantCulture) + " 个未达标、临界或无法判断网格。";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class ResultRow
        {
            public string LevelName { get; private set; }
            public string GridNumber { get; private set; }
            public string CenterX { get; private set; }
            public string CenterY { get; private set; }
            public string ClearHeight { get; private set; }
            public string Threshold { get; private set; }
            public string Difference { get; private set; }
            public string RiskLevel { get; private set; }
            public string ControllingElement { get; private set; }
            public string Category { get; private set; }
            public string SourceModel { get; private set; }
            public string ElementKey { get; private set; }
            public bool IsFromLink { get; private set; }

            public static ResultRow FromResult(ClearHeightResult result)
            {
                return new ResultRow
                {
                    LevelName = result.Cell.LevelName,
                    GridNumber = result.Cell.Number,
                    CenterX = result.Cell.Bounds.CenterX.ToString("0", CultureInfo.InvariantCulture),
                    CenterY = result.Cell.Bounds.CenterY.ToString("0", CultureInfo.InvariantCulture),
                    ClearHeight = result.ClearHeightMillimeters.HasValue ? result.ClearHeightMillimeters.Value.ToString("0", CultureInfo.InvariantCulture) : "未判断",
                    Threshold = result.ThresholdMillimeters.ToString("0", CultureInfo.InvariantCulture),
                    Difference = result.DifferenceMillimeters.HasValue ? result.DifferenceMillimeters.Value.ToString("0", CultureInfo.InvariantCulture) : string.Empty,
                    RiskLevel = result.RiskLevel.ToString(),
                    ControllingElement = result.ControllingObstacle?.ElementName ?? string.Empty,
                    Category = result.ControllingObstacle?.CategoryName ?? string.Empty,
                    SourceModel = result.ControllingObstacle?.SourceModelName ?? string.Empty,
                    ElementKey = result.ControllingObstacle?.ElementKey ?? string.Empty,
                    IsFromLink = result.ControllingObstacle?.IsFromLink ?? false
                };
            }
        }
    }
}
