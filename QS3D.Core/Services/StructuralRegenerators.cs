using System;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class StructuralRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.Beam || category == ElementCategory.Slab || category == ElementCategory.Column || category == ElementCategory.StructuralWall || category == ElementCategory.Foundation || category == ElementCategory.Earthwork;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project)); if (element == null) throw new ArgumentNullException(nameof(element)); StructuralQuantityResult result;
            switch (element.Category)
            {
                case ElementCategory.Beam:
                    result = StructuralQuantityCalculator.Beam(N(element, "LengthM"), N(element, "WidthM"), N(element, "HeightM"), N(element, "DeductionM3")); element.SetQuantity("LengthM", N(element, "LengthM")); break;
                case ElementCategory.Slab:
                    result = StructuralQuantityCalculator.Slab(N(element, "AreaM2"), N(element, "PerimeterM"), N(element, "ThicknessM"), N(element, "DeductionM3")); element.SetQuantity("BottomAreaM2", result.FootprintAreaM2); break;
                case ElementCategory.Column:
                    var columnArea = N(element, "AreaM2"); var columnPerimeter = N(element, "PerimeterM");
                    result = columnArea > 0d && columnPerimeter > 0d ? StructuralQuantityCalculator.FootprintPrism(columnArea, columnPerimeter, N(element, "HeightM"), N(element, "DeductionM3")) : StructuralQuantityCalculator.Column(N(element, "WidthM"), N(element, "DepthM"), N(element, "HeightM"), N(element, "DeductionM3")); break;
                case ElementCategory.StructuralWall:
                    result = StructuralQuantityCalculator.StructuralWall(N(element, "LengthM"), N(element, "HeightM"), N(element, "ThicknessM"), N(element, "DeductionM3")); element.SetQuantity("LengthM", N(element, "LengthM")); break;
                case ElementCategory.Foundation:
                    var area = N(element, "AreaM2"); result = area > 0d ? StructuralQuantityCalculator.FootprintPrism(area, N(element, "PerimeterM"), N(element, "HeightM"), N(element, "DeductionM3")) : StructuralQuantityCalculator.Foundation(N(element, "LengthM"), N(element, "WidthM"), N(element, "HeightM"), N(element, "DeductionM3")); break;
                case ElementCategory.Earthwork:
                    result = StructuralQuantityCalculator.Earthwork(N(element, "AreaM2"), N(element, "DepthM"), N(element, "SwellFactor")); element.SetQuantity("ExcavationVolumeM3", result.NetVolumeM3); element.SetQuantity("LooseExcavationVolumeM3", result.LooseVolumeM3); element.SetQuantity("FootprintAreaM2", result.FootprintAreaM2); element.SetQuantity("GrossVolumeM3", result.GrossVolumeM3); element.SetQuantity("NetVolumeM3", result.NetVolumeM3); return;
                default: throw new InvalidOperationException("Unsupported structural category: " + element.Category);
            }
            element.SetQuantity("GrossConcreteM3", result.GrossVolumeM3); element.SetQuantity("DeductionM3", result.DeductionM3); element.SetQuantity("NetConcreteM3", result.NetVolumeM3); element.SetQuantity("FormworkM2", result.FormworkM2); element.SetQuantity("FootprintAreaM2", result.FootprintAreaM2);
        }
        private static double N(ProjectElement element, string name) => SemanticNumber.Get(element, name);
    }
}
