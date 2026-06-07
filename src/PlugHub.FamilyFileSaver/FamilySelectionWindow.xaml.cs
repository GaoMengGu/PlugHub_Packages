using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;

namespace PlugHub.FamilyFileSaver
{
    public class FamilyViewModel : INotifyPropertyChanged
    {
        private bool _isChecked;

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }

        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int InstanceCount { get; set; }
        public FamilyItem Item { get; set; } = null!;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class FamilySelectionWindow : Window
    {
        private readonly List<FamilyItem> _allFamilies;
        private readonly ObservableCollection<FamilyViewModel> _filteredFamilies;

        public List<FamilyItem> SelectedFamilies { get; private set; } = new List<FamilyItem>();

        public FamilySelectionWindow(List<FamilyItem> families)
        {
            InitializeComponent();
            _allFamilies = families;
            _filteredFamilies = new ObservableCollection<FamilyViewModel>();

            TitleText.Text = $"当前项目共有 {_allFamilies.Count} 个可保存的族";

            // 填充分类下拉框
            CategoryCombo.Items.Add("全部");
            foreach (var cat in _allFamilies.Select(f => f.Category).Distinct().OrderBy(c => c))
            {
                CategoryCombo.Items.Add(cat);
            }
            CategoryCombo.SelectedIndex = 0;

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string searchText = SearchBox.Text ?? "";
            string selectedCategory = CategoryCombo.SelectedItem?.ToString() ?? "全部";

            var filtered = _allFamilies.Where(f =>
            {
                bool matchesSearch = string.IsNullOrEmpty(searchText)
                    || f.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesCategory = selectedCategory == "全部" || f.Category == selectedCategory;
                return matchesSearch && matchesCategory;
            }).ToList();

            // 保留之前的勾选状态
            var checkedIds = new HashSet<ElementId>(
                _filteredFamilies.Where(f => f.IsChecked).Select(f => f.Item.Id));

            _filteredFamilies.Clear();
            foreach (var item in filtered)
            {
                _filteredFamilies.Add(new FamilyViewModel
                {
                    Name = item.Name,
                    Category = item.Category,
                    InstanceCount = item.InstanceCount,
                    Item = item,
                    IsChecked = checkedIds.Contains(item.Id)
                });
            }

            FamilyListView.ItemsSource = null;
            FamilyListView.ItemsSource = _filteredFamilies;

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            int checkedCount = _filteredFamilies.Count(f => f.IsChecked);
            StatusText.Text = $"已选中 {checkedCount} 个族 / 共 {_allFamilies.Count} 个";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SelectAllCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = SelectAllCheckBox.IsChecked == true;
            foreach (var item in _filteredFamilies)
            {
                item.IsChecked = isChecked;
            }
            UpdateStatus();
        }

        private void ItemCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateStatus();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedFamilies = _filteredFamilies
                .Where(f => f.IsChecked)
                .Select(f => f.Item)
                .ToList();

            if (SelectedFamilies.Count == 0)
            {
                MessageBox.Show("请至少选择一个族文件。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
