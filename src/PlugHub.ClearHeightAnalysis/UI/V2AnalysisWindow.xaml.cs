#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.UI
{
    public partial class V2AnalysisWindow : Window
    {
        public V2AnalysisWindow(
            IReadOnlyList<AnalysisLevelChoice> levels,
            IReadOnlyList<SourceModelChoice> sourceModels,
            bool hasStoredBatches = false)
        {
            InitializeComponent();
            Request = new AnalysisRequest(levels, sourceModels);
            LevelComboBox.ItemsSource = levels;
            LevelComboBox.SelectedIndex = levels.Count > 0 ? 0 : -1;
            SourceModelsItems.ItemsSource = sourceModels;
            BoundaryModeComboBox.ItemsSource = new[]
            {
                new BoundaryModeItem("选择当前或链接楼板", AnalysisBoundaryMode.SelectFloor),
                new BoundaryModeItem("自动识别当前楼层楼板", AnalysisBoundaryMode.AutomaticHostFloors),
                new BoundaryModeItem("当前视图裁剪矩形", AnalysisBoundaryMode.ActiveViewCrop),
                new BoundaryModeItem("手动矩形范围", AnalysisBoundaryMode.ManualRectangle)
            };
            BoundaryModeComboBox.SelectedIndex = 0;
            HistoryButton.IsEnabled = hasStoredBatches;
        }

        public AnalysisRequest Request { get; }
        public bool OpenHistoryRequested { get; private set; }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHistoryRequested = true;
            DialogResult = true;
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            Request.SelectedLevel = LevelComboBox.SelectedItem as AnalysisLevelChoice;
            Request.BoundaryMode = ((BoundaryModeItem)BoundaryModeComboBox.SelectedItem).Mode;
            if (!TryReadNumber(GridSizeTextBox.Text, out double grid) ||
                !TryReadNumber(ThresholdTextBox.Text, out double threshold) ||
                !TryReadNumber(FinishOffsetTextBox.Text, out double finishOffset) ||
                !TryReadNumber(SearchHeightTextBox.Text, out double searchHeight) ||
                !TryReadNumber(MinimumPipeTextBox.Text, out double minimumPipe))
            {
                ValidationText.Text = "所有计算参数必须为有效数字。";
                return;
            }

            Request.GridSizeMillimeters = grid;
            Request.ClearHeightThresholdMillimeters = threshold;
            Request.FinishFloorOffsetMillimeters = finishOffset;
            Request.SearchHeightMillimeters = searchHeight;
            Request.MinimumPipeDiameterMillimeters = minimumPipe;
            Request.IncludeCeilings = IncludeCeilingsCheckBox.IsChecked == true;
            Request.IncludeMep = IncludeMepCheckBox.IsChecked == true;

            IReadOnlyList<string> errors = AnalysisRequestValidator.Validate(Request);
            if (errors.Count > 0)
            {
                ValidationText.Text = string.Join(Environment.NewLine, errors);
                return;
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private static bool TryReadNumber(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                   double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private sealed class BoundaryModeItem
        {
            public BoundaryModeItem(string name, AnalysisBoundaryMode mode)
            {
                Name = name;
                Mode = mode;
            }

            public string Name { get; }
            public AnalysisBoundaryMode Mode { get; }
            public override string ToString() => Name;
        }
    }
}
