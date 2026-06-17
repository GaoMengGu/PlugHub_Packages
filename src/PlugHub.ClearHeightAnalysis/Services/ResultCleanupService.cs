using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public sealed class ResultCleanupService
    {
        public int DeleteExistingResults(Document document)
        {
            var idsToDelete = new List<ElementId>();
            foreach (Element element in new FilteredElementCollector(document).OfClass(typeof(FilledRegion)))
            {
                if (ResultTagService.IsTaggedResult(element))
                {
                    idsToDelete.Add(element.Id);
                }
            }

            if (idsToDelete.Count == 0)
            {
                return 0;
            }

            document.Delete(idsToDelete);
            return idsToDelete.Count;
        }
    }
}
