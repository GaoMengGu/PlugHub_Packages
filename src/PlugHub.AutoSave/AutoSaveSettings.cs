using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PlugHub.AutoSave
{
    /// <summary>
    /// 自动保存设置数据模型，持久化到本地 JSON 文件。
    /// </summary>
    [Serializable]
    public class AutoSaveSettings : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        private int _intervalMinutes = 10;
        private bool _showNotification = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public int IntervalMinutes
        {
            get => _intervalMinutes;
            set { _intervalMinutes = Math.Max(1, Math.Min(120, value)); OnPropertyChanged(); }
        }

        public bool ShowNotification
        {
            get => _showNotification;
            set { _showNotification = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PlugHub", "AutoSaveSettings.json");

        public static AutoSaveSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    var serializer = new DataContractJsonSerializer(typeof(AutoSaveSettings));
                    var result = serializer.ReadObject(stream) as AutoSaveSettings;
                    if (result != null) return result;
                }
            }
            catch { /* 读取失败时使用默认值 */ }
            return new AutoSaveSettings();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using var stream = new MemoryStream();
                var serializer = new DataContractJsonSerializer(typeof(AutoSaveSettings));
                serializer.WriteObject(stream, this);
                string json = Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* 静默失败 */ }
        }
    }
}
