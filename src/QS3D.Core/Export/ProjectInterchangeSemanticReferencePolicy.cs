using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public sealed class InterchangeSemanticPropertyReference
    {
        internal InterchangeSemanticPropertyReference(string propertyKey, InterchangeRemapIdentityKind kind, string label)
        {
            PropertyKey = propertyKey ?? throw new ArgumentNullException(nameof(propertyKey));
            Kind = kind;
            Label = label ?? string.Empty;
        }

        public string PropertyKey { get; }
        public InterchangeRemapIdentityKind Kind { get; }
        public string Label { get; }
    }

    /// <summary>
    /// Canonical registry for portable semantic references stored inside ProjectElement.Properties.
    /// Direct FamilyId/FloorId/ZoneId and DependsOn remain first-class ProjectElement fields.
    /// </summary>
    public static class ProjectInterchangeSemanticReferencePolicy
    {
        public const string HostWallIdKey = "HostWallId";

        private static readonly IReadOnlyDictionary<string, InterchangeSemanticPropertyReference> References =
            new Dictionary<string, InterchangeSemanticPropertyReference>(StringComparer.OrdinalIgnoreCase)
            {
                [HostWallIdKey] = new InterchangeSemanticPropertyReference(HostWallIdKey, InterchangeRemapIdentityKind.Element, "host Element"),
                [ProjectFloorService.BottomLevelIdKey] = new InterchangeSemanticPropertyReference(ProjectFloorService.BottomLevelIdKey, InterchangeRemapIdentityKind.Floor, "bottom Floor/Level"),
                [ProjectFloorService.TopLevelIdKey] = new InterchangeSemanticPropertyReference(ProjectFloorService.TopLevelIdKey, InterchangeRemapIdentityKind.Floor, "top Floor/Level")
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
            return key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Ids", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Ref", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Refs", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("RefId", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("RefIds", StringComparison.OrdinalIgnoreCase);
        }
    }
}
