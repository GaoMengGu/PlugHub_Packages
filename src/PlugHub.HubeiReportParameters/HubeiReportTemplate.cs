using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Microsoft.VisualBasic.FileIO;

namespace PlugHub.HubeiReportParameters
{
    public static class HubeiReportTemplateReader
    {
        private static readonly string[] Headers =
        {
            "属性集名称", "参数类型", "IFC构件", "Revit构件", "属性名称", "属性类型", "默认值", "真实数据"
        };

        public static HubeiReportTemplate Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new InvalidOperationException("请选择存在的 CSV 模板文件。");
            }

            var rows = new List<HubeiReportTemplateRow>();
            using (var parser = new TextFieldParser(filePath, Encoding.UTF8, true))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                string[] header = parser.ReadFields();
                if (header == null || header.Length != Headers.Length || !header.SequenceEqual(Headers, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException("CSV 表头必须为：" + string.Join(",", Headers));
                }

                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    int rowNumber = checked((int)parser.LineNumber - 1);
                    if (fields == null || fields.All(field => string.IsNullOrWhiteSpace(field)))
                    {
                        continue;
                    }

                    if (fields.Length != Headers.Length)
                    {
                        throw new InvalidOperationException("第 " + rowNumber + " 行必须包含 " + Headers.Length + " 列。");
                    }

                    rows.Add(CreateRow(fields, rowNumber));
                }
            }

            if (rows.Count == 0)
            {
                throw new InvalidOperationException("CSV 模板中没有参数行。");
            }

            ValidateDuplicateNames(rows);
            return new HubeiReportTemplate { FilePath = filePath, Rows = rows };
        }

        public static IReadOnlyList<HubeiReportTemplateRow> MergeParameterRows(IReadOnlyCollection<HubeiReportTemplateRow> rows)
        {
            return rows.GroupBy(row => row.Name, StringComparer.Ordinal)
                .Select(group =>
                {
                    HubeiReportTemplateRow first = group.First();
                    return new HubeiReportTemplateRow
                    {
                        RowNumber = first.RowNumber,
                        PropertySetName = first.PropertySetName,
                        BindingKind = first.BindingKind,
                        IfcEntityName = first.IfcEntityName,
                        RevitCategories = group.SelectMany(row => row.RevitCategories).Distinct().ToArray(),
                        Name = first.Name,
                        ParameterType = first.ParameterType,
                        DefaultValue = first.DefaultValue,
                        ActualValue = first.ActualValue
                    };
                })
                .OrderBy(row => row.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static HubeiReportTemplateRow CreateRow(IReadOnlyList<string> fields, int rowNumber)
        {
            string bindingKind = Required(fields[1], rowNumber, "参数类型").ToUpperInvariant();
            if (bindingKind != "I" && bindingKind != "T")
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 参数类型 必须为 I 或 T。");
            }

            string typeName = Required(fields[5], rowNumber, "属性类型");
            if (!Enum.TryParse(typeName, true, out ParameterType parameterType) || !Enum.IsDefined(typeof(ParameterType), parameterType) || parameterType == ParameterType.Invalid)
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 属性类型不是当前 Revit 支持的 ParameterType：" + typeName + "。");
            }

            return new HubeiReportTemplateRow
            {
                RowNumber = rowNumber,
                PropertySetName = Required(fields[0], rowNumber, "属性集名称"),
                BindingKind = bindingKind,
                IfcEntityName = Required(fields[2], rowNumber, "IFC构件"),
                RevitCategories = RevitCategoryCatalog.Parse(fields[3], rowNumber),
                Name = Required(fields[4], rowNumber, "属性名称"),
                ParameterType = parameterType,
                DefaultValue = fields[6] ?? string.Empty,
                ActualValue = fields[7] ?? string.Empty
            };
        }

        private static void ValidateDuplicateNames(IReadOnlyCollection<HubeiReportTemplateRow> rows)
        {
            foreach (IGrouping<string, HubeiReportTemplateRow> group in rows.GroupBy(row => row.Name, StringComparer.Ordinal))
            {
                HubeiReportTemplateRow first = group.First();
                if (group.Any(row => row.BindingKind != first.BindingKind || row.ParameterType != first.ParameterType || row.DefaultValue != first.DefaultValue || row.ActualValue != first.ActualValue))
                {
                    throw new InvalidOperationException("同名参数 " + first.Name + " 的参数类型、属性类型、默认值和真实数据必须一致，请修改模板后重试。");
                }
            }
        }

        private static string Required(string value, int rowNumber, string columnName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 " + columnName + " 不能为空。");
            }

            return value.Trim();
        }
    }
}
