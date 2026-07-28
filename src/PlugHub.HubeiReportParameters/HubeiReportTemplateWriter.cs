using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlugHub.HubeiReportParameters
{
    public static class HubeiReportTemplateWriter
    {
        public static string BuildHifcText(IReadOnlyCollection<HubeiReportTemplateRow> rows)
        {
            var builder = new StringBuilder();
            foreach (IGrouping<string, HubeiReportTemplateRow> group in rows
                .GroupBy(row => row.PropertySetName + "|" + row.BindingKind + "|" + row.IfcEntityName, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                HubeiReportTemplateRow first = group.First();
                builder.Append("PropertySet:\t").Append(first.PropertySetName).Append("\t").Append(first.BindingKind).Append("\t").Append(first.IfcEntityName).AppendLine();
                foreach (HubeiReportTemplateRow row in group.OrderBy(row => row.Name, StringComparer.Ordinal))
                {
                    builder.Append("    ").Append(row.Name).Append("\t").Append(row.IfcDataType).Append("\t").Append(row.Name).AppendLine();
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
