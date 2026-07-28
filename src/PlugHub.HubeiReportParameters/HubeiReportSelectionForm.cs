using System.Drawing;
using System.Windows.Forms;

namespace PlugHub.HubeiReportParameters
{
    public sealed class HubeiReportSelectionForm : Form
    {
        private readonly TextBox _templatePathTextBox;
        private readonly CheckBox _removeExistingParametersCheckBox;

        public HubeiReportSelectionForm()
        {
            Text = "湖北报规参数";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(620, 176);
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);

            var instructionLabel = new Label
            {
                AutoSize = false,
                Text = "选择 CSV 模板，插件将按模板创建共享参数并生成项目 HIFC 映射文件。",
                Location = new Point(20, 16),
                Size = new Size(570, 28)
            };

            var templateLabel = new Label { AutoSize = true, Text = "模板文件", Location = new Point(20, 57) };
            _templatePathTextBox = new TextBox { Location = new Point(92, 53), Size = new Size(410, 26), ReadOnly = true };
            var browseButton = new Button { Text = "选择...", Location = new Point(514, 52), Size = new Size(80, 29) };
            browseButton.Click += SelectTemplate;

            _removeExistingParametersCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "清除当前项目同名参数",
                Location = new Point(20, 96)
            };

            var okButton = new Button { Text = "执行", DialogResult = DialogResult.OK, Location = new Point(424, 132), Size = new Size(80, 30) };
            var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(514, 132), Size = new Size(80, 30) };

            AcceptButton = okButton;
            CancelButton = cancelButton;
            Controls.Add(instructionLabel);
            Controls.Add(templateLabel);
            Controls.Add(_templatePathTextBox);
            Controls.Add(browseButton);
            Controls.Add(_removeExistingParametersCheckBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
        }

        public HubeiReportSelection Selection => new HubeiReportSelection
        {
            TemplatePath = _templatePathTextBox.Text,
            RemoveExistingParameters = _removeExistingParametersCheckBox.Checked
        };

        private void SelectTemplate(object sender, System.EventArgs eventArgs)
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
