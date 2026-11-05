using System;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class SourceModelChoice
    {
        public SourceModelChoice(
            string key,
            string displayName,
            bool isHost,
            bool isLoaded,
            bool isSelected)
        {
            Key = string.IsNullOrWhiteSpace(key)
                ? throw new ArgumentException("来源模型标识不能为空。", nameof(key))
                : key;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? key : displayName;
            IsHost = isHost;
            IsLoaded = isLoaded;
            IsSelected = isSelected && isLoaded;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public bool IsHost { get; }
        public bool IsLoaded { get; }
        public bool IsSelected { get; set; }
    }
}
