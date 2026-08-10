using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public enum InterchangeSemanticReferenceKind
    {
        Zone = 0,
        Floor = 1,
        Family = 2,
        Element = 3
    }

    public sealed class InterchangeSemanticPropertyReference
    {
        internal InterchangeSemanticPropertyReference(string propertyKey, InterchangeSemanticReferenceKind kind)
        {
            PropertyKey = propertyKey;
            Kind = kind;
        }

        public string PropertyKey { get; }
        public InterchangeSemanticReferenceKind Kind { get; }
    }

    public static class ProjectInterchangeSemanticReferencePolicy
    {
        private static readonly IReadOnlyDictionary<string, InterchangeSemanticPropertyReference> References =
            new Dictionary<string, InterchangeSemanticPropertyReference>(StringComparer.OrdinalIgnoreCase)
            {
                [ProjectFloorService.BottomLevelIdKey] = new InterchangeSemanticPropertyReference(ProjectFloorService.BottomLevelIdKey, InterchangeSemanticReferenceKind.Floor),
                [ProjectFloorService.TopLevelIdKey] = new InterchangeSemanticPropertyReference(ProjectFloorService.TopLevelIdKey, InterchangeSemanticReferenceKind.Floor)
            };

        public static IReadOnlyCollection<InterchangeSemanticPropertyReference> KnownPropertyReferences =>
            new List<InterchangeSemanticPropertyReference>(References.Values).AsReadOnly();

        public static bool TryGetPropertyReference(string propertyKey, out InterchangeSemanticPropertyReference reference)
        {
            if (string.IsNullOrWhiteSpace(propertyKey))
            {
                reference = null!;
                return false;
            }
            return References.TryGetValue(propertyKey.Trim(), out reference!);
        }

        public static bool LooksLikeSemanticReferenceKey(string propertyKey)
        {
            if (string.IsNullOrWhiteSpace(propertyKey)) return false;
            var key = propertyKey.Trim();
            if (key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Ids", StringComparison.OrdinalIgnoreCase)) return true;
            return key.IndexOf("Reference", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Ref", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Host", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Parent", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Zone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Family", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
