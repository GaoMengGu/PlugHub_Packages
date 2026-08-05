using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace PlugHub.HubeiReportParameters
{
    public static class HubeiReportTemplateReader
    {
        private static readonly string[] Headers =
        {
            "属性集名称", "参数类型", "IFC构件", "Revit类别", "属性名称", "IFC属性类型", "Revit参数类型", "默认值", "真实数据"
        };

        public static HubeiReportTemplate Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new InvalidOperationException("请选择存在的 CSV 模板文件。");
            }

            var rows = new List<HubeiReportTemplateRow>();
            foreach (CsvRecord record in ReadCsvRecords(filePath))
            {
                if (record.RowNumber == 1)
                {
                    if (record.Fields.Length != Headers.Length || !record.Fields.SequenceEqual(Headers, StringComparer.Ordinal))
                    {
                        throw new InvalidOperationException("CSV 表头必须为：" + string.Join(",", Headers));
                    }

                    continue;
                }

                if (record.Fields.All(field => string.IsNullOrWhiteSpace(field)))
                {
                    continue;
                }

                if (record.Fields.Length != Headers.Length)
                {
                    throw new InvalidOperationException("第 " + record.RowNumber + " 行必须包含 " + Headers.Length + " 列。");
                }

                rows.Add(CreateRow(record.Fields, record.RowNumber));
            }

            if (rows.Count == 0)
            {
                throw new InvalidOperationException("CSV 模板中没有参数行。");
            }

            return new HubeiReportTemplate { FilePath = filePath, Rows = rows };
        }

        public static void PrepareForDocument(HubeiReportTemplate template, Document document, HubeiReportValueMode valueMode)
        {
            if (template == null || document == null)
            {
                throw new InvalidOperationException("未收到模板或 Revit 项目。");
            }

            RevitCategoryCatalog.Resolve(document, template.Rows);
            ResolveRevitParameterNames(template.Rows);
            ValidateDuplicateNames(template.Rows, valueMode);
        }

        public static IReadOnlyList<HubeiReportTemplateRow> MergeParameterRows(IReadOnlyCollection<HubeiReportTemplateRow> rows)
        {
            return rows.GroupBy(row => row.RevitParameterName, StringComparer.Ordinal)
                .Select(group =>
                {
                    HubeiReportTemplateRow first = group.First();
                    return new HubeiReportTemplateRow
                    {
                        RowNumber = first.RowNumber,
                        PropertySetName = first.PropertySetName,
                        BindingKind = first.BindingKind,
                        IfcEntityName = first.IfcEntityName,
                        RevitCategoryNames = group.SelectMany(row => row.RevitCategoryNames).Distinct(StringComparer.Ordinal).ToArray(),
                        RevitCategories = group.SelectMany(row => row.RevitCategories)
                            .GroupBy(category => category.Id.IntegerValue)
                            .Select(categoryGroup => categoryGroup.First())
                            .ToArray(),
                        Name = first.Name,
                        RevitParameterName = first.RevitParameterName,
                        IfcDataType = first.IfcDataType,
                        RevitParameterType = first.RevitParameterType,
                        DefaultValue = first.DefaultValue,
                        ActualValue = first.ActualValue
                    };
                })
                .OrderBy(row => row.RevitParameterName, StringComparer.Ordinal)
                .ToArray();
        }

        private static HubeiReportTemplateRow CreateRow(IReadOnlyList<string> fields, int rowNumber)
        {
            string bindingKind = Required(fields[1], rowNumber, "参数类型").ToUpperInvariant();
            if (bindingKind != "I" && bindingKind != "T")
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 参数类型 必须为 I 或 T。");
            }

            string ifcDataType = ValidateIfcDataType(Required(fields[5], rowNumber, "IFC属性类型"), rowNumber);

            string typeName = Required(fields[6], rowNumber, "Revit参数类型");
            if (!Enum.TryParse(typeName, true, out ParameterType revitParameterType) || !Enum.IsDefined(typeof(ParameterType), revitParameterType) || revitParameterType == ParameterType.Invalid)
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 Revit参数类型不是当前 Revit 支持的 ParameterType：" + typeName + "。");
            }

            return new HubeiReportTemplateRow
            {
                RowNumber = rowNumber,
                PropertySetName = Required(fields[0], rowNumber, "属性集名称"),
                BindingKind = bindingKind,
                IfcEntityName = Required(fields[2], rowNumber, "IFC构件"),
                RevitCategoryNames = RevitCategoryCatalog.ParseNames(fields[3], rowNumber),
                Name = Required(fields[4], rowNumber, "属性名称"),
                RevitParameterName = Required(fields[4], rowNumber, "属性名称"),
                IfcDataType = ifcDataType,
                RevitParameterType = revitParameterType,
                DefaultValue = fields[7] ?? string.Empty,
                ActualValue = fields[8] ?? string.Empty
            };
        }

        private static void ValidateDuplicateNames(IReadOnlyCollection<HubeiReportTemplateRow> rows, HubeiReportValueMode valueMode)
        {
            foreach (IGrouping<string, HubeiReportTemplateRow> group in rows.GroupBy(row => row.RevitParameterName, StringComparer.Ordinal))
            {
                HubeiReportTemplateRow first = group.First();
                if (group.Any(row => row.BindingKind != first.BindingKind || row.RevitParameterType != first.RevitParameterType))
                {
                    throw new InvalidOperationException("Revit 参数 " + first.RevitParameterName + " 的参数类型或 Revit参数类型不一致，请修改模板后重试。");
                }
            }

            if (valueMode == HubeiReportValueMode.None)
            {
                return;
            }

            foreach (var group in rows
                .SelectMany(row => row.RevitCategories.Select(category => new { row, category }))
                .GroupBy(item => item.row.RevitParameterName + "|" + item.category, StringComparer.Ordinal))
            {
                string[] values = group
                    .Where(item => item.row.HasValue(valueMode))
                    .Select(item => item.row.GetValue(valueMode))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                if (values.Length > 1)
                {
                    HubeiReportTemplateRow first = group.First().row;
                    string valueKind = valueMode == HubeiReportValueMode.ActualValue
                        ? "非空真实数据"
                        : "默认值";
                    throw new InvalidOperationException("Revit 参数 " + first.RevitParameterName + " 在同一 Revit类别中存在不同的" + valueKind + "，请修改模板后重试。");
                }
            }
        }

        private static void ResolveRevitParameterNames(IReadOnlyCollection<HubeiReportTemplateRow> rows)
        {
            foreach (HubeiReportTemplateRow row in rows)
            {
                row.RevitParameterName = row.PropertySetName + "_" + row.Name;
            }
        }

        private static string ValidateIfcDataType(string value, int rowNumber)
        {
            string typeName = value.StartsWith("Ifc", StringComparison.OrdinalIgnoreCase) ? value.Substring(3) : value;
            string[] supportedTypes =
            {
                "Acceleration", "AngularVelocity", "Area", "AreaDensity", "Boolean", "ClassificationReference", "ColorTemperature", "Count", "Currency",
                "DynamicViscosity", "ElectricCurrent", "ElectricVoltage", "Energy", "ElectricalEfficacy", "Force", "Frequency", "HeatFluxDensity",
                "HeatingValue", "Identifier", "Illuminance", "Integer", "IonConcentration", "IsothermalMoistureCapacity", "Label", "Length",
                "LinearForce", "LinearMoment", "LinearStiffness", "LinearVelocity", "Logical", "LuminousFlux", "LuminousIntensity", "Mass", "MassDensity", "MassFlowRate", "MassPerLength",
                "ModulusOfElasticity", "MoistureDiffusivity", "MomentOfInertia", "NormalisedRatio", "Numeric", "PlanarForce", "PlaneAngle",
                "PositiveLength", "PositivePlaneAngle", "PositiveRatio", "Power", "Pressure", "Ratio", "Real", "RotationalFrequency",
                "SoundPower", "SoundPressure", "SpecificHeatCapacity", "Text", "ThermalConductivity", "ThermalExpansionCoefficient", "ThermalResistance",
                "ThermalTransmittance", "ThermodynamicTemperature", "Time", "Torque", "VaporPermeability", "Volume", "VolumetricFlowRate", "WarpingConstant"
            };

            if (!supportedTypes.Contains(typeName, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 IFC属性类型不受 IFC Exporter 支持：" + value + "。");
            }

            return value.Trim();
        }

        private static string Required(string value, int rowNumber, string columnName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 " + columnName + " 不能为空。");
            }

            return value.Trim();
        }

        private static IReadOnlyList<CsvRecord> ReadCsvRecords(string filePath)
        {
            string text = File.ReadAllText(filePath, Encoding.UTF8);
            var records = new List<CsvRecord>();
            var fields = new List<string>();
            var field = new StringBuilder();
            bool isQuoted = false;
            int rowNumber = 1;
            int recordRowNumber = 1;

            for (int index = 0; index < text.Length; index++)
            {
                char current = text[index];
                if (isQuoted)
                {
                    if (current == '"')
                    {
                        if (index + 1 < text.Length && text[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else
                        {
                            isQuoted = false;
                        }
                    }
                    else
                    {
                        field.Append(current);
                        if (current == '\n')
                        {
                            rowNumber++;
                        }
                    }

                    continue;
                }

                if (current == '"' && field.Length == 0)
                {
                    isQuoted = true;
                    continue;
                }

                if (current == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if (current == '\r' || current == '\n')
                {
                    if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    fields.Add(field.ToString());
                    records.Add(new CsvRecord(recordRowNumber, fields.ToArray()));
                    fields.Clear();
                    field.Clear();
                    rowNumber++;
                    recordRowNumber = rowNumber;
                    continue;
                }

                field.Append(current);
            }

            if (isQuoted)
            {
                throw new InvalidOperationException("CSV 模板存在未闭合的双引号。");
            }

            if (field.Length > 0 || fields.Count > 0)
            {
                fields.Add(field.ToString());
                records.Add(new CsvRecord(recordRowNumber, fields.ToArray()));
            }

            return records;
        }

        private sealed class CsvRecord
        {
            public CsvRecord(int rowNumber, string[] fields)
            {
                RowNumber = rowNumber;
                Fields = fields;
            }

            public int RowNumber { get; }

            public string[] Fields { get; }
        }
    }
}
