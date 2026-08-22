using System;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public static class LevelReferenceNativeIntegrationPolicy
    {
        public static bool IsQualified(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.GlassWall:
                case ElementCategory.WallPier:
                case ElementCategory.StructuralWall:
                case ElementCategory.Beam:
                case ElementCategory.Slab:
                case ElementCategory.Column:
                case ElementCategory.Foundation:
                case ElementCategory.Stair:
                case ElementCategory.Railing:
                case ElementCategory.Door:
                case ElementCategory.WallOpening:
                    return true;
                default:
                    return false;
            }
        }

        public static bool HasConfiguredReferences(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return Configured(element, ProjectFloorService.BottomLevelIdKey) ||
                   Configured(element, ProjectFloorService.BottomLevelOffsetKey) ||
                   Configured(element, ProjectFloorService.TopLevelIdKey) ||
                   Configured(element, ProjectFloorService.TopLevelOffsetKey);
        }

        public static void EnsureQualified(ProjectElement element, string operation)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!HasConfiguredReferences(element) || IsQualified(element.Category)) return;
            throw new InvalidOperationException(
                (string.IsNullOrWhiteSpace(operation) ? "Level-aware operation" : operation.Trim()) +
                " is blocked for " + element.Category + " element " + element.Id +
                " until its native host and dependent placement chain is qualified.");
        }

        private static bool Configured(ProjectElement element, string key)
        {
            return element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);
        }
    }
}
