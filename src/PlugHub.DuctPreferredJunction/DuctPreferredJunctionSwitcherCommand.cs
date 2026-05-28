using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace PlugHub.DuctPreferredJunction
{
    [Transaction(TransactionMode.Manual)]
    public sealed class DuctPreferredJunctionSwitcherCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDocument = commandData?.Application?.ActiveUIDocument;
            if (uiDocument?.Document == null)
            {
                message = "未找到当前 Revit 文档。";
                return Result.Failed;
            }

            var document = uiDocument.Document;
            var duct = GetSelectedDuct(uiDocument, document);
            if (duct == null)
            {
                try
                {
                    var reference = uiDocument.Selection.PickObject(ObjectType.Element, new DuctSelectionFilter());
                    duct = document.GetElement(reference) as Duct;
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
            }

            if (duct == null)
            {
                message = "请选择一个风管。";
                return Result.Failed;
            }

            var ductType = document.GetElement(duct.GetTypeId()) as DuctType;
            if (ductType == null)
            {
                message = "未找到风管类型。";
                return Result.Failed;
            }

            using (var transaction = new Transaction(document, "切换风管首选连接类型"))
            {
                transaction.Start();
                var manager = ductType.RoutingPreferenceManager;
                manager.PreferredJunctionType = manager.PreferredJunctionType == PreferredJunctionType.Tee
                    ? PreferredJunctionType.Tap
                    : PreferredJunctionType.Tee;
                transaction.Commit();
            }

            return Result.Succeeded;
        }

        private static Duct? GetSelectedDuct(UIDocument uiDocument, Document document)
        {
            ICollection<ElementId> selectedIds = uiDocument.Selection.GetElementIds();
            if (selectedIds == null || selectedIds.Count == 0)
            {
                return null;
            }

            return selectedIds
                .Select(id => document.GetElement(id))
                .OfType<Duct>()
                .FirstOrDefault();
        }

        private sealed class DuctSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element element)
            {
                return element is Duct;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
}
