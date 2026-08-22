using System;
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
                case ElementCategory.Beam: RegenerateBeam(project, element); break;
                case ElementCategory.Slab: RegenerateSlab(project, element); break;
                case ElementCategory.Column: RegenerateColumn(project, element); break;
                case ElementCategory.StructuralWall: RegenerateWall(project, element); break;
                case ElementCategory.Foundation: RegenerateFoundation(project, element); break;
                case ElementCategory.Stair: RegenerateStair(element); break;
                case ElementCategory.Railing: RegenerateRailing(element); break;
                case ElementCategory.Earthwork: RegenerateEarthwork(element); break;
            }
        }

        private static void RegenerateBeam(ProjectState project, ProjectElement element)
        {
            var length = QuantityMath.Positive(SemanticNumber.Get(element, "LengthM"));
            var width = QuantityMath.Positive(SemanticNumber.Get(element, "WidthM"));
            var legacyHeight = SemanticNumber.Get(element, "HeightM");
            var height = QuantityMath.Positive(QualifiedVerticalQuantity.EffectiveHeight(project, element, legacyHeight));
            var crossSection = QuantityMath.Multiply(width, height, element.Id + "/beam cross section");
            var gross = QuantityMath.Multiply(crossSection, length, element.Id + "/beam gross volume");
            var deduction = QuantityMath.Clamp(SemanticNumber.Get(element, "DeductionM3"), 0d, gross, element.Id + "/beam deduction");
            var net = QuantityMath.SubtractFloorZero(gross, deduction, element.Id + "/beam net volume");
            var doubleHeight = QuantityMath.Multiply(2d, height, element.Id + "/beam formwork height");
            var exposedPerimeter = QuantityMath.Add(doubleHeight, width, element.Id + "/beam exposed perimeter");
            var formwork = QuantityMath.Multiply(exposedPerimeter, length, element.Id + "/beam formwork");

            element.SetQuantity("LengthM", length);
            element.SetQuantity("CrossSectionAreaM2", crossSection);
            element.SetQuantity("GrossVolumeM3", gross);
            element.SetQuantity("DeductionM3", deduction);
            element.SetQuantity("NetVolumeM3", net);
            element.SetQuantity("FormworkM2", formwork);
        }

        private static void RegenerateSlab(ProjectState project, ProjectElement element)
        {
            var grossArea = QuantityMath.Positive(SemanticNumber.Get(element, "AreaM2"));
            var openingArea = QuantityMath.Clamp(SemanticNumber.Get(element, "OpeningAreaM2"), 0d, grossArea, element.Id + "/slab opening area");
            var legacyThickness = SemanticNumber.Get(element, "ThicknessM");
            var thickness = QuantityMath.Positive(QualifiedVerticalQuantity.EffectiveHeight(project, element, legacyThickness));
            var perimeter = QuantityMath.Positive(SemanticNumber.Get(element, "PerimeterM"));
            var netArea = QuantityMath.SubtractFloorZero(grossArea, openingArea, element.Id + "/slab net area");
            var grossVolume = QuantityMath.Multiply(grossArea, thickness, element.Id + "/slab gross volume");
            var deduction = QuantityMath.Multiply(openingArea, thickness, element.Id + "/slab deduction");
            var netVolume = QuantityMath.Multiply(netArea, thickness, element.Id + "/slab net volume");
            var edgeFormwork = QuantityMath.Multiply(perimeter, thickness, element.Id + "/slab edge formwork");
            var formwork = QuantityMath.Add(netArea, edgeFormwork, element.Id + "/slab formwork");

            element.SetQuantity("AreaM2", grossArea);
            element.SetQuantity("OpeningAreaM2", openingArea);
            element.SetQuantity("NetAreaM2", netArea);
            element.SetQuantity("GrossVolumeM3", grossVolume);
            element.SetQuantity("DeductionM3", deduction);
            element.SetQuantity("NetVolumeM3", netVolume);
            element.SetQuantity("FormworkM2", formwork);
        }

        private static void RegenerateColumn(ProjectState project, ProjectElement element)
        {
            var width = QuantityMath.Positive(SemanticNumber.Get(element, "WidthM"));
            var depth = QuantityMath.Positive(SemanticNumber.Get(element, "DepthM", width));
            var legacyHeight = SemanticNumber.Get(element, "HeightM");
            var height = QuantityMath.Positive(QualifiedVerticalQuantity.EffectiveHeight(project, element, legacyHeight));
            var crossSection = QuantityMath.Multiply(width, depth, element.Id + "/column cross section");
            var gross = QuantityMath.Multiply(crossSection, height, element.Id + "/column gross volume");
            var widthDepth = QuantityMath.Add(width, depth, element.Id + "/column perimeter half");
            var perimeter = QuantityMath.Multiply(2d, widthDepth, element.Id + "/column perimeter");
            var formwork = QuantityMath.Multiply(perimeter, height, element.Id + "/column formwork");

            element.SetQuantity("HeightM", height);
            element.SetQuantity("CrossSectionAreaM2", crossSection);
            element.SetQuantity("GrossVolumeM3", gross);
            element.SetQuantity("NetVolumeM3", gross);
            element.SetQuantity("FormworkM2", formwork);
        }

        private static void RegenerateWall(ProjectState project, ProjectElement element)
        {
            var length = QuantityMath.Positive(SemanticNumber.Get(element, "LengthM"));
            var legacyHeight = SemanticNumber.Get(element, "HeightM");
            var height = QuantityMath.Positive(QualifiedVerticalQuantity.EffectiveHeight(project, element, legacyHeight));
            var thickness = QuantityMath.Positive(SemanticNumber.Get(element, "ThicknessM"));
            var grossArea = QuantityMath.Multiply(length, height, element.Id + "/structural wall gross area");
            var linkedOpeningArea = LinkedOpeningArea(project, element);
            var explicitOpeningArea = QuantityMath.Positive(SemanticNumber.Get(element, "OpeningAreaM2"));
            var requestedOpeningArea = Math.Max(explicitOpeningArea, linkedOpeningArea);
            var openingArea = QuantityMath.Clamp(requestedOpeningArea, 0d, grossArea, element.Id + "/structural wall opening area");
            var netArea = QuantityMath.SubtractFloorZero(grossArea, openingArea, element.Id + "/structural wall net area");
            var grossVolume = QuantityMath.Multiply(grossArea, thickness, element.Id + "/structural wall gross volume");
            var deduction = QuantityMath.Multiply(openingArea, thickness, element.Id + "/structural wall deduction");
            var netVolume = QuantityMath.Multiply(netArea, thickness, element.Id + "/structural wall net volume");
            var formwork = QuantityMath.Multiply(2d, netArea, element.Id + "/structural wall formwork");

            element.SetQuantity("LengthM", length);
            element.SetQuantity("GrossWallAreaM2", grossArea);
            element.SetQuantity("OpeningAreaM2", openingArea);
            element.SetQuantity("NetWallAreaM2", netArea);
            element.SetQuantity("GrossVolumeM3", grossVolume);
            element.SetQuantity("DeductionM3", deduction);
            element.SetQuantity("NetVolumeM3", netVolume);
            element.SetQuantity("FormworkM2", formwork);
        }

        private static void RegenerateFoundation(ProjectState project, ProjectElement element)
        {
            var area = QuantityMath.Positive(SemanticNumber.Get(element, "BaseAreaM2", SemanticNumber.Get(element, "AreaM2")));
            var legacyThickness = SemanticNumber.Get(element, "ThicknessM", SemanticNumber.Get(element, "HeightM"));
            var thickness = QuantityMath.Positive(QualifiedVerticalQuantity.EffectiveHeight(project, element, legacyThickness));
            var perimeter = QuantityMath.Positive(SemanticNumber.Get(element, "PerimeterM"));
            var gross = QuantityMath.Multiply(area, thickness, element.Id + "/foundation volume");
            var formwork = QuantityMath.Multiply(perimeter, thickness, element.Id + "/foundation formwork");

            element.SetQuantity("AreaM2", area);
            element.SetQuantity("GrossVolumeM3", gross);
            element.SetQuantity("NetVolumeM3", gross);
            element.SetQuantity("FormworkM2", formwork);
        }

        private static void RegenerateStair(ProjectElement element)
        {
            var planArea = QuantityMath.Positive(SemanticNumber.Get(element, "AreaM2"));
            var width = QuantityMath.Positive(SemanticNumber.Get(element, "WidthM"));
            var runLength = QuantityMath.Positive(SemanticNumber.Get(element, "RunLengthM"));
            var totalRise = QuantityMath.Positive(SemanticNumber.Get(element, "TotalRiseM", SemanticNumber.Get(element, "HeightM")));
            var thickness = QuantityMath.Positive(SemanticNumber.Get(element, "ThicknessM"));
            var stepCount = QuantityMath.Positive(SemanticNumber.Get(element, "StepCount"));
            var treadFallback = stepCount > 0d && runLength > 0d ? QuantityMath.Divide(runLength, stepCount, element.Id + "/stair tread fallback") : 0d;
            var riserFallback = stepCount > 0d && totalRise > 0d ? QuantityMath.Divide(totalRise, stepCount, element.Id + "/stair riser fallback") : 0d;
            var tread = QuantityMath.Positive(SemanticNumber.Get(element, "TreadM", treadFallback));
            var riser = QuantityMath.Positive(SemanticNumber.Get(element, "RiserM", riserFallback));
            var slopeLength = runLength > 0d && totalRise > 0d ? QuantityMath.Hypot(runLength, totalRise, element.Id + "/stair slope") : 0d;
            var waistArea = width > 0d && slopeLength > 0d ? QuantityMath.Multiply(width, slopeLength, element.Id + "/stair waist area") : planArea;
            var waistVolume = QuantityMath.Multiply(waistArea, thickness, element.Id + "/stair waist volume");
            var stepVolume = 0d;
            if (stepCount > 0d && width > 0d && tread > 0d && riser > 0d)
            {
                var stepBase = QuantityMath.Multiply(width, tread, element.Id + "/stair step width-tread");
                stepBase = QuantityMath.Multiply(stepBase, riser, element.Id + "/stair step prism");
                stepBase = QuantityMath.Multiply(stepBase, stepCount, element.Id + "/stair all steps");
                stepVolume = QuantityMath.Multiply(.5d, stepBase, element.Id + "/stair step volume");
            }
            var gross = QuantityMath.Add(waistVolume, stepVolume, element.Id + "/stair gross volume");

            element.SetQuantity("AreaM2", planArea);
            element.SetQuantity("RunLengthM", runLength);
            element.SetQuantity("TotalRiseM", totalRise);
            element.SetQuantity("SlopeLengthM", slopeLength);
            element.SetQuantity("StepCount", stepCount);
            element.SetQuantity("StairWaistAreaM2", waistArea);
            element.SetQuantity("StepVolumeM3", stepVolume);
            element.SetQuantity("GrossVolumeM3", gross);
            element.SetQuantity("NetVolumeM3", gross);
            element.SetQuantity("SoffitAreaM2", waistArea);
            element.SetQuantity("FormworkM2", waistArea);
        }

        private static void RegenerateRailing(ProjectElement element)
        {
            var length = QuantityMath.Positive(SemanticNumber.Get(element, "LengthM"));
            var height = QuantityMath.Positive(SemanticNumber.Get(element, "HeightM", 1.1d));
            var postSpacing = QuantityMath.Positive(SemanticNumber.Get(element, "PostSpacingM", 1d));
            var postCount = 0d;
            if (length > 0d)
            {
                var intervals = postSpacing > 0d ? QuantityMath.Divide(length, postSpacing, element.Id + "/railing post intervals") : 1d;
                var rounded = Math.Ceiling(intervals);
                postCount = QuantityMath.Add(rounded, 1d, element.Id + "/railing post count");
            }
            var infillArea = QuantityMath.Multiply(length, height, element.Id + "/railing infill area");
            var count = length > 0d ? 1d : 0d;

            element.SetQuantity("LengthM", length);
            element.SetQuantity("HeightM", height);
            element.SetQuantity("HandrailLengthM", length);
            element.SetQuantity("PostCount", postCount);
            element.SetQuantity("InfillAreaM2", infillArea);
            element.SetQuantity("Count", count);
        }

        private static void RegenerateEarthwork(ProjectElement element)
        {
            var area = QuantityMath.Positive(SemanticNumber.Get(element, "ExcavationAreaM2", SemanticNumber.Get(element, "AreaM2")));
            var depth = QuantityMath.Positive(SemanticNumber.Get(element, "DepthM"));
            var bulkingFactor = SemanticNumber.Get(element, "BulkingFactor", 1d);
            if (double.IsNaN(bulkingFactor) || double.IsInfinity(bulkingFactor) || bulkingFactor <= 0d) bulkingFactor = 1d;
            var backfill = QuantityMath.Positive(SemanticNumber.Get(element, "BackfillM3"));
            var cutVolume = QuantityMath.Multiply(area, depth, element.Id + "/earthwork cut volume");
            var bulkedVolume = QuantityMath.Multiply(cutVolume, bulkingFactor, element.Id + "/earthwork bulked volume");
            var netExport = QuantityMath.SubtractFloorZero(bulkedVolume, backfill, element.Id + "/earthwork net export");

            element.SetQuantity("AreaM2", area);
            element.SetQuantity("DepthM", depth);
            element.SetQuantity("CutVolumeM3", cutVolume);
            element.SetQuantity("GrossVolumeM3", cutVolume);
            element.SetQuantity("NetVolumeM3", cutVolume);
            element.SetQuantity("BulkedVolumeM3", bulkedVolume);
            element.SetQuantity("BackfillM3", backfill);
            element.SetQuantity("NetExportM3", netExport);
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
                    var legacyHeight = SemanticNumber.Get(child, "HeightM");
                    var height = QuantityMath.Positive(QualifiedVerticalQuantity.EffectiveHeight(project, child, legacyHeight));
                    area = QuantityMath.Multiply(width, height, child.Id + "/opening area");
                }
                total = QuantityMath.Add(total, area, wall.Id + "/linked opening area");
            }
            return total;
        }
    }

    public sealed class GenericTakeoffRegenerator : IElementRegenerator
    {
        public bool CanRegenerate(ElementCategory category) => category == ElementCategory.CustomQuantity || category == ElementCategory.Grid;

        public void Regenerate(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!CanRegenerate(element.Category)) throw new InvalidOperationException("Unsupported takeoff category: " + element.Category);

            var length = QuantityMath.Positive(SemanticNumber.Get(element, "LengthM"));
            var area = QuantityMath.Positive(SemanticNumber.Get(element, "AreaM2"));
            element.SetQuantity("LengthM", length);
            element.SetQuantity("AreaM2", area);
            element.SetQuantity("Count", 1d);
        }
    }
}
