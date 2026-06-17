namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class SourceModelInfo
    {
        public SourceModelInfo(string name, bool isLinked)
        {
            Name = name;
            IsLinked = isLinked;
        }

        public string Name { get; }
        public bool IsLinked { get; }
    }
}
