using System;
using System.Drawing;
using System.Windows.Forms;

namespace PlugHub.HubeiReportParameters
{
    public sealed class HubeiReportSelectionForm : Form
    {
        private static readonly Color PrimaryColor = Color.FromArgb(30, 58, 95);
        private static readonly Color AccentColor = Color.FromArgb(37, 99, 235);
        private static readonly Color BackgroundColor = Color.FromArgb(248, 250, 252);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private static readonly Color TextColor = Color.FromArgb(15, 23, 42);
        private static readonly Color MutedTextColor = Color.FromArgb(100, 116, 139);
        private readonly TextBox _templatePathTextBox;
        private readonly CheckBox _removeExistingParametersCheckBox;
        private readonly CheckBox _writeActualValuesCheckBox;
        private readonly CheckBox _exportHifcMappingFileCheckBox;
        private readonly CheckBox _createPropertySetSchedulesCheckBox;
        private readonly Button _executeButton;

        public HubeiReportSelectionForm()
        {
            Text = "湖北报规参数";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(680, 500);
            BackColor = BackgroundColor;
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);

            Panel headerPanel = CreateHeaderPanel();
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BackgroundColor,
                Padding = new Padding(24, 16, 24, 18)
            };

            GroupBox templateSection = CreateSection("1  选择参数模板", new Rectangle(24, 14, 632, 90));
            var templateLabel = new Label
            {
                AutoSize = true,
                Text = "CSV 文件",
                ForeColor = TextColor,
                Location = new Point(16, 38)
            };
            _templatePathTextBox = new TextBox
            {
                Location = new Point(82, 34),
                Size = new Size(430, 28),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AccessibleName = "湖北报规 CSV 模板路径"
            };
            _templatePathTextBox.TextChanged += UpdateExecutionState;
            Button browseButton = CreateButton("选择文件", AccentColor, Color.White, new Rectangle(524, 32, 88, 32));
            browseButton.AccessibleDescription = "选择本地湖北报规 CSV 参数模板";
            browseButton.Click += SelectTemplate;
            templateSection.Controls.Add(templateLabel);
            templateSection.Controls.Add(_templatePathTextBox);
            templateSection.Controls.Add(browseButton);

            GroupBox parameterSection = CreateSection("2  参数与数据", new Rectangle(24, 116, 632, 100));
            _removeExistingParametersCheckBox = CreateCheckBox("清除当前项目同名参数", new Point(18, 30));
            var parameterHint = new Label
            {
                AutoSize = true,
                Text = "仅在需要重建旧参数定义或绑定时勾选",
                ForeColor = MutedTextColor,
                Location = new Point(246, 32)
            };
            _writeActualValuesCheckBox = CreateCheckBox("写入模板真实数据", new Point(18, 58));
            _writeActualValuesCheckBox.Checked = true;
            var actualValueHint = new Label
            {
                AutoSize = true,
                Text = "未勾选时仅使用模板默认值",
                ForeColor = MutedTextColor,
                Location = new Point(246, 60)
            };
            parameterSection.Controls.Add(_removeExistingParametersCheckBox);
            parameterSection.Controls.Add(parameterHint);
            parameterSection.Controls.Add(_writeActualValuesCheckBox);
            parameterSection.Controls.Add(actualValueHint);

            GroupBox outputSection = CreateSection("3  输出内容", new Rectangle(24, 228, 632, 116));
            _exportHifcMappingFileCheckBox = CreateCheckBox("导出项目名-HIFC.txt 映射文件", new Point(18, 30));
            _exportHifcMappingFileCheckBox.Checked = true;
            _createPropertySetSchedulesCheckBox = CreateCheckBox("创建属性集明细表", new Point(18, 58));
            var scheduleHint = new Label
            {
                AutoSize = false,
                Text = "项目信息不创建明细表；已有同名属性集明细表将按当前模板重建。",
                ForeColor = MutedTextColor,
                Location = new Point(38, 84),
                Size = new Size(560, 22)
            };
            outputSection.Controls.Add(_exportHifcMappingFileCheckBox);
            outputSection.Controls.Add(_createPropertySetSchedulesCheckBox);
            outputSection.Controls.Add(scheduleHint);

