using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace PlugHub.AutoSave
{
    /// <summary>
    /// 自动保存设置 WPF 窗口。
    /// </summary>
    public partial class AutoSaveSettingsWindow : Window
    {
        private readonly AutoSaveSettings _settings;
        private readonly AutoSaveService _service;

        public AutoSaveSettingsWindow(AutoSaveSettings settings, AutoSaveService service, UIApplication uiApp)
        {
            _settings = settings;
            _service = service;
            DataContext = _settings;
            Title = "自动保存设置";
            Width = 360;
            Height = 260;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            // 设置 Revit 主窗口为 Owner
            IntPtr revitHandle = uiApp.MainWindowHandle;
            if (revitHandle != IntPtr.Zero)
            {
                var helper = new WindowInteropHelper(this);
                helper.Owner = revitHandle;
            }

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 构建 UI 布局
            var mainStack = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(20, 16, 20, 16)
            };

            // 1. 启用自动保存
            var enableCheck = new System.Windows.Controls.CheckBox
            {
                Content = "启用自动保存",
                IsChecked = _settings.IsEnabled,
                Margin = new Thickness(0, 0, 0, 12),
                FontSize = 13
            };
            enableCheck.SetBinding(System.Windows.Controls.CheckBox.IsCheckedProperty,
                new System.Windows.Data.Binding("IsEnabled") { Mode = System.Windows.Data.BindingMode.TwoWay });
            mainStack.Children.Add(enableCheck);

            // 2. 间隔设置
            var intervalPanel = new System.Windows.Controls.DockPanel
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            var intervalLabel = new System.Windows.Controls.TextBlock
            {
                Text = "保存间隔：",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 13
            };
            System.Windows.Controls.DockPanel.SetDock(intervalLabel, Dock.Left);
            intervalPanel.Children.Add(intervalLabel);

            var intervalBox = new System.Windows.Controls.TextBox
            {
                Text = _settings.IntervalMinutes.ToString(),
                Width = 60,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                FontSize = 13
            };
            intervalBox.TextChanged += (s, e) =>
            {
                if (int.TryParse(intervalBox.Text, out int val) && val >= 1 && val <= 120)
                    _settings.IntervalMinutes = val;
            };
            intervalPanel.Children.Add(intervalBox);

            var unitLabel = new System.Windows.Controls.TextBlock
            {
                Text = "分钟（1-120）",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13
            };
            intervalPanel.Children.Add(unitLabel);
            mainStack.Children.Add(intervalPanel);

            // 3. 弹窗提示
            var notifyCheck = new System.Windows.Controls.CheckBox
            {
                Content = "保存完成时弹窗提示",
                IsChecked = _settings.ShowNotification,
                Margin = new Thickness(0, 0, 0, 16),
                FontSize = 13
            };
            notifyCheck.SetBinding(System.Windows.Controls.CheckBox.IsCheckedProperty,
                new System.Windows.Data.Binding("ShowNotification") { Mode = System.Windows.Data.BindingMode.TwoWay });
            mainStack.Children.Add(notifyCheck);

            // 4. 按钮行
            var buttonPanel = new System.Windows.Controls.DockPanel();

            var saveNowButton = new System.Windows.Controls.Button
            {
                Content = "立即保存",
                Width = 90,
                Height = 30,
                FontSize = 13
            };
            saveNowButton.Click += (s, e) => _service.ManualSave();
            System.Windows.Controls.DockPanel.SetDock(saveNowButton, Dock.Left);
            buttonPanel.Children.Add(saveNowButton);

            var okButton = new System.Windows.Controls.Button
            {
                Content = "确定",
                Width = 80,
                Height = 30,
                FontSize = 13
            };
            okButton.Click += (s, e) =>
            {
                _settings.Save();
                DialogResult = true;
                Close();
            };
            System.Windows.Controls.DockPanel.SetDock(okButton, Dock.Right);
            buttonPanel.Children.Add(okButton);

            mainStack.Children.Add(buttonPanel);

            Content = mainStack;
        }
    }
}
