using Autodesk.Revit.DB;

namespace PlugHub.FamilyFileSaver
{
    public class FamilyItem
    {
        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Family? FamilyObject { get; set; }
        public int InstanceCount { get; set; }
    }
}
