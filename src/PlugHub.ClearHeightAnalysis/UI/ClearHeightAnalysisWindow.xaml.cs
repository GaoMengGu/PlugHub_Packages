using System;
using System.Windows;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.UI
{
    public partial class ClearHeightAnalysisWindow : Window
    {
        public ClearHeightAnalysisWindow()
        {
            InitializeComponent();
        }

        public AnalysisSettings Settings { get; private set; } = new AnalysisSettings();

        public bool TryCreateSettings(out AnalysisSettings settings, out string validationMessage)
        {
            settings = new AnalysisSettings();
            validationMessage = string.Empty;

            if (!double.TryParse(LevelElevationTextBox.Text, out double levelElevation))
            {
                validationMessage = "楼层标高需为数字。";
                return false;
            }

            if (!double.TryParse(GridSizeTextBox.Text, out double gridSize) || gridSize < 100 || gridSize > 5000)
            {
                validationMessage = "网格尺寸需为 100-5000mm。";
                return false;
            }

            if (!double.TryParse(ClearHeightThresholdTextBox.Text, out double threshold) || threshold < 1000 || threshold > 10000)
            {
                validationMessage = "净高阈值需为 1000-10000mm。";
                return false;
            }

            if (!double.TryParse(FinishFloorOffsetTextBox.Text, out double finishOffset) || finishOffset < 0 || finishOffset > 1000)
            {
                validationMessage = "完成面偏移需为 0-1000mm。";
                return false;
            }

            if (!double.TryParse(SearchHeightTextBox.Text, out double searchHeight) || searchHeight < 1000 || searchHeight > 20000)
            {
                validationMessage = "搜索高度需为 1000-20000mm。";
                return false;
            }

            if (!double.TryParse(MinimumPipeDiameterTextBox.Text, out double minimumPipeDiameter) || minimumPipeDiameter < 0 || minimumPipeDiameter > 2000)
            {
                validationMessage = "最小管径需为 0-2000mm。";
                return false;
            }

            settings.LevelName = string.IsNullOrWhiteSpace(LevelNameTextBox.Text) ? "当前楼层" : LevelNameTextBox.Text.Trim();
            settings.LevelElevationMillimeters = levelElevation;
            settings.GridSizeMillimeters = gridSize;
            settings.ClearHeightThresholdMillimeters = threshold;
            settings.FinishFloorOffsetMillimeters = finishOffset;
            settings.SearchHeightMillimeters = searchHeight;
            settings.MinimumPipeDiameterMillimeters = minimumPipeDiameter;
            settings.UseActiveViewCropAsFallback = UseCropFallbackCheckBox.IsChecked == true;
            settings.IncludeCurrentModel = IncludeCurrentModelCheckBox.IsChecked == true;
            settings.IncludeLinkedModels = IncludeLinkedModelsCheckBox.IsChecked == true;
            settings.IncludeCeilings = IncludeCeilingsCheckBox.IsChecked == true;
            settings.IncludeMep = IncludeMepCheckBox.IsChecked == true;
            settings.RenderOnlyProblemCells = RenderOnlyProblemCellsCheckBox.IsChecked == true;

            if (!settings.IncludeCurrentModel && !settings.IncludeLinkedModels)
            {
                validationMessage = "请至少选择当前模型或链接模型。";
                return false;
            }

            return true;
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryCreateSettings(out AnalysisSettings settings, out string validationMessage))
            {
                ValidationText.Text = validationMessage;
                return;
            }

            Settings = settings;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
