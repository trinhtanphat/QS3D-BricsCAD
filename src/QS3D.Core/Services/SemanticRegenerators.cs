using System;
using System.Globalization;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;

namespace QS3D.Core.Services
{
    internal static class SemanticNumber
    {
        public static double Get(ProjectElement element, string name, double fallback = 0d)
        {
            if (!element.Properties.TryGetValue(name, out var value)) return fallback;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ||
                double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidOperationException(element.Id + "/" + name + " must be a finite invariant numeric value.");
            return result;
        }
    }

    internal static class QualifiedVerticalQuantity
    {
        public static double EffectiveHeight(ProjectState project, ProjectElement element, double legacyHeightM)
        {
            var effectiveHeight = ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, legacyHeightM);
            LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Quantity regeneration with Level references");
            return effectiveHeight;
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
            var legacyHeight = SemanticNumber.Get(element, "HeightM");
            var height = QuantityMath.Positive(QualifiedVerticalQuantity.EffectiveHeight(project, element, legacyHeight));
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

            if (element.Category == ElementCategory.GlassWall)
            {
                var curtain = CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
                {
                    LengthM = length,
                    HeightM = height,
                    MaxPanelWidthM = SemanticNumber.Get(element, "CurtainMaxPanelWidthM", 1.2d),
                    MaxPanelHeightM = SemanticNumber.Get(element, "CurtainMaxPanelHeightM", 1.5d),
                    PerimeterFrameWidthM = SemanticNumber.Get(element, "CurtainPerimeterFrameWidthM", 0.05d),
                    MullionWidthM = SemanticNumber.Get(element, "CurtainMullionWidthM", 0.05d),
                    TransomWidthM = SemanticNumber.Get(element, "CurtainTransomWidthM", 0.05d)
                });
                var netGlassAreaM2 = QuantityMath.SubtractFloorZero(curtain.ClearGlassAreaM2, openingArea, element.Id + "/curtain net glass area");

                element.SetQuantity("CurtainPanelColumns", curtain.Columns);
                element.SetQuantity("CurtainPanelRows", curtain.Rows);
                element.SetQuantity("CurtainPanelCount", curtain.PanelCount);
                element.SetQuantity("CurtainBayWidthM", curtain.BayWidthM);
                element.SetQuantity("CurtainBayHeightM", curtain.BayHeightM);
                element.SetQuantity("CurtainMinClearPanelWidthM", curtain.MinimumClearPanelWidthM);
                element.SetQuantity("CurtainMaxClearPanelWidthM", curtain.MaximumClearPanelWidthM);
                element.SetQuantity("CurtainMinClearPanelHeightM", curtain.MinimumClearPanelHeightM);
                element.SetQuantity("CurtainMaxClearPanelHeightM", curtain.MaximumClearPanelHeightM);
                element.SetQuantity("CurtainVerticalFrameCount", curtain.VerticalFrameCount);
                element.SetQuantity("CurtainHorizontalFrameCount", curtain.HorizontalFrameCount);
                element.SetQuantity("CurtainVerticalFrameLengthM", curtain.VerticalFrameLengthM);
                element.SetQuantity("CurtainHorizontalFrameLengthM", curtain.HorizontalFrameLengthM);
                element.SetQuantity("CurtainFrameLengthM", curtain.TotalFrameLengthM);
                element.SetQuantity("CurtainClearGlassAreaM2", curtain.ClearGlassAreaM2);
                element.SetQuantity("CurtainNetGlassAreaM2", netGlassAreaM2);
                element.SetQuantity("CurtainFrameFaceAreaM2", curtain.FrameFaceAreaM2);
            }

