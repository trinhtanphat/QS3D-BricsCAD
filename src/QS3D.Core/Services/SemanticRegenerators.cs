using System;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    internal static class SemanticNumber
    {
        public static double Get(ProjectElement element, string name, double fallback = 0d)
        {
            if (element.Properties.TryGetValue(name, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
            return fallback;
        }
    }

    public sealed class WallRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier;
        public void Regenerate(ProjectState project, ProjectElement element)
        {
            var length = SemanticNumber.Get(element, "LengthM");
            var height = SemanticNumber.Get(element, "HeightM");
            var thickness = SemanticNumber.Get(element, "ThicknessM");
            var openingArea = SemanticNumber.Get(element, "OpeningAreaM2");
            var grossArea = Math.Max(0d, length * height);
            var netArea = Math.Max(0d, grossArea - openingArea);
            element.SetQuantity("LengthM", length);
            element.SetQuantity("GrossWallAreaM2", grossArea);
            element.SetQuantity("OpeningAreaM2", Math.Max(0d, openingArea));
            element.SetQuantity("NetWallAreaM2", netArea);
            element.SetQuantity("GrossVolumeM3", Math.Max(0d, grossArea * thickness));
            element.SetQuantity("NetVolumeM3", Math.Max(0d, netArea * thickness));
        }
    }

    public sealed class RoomRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.Room || category == ElementCategory.FloorFinish || category == ElementCategory.Waterproofing || category == ElementCategory.Skirting || category == ElementCategory.WallFinish || category == ElementCategory.CeilingFinish;
        public void Regenerate(ProjectState project, ProjectElement element)
        {
            var area = SemanticNumber.Get(element, "AreaM2");
            var perimeter = SemanticNumber.Get(element, "PerimeterM");
            var height = SemanticNumber.Get(element, "HeightM");
            var openings = SemanticNumber.Get(element, "OpeningAreaM2");
            element.SetQuantity("AreaM2", Math.Max(0d, area));
            element.SetQuantity("PerimeterM", Math.Max(0d, perimeter));
            if (element.Category == ElementCategory.WallFinish)
                element.SetQuantity("NetFinishAreaM2", Math.Max(0d, perimeter * height - openings));
            if (element.Category == ElementCategory.Skirting)
                element.SetQuantity("SkirtingLengthM", Math.Max(0d, perimeter - SemanticNumber.Get(element, "DoorWidthM")));
        }
    }

    public sealed class OpeningRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.WallOpening || category == ElementCategory.Door;
        public void Regenerate(ProjectState project, ProjectElement element)
        {
            var width = SemanticNumber.Get(element, "WidthM");
            var height = SemanticNumber.Get(element, "HeightM");
            var area = Math.Max(0d, width * height);
            element.SetQuantity("OpeningAreaM2", area);
            element.SetQuantity("Count", 1d);
        }
    }
}
