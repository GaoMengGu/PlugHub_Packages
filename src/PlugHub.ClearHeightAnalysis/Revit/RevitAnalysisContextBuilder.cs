#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class RevitSourceContext
    {
        public RevitSourceContext(string key, Document document, RevitLinkInstance? linkInstance, Transform transform)
        {
            Key = key;
            Document = document;
            LinkInstance = linkInstance;
            Transform = transform;
        }

        public string Key { get; }
        public Document Document { get; }
        public RevitLinkInstance? LinkInstance { get; }
        public Transform Transform { get; }
        public bool IsHost => LinkInstance == null;
    }

    public sealed class RevitAnalysisContext
    {
        public RevitAnalysisContext(Document hostDocument, Level level, AnalysisRequest request, IReadOnlyList<RevitSourceContext> sources)
        {
            HostDocument = hostDocument;
            Level = level;
            Request = request;
            Sources = sources;
        }

        public Document HostDocument { get; }
        public Level Level { get; }
        public AnalysisRequest Request { get; }
        public IReadOnlyList<RevitSourceContext> Sources { get; }
    }

    public static class RevitAnalysisContextBuilder
    {
        public static IReadOnlyList<AnalysisLevelChoice> GetLevelChoices(Document document)
        {
            return new FilteredElementCollector(document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(level => level.Elevation)
                .Select(level => new AnalysisLevelChoice(
                    level.UniqueId,
                    level.Name,
                    Services.UnitConversion.FeetToMillimeters(level.Elevation)))
                .ToList();
        }

        public static IReadOnlyList<SourceModelChoice> GetSourceChoices(Document document)
        {
            var choices = new List<SourceModelChoice>
            {
                new SourceModelChoice("host", "当前模型：" + document.Title, true, true, true)
            };
            foreach (RevitLinkInstance link in new FilteredElementCollector(document)
                         .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
            {
                Document? linked = link.GetLinkDocument();
                choices.Add(new SourceModelChoice(
                    "link:" + link.UniqueId,
                    "链接实例：" + link.Name,
                    false,
                    linked != null,
                    linked != null));
            }
            return choices;
        }

        public static RevitAnalysisContext Build(Document document, AnalysisRequest request)
        {
            IReadOnlyList<string> errors = Core.Services.AnalysisRequestValidator.Validate(request);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

            Level? level = document.GetElement(request.SelectedLevel!.UniqueId) as Level;
            if (level == null)
                throw new InvalidOperationException("所选楼层已不存在，请重新打开设置窗口。");

            var links = new FilteredElementCollector(document).OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>().ToDictionary(link => "link:" + link.UniqueId, StringComparer.Ordinal);
            var sources = new List<RevitSourceContext>();
            foreach (SourceModelChoice choice in request.SourceModels.Where(item => item.IsSelected))
            {
                if (choice.IsHost)
                {
                    sources.Add(new RevitSourceContext("host", document, null, Transform.Identity));
                    continue;
                }

                if (!links.TryGetValue(choice.Key, out RevitLinkInstance link) || link.GetLinkDocument() == null)
                    throw new InvalidOperationException("链接实例已卸载或删除：" + choice.DisplayName);
                sources.Add(new RevitSourceContext(choice.Key, link.GetLinkDocument(), link, link.GetTotalTransform()));
            }
            return new RevitAnalysisContext(document, level, request, sources);
        }
    }
}