            var divider = new Panel
            {
                BackColor = BorderColor,
                Location = new Point(24, 360),
                Size = new Size(632, 1)
            };
            _executeButton = CreateButton("开始执行", AccentColor, Color.White, new Rectangle(454, 378, 96, 34));
            _executeButton.DialogResult = DialogResult.OK;
            _executeButton.Enabled = false;
            var cancelButton = CreateButton("取消", Color.White, TextColor, new Rectangle(560, 378, 96, 34));
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.FlatAppearance.BorderColor = BorderColor;

            contentPanel.Controls.Add(templateSection);
            contentPanel.Controls.Add(parameterSection);
            contentPanel.Controls.Add(outputSection);
            contentPanel.Controls.Add(divider);
            contentPanel.Controls.Add(_executeButton);
            contentPanel.Controls.Add(cancelButton);
            Controls.Add(contentPanel);
            Controls.Add(headerPanel);

            AcceptButton = _executeButton;
            CancelButton = cancelButton;
        }

        public HubeiReportSelection Selection => new HubeiReportSelection
        {
            TemplatePath = _templatePathTextBox.Text,
            RemoveExistingParameters = _removeExistingParametersCheckBox.Checked,
            WriteActualValues = _writeActualValuesCheckBox.Checked,
            ExportHifcMappingFile = _exportHifcMappingFileCheckBox.Checked,
            CreatePropertySetSchedules = _createPropertySetSchedulesCheckBox.Checked
        };

        private static Panel CreateHeaderPanel()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = PrimaryColor };
            var title = new Label
            {
                AutoSize = true,
                Text = "湖北报规参数",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 0),
                Location = new Point(24, 10)
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Text = "根据 CSV 模板同步共享参数、赋值并生成交付内容",
                ForeColor = Color.FromArgb(219, 234, 254),
                Location = new Point(26, 38)
            };
            panel.Controls.Add(title);
            panel.Controls.Add(subtitle);
            return panel;
        }

        private static GroupBox CreateSection(string title, Rectangle bounds)
        {
            return new GroupBox
            {
                Text = title,
                Bounds = bounds,
                BackColor = Color.White,
                ForeColor = PrimaryColor,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0),
                Padding = new Padding(12)
            };
        }

        private static CheckBox CreateCheckBox(string text, Point location)
        {
            return new CheckBox
            {
                AutoSize = true,
                Text = text,
                ForeColor = TextColor,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0),
                Location = location,
                UseVisualStyleBackColor = true
            };
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor, Rectangle bounds)
        {
            var button = new Button
            {
                Text = text,
                Bounds = bounds,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = backColor == Color.White ? 1 : 0;
            button.FlatAppearance.MouseOverBackColor = backColor == Color.White
                ? Color.FromArgb(241, 245, 249)
                : Color.FromArgb(29, 78, 216);
            button.FlatAppearance.MouseDownBackColor = backColor == Color.White
                ? Color.FromArgb(226, 232, 240)
                : Color.FromArgb(30, 64, 175);
            return button;
        }

        private void UpdateExecutionState(object sender, EventArgs eventArgs)
        {
            _executeButton.Enabled = !string.IsNullOrWhiteSpace(_templatePathTextBox.Text);
        }

        private void SelectTemplate(object sender, EventArgs eventArgs)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "CSV 模板 (*.csv)|*.csv|所有文件 (*.*)|*.*";
                dialog.Title = "选择湖北报规参数模板";
                dialog.Multiselect = false;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _templatePathTextBox.Text = dialog.FileName;
                }
            }
        }
    }
}
