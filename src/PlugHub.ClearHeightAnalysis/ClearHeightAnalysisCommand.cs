using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PlugHub.ClearHeightAnalysis.Revit;

namespace PlugHub.ClearHeightAnalysis
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ClearHeightAnalysisCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return new AnalysisWorkflowController().Execute(commandData, ref message);
        }
    }
}
