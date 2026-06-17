using System;
using System.IO;
using System.Xml.Serialization;

namespace PlugHub.FamilyFileSaver
{
    public sealed class AutoSaveSettings
    {
        public const int DefaultIntervalMinutes = 10;
        public const int MinimumIntervalMinutes = 1;
        public const int MaximumIntervalMinutes = 240;

        public bool IsEnabled { get; set; }
        public int IntervalMinutes { get; set; } = DefaultIntervalMinutes;
        public bool ShowNotification { get; set; } = true;

        public static AutoSaveSettings Load()
        {
            string path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return new AutoSaveSettings();
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AutoSaveSettings));
                using (FileStream stream = File.OpenRead(path))
                {
                    AutoSaveSettings? settings = serializer.Deserialize(stream) as AutoSaveSettings;
                    return Normalize(settings ?? new AutoSaveSettings());
                }
            }
            catch
            {
                return new AutoSaveSettings();
            }
        }

        public void Save()
        {
            AutoSaveSettings settings = Normalize(this);
            string path = GetSettingsPath();
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(AutoSaveSettings));
            using (FileStream stream = File.Create(path))
            {
                serializer.Serialize(stream, settings);
            }
        }

        public static AutoSaveSettings Normalize(AutoSaveSettings settings)
        {
            int interval = settings.IntervalMinutes;
            if (interval < MinimumIntervalMinutes)
            {
                interval = MinimumIntervalMinutes;
            }
            else if (interval > MaximumIntervalMinutes)
            {
                interval = MaximumIntervalMinutes;
            }

            return new AutoSaveSettings
            {
                IsEnabled = settings.IsEnabled,
                IntervalMinutes = interval,
                ShowNotification = settings.ShowNotification
            };
        }

        private static string GetSettingsPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "PlugHub", "FamilyFileSaver", "auto-save-settings.xml");
        }
    }
}
