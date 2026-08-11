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

        private static readonly ISet<string> GeneratedOutputProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Material", "CurtainFrameMaterial"
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
            return RequiresGeneratedGeometry(category) &&
                   !string.IsNullOrWhiteSpace(propertyName) &&
                   GeometryProperties.Contains(propertyName!.Trim());
        }

        public static bool AffectsGeneratedOutput(ElementCategory category, string? propertyName)
        {
            return RequiresGeneratedGeometry(category) &&
                   !string.IsNullOrWhiteSpace(propertyName) &&
                   (GeometryProperties.Contains(propertyName!.Trim()) || GeneratedOutputProperties.Contains(propertyName.Trim()));
        }

        public static ElementDirtyFlags SemanticCleanFlags(ElementCategory category)
        {
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
            if (!RequiresGeneratedGeometry(category)) flags |= ElementDirtyFlags.Geometry;
            return flags;
        }

        private static void RequireDefinedCategory(ElementCategory category)
        {
            if (!Enum.IsDefined(typeof(ElementCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category), category, "Geometry policy requires a defined ElementCategory.");
        }
    }
}
