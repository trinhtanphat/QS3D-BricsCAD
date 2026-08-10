using System;
using System.Globalization;
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

            var length = QuantityMath.Positive(SemanticNumber.Get(element, "LengthM"));
            var height = QuantityMath.Positive(SemanticNumber.Get(element, "HeightM"));
            var thickness = QuantityMath.Positive(SemanticNumber.Get(element, "ThicknessM"));
            var grossArea = QuantityMath.Multiply(length, height, element.Id + "/gross wall area");
            var linkedOpeningArea = LinkedOpeningArea(project, element);
            var explicitOpeningArea = QuantityMath.Positive(SemanticNumber.Get(element, "OpeningAreaM2"));
            var requestedOpeningArea = Math.Max(explicitOpeningArea, linkedOpeningArea);
            var openingArea = QuantityMath.Clamp(requestedOpeningArea, 0d, grossArea, element.Id + "/opening area");
            var netArea = QuantityMath.SubtractFloorZero(grossArea, openingArea, element.Id + "/net wall area");
            var grossVolume = QuantityMath.Multiply(grossArea, thickness, element.Id + "/gross wall volume");
            var netVolume = QuantityMath.Multiply(netArea, thickness, element.Id + "/net wall volume");

            element.SetQuantity("LengthM", length);
            element.SetQuantity("GrossWallAreaM2", grossArea);
            element.SetQuantity("OpeningAreaM2", openingArea);
            element.SetQuantity("NetWallAreaM2", netArea);
            element.SetQuantity("GrossVolumeM3", grossVolume);
            element.SetQuantity("NetVolumeM3", netVolume);
        }

        private static double LinkedOpeningArea(ProjectState project, ProjectElement wall)
        {
            var total = 0d;
            foreach (var child in project.Elements)
            {
                if (child.Category != ElementCategory.WallOpening && child.Category != ElementCategory.Door) continue;
                if (!child.Properties.TryGetValue("HostWallId", out var host) || !string.Equals(host, wall.Id, StringComparison.OrdinalIgnoreCase)) continue;
                double area;
                if (child.Quantities.TryGetValue("OpeningAreaM2", out var stored)) area = QuantityMath.Positive(stored);
                else
                {
                    var width = QuantityMath.Positive(SemanticNumber.Get(child, "WidthM"));
                    var height = QuantityMath.Positive(SemanticNumber.Get(child, "HeightM"));
                    area = QuantityMath.Multiply(width, height, child.Id + "/opening area");
                }
                total = QuantityMath.Add(total, area, wall.Id + "/linked opening area");
            }
            return total;
        }
    }

    public sealed class RoomRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.Room || category == ElementCategory.FloorFinish || category == ElementCategory.Waterproofing || category == ElementCategory.Skirting || category == ElementCategory.WallFinish || category == ElementCategory.CeilingFinish;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var area = QuantityMath.Positive(SemanticNumber.Get(element, "AreaM2"));
            var perimeter = QuantityMath.Positive(SemanticNumber.Get(element, "PerimeterM"));
            var height = QuantityMath.Positive(SemanticNumber.Get(element, "HeightM"));
            var openings = QuantityMath.Positive(SemanticNumber.Get(element, "OpeningAreaM2"));
            var doorWidth = QuantityMath.Positive(SemanticNumber.Get(element, "DoorWidthM"));

            double? netFinishArea = null;
            double? skirtingLength = null;
            if (element.Category == ElementCategory.WallFinish)
            {
                var grossFinishArea = QuantityMath.Multiply(perimeter, height, element.Id + "/gross finish area");
                netFinishArea = QuantityMath.SubtractFloorZero(grossFinishArea, openings, element.Id + "/net finish area");
            }
            if (element.Category == ElementCategory.Skirting)
                skirtingLength = QuantityMath.SubtractFloorZero(perimeter, doorWidth, element.Id + "/skirting length");

            element.SetQuantity("AreaM2", area);
            element.SetQuantity("PerimeterM", perimeter);
            if (netFinishArea.HasValue) element.SetQuantity("NetFinishAreaM2", netFinishArea.Value);
            if (skirtingLength.HasValue) element.SetQuantity("SkirtingLengthM", skirtingLength.Value);
        }
    }

    public sealed class OpeningRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.WallOpening || category == ElementCategory.Door;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var width = QuantityMath.Positive(SemanticNumber.Get(element, "WidthM"));
            var height = QuantityMath.Positive(SemanticNumber.Get(element, "HeightM"));
            var area = QuantityMath.Multiply(width, height, element.Id + "/opening area");

            element.SetQuantity("OpeningAreaM2", area);
            element.SetQuantity("Count", 1d);
            if (element.Properties.TryGetValue("HostWallId", out var hostId) && !string.IsNullOrWhiteSpace(hostId)) project.FindElement(hostId)?.MarkDirty(ElementDirtyFlags.Quantity);
        }
    }
}
