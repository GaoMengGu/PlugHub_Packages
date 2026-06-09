using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace PlugHub.MepTypeFilterVisibility
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ApplyMepTypeFilterVisibilityCommand : IExternalCommand
    {
        private static readonly FilterKind[] FilterKinds =
        {
            new FilterKind(
                "duct",
                BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM,
                new[]
                {
                    BuiltInCategory.OST_DuctCurves,
                    BuiltInCategory.OST_DuctFitting,
                    BuiltInCategory.OST_DuctAccessory,
                    BuiltInCategory.OST_DuctTerminal
                }),
            new FilterKind(
                "pipe",
                BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM,
                new[]
                {
                    BuiltInCategory.OST_PipeCurves,
                    BuiltInCategory.OST_PipeFitting,
                    BuiltInCategory.OST_PipeAccessory
                }),
            new FilterKind(
                "cable-tray",
                BuiltInParameter.RBS_CABLETRAYCONDUIT_SYSTEM_TYPE,
                new[]
                {
                    BuiltInCategory.OST_CableTray,
                    BuiltInCategory.OST_CableTrayFitting
                })
        };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDocument = commandData?.Application?.ActiveUIDocument;
            var document = uiDocument?.Document;
            if (uiDocument == null || document == null)
            {
                message = "未找到当前 Revit 文档。";
                return Result.Failed;
            }

            var view = document.ActiveView;
            if (view == null)
            {
                message = "未找到当前视图。";
                return Result.Failed;
            }

            if (!view.AreGraphicsOverridesAllowed())
            {
                message = "当前视图不支持可见性/图形过滤器。";
                return Result.Failed;
            }

            var targets = CollectSelectedTargets(document, GetCurrentSelection(uiDocument, document));
            if (targets.Count == 0)
            {
                try
                {
                    var pickedElements = uiDocument.Selection.PickElementsByRectangle(
                        new MepSelectionFilter(),
                        "框选风管、管道或桥架图元。");
                    targets = CollectSelectedTargets(document, pickedElements);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
            }

            if (targets.Count == 0)
            {
                message = "请选择至少一个带有系统类型或设备类型的风管、管道或桥架图元。";
                return Result.Failed;
            }

            try
            {
                using (var transaction = new Transaction(document, "按机电类型切换过滤器"))
                {
                    transaction.Start();
                    ApplyFilterVisibility(document, view, targets);
                    transaction.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static List<Element> GetCurrentSelection(UIDocument uiDocument, Document document)
        {
            return uiDocument.Selection.GetElementIds()
                .Select(document.GetElement)
                .Where(element => element != null)
                .Cast<Element>()
                .ToList();
        }

        private static List<SelectedFilterTarget> CollectSelectedTargets(Document document, IEnumerable<Element> elements)
        {
            var targetsByKey = new Dictionary<string, SelectedFilterTarget>(StringComparer.Ordinal);
            foreach (var element in elements)
            {
                var kind = FindKindForElement(element);
                if (kind == null)
                {
                    continue;
                }

                var target = CreateSelectedTarget(document, element, kind);
                if (target == null)
                {
                    continue;
                }

                targetsByKey[target.IdentityKey] = target;
            }

            return targetsByKey.Values
                .OrderBy(target => target.FilterName, StringComparer.Ordinal)
                .ToList();
        }

        private static SelectedFilterTarget? CreateSelectedTarget(Document document, Element element, FilterKind kind)
        {
            var parameter = FindUsableParameter(document, element, kind.Parameter);
            if (parameter == null)
            {
                return null;
            }

            var filterName = GetFilterName(document, parameter);
            if (string.IsNullOrWhiteSpace(filterName))
            {
                return null;
            }

            var rule = CreateEqualsRule(kind, parameter, filterName, out var valueKey);
            if (rule == null)
            {
                return null;
            }

            return new SelectedFilterTarget(kind, filterName, kind.Key + ":" + valueKey, rule);
        }

        private static Parameter? FindUsableParameter(Document document, Element element, BuiltInParameter parameterId)
        {
            var parameter = element.get_Parameter(parameterId);
            if (HasUsableValue(parameter))
            {
                return parameter;
            }

            var typeId = element.GetTypeId();
            if (typeId == null || typeId.IntegerValue < 0)
            {
                return null;
            }

            var elementType = document.GetElement(typeId);
            var typeParameter = elementType?.get_Parameter(parameterId);
            return HasUsableValue(typeParameter) ? typeParameter : null;
        }

        private static bool HasUsableValue(Parameter? parameter)
        {
            if (parameter == null || !parameter.HasValue)
            {
                return false;
            }

            switch (parameter.StorageType)
            {
                case StorageType.ElementId:
                    var elementId = parameter.AsElementId();
                    return elementId != null && elementId.IntegerValue >= 0;
                case StorageType.String:
                    return !string.IsNullOrWhiteSpace(parameter.AsString()) || !string.IsNullOrWhiteSpace(parameter.AsValueString());
                case StorageType.Integer:
                case StorageType.Double:
                    return true;
                default:
                    return false;
            }
        }

        private static string GetFilterName(Document document, Parameter parameter)
        {
            if (parameter.StorageType == StorageType.ElementId)
            {
                var elementId = parameter.AsElementId();
                if (elementId != null && elementId.IntegerValue >= 0)
                {
                    var referencedElement = document.GetElement(elementId);
                    if (referencedElement != null && !string.IsNullOrWhiteSpace(referencedElement.Name))
                    {
                        return NormalizeFilterName(referencedElement.Name);
                    }
                }
            }

            var valueString = parameter.AsValueString();
            if (!string.IsNullOrWhiteSpace(valueString))
            {
                return NormalizeFilterName(valueString);
            }

            if (parameter.StorageType == StorageType.String)
            {
                var stringValue = parameter.AsString();
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    return NormalizeFilterName(stringValue);
                }
            }

            if (parameter.StorageType == StorageType.Integer)
            {
                return parameter.AsInteger().ToString();
            }

            return string.Empty;
        }

        private static FilterRule? CreateEqualsRule(FilterKind kind, Parameter parameter, string filterName, out string valueKey)
        {
            var parameterElementId = new ElementId(kind.Parameter);
            switch (parameter.StorageType)
            {
                case StorageType.ElementId:
                    var elementId = parameter.AsElementId();
                    if (elementId == null || elementId.IntegerValue < 0)
                    {
                        valueKey = string.Empty;
                        return null;
                    }

                    valueKey = "id:" + elementId.IntegerValue;
                    return ParameterFilterRuleFactory.CreateEqualsRule(parameterElementId, elementId);

                case StorageType.String:
                    var stringValue = parameter.AsString();
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        stringValue = filterName;
                    }

                    valueKey = "text:" + stringValue;
                    return ParameterFilterRuleFactory.CreateEqualsRule(parameterElementId, stringValue, false);

                case StorageType.Integer:
                    var integerValue = parameter.AsInteger();
                    valueKey = "int:" + integerValue;
                    return ParameterFilterRuleFactory.CreateEqualsRule(parameterElementId, integerValue);

                default:
                    valueKey = string.Empty;
                    return null;
            }
        }

        private static void ApplyFilterVisibility(Document document, View view, IReadOnlyCollection<SelectedFilterTarget> targets)
        {
            var parameterFilters = CollectParameterFiltersByName(document);
            var visibleFilterIds = new HashSet<int>();

            foreach (var targetGroup in targets.GroupBy(target => target.FilterName, StringComparer.Ordinal))
            {
                if (!parameterFilters.TryGetValue(targetGroup.Key, out var filter))
                {
                    filter = CreateFilter(document, targetGroup.Key, targetGroup.ToList());
                    parameterFilters[targetGroup.Key] = filter;
                }

                visibleFilterIds.Add(filter.Id.IntegerValue);
                view.SetFilterVisibility(filter.Id, true);
            }

            foreach (var filterId in view.GetFilters().ToList())
            {
                if (!visibleFilterIds.Contains(filterId.IntegerValue))
                {
                    view.SetFilterVisibility(filterId, false);
                }
            }
        }

        private static Dictionary<string, ParameterFilterElement> CollectParameterFiltersByName(Document document)
        {
            return new FilteredElementCollector(document)
                .OfClass(typeof(ParameterFilterElement))
                .Cast<ParameterFilterElement>()
                .GroupBy(filter => filter.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private static ParameterFilterElement CreateFilter(Document document, string filterName, IReadOnlyCollection<SelectedFilterTarget> targets)
        {
            var categoryIds = targets
                .SelectMany(target => target.Kind.CreateCategoryIds())
                .GroupBy(id => id.IntegerValue)
                .Select(group => group.First())
                .ToList();

            var elementFilter = CreateElementFilter(targets);
            return ParameterFilterElement.Create(document, filterName, categoryIds, elementFilter);
        }

        private static ElementFilter CreateElementFilter(IReadOnlyCollection<SelectedFilterTarget> targets)
        {
            var filters = new List<ElementFilter>();
            foreach (var target in targets)
            {
                foreach (var categoryId in target.Kind.CreateCategoryIds())
                {
                    var rules = new List<FilterRule>
                    {
                        new FilterCategoryRule(new List<ElementId> { categoryId }),
                        target.Rule
                    };
                    filters.Add(new ElementParameterFilter(rules));
                }
            }

            if (filters.Count == 1)
            {
                return new ElementParameterFilter(targets.First().Rule);
            }

            return new LogicalOrFilter(filters);
        }

        private static FilterKind? FindKindForElement(Element element)
        {
            var categoryId = GetCategoryId(element);
            if (!categoryId.HasValue)
            {
                return null;
            }

            return FilterKinds.FirstOrDefault(kind => kind.IsSelectionCategory(categoryId.Value));
        }

        private static int? GetCategoryId(Element element)
        {
            var category = element.Category;
            return category?.Id?.IntegerValue;
        }

        private static string NormalizeFilterName(string value)
        {
            return value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ')
                .Trim();
        }

        private sealed class MepSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element element)
            {
                return FindKindForElement(element) != null;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        private sealed class FilterKind
        {
            private readonly BuiltInCategory[] _categories;
            private readonly HashSet<int> _selectionCategoryIds;

            public FilterKind(string key, BuiltInParameter parameter, BuiltInCategory[] categories)
            {
                Key = key;
                Parameter = parameter;
                _categories = categories;
                _selectionCategoryIds = new HashSet<int>(categories.Select(category => (int)category));
            }

            public string Key { get; }

            public BuiltInParameter Parameter { get; }

            public bool IsSelectionCategory(int categoryId)
            {
                return _selectionCategoryIds.Contains(categoryId);
            }

            public List<ElementId> CreateCategoryIds()
            {
                return _categories.Select(category => new ElementId(category)).ToList();
            }
        }

        private sealed class SelectedFilterTarget
        {
            public SelectedFilterTarget(FilterKind kind, string filterName, string identityKey, FilterRule rule)
            {
                Kind = kind;
                FilterName = filterName;
                IdentityKey = identityKey;
                Rule = rule;
            }

            public FilterKind Kind { get; }

            public string FilterName { get; }

            public string IdentityKey { get; }

            public FilterRule Rule { get; }
        }
    }
}
