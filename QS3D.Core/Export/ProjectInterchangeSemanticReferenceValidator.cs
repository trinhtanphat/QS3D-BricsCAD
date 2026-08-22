using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    /// <summary>
    /// Validates portable semantic references stored in ProjectElement properties.
    /// This is independent from CAD/native ownership and is reusable by importers after mixed semantic composition.
    /// </summary>
    public static class ProjectInterchangeSemanticReferenceValidator
    {
        public static void Validate(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            foreach (var element in project.Elements.OrderBy(x => x?.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic Element.");
                ValidateRegisteredReferences(
                    element.Id,
                    element.Properties,
                    id => project.FindZone(id) != null,
                    id => project.FindFloor(id) != null,
                    id => project.FindFamily(id) != null,
                    id => project.FindElement(id) != null);
                ValidateLevelConsistency(
                    element.Id,
                    element.Properties,
                    id => project.FindFloor(id)?.ElevationM);
            }
        }

        public static void Validate(ProjectInterchangeValidatedSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            try
            {
                var zones = new HashSet<string>(snapshot.Zones.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                var floors = snapshot.Floors.ToDictionary(x => x.Id, x => x.ElevationM, StringComparer.OrdinalIgnoreCase);
                var families = new HashSet<string>(snapshot.Families.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                var elements = new HashSet<string>(snapshot.Elements.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

                foreach (var element in snapshot.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    ValidateRegisteredReferences(
                        element.Id,
                        element.Properties,
                        zones.Contains,
                        floors.ContainsKey,
                        families.Contains,
                        elements.Contains);
                    ValidateLevelConsistency(
                        element.Id,
                        element.Properties,
                        id => floors.TryGetValue(id, out var elevation) ? elevation : (double?)null);
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException("Semantic snapshot contains invalid registered property-carried references or level relations.", ex);
            }
        }

        private static void ValidateRegisteredReferences(
            string elementId,
            IEnumerable<KeyValuePair<string, string>> properties,
            Func<string, bool> zoneExists,
            Func<string, bool> floorExists,
            Func<string, bool> familyExists,
            Func<string, bool> elementExists)
        {
            foreach (var reference in ProjectInterchangeSemanticReferencePolicy.KnownPropertyReferences)
            {
                if (!TryProperty(properties, reference.PropertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var id = raw.Trim();
                bool exists;
                switch (reference.Kind)
                {
                    case InterchangeRemapIdentityKind.Zone: exists = zoneExists(id); break;
                    case InterchangeRemapIdentityKind.Floor: exists = floorExists(id); break;
                    case InterchangeRemapIdentityKind.Family: exists = familyExists(id); break;
                    case InterchangeRemapIdentityKind.Element: exists = elementExists(id); break;
                    default: throw new InvalidOperationException("Unsupported registered semantic reference kind: " + reference.Kind + ".");
                }
                if (!exists)
                    throw new InvalidOperationException(
                        "Element " + elementId + " property " + reference.PropertyKey +
                        " references missing " + reference.Kind + " identity " + id + ".");
            }
        }

        private static void ValidateLevelConsistency(
            string elementId,
            IEnumerable<KeyValuePair<string, string>> properties,
            Func<string, double?> floorElevation)
        {
            var bottomId = Property(properties, ProjectFloorService.BottomLevelIdKey);
            var topId = Property(properties, ProjectFloorService.TopLevelIdKey);
            var hasBottomOffset = HasConfiguredProperty(properties, ProjectFloorService.BottomLevelOffsetKey);
            var hasTopOffset = HasConfiguredProperty(properties, ProjectFloorService.TopLevelOffsetKey);

            if (bottomId.Length == 0)
            {
                if (topId.Length > 0)
                    throw new InvalidOperationException("Element " + elementId + " has TopLevelId without BottomLevelId.");
                if (hasBottomOffset || hasTopOffset)
                    throw new InvalidOperationException("Element " + elementId + " has a level offset without its level reference.");
                return;
            }

            var bottomBase = floorElevation(bottomId)
                ?? throw new InvalidOperationException("Element " + elementId + " references missing bottom Floor/Level " + bottomId + ".");
            var bottom = AddFinite(bottomBase, Offset(properties, ProjectFloorService.BottomLevelOffsetKey), elementId + "/bottom level elevation");

            if (topId.Length == 0)
            {
                if (hasTopOffset)
                    throw new InvalidOperationException("Element " + elementId + " has TopLevelOffsetM without TopLevelId.");
                return;
            }

            var topBase = floorElevation(topId)
                ?? throw new InvalidOperationException("Element " + elementId + " references missing top Floor/Level " + topId + ".");
            var top = AddFinite(topBase, Offset(properties, ProjectFloorService.TopLevelOffsetKey), elementId + "/top level elevation");
            if (top <= bottom)
                throw new InvalidOperationException("Element " + elementId + " top level elevation must be above bottom level elevation.");
        }

        private static double Offset(IEnumerable<KeyValuePair<string, string>> properties, string key)
        {
            if (!TryProperty(properties, key, out var raw) || string.IsNullOrWhiteSpace(raw)) return 0d;
            if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Level offset property " + key + " must be a finite invariant-culture number.");
            return value;
        }

        private static double AddFinite(double left, double right, string label)
        {
            var value = left + right;
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(label + " must be finite.");
            return value;
        }

        private static bool HasConfiguredProperty(IEnumerable<KeyValuePair<string, string>> properties, string key) =>
            TryProperty(properties, key, out var raw) && !string.IsNullOrWhiteSpace(raw);

        private static string Property(IEnumerable<KeyValuePair<string, string>> properties, string key) =>
            TryProperty(properties, key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;

        private static bool TryProperty(IEnumerable<KeyValuePair<string, string>> properties, string key, out string value)
        {
            foreach (var pair in properties)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                value = pair.Value ?? string.Empty;
                return true;
            }
            value = string.Empty;
            return false;
        }
    }
}
