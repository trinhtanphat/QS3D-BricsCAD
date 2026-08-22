using System;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CadGeometryGuard
    {
        public static double Number(ProjectElement element, ProjectFamily? family, string name, double fallback)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Property name is required.", nameof(name));

            if (element.Properties.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                return ParseFinite(value, element.Id + "/" + name);
            if (family != null && family.Properties.TryGetValue(name, out value) && !string.IsNullOrWhiteSpace(value))
                return ParseFinite(value, "family " + family.Id + "/" + name);
            return Finite(fallback, "fallback " + name);
        }

        public static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (value <= 0d) throw new InvalidOperationException(label + " phải lớn hơn 0.");
            return value;
        }

        public static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException(label + " phải là số hữu hạn.");
            return value;
        }

        public static double ToDrawingUnits(Document document, double meters, string label)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            Finite(meters, label);
            return Finite(CadUnitService.MetersToDrawingUnits(document, meters), label + " (drawing units)");
        }

        public static double ToMeters(Document document, double drawingUnits, string label)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            Finite(drawingUnits, label);
            return Finite(CadUnitService.DrawingUnitsToMeters(document, drawingUnits), label + " (meters)");
        }

        public static double Hypot(double first, double second, string label)
        {
            first = Math.Abs(Finite(first, label + "/x"));
            second = Math.Abs(Finite(second, label + "/y"));
            var maximum = Math.Max(first, second);
            if (maximum <= 0d) return 0d;
            var minimum = Math.Min(first, second);
            var ratio = minimum / maximum;
            return Finite(maximum * Math.Sqrt(1d + ratio * ratio), label);
        }

        public static double Midpoint(double first, double second, string label)
        {
            first = Finite(first, label + "/first");
            second = Finite(second, label + "/second");
            return Finite(first / 2d + second / 2d, label);
        }

        public static double Add(double first, double second, string label)
        {
            first = Finite(first, label + "/first");
            second = Finite(second, label + "/second");
            return Finite(first + second, label);
        }

        private static double ParseFinite(string text, string label)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(label + " không phải số hữu hạn hợp lệ: " + text);
            return value;
        }
    }
}
