using System;
using System.Windows;

namespace PlugHub.ProjectAutoSave
{
    public partial class AutoSaveSettingsWindow : Window
    {
        public AutoSaveSettings Settings { get; private set; }

        public AutoSaveSettingsWindow(AutoSaveSettings settings)
        {
            InitializeComponent();
            Settings = AutoSaveSettings.Normalize(settings ?? new AutoSaveSettings());
            EnableAutoSaveCheckBox.IsChecked = Settings.IsEnabled;
            IntervalTextBox.Text = Settings.IntervalMinutes.ToString();
            ShowNotificationCheckBox.IsChecked = Settings.ShowNotification;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(IntervalTextBox.Text, out int intervalMinutes))
            {
                ValidationText.Text = "请输入有效的分钟间隔。";
                return;
            }

            if (intervalMinutes < AutoSaveSettings.MinimumIntervalMinutes || intervalMinutes > AutoSaveSettings.MaximumIntervalMinutes)
            {
                ValidationText.Text = $"分钟间隔需在 {AutoSaveSettings.MinimumIntervalMinutes}-{AutoSaveSettings.MaximumIntervalMinutes} 之间。";
                return;
            }

            Settings = new AutoSaveSettings
            {
                IsEnabled = EnableAutoSaveCheckBox.IsChecked == true,
                IntervalMinutes = intervalMinutes,
                ShowNotification = ShowNotificationCheckBox.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