            if (element.Category == ElementCategory.WallPier)
            {
                var mode = ResolveWallPierProfileMode(project, element);
                var chamfer = mode == WallPierProfileMode.Chamfered ? ResolveWallPierNumber(project, element, "WallPierChamferM", 0.02d) : 0d;
                double profileAreaM2;
                double profilePerimeterM;
                double profileGrossVolumeM3;
                double profileLateralAreaM2;
                if (!TryReadCurrentWallPierPathProfile(
                    element,
                    mode,
                    chamfer,
                    length,
                    thickness,
                    height,
                    out profileAreaM2,
                    out profilePerimeterM,
                    out profileGrossVolumeM3,
                    out profileLateralAreaM2))
                {
                    var profile = WallPierProfilePlanner.Plan(new WallPierProfileInput
                    {
                        Mode = mode,
                        WidthM = length,
                        DepthM = thickness,
                        HeightM = height,
                        ChamferM = chamfer
                    });
                    profileAreaM2 = profile.CrossSectionAreaM2;
                    profilePerimeterM = profile.CrossSectionPerimeterM;
                    profileGrossVolumeM3 = profile.VolumeM3;
                    profileLateralAreaM2 = profile.LateralAreaM2;
                }

                var openingVolumeM3 = QuantityMath.Multiply(openingArea, thickness, element.Id + "/wall-pier opening volume");
                var profileNetVolumeM3 = QuantityMath.SubtractFloorZero(profileGrossVolumeM3, openingVolumeM3, element.Id + "/wall-pier net profile volume");

                element.SetQuantity("WallPierProfileCrossSectionAreaM2", profileAreaM2);
                element.SetQuantity("WallPierProfilePerimeterM", profilePerimeterM);
                element.SetQuantity("WallPierProfileLateralAreaM2", profileLateralAreaM2);
                element.SetQuantity("WallPierProfileGrossVolumeM3", profileGrossVolumeM3);
                element.SetQuantity("WallPierProfileNetVolumeM3", profileNetVolumeM3);
                element.SetQuantity("GrossVolumeM3", profileGrossVolumeM3);
                element.SetQuantity("NetVolumeM3", profileNetVolumeM3);
            }
        }

        private static WallPierProfileMode ResolveWallPierProfileMode(ProjectState project, ProjectElement element)
        {
            var raw = ResolveWallPierText(project, element, "WallPierProfileMode", "Rectangular");
            if (Enum.TryParse(raw, true, out WallPierProfileMode mode)) return mode;
            throw new InvalidOperationException(element.Id + "/WallPierProfileMode không hợp lệ: " + raw);
        }

        private static double ResolveWallPierNumber(ProjectState project, ProjectElement element, string key, double fallback)
        {
            var raw = ResolveWallPierText(project, element, key, fallback.ToString("R", CultureInfo.InvariantCulture));
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new InvalidOperationException(element.Id + "/" + key + " không hợp lệ: " + raw);
            return value;
        }

        private static string ResolveWallPierText(ProjectState project, ProjectElement element, string key, string fallback)
        {
            if (element.Properties.TryGetValue(key, out var own) && !string.IsNullOrWhiteSpace(own)) return own.Trim();
            var family = project.FindFamily(element.FamilyId);
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return fallback;
        }

        private static bool TryReadCurrentWallPierPathProfile(
            ProjectElement element,
            WallPierProfileMode mode,
            double chamferM,
            double lengthM,
            double thicknessM,
            double heightM,
            out double areaM2,
            out double perimeterM,
            out double grossVolumeM3,
            out double lateralAreaM2)
        {
            areaM2 = perimeterM = grossVolumeM3 = lateralAreaM2 = 0d;
            if (element.IsGeneratedSolidStale()) return false;
            if (!element.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle)) return false;
            if (!element.Properties.TryGetValue("WallPierPathProfileKind", out var kind) || !string.Equals(kind, "OpenPolyline", StringComparison.OrdinalIgnoreCase)) return false;
            if (!element.Properties.TryGetValue("WallPierPathProfileMode", out var modeText) || !Enum.TryParse(modeText, true, out WallPierProfileMode storedMode) || storedMode != mode) return false;

            if (!TryFinite(element, "WallPierPathProfileChamferM", out var storedChamfer) || !NearlyEqual(storedChamfer, chamferM)) return false;
            if (!TryFinitePositive(element, "WallPierPathProfileCenterlineLengthM", out var storedLength) || !NearlyEqual(storedLength, lengthM)) return false;
            if (!TryFinitePositive(element, "WallPierPathProfileThicknessM", out var storedThickness) || !NearlyEqual(storedThickness, thicknessM)) return false;
            if (!TryFinitePositive(element, "WallPierPathProfileHeightM", out var storedHeight) || !NearlyEqual(storedHeight, heightM)) return false;
            if (!TryFinitePositive(element, "WallPierPathProfileAreaM2", out areaM2)) return false;
            if (!TryFinitePositive(element, "WallPierPathProfilePerimeterM", out perimeterM)) return false;
            if (!TryFinitePositive(element, "WallPierPathProfileGrossVolumeM3", out grossVolumeM3)) return false;
            if (!TryFinitePositive(element, "WallPierPathProfileLateralAreaM2", out lateralAreaM2)) return false;
            if (!NearlyEqual(grossVolumeM3, areaM2 * heightM)) return false;
            if (!NearlyEqual(lateralAreaM2, perimeterM * heightM)) return false;
            return true;
        }

        private static bool TryFinite(ProjectElement element, string key, out double value)
        {
            value = 0d;
            return element.Properties.TryGetValue(key, out var raw) &&
                   double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool TryFinitePositive(ProjectElement element, string key, out double value) =>
            TryFinite(element, key, out value) && value > 0d;

        private static bool NearlyEqual(double left, double right)
        {
            if (double.IsNaN(left) || double.IsInfinity(left) || double.IsNaN(right) || double.IsInfinity(right)) return false;
            var scale = Math.Max(1d, Math.Max(Math.Abs(left), Math.Abs(right)));
            return Math.Abs(left - right) <= scale * 1e-9d;
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
            var legacyHeight = SemanticNumber.Get(element, "HeightM");
            var height = QuantityMath.Positive(QualifiedVerticalQuantity.EffectiveHeight(project, element, legacyHeight));
            var area = QuantityMath.Multiply(width, height, element.Id + "/opening area");

            element.SetQuantity("OpeningAreaM2", area);
            element.SetQuantity("Count", 1d);
            if (element.Properties.TryGetValue("HostWallId", out var hostId) && !string.IsNullOrWhiteSpace(hostId))
            {
                var host = project.FindElement(hostId);
                if (host != null)
                {
                    host.MarkDirty(ElementDirtyFlags.Quantity);
                    if (host.Category == ElementCategory.GlassWall)
                    {
                        host.MarkGeneratedCurtainFrameStale("Linked opening " + element.Id + " changed.");
                        host.MarkGeneratedCurtainPanelStale("Linked opening " + element.Id + " changed.");
                    }
                }
            }
        }
    }
}
