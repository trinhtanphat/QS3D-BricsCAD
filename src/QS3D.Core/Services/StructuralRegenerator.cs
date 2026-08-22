using System;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class StructuralRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Beam:
                case ElementCategory.Slab:
                case ElementCategory.Column:
                case ElementCategory.StructuralWall:
                case ElementCategory.Foundation:
                case ElementCategory.Stair:
                case ElementCategory.Railing:
                case ElementCategory.Earthwork:
                    return true;
                default:
                    return false;
            }
        }

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!CanRegenerate(element.Category)) throw new InvalidOperationException("Unsupported structural category: " + element.Category);

            switch (element.Category)
            {
                case ElementCategory.Beam: RegenerateBeam(element); break;
                case ElementCategory.Slab: RegenerateSlab(element); break;
                case ElementCategory.Column: RegenerateColumn(element); break;
                case ElementCategory.StructuralWall: RegenerateWall(project, element); break;
                case ElementCategory.Foundation: RegenerateFoundation(element); break;
                case ElementCategory.Stair: RegenerateStair(element); break;
                case ElementCategory.Railing: RegenerateRailing(element); break;
                case ElementCategory.Earthwork: RegenerateEarthwork(element); break;
            }
        }

        private static void RegenerateBeam(ProjectElement element)
        {
            var length = Positive(SemanticNumber.Get(element, "LengthM"));
            var width = Positive(SemanticNumber.Get(element, "WidthM"));
            var height = Positive(SemanticNumber.Get(element, "HeightM"));
            var gross = length * width * height;
            var deduction = Clamp(SemanticNumber.Get(element, "DeductionM3"), 0d, gross);
            element.SetQuantity("LengthM", length);
            element.SetQuantity("CrossSectionAreaM2", width * height);
            element.SetQuantity("GrossVolumeM3", gross);
            element.SetQuantity("DeductionM3", deduction);
            element.SetQuantity("NetVolumeM3", Math.Max(0d, gross - deduction));
            element.SetQuantity("FormworkM2", Math.Max(0d, (2d * height + width) * length));
        }

        private static void RegenerateSlab(ProjectElement element)
        {
            var grossArea = Positive(SemanticNumber.Get(element, "AreaM2"));
            var openingArea = Clamp(SemanticNumber.Get(element, "OpeningAreaM2"), 0d, grossArea);
            var thickness = Positive(SemanticNumber.Get(element, "ThicknessM"));
            var perimeter = Positive(SemanticNumber.Get(element, "PerimeterM"));
            var netArea = Math.Max(0d, grossArea - openingArea);
            element.SetQuantity("AreaM2", grossArea);
            element.SetQuantity("OpeningAreaM2", openingArea);
            element.SetQuantity("NetAreaM2", netArea);
            element.SetQuantity("GrossVolumeM3", grossArea * thickness);
            element.SetQuantity("DeductionM3", openingArea * thickness);
            element.SetQuantity("NetVolumeM3", netArea * thickness);
            element.SetQuantity("FormworkM2", netArea + perimeter * thickness);
        }

        private static void RegenerateColumn(ProjectElement element)
        {
            var width = Positive(SemanticNumber.Get(element, "WidthM"));
            var depth = Positive(SemanticNumber.Get(element, "DepthM", width));
            var height = Positive(SemanticNumber.Get(element, "HeightM"));
            var gross = width * depth * height;
            element.SetQuantity("HeightM", height);
            element.SetQuantity("CrossSectionAreaM2", width * depth);
            element.SetQuantity("GrossVolumeM3", gross);
            element.SetQuantity("NetVolumeM3", gross);
            element.SetQuantity("FormworkM2", 2d * (width + depth) * height);
        }

        private static void RegenerateWall(ProjectState project, ProjectElement element)
        {
            var length = Positive(SemanticNumber.Get(element, "LengthM"));
            var height = Positive(SemanticNumber.Get(element, "HeightM"));
            var thickness = Positive(SemanticNumber.Get(element, "ThicknessM"));
            var grossArea = length * height;
            var linkedOpeningArea = project.Elements
                .Where(x => (x.Category == ElementCategory.WallOpening || x.Category == ElementCategory.Door) &&
                            x.Properties.TryGetValue("HostWallId", out var host) &&
                            string.Equals(host, element.Id, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Quantities.TryGetValue("OpeningAreaM2", out var area)
                    ? Math.Max(0d, area)
                    : Positive(SemanticNumber.Get(x, "WidthM")) * Positive(SemanticNumber.Get(x, "HeightM")));
            var explicitOpeningArea = Positive(SemanticNumber.Get(element, "OpeningAreaM2"));
            var openingArea = Clamp(Math.Max(explicitOpeningArea, linkedOpeningArea), 0d, grossArea);
            var netArea = Math.Max(0d, grossArea - openingArea);
            element.SetQuantity("LengthM", length);
            element.SetQuantity("GrossWallAreaM2", grossArea);
            element.SetQuantity("OpeningAreaM2", openingArea);
            element.SetQuantity("NetWallAreaM2", netArea);
            element.SetQuantity("GrossVolumeM3", grossArea * thickness);
            element.SetQuantity("DeductionM3", openingArea * thickness);
            element.SetQuantity("NetVolumeM3", netArea * thickness);
            element.SetQuantity("FormworkM2", 2d * netArea);
        }

        private static void RegenerateFoundation(ProjectElement element)
        {
            var area = Positive(SemanticNumber.Get(element, "BaseAreaM2", SemanticNumber.Get(element, "AreaM2")));
            var thickness = Positive(SemanticNumber.Get(element, "ThicknessM", SemanticNumber.Get(element, "HeightM")));
            var perimeter = Positive(SemanticNumber.Get(element, "PerimeterM"));
            var gross = area * thickness;
            element.SetQuantity("AreaM2", area);
            element.SetQuantity("GrossVolumeM3", gross);
            element.SetQuantity("NetVolumeM3", gross);
            element.SetQuantity("FormworkM2", perimeter * thickness);
        }

        private static void RegenerateStair(ProjectElement element)
        {
            var area = Positive(SemanticNumber.Get(element, "AreaM2"));
            var thickness = Positive(SemanticNumber.Get(element, "ThicknessM"));
            var gross = area * thickness;
            element.SetQuantity("AreaM2", area);
            element.SetQuantity("GrossVolumeM3", gross);
            element.SetQuantity("NetVolumeM3", gross);
            element.SetQuantity("FormworkM2", area);
        }

        private static void RegenerateRailing(ProjectElement element)
        {
            var length = Positive(SemanticNumber.Get(element, "LengthM"));
            element.SetQuantity("LengthM", length);
            element.SetQuantity("Count", 1d);
        }

        private static void RegenerateEarthwork(ProjectElement element)
        {
            var area = Positive(SemanticNumber.Get(element, "ExcavationAreaM2", SemanticNumber.Get(element, "AreaM2")));
            var depth = Positive(SemanticNumber.Get(element, "DepthM"));
            var volume = area * depth;
            element.SetQuantity("AreaM2", area);
            element.SetQuantity("DepthM", depth);
            element.SetQuantity("GrossVolumeM3", volume);
            element.SetQuantity("NetVolumeM3", volume);
        }

        private static double Positive(double value) => value > 0d ? value : 0d;
        private static double Clamp(double value, double minimum, double maximum) => Math.Max(minimum, Math.Min(maximum, value));
    }

    public sealed class GenericTakeoffRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.CustomQuantity || category == ElementCategory.Grid;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!CanRegenerate(element.Category)) throw new InvalidOperationException("Unsupported takeoff category: " + element.Category);

            var length = Math.Max(0d, SemanticNumber.Get(element, "LengthM"));
            var area = Math.Max(0d, SemanticNumber.Get(element, "AreaM2"));
            element.SetQuantity("LengthM", length);
            element.SetQuantity("AreaM2", area);
            element.SetQuantity("Count", 1d);
        }
    }
}
