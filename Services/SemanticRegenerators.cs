using System;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    internal static class SemanticNumber
    {
        public static double Get(ProjectElement element, string name, double fallback = 0d)
        {
            if (element.Properties.TryGetValue(name, out var value) &&
                double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) &&
                !double.IsNaN(result) && !double.IsInfinity(result))
                return result;
            return fallback;
        }
    }

    public sealed class WallRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var length = Positive(SemanticNumber.Get(element, "LengthM"));
            var height = Positive(SemanticNumber.Get(element, "HeightM"));
            var thickness = Positive(SemanticNumber.Get(element, "ThicknessM"));
            var grossArea = length * height;
            var linkedOpeningArea = project.Elements
                .Where(x => (x.Category == ElementCategory.WallOpening || x.Category == ElementCategory.Door) &&
                            x.Properties.TryGetValue("HostWallId", out var host) &&
                            string.Equals(host, element.Id, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Quantities.TryGetValue("OpeningAreaM2", out var area)
                    ? Positive(area)
                    : Positive(SemanticNumber.Get(x, "WidthM")) * Positive(SemanticNumber.Get(x, "HeightM")));
            var explicitOpeningArea = Positive(SemanticNumber.Get(element, "OpeningAreaM2"));
            var openingArea = Clamp(Math.Max(explicitOpeningArea, linkedOpeningArea), 0d, grossArea);
            var netArea = grossArea - openingArea;

            element.SetQuantity("LengthM", length);
            element.SetQuantity("GrossWallAreaM2", grossArea);
            element.SetQuantity("OpeningAreaM2", openingArea);
            element.SetQuantity("NetWallAreaM2", netArea);
            element.SetQuantity("GrossVolumeM3", grossArea * thickness);
            element.SetQuantity("NetVolumeM3", netArea * thickness);
        }

        private static double Positive(double value) => value > 0d && !double.IsNaN(value) && !double.IsInfinity(value) ? value : 0d;
        private static double Clamp(double value, double minimum, double maximum) => Math.Max(minimum, Math.Min(maximum, value));
    }

    public sealed class RoomRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.Room || category == ElementCategory.FloorFinish || category == ElementCategory.Waterproofing || category == ElementCategory.Skirting || category == ElementCategory.WallFinish || category == ElementCategory.CeilingFinish;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var area = Positive(SemanticNumber.Get(element, "AreaM2"));
            var perimeter = Positive(SemanticNumber.Get(element, "PerimeterM"));
            var height = Positive(SemanticNumber.Get(element, "HeightM"));
            var openings = Positive(SemanticNumber.Get(element, "OpeningAreaM2"));
            var doorWidth = Positive(SemanticNumber.Get(element, "DoorWidthM"));

            element.SetQuantity("AreaM2", area);
            element.SetQuantity("PerimeterM", perimeter);
            if (element.Category == ElementCategory.WallFinish) element.SetQuantity("NetFinishAreaM2", Math.Max(0d, perimeter * height - openings));
            if (element.Category == ElementCategory.Skirting) element.SetQuantity("SkirtingLengthM", Math.Max(0d, perimeter - doorWidth));
        }

        private static double Positive(double value) => value > 0d && !double.IsNaN(value) && !double.IsInfinity(value) ? value : 0d;
    }

    public sealed class OpeningRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.WallOpening || category == ElementCategory.Door;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var width = Positive(SemanticNumber.Get(element, "WidthM"));
            var height = Positive(SemanticNumber.Get(element, "HeightM"));
            var area = width * height;
            element.SetQuantity("OpeningAreaM2", area);
            element.SetQuantity("Count", 1d);
            if (element.Properties.TryGetValue("HostWallId", out var hostId) && !string.IsNullOrWhiteSpace(hostId)) project.FindElement(hostId)?.MarkDirty(ElementDirtyFlags.Quantity);
        }

        private static double Positive(double value) => value > 0d && !double.IsNaN(value) && !double.IsInfinity(value) ? value : 0d;
    }
}
