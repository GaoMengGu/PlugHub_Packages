namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public readonly struct ElevationPlane
    {
        public ElevationPlane(double a, double b, double c)
        {
            A = a;
            B = b;
            C = c;
        }

        public double A { get; }
        public double B { get; }
        public double C { get; }

        public double At(double x, double y)
        {
            return A * x + B * y + C;
        }

        public static ElevationPlane Constant(double elevationMillimeters)
        {
            return new ElevationPlane(0, 0, elevationMillimeters);
        }
    }
}
