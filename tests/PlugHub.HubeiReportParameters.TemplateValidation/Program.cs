using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

internal static class Program
{
    private static readonly string[] Headers =
    {
        "属性集名称", "参数类型", "IFC构件", "Revit类别", "属性名称", "IFC属性类型", "Revit参数类型", "默认值", "真实数据"
    };

    private static readonly HashSet<string> IfcDataTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

    private static readonly HashSet<string> Revit2020ParameterTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Text", "Integer", "Number", "Length", "Area", "Volume", "Angle", "URL", "Material", "YesNo", "Force", "LinearForce", "AreaForce", "Moment",
        "NumberOfPoles", "FixtureUnit", "FamilyType", "LoadClassification", "Image", "MultilineText", "HVACDensity", "HVACEnergy", "HVACFriction", "HVACPower",
        "HVACPowerDensity", "HVACPressure", "HVACTemperature", "HVACVelocity", "HVACAirflow", "HVACDuctSize", "HVACCrossSection", "HVACHeatGain",
        "ElectricalCurrent", "ElectricalPotential", "ElectricalFrequency", "ElectricalIlluminance", "ElectricalLuminousFlux", "ElectricalPower", "HVACRoughness",
        "ElectricalApparentPower", "ElectricalPowerDensity", "PipingDensity", "PipingFlow", "PipingFriction", "PipingPressure", "PipingTemperature",
        "PipingVelocity", "PipingViscosity", "PipeSize", "PipingRoughness", "Stress", "UnitWeight", "ThermalExpansion", "LinearMoment",
        "ForcePerLength", "ForceLengthPerAngle", "LinearForcePerLength", "LinearForceLengthPerAngle", "AreaForcePerLength", "PipingVolume",
        "HVACViscosity", "HVACCoefficientOfHeatTransfer", "HVACAirflowDensity", "Slope", "HVACCoolingLoad", "HVACCoolingLoadDividedByArea",
        "HVACCoolingLoadDividedByVolume", "HVACHeatingLoad", "HVACHeatingLoadDividedByArea", "HVACHeatingLoadDividedByVolume", "HVACAirflowDividedByVolume",
        "HVACAirflowDividedByCoolingLoad", "HVACAreaDividedByCoolingLoad", "WireSize", "HVACSlope", "PipingSlope", "Currency",
        "ElectricalEfficacy", "ElectricalWattage", "ColorTemperature", "ElectricalLuminousIntensity", "ElectricalLuminance", "HVACAreaDividedByHeatingLoad",
        "HVACFactor", "ElectricalTemperature", "ElectricalCableTraySize", "ElectricalConduitSize", "ReinforcementVolume", "ReinforcementLength",
        "ElectricalDemandFactor", "HVACDuctInsulationThickness", "HVACDuctLiningThickness", "PipeInsulationThickness", "HVACThermalResistance",
        "HVACThermalMass", "Acceleration", "BarDiameter", "CrackWidth", "DisplacementDeflection", "Energy", "StructuralFrequency", "Mass",
        "MassPerUnitLength", "MomentOfInertia", "SurfaceArea", "Period", "Pulsation", "ReinforcementArea", "ReinforcementAreaPerUnitLength",
        "ReinforcementCover", "ReinforcementSpacing", "Rotation", "SectionArea", "SectionDimension", "SectionModulus", "SectionProperty",
        "StructuralVelocity", "WarpingConstant", "Weight", "WeightPerUnitLength", "HVACThermalConductivity", "HVACSpecificHeat",
        "HVACSpecificHeatOfVaporization", "HVACPermeability", "ElectricalResistivity", "MassDensity", "MassPerUnitArea", "PipeDimension",
        "PipeMass", "PipeMassPerUnitLength", "HVACTemperatureDifference", "PipingTemperatureDifference", "ElectricalTemperatureDifference",
        "TimeInterval", "Speed"
    };

    private static int Main(string[] arguments)
    {
        if (arguments.Length != 2)
        {
            Console.Error.WriteLine("Usage: HubeiReportTemplateValidation <single-template.csv> <site-template.csv>");
            return 2;
        }

        try
        {
            Validate(arguments[0]);
            Validate(arguments[1]);
            Console.WriteLine("Hubei report CSV template validation passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void Validate(string path)
    {
        List<Row> rows = ReadRows(path);
        ValidateDefinitions(rows, path);
        ValidateValues(rows, path);
        string hifcText = BuildHifcText(rows);
        foreach (Row row in rows)
        {
            string expectedProperty = "    " + row.Name + "\t" + row.IfcDataType + "\t" + row.Name;
            if (!hifcText.Contains(expectedProperty))
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " did not preserve IFC data type for " + row.Name + ".");
            }
        }
    }

    private static List<Row> ReadRows(string path)
    {
        List<string[]> records = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
        if (records.Count == 0 || records[0].Length != Headers.Length || !records[0].SequenceEqual(Headers, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(Path.GetFileName(path) + " must use the nine-column template header.");
        }

        var rows = new List<Row>();
        for (int index = 1; index < records.Count; index++)
        {
            string[] fields = records[index];
            if (fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Length != Headers.Length)
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " row " + (index + 1) + " must have nine columns.");
            }

            string ifcDataType = NormalizeIfcDataType(fields[5]);
            if (!IfcDataTypes.Contains(ifcDataType))
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " row " + (index + 1) + " has unsupported IFC data type " + fields[5] + ".");
            }

            string revitParameterType = fields[6].Trim();
            if (!Revit2020ParameterTypes.Contains(revitParameterType))
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " row " + (index + 1) + " has unsupported Revit 2020 parameter type " + fields[6] + ".");
            }

            rows.Add(new Row(index + 1, fields[0].Trim(), fields[1].Trim().ToUpperInvariant(), fields[2].Trim(), SplitCategories(fields[3]), fields[4].Trim(), ifcDataType, revitParameterType, fields[7], fields[8]));
        }

        return rows;
    }

    private static void ValidateDefinitions(IReadOnlyCollection<Row> rows, string path)
    {
        foreach (IGrouping<string, Row> group in rows.GroupBy(row => row.Name, StringComparer.Ordinal))
        {
            Row first = group.First();
            if (first.BindingKind != "I" && first.BindingKind != "T")
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " row " + first.RowNumber + " must use I or T.");
            }

            if (group.Any(row => row.BindingKind != first.BindingKind || !string.Equals(row.RevitParameterType, first.RevitParameterType, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " has incompatible shared parameter definitions for " + first.Name + ".");
            }

            if (group.SelectMany(row => row.RevitCategories).Distinct(StringComparer.Ordinal).Count() == 0)
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " must bind " + first.Name + " to at least one Revit category.");
            }
        }
    }

    private static void ValidateValues(IReadOnlyCollection<Row> rows, string path)
    {
        foreach (var group in rows.SelectMany(row => row.RevitCategories.Select(category => new { row, category }))
            .GroupBy(item => item.row.Name + "|" + item.category, StringComparer.Ordinal))
        {
            if (group.Select(item => item.row.Value).Distinct(StringComparer.Ordinal).Count() > 1)
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " assigns conflicting values to " + group.First().row.Name + " in " + group.First().category + ".");
            }
        }
    }

    private static string BuildHifcText(IReadOnlyCollection<Row> rows)
    {
        var builder = new StringBuilder();
        foreach (IGrouping<string, Row> group in rows.GroupBy(row => row.PropertySetName + "|" + row.BindingKind + "|" + row.IfcEntityName, StringComparer.Ordinal))
        {
            Row first = group.First();
            builder.Append("PropertySet:\t").Append(first.PropertySetName).Append("\t").Append(first.BindingKind).Append("\t").Append(first.IfcEntityName).AppendLine();
            foreach (Row row in group)
            {
                builder.Append("    ").Append(row.Name).Append("\t").Append(row.IfcDataType).Append("\t").Append(row.Name).AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string NormalizeIfcDataType(string value)
    {
        string trimmed = value.Trim();
        return trimmed.StartsWith("Ifc", StringComparison.OrdinalIgnoreCase) ? trimmed.Substring(3) : trimmed;
    }

    private static IReadOnlyCollection<string> SplitCategories(string value)
    {
        return value.Split(',').Select(item => item.Trim()).Where(item => item.Length > 0).ToArray();
    }

    private static List<string[]> ParseCsv(string text)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        bool isQuoted = false;
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            if (isQuoted)
            {
                if (current == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (current == '"')
                {
                    isQuoted = false;
                }
                else
                {
                    field.Append(current);
                }

                continue;
            }

            if (current == '"' && field.Length == 0)
            {
                isQuoted = true;
            }
            else if (current == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else if (current == '\r' || current == '\n')
            {
                if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                fields.Add(field.ToString());
                records.Add(fields.ToArray());
                fields.Clear();
                field.Clear();
            }
            else
            {
                field.Append(current);
            }
        }

        if (isQuoted)
        {
            throw new InvalidOperationException("CSV contains an unterminated quoted value.");
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            records.Add(fields.ToArray());
        }

        return records;
    }

    private sealed class Row
    {
        public Row(int rowNumber, string propertySetName, string bindingKind, string ifcEntityName, IReadOnlyCollection<string> revitCategories, string name, string ifcDataType, string revitParameterType, string defaultValue, string actualValue)
        {
            RowNumber = rowNumber;
            PropertySetName = propertySetName;
            BindingKind = bindingKind;
            IfcEntityName = ifcEntityName;
            RevitCategories = revitCategories;
            Name = name;
            IfcDataType = ifcDataType;
            RevitParameterType = revitParameterType;
            DefaultValue = defaultValue;
            ActualValue = actualValue;
        }

        public int RowNumber { get; }
        public string PropertySetName { get; }
        public string BindingKind { get; }
        public string IfcEntityName { get; }
        public IReadOnlyCollection<string> RevitCategories { get; }
        public string Name { get; }
        public string IfcDataType { get; }
        public string RevitParameterType { get; }
        public string DefaultValue { get; }
        public string ActualValue { get; }
        public string Value => string.IsNullOrWhiteSpace(ActualValue) ? DefaultValue : ActualValue;
    }
}
