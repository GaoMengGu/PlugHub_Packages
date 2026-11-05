#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class DerivedOutputCleanupService
    {
        public int Delete(Document document, string batchId, string? outputId = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            List<ElementId> ids = new FilteredElementCollector(document)
                .Where(element => OutputTagService.Matches(element, batchId, outputId))
                .Select(element => element.Id).Distinct().ToList();
            using (var transaction = new Transaction(document, "删除净高分析派生成果"))
            {
                transaction.Start();
                if (ids.Count > 0) document.Delete(ids);
                transaction.Commit();
            }
            return ids.Count;
        }

        public int DeleteLegacyFilledRegions(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            List<ElementId> ids = new FilteredElementCollector(document).OfClass(typeof(FilledRegion))
                .Where(element => ResultTagService.IsTaggedResult(element)).Select(element => element.Id).ToList();
            using (var transaction = new Transaction(document, "清理旧版净高填充图"))
            {
                transaction.Start(); if (ids.Count > 0) document.Delete(ids); transaction.Commit();
            }
            return ids.Count;
        }
    }
}
