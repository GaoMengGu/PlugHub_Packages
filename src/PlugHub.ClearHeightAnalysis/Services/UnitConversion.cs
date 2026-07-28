using Autodesk.Revit.DB;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class UnitConversion
    {
        private const double MillimetersPerFoot = 304.8;

        public static double FeetToMillimeters(double feet)
        {
            return feet * MillimetersPerFoot;
        }

        public static double MillimetersToFeet(double millimeters)
        {
            return millimeters / MillimetersPerFoot;
        }

        public static XYZ ToMillimeterPoint(XYZ point)
        {
            return new XYZ(FeetToMillimeters(point.X), FeetToMillimeters(point.Y), FeetToMillimeters(point.Z));
        }
    }
}
