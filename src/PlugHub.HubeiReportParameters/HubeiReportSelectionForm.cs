using System;
using System.Drawing;
using System.Windows.Forms;

namespace PlugHub.HubeiReportParameters
{
    public sealed class HubeiReportSelectionForm : Form
    {
        private bool _suppressEvents;
        private readonly CheckBox _globalCheckBox;
        private readonly CheckBox _totalPlanCheckBox;
        private readonly CheckBox _monolithicCheckBox;
        private readonly CheckBox _miniCheckBox;
        private readonly TextBox _textDefaultTextBox;
        private readonly TextBox _numberDefaultTextBox;
        private readonly ComboBox _yesNoDefaultComboBox;

        public HubeiReportSelectionForm()
        {
            Text = "湖北报规参数";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(480, 340);
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);

            var titleLabel = new Label
            {
                AutoSize = false,
                Text = "选择需要创建的属性分类",
                Location = new Point(20, 16),
                Size = new Size(360, 24)
            };

            _globalCheckBox = CreateScopeCheckBox("全局", 52);
            _totalPlanCheckBox = CreateScopeCheckBox("总图", 84);
            _monolithicCheckBox = CreateScopeCheckBox("单体", 116);
            _miniCheckBox = CreateScopeCheckBox("最小报建", 148);
            _miniCheckBox.CheckedChanged += MiniCheckBox_CheckedChanged;

            var defaultsGroup = new GroupBox
            {
                Text = "默认值",
                Location = new Point(20, 182),
                Size = new Size(440, 108)
            };

            var textLabel = new Label { AutoSize = true, Text = "文字", Location = new Point(18, 31) };
            _textDefaultTextBox = new TextBox { Text = "其他", Location = new Point(70, 27), Size = new Size(110, 26) };

            var numberLabel = new Label { AutoSize = true, Text = "数值", Location = new Point(200, 31) };
            _numberDefaultTextBox = new TextBox { Text = "0", Location = new Point(252, 27), Size = new Size(80, 26) };

            var yesNoLabel = new Label { AutoSize = true, Text = "布尔", Location = new Point(18, 66) };
            _yesNoDefaultComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(70, 62),
                Size = new Size(80, 26)
            };
            _yesNoDefaultComboBox.Items.AddRange(new object[] { "否", "是" });
            _yesNoDefaultComboBox.SelectedIndex = 0;

            defaultsGroup.Controls.Add(textLabel);
            defaultsGroup.Controls.Add(_textDefaultTextBox);
            defaultsGroup.Controls.Add(numberLabel);
            defaultsGroup.Controls.Add(_numberDefaultTextBox);
            defaultsGroup.Controls.Add(yesNoLabel);
            defaultsGroup.Controls.Add(_yesNoDefaultComboBox);

            var okButton = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(290, 302), Size = new Size(80, 30) };
            var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(380, 302), Size = new Size(80, 30) };

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.Add(titleLabel);
            Controls.Add(_globalCheckBox);
            Controls.Add(_totalPlanCheckBox);
            Controls.Add(_monolithicCheckBox);
            Controls.Add(_miniCheckBox);
            Controls.Add(defaultsGroup);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            _globalCheckBox.Checked = true;
            _totalPlanCheckBox.Checked = true;
            _monolithicCheckBox.Checked = true;
            _miniCheckBox.Checked = false;
        }

        public HubeiReportSelection Selection => new HubeiReportSelection
        {
            IncludeGlobal = _globalCheckBox.Checked,
            IncludeTotalPlan = _totalPlanCheckBox.Checked,
            IncludeMonolithic = _monolithicCheckBox.Checked,
            IncludeMiniReport = _miniCheckBox.Checked,
            Defaults = new HubeiReportDefaults
            {
                TextValue = string.IsNullOrWhiteSpace(_textDefaultTextBox.Text) ? "其他" : _textDefaultTextBox.Text,
                NumberValue = string.IsNullOrWhiteSpace(_numberDefaultTextBox.Text) ? "0" : _numberDefaultTextBox.Text,
                YesNoValue = _yesNoDefaultComboBox.SelectedIndex == 1
            }
        };

        private CheckBox CreateScopeCheckBox(string text, int top)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                AutoSize = true,
                Location = new Point(24, top),
                Checked = true
            };

            checkBox.CheckedChanged += ScopeCheckBox_CheckedChanged;
            return checkBox;
        }

        private void MiniCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressEvents)
            {
                return;
            }

            if (_miniCheckBox.Checked)
            {
                _suppressEvents = true;
                _globalCheckBox.Checked = false;
                _totalPlanCheckBox.Checked = false;
                _monolithicCheckBox.Checked = false;
                _suppressEvents = false;
                return;
            }

            if (!_globalCheckBox.Checked && !_totalPlanCheckBox.Checked && !_monolithicCheckBox.Checked)
            {
                _suppressEvents = true;
                _globalCheckBox.Checked = true;
                _totalPlanCheckBox.Checked = true;
                _monolithicCheckBox.Checked = true;
                _suppressEvents = false;
            }
        }

        private void ScopeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressEvents)
            {
                return;
            }

            if (_miniCheckBox.Checked && (ReferenceEquals(sender, _globalCheckBox) || ReferenceEquals(sender, _totalPlanCheckBox) || ReferenceEquals(sender, _monolithicCheckBox)))
            {
                _suppressEvents = true;
                _miniCheckBox.Checked = false;
                _suppressEvents = false;
            }
        }
    }
}
