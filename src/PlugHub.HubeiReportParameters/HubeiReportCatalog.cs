using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PlugHub.HubeiReportParameters
{
    public static class HubeiReportCatalog
    {
        private static readonly Lazy<IReadOnlyList<HubeiReportParameterDefinition>> AllDefinitionsLazy =
            new Lazy<IReadOnlyList<HubeiReportParameterDefinition>>(LoadDefinitions, true);

        public static IReadOnlyList<HubeiReportParameterDefinition> AllDefinitions => AllDefinitionsLazy.Value;

        public static IReadOnlyList<HubeiReportParameterDefinition> GetDefinitions(HubeiReportSelection selection)
        {
            if (selection == null || !selection.HasAnyScope)
            {
                return Array.Empty<HubeiReportParameterDefinition>();
            }

            if (selection.IncludeMiniReport)
            {
                return AllDefinitions.Where(definition => definition.Source == HubeiReportSource.Mini).ToArray();
            }

            var selectedScopes = new HashSet<HubeiReportScope>();
            if (selection.IncludeGlobal)
            {
                selectedScopes.Add(HubeiReportScope.Global);
            }

            if (selection.IncludeTotalPlan)
            {
                selectedScopes.Add(HubeiReportScope.TotalPlan);
            }

            if (selection.IncludeMonolithic)
            {
                selectedScopes.Add(HubeiReportScope.Monolithic);
            }

            return AllDefinitions.Where(definition => definition.Source == HubeiReportSource.Hifc && definition.Scopes.Any(selectedScopes.Contains)).ToArray();
        }

        private static IReadOnlyList<HubeiReportParameterDefinition> LoadDefinitions()
        {
            var definitions = new List<HubeiReportParameterDefinition>();
            HubeiReportParameterDefinition[] hifcDefinitions = ParseHifc().ToArray();
            Dictionary<string, HubeiParameterType> hifcTypes = hifcDefinitions
                .GroupBy(definition => definition.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Select(definition => definition.ParameterType).FirstOrDefault(type => type != HubeiParameterType.Text), StringComparer.Ordinal);

            foreach (var definition in hifcDefinitions)
            {
                definitions.Add(definition);
            }

            foreach (var definition in ParseMini(hifcTypes))
            {
                definitions.Add(definition);
            }

            return definitions
                .GroupBy(definition => definition.Source + "|" + definition.Name, StringComparer.Ordinal)
                .Select(MergeGroup)
                .OrderBy(definition => definition.Source)
                .ThenBy(definition => definition.Scopes.Min(scope => (int)scope))
                .ThenBy(definition => definition.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static HubeiReportParameterDefinition MergeGroup(IGrouping<string, HubeiReportParameterDefinition> group)
        {
            HubeiReportParameterDefinition first = group.First();
            return new HubeiReportParameterDefinition
            {
                PsetName = first.PsetName,
                Name = first.Name,
                IfcTypeName = first.IfcTypeName,
                ParameterType = group.Select(definition => definition.ParameterType).FirstOrDefault(type => type != HubeiParameterType.Text),
                Scopes = group.SelectMany(definition => definition.Scopes).Distinct().ToArray(),
                Source = first.Source
            };
        }

        private static IEnumerable<HubeiReportParameterDefinition> ParseHifc()
        {
            return ParseResource("HIFC.txt", HubeiReportSource.Hifc, false);
        }

        private static IEnumerable<HubeiReportParameterDefinition> ParseMini(IReadOnlyDictionary<string, HubeiParameterType> hifcTypes)
        {
            string text = ReadEmbeddedText("mini.txt");
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (string rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                IReadOnlyList<string> columns = SplitColumns(line);
                if (columns.Count < 2)
                {
                    continue;
                }

                string psetName = columns[0];
                string name = columns[1];
                if (!seen.Add(psetName + "|" + name))
                {
                    continue;
                }

                HubeiParameterType type = hifcTypes.TryGetValue(name, out HubeiParameterType hifcType) && hifcType != HubeiParameterType.Text
                    ? hifcType
                    : InferMiniParameterType(name);

                yield return new HubeiReportParameterDefinition
                {
                    PsetName = psetName,
                    Name = name,
                    IfcTypeName = string.Empty,
                    ParameterType = type,
                    Scopes = DetermineMiniScopes(psetName).ToArray(),
                    Source = HubeiReportSource.Mini
                };
            }
        }

        private static IEnumerable<HubeiReportParameterDefinition> ParseResource(string fileName, HubeiReportSource source, bool isMini)
        {
            string text = ReadEmbeddedText(fileName);
            string psetName = string.Empty;
            List<string> ifcs = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (string rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith("PropertySet:", StringComparison.Ordinal))
                {
                    IReadOnlyList<string> psetColumns = SplitColumns(line);
                    psetName = psetColumns.Count > 1 ? psetColumns[1] : string.Empty;
                    ifcs = psetColumns.Count > 3
                        ? psetColumns[3].Split(',').Select(value => value.Trim()).Where(value => value.Length > 0).ToList()
                        : new List<string>();
                    continue;
                }

                IReadOnlyList<string> propertyColumns = SplitColumns(line);
                if (propertyColumns.Count < 2)
                {
                    continue;
                }

                string name = propertyColumns[0];
                if (!seen.Add(psetName + "|" + name))
                {
                    continue;
                }

                yield return new HubeiReportParameterDefinition
                {
                    PsetName = psetName,
                    Name = name,
                    IfcTypeName = ifcs.Count > 0 ? string.Join(",", ifcs) : string.Empty,
                    ParameterType = MapParameterType(propertyColumns[1]),
                    Scopes = DetermineScopes(ifcs, psetName, isMini).ToArray(),
                    Source = source
                };
            }
        }

        private static IReadOnlyCollection<HubeiReportScope> DetermineScopes(IReadOnlyCollection<string> ifcs, string psetName, bool isMini)
        {
            var scopes = new HashSet<HubeiReportScope>();

            if (isMini)
            {
                scopes.UnionWith(DetermineMiniScopes(psetName));
                return scopes.ToArray();
            }

            if (ifcs.Contains("IfcProject") || string.Equals(psetName, "Pset_Manifest", StringComparison.Ordinal))
            {
                scopes.Add(HubeiReportScope.Global);
            }

            if (ifcs.Contains("IfcSite"))
            {
                scopes.Add(HubeiReportScope.TotalPlan);
            }

            if (ifcs.Contains("IfcBuilding") || ifcs.Contains("IfcBuildingStorey") || ifcs.Contains("IfcSpace") || ifcs.Contains("IfcSpatialZone") || ifcs.Contains("IfcSlab"))
            {
                scopes.Add(HubeiReportScope.Monolithic);
            }

            if (scopes.Count == 0)
            {
                scopes.Add(HubeiReportScope.Monolithic);
            }

            return scopes.ToArray();
        }

        private static IReadOnlyCollection<HubeiReportScope> DetermineMiniScopes(string psetName)
        {
            var scopes = new HashSet<HubeiReportScope> { HubeiReportScope.MiniReport };
            if (psetName.IndexOf("申报", StringComparison.Ordinal) >= 0)
            {
                scopes.Add(HubeiReportScope.Global);
            }

            if (psetName.IndexOf("道路", StringComparison.Ordinal) >= 0 || psetName.IndexOf("绿地", StringComparison.Ordinal) >= 0 || psetName.IndexOf("规划", StringComparison.Ordinal) >= 0 || psetName.IndexOf("场地", StringComparison.Ordinal) >= 0)
            {
                scopes.Add(HubeiReportScope.TotalPlan);
            }

            if (psetName.IndexOf("建筑", StringComparison.Ordinal) >= 0 || psetName.IndexOf("停车", StringComparison.Ordinal) >= 0)
            {
                scopes.Add(HubeiReportScope.Monolithic);
            }

            if (scopes.Count == 1)
            {
                scopes.Add(HubeiReportScope.Monolithic);
            }

            return scopes.ToArray();
        }

        private static HubeiParameterType MapParameterType(string ifcType)
        {
            switch (ifcType)
            {
                case "IfcBoolean":
                    return HubeiParameterType.YesNo;
                case "IfcInteger":
                    return HubeiParameterType.Integer;
                case "IfcReal":
                    return HubeiParameterType.Number;
                default:
                    return HubeiParameterType.Text;
            }
        }

        private static HubeiParameterType InferMiniParameterType(string name)
        {
            if (name.StartsWith("是否", StringComparison.Ordinal))
            {
                return HubeiParameterType.YesNo;
            }

            if (name.IndexOf("数量", StringComparison.Ordinal) >= 0 || name.IndexOf("户数", StringComparison.Ordinal) >= 0 || name.IndexOf("层数", StringComparison.Ordinal) >= 0 || name.IndexOf("人数", StringComparison.Ordinal) >= 0)
            {
                return HubeiParameterType.Integer;
            }

            if (name.IndexOf("面积", StringComparison.Ordinal) >= 0 || name.IndexOf("坐标", StringComparison.Ordinal) >= 0 || name.IndexOf("长度", StringComparison.Ordinal) >= 0 || name.IndexOf("宽度", StringComparison.Ordinal) >= 0 || name.IndexOf("高度", StringComparison.Ordinal) >= 0 || name.IndexOf("标高", StringComparison.Ordinal) >= 0 || name.IndexOf("率", StringComparison.Ordinal) >= 0 || name.IndexOf("系数", StringComparison.Ordinal) >= 0 || name.IndexOf("体积", StringComparison.Ordinal) >= 0)
            {
                return HubeiParameterType.Number;
            }

            return HubeiParameterType.Text;
        }

        private static IReadOnlyList<string> SplitColumns(string line)
        {
            return line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .ToArray();
        }

        private static string ReadEmbeddedText(string fileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("Resources." + fileName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new InvalidOperationException("未找到嵌入资源 " + fileName + "。");
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("无法读取嵌入资源 " + fileName + "。");
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
