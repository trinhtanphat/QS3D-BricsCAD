using System;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    public static class ElementGeometryPolicy
    {
        private static readonly ISet<string> GeometryProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LengthM", "WidthM", "HeightM", "DepthM", "ThicknessM",
            "BottomOffsetM", "TopOffsetM", "ProfileWidthM", "AreaM2", "PerimeterM",
            ProjectFloorService.BottomLevelIdKey,
            ProjectFloorService.BottomLevelOffsetKey,
            ProjectFloorService.TopLevelIdKey,
            ProjectFloorService.TopLevelOffsetKey
        };

        private static readonly ISet<string> CurtainGeometryProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CurtainMaxPanelWidthM",
            "CurtainMaxPanelHeightM",
            "CurtainPerimeterFrameWidthM",
            "CurtainMullionWidthM",
            "CurtainTransomWidthM"
        };

        private static readonly ISet<string> WallPierGeometryProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "WallPierProfileMode",
            "WallPierChamferM"
        };

        private static readonly ISet<string> GeneratedOutputProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Material"
        };

        private static readonly ISet<string> CurtainGeneratedOutputProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CurtainFrameMaterial"
        };

        public static bool RequiresGeneratedGeometry(ElementCategory category)
        {
            RequireDefinedCategory(category);
            return category == ElementCategory.ArchitecturalWall ||
                   category == ElementCategory.GlassWall ||
                   category == ElementCategory.WallPier ||
                   category == ElementCategory.Beam ||
                   category == ElementCategory.Slab ||
                   category == ElementCategory.Column ||
                   category == ElementCategory.StructuralWall ||
                   category == ElementCategory.Foundation ||
                   category == ElementCategory.Stair ||
                   category == ElementCategory.Railing ||
                   category == ElementCategory.Earthwork;
        }

        public static bool AffectsGeneratedGeometry(ElementCategory category, string? propertyName)
        {
            if (!RequiresGeneratedGeometry(category) || string.IsNullOrWhiteSpace(propertyName)) return false;
            return IsGeometryProperty(category, propertyName!.Trim());
        }

        public static bool AffectsGeneratedOutput(ElementCategory category, string? propertyName)
        {
            if (!RequiresGeneratedGeometry(category) || string.IsNullOrWhiteSpace(propertyName)) return false;
            var key = propertyName!.Trim();
            return IsGeometryProperty(category, key) ||
                   GeneratedOutputProperties.Contains(key) ||
                   (category == ElementCategory.GlassWall && CurtainGeneratedOutputProperties.Contains(key));
        }

        public static ElementDirtyFlags SemanticCleanFlags(ElementCategory category)
        {
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
            if (!RequiresGeneratedGeometry(category)) flags |= ElementDirtyFlags.Geometry;
            return flags;
        }

        private static bool IsGeometryProperty(ElementCategory category, string key)
        {
            return GeometryProperties.Contains(key) ||
                   (category == ElementCategory.GlassWall && CurtainGeometryProperties.Contains(key)) ||
                   (category == ElementCategory.WallPier && WallPierGeometryProperties.Contains(key));
        }

        private static void RequireDefinedCategory(ElementCategory category)
        {
            if (!Enum.IsDefined(typeof(ElementCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category), category, "Geometry policy requires a defined ElementCategory.");
        }
    }
}
