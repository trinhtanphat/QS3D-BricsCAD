using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public enum InterchangeRemapIdentityKind
    {
        Zone = 0,
        Floor = 1,
        Family = 2,
        Element = 3
    }

    public sealed class ProjectInterchangeRemapItem
    {
        public InterchangeRemapIdentityKind Kind { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public bool IdChanged { get; set; }
        public bool NameChanged { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class ProjectInterchangeReferenceRewrite
    {
        public string OwnerElementSourceId { get; set; } = string.Empty;
        public string ReferenceKind { get; set; } = string.Empty;
        public string PropertyKey { get; set; } = string.Empty;
        public string SourceReferenceId { get; set; } = string.Empty;
        public string TargetReferenceId { get; set; } = string.Empty;
    }

    public sealed class ProjectInterchangeOpaqueReferenceWarning
    {
        public string OwnerElementSourceId { get; set; } = string.Empty;
        public string PropertyKey { get; set; } = string.Empty;
        public string PropertyValue { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class ProjectInterchangeRemapPlan
    {
        public string SourceProjectId { get; set; } = string.Empty;
        public int ValidationWarnings { get; set; }
        public IReadOnlyList<ProjectInterchangeRemapItem> Items { get; set; } = Array.Empty<ProjectInterchangeRemapItem>();
        public IReadOnlyList<ProjectInterchangeReferenceRewrite> ReferenceRewrites { get; set; } = Array.Empty<ProjectInterchangeReferenceRewrite>();
        public IReadOnlyList<ProjectInterchangeOpaqueReferenceWarning> OpaqueReferenceWarnings { get; set; } = Array.Empty<ProjectInterchangeOpaqueReferenceWarning>();
        public int IdRemapCount => Items.Count(x => x.IdChanged);
        public int NameRemapCount => Items.Count(x => x.NameChanged);
        public int IdentityCount => Items.Count;
        public bool CanAppendAsNew => OpaqueReferenceWarnings.Count == 0;

        public string MapId(InterchangeRemapIdentityKind kind, string sourceId)
        {
            var item = Items.SingleOrDefault(x => x.Kind == kind && string.Equals(x.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));
            if (item == null) throw new InvalidOperationException("Remap plan does not contain " + kind + " source identity " + sourceId + ".");
            return item.TargetId;
        }
    }

    public static class ProjectInterchangeRemapPlanner
    {
        private const int ZoneMaxIdLength = 64;
        private const int ZoneMaxNameLength = 120;
        private const int FloorMaxIdLength = 64;
        private const int FloorMaxNameLength = 120;
        private const int FamilyMaxIdLength = 80;
        private const int FamilyMaxNameLength = 160;
        private const int ElementMaxIdLength = 128;

        public static ProjectInterchangeRemapPlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);

            var items = new List<ProjectInterchangeRemapItem>();
            items.AddRange(PlanNamedIdentities(InterchangeRemapIdentityKind.Zone,
                source.Zones.Select(x => new NamedIdentity(x.Id, x.Name)),
                target.Zones.Select(x => new NamedIdentity(x.Id, x.Name)), ZoneMaxIdLength, ZoneMaxNameLength));
            items.AddRange(PlanNamedIdentities(InterchangeRemapIdentityKind.Floor,
                source.Floors.Select(x => new NamedIdentity(x.Id, x.Name)),
                target.Floors.Select(x => new NamedIdentity(x.Id, x.Name)), FloorMaxIdLength, FloorMaxNameLength));
            items.AddRange(PlanNamedIdentities(InterchangeRemapIdentityKind.Family,
                source.Families.Select(x => new NamedIdentity(x.Id, x.Name, x.Category.ToString())),
                target.Families.Select(x => new NamedIdentity(x.Id, x.Name, x.Category.ToString())), FamilyMaxIdLength, FamilyMaxNameLength));
            items.AddRange(PlanElements(source, target));

            var zoneMap = BuildMap(items, InterchangeRemapIdentityKind.Zone);
            var floorMap = BuildMap(items, InterchangeRemapIdentityKind.Floor);
            var familyMap = BuildMap(items, InterchangeRemapIdentityKind.Family);
            var elementMap = BuildMap(items, InterchangeRemapIdentityKind.Element);
            var rewrites = new List<ProjectInterchangeReferenceRewrite>();
            var opaque = new List<ProjectInterchangeOpaqueReferenceWarning>();

            foreach (var family in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var property in family.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (IsImportedOwnershipMetadata(property.Key)) continue;
                    if (string.IsNullOrWhiteSpace(property.Value) || !ProjectInterchangeSemanticReferencePolicy.LooksLikeSemanticReferenceKey(property.Key)) continue;
                    opaque.Add(new ProjectInterchangeOpaqueReferenceWarning
                    {
                        OwnerElementSourceId = "Family " + family.Id,
                        PropertyKey = property.Key,
                        PropertyValue = property.Value ?? string.Empty,
                        Reason = "Family property looks like a semantic identity/reference but no explicit Family-property rewrite policy is registered for this key."
                    });
                }
            }

            foreach (var element in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                AddTypedRewrite(rewrites, element.Id, "FamilyId", string.Empty, element.FamilyId, familyMap);
                AddTypedRewrite(rewrites, element.Id, "FloorId", string.Empty, element.FloorId, floorMap);
                AddTypedRewrite(rewrites, element.Id, "ZoneId", string.Empty, element.ZoneId, zoneMap);
                foreach (var dependency in element.Dependencies)
                    AddTypedRewrite(rewrites, element.Id, "DependsOn", string.Empty, dependency, elementMap);

                foreach (var property in element.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (IsImportedOwnershipMetadata(property.Key)) continue;
                    if (ProjectInterchangeSemanticReferencePolicy.TryGetPropertyReference(property.Key, out var reference))
                    {
                        var sourceReference = (property.Value ?? string.Empty).Trim();
                        if (sourceReference.Length == 0) continue;
                        var referenceMap = MapFor(reference.Kind, zoneMap, floorMap, familyMap, elementMap);
                        if (!referenceMap.ContainsKey(sourceReference))
                        {
                            opaque.Add(new ProjectInterchangeOpaqueReferenceWarning
                            {
                                OwnerElementSourceId = element.Id,
                                PropertyKey = property.Key,
                                PropertyValue = property.Value ?? string.Empty,
                                Reason = reference.PropertyKey + " is a registered " + reference.Label + " reference but does not resolve inside the source snapshot; import-as-new must not guess a target identity."
                            });
                            continue;
                        }
                        AddTypedRewrite(rewrites, element.Id, "Property" + reference.Kind + "Id", reference.PropertyKey, sourceReference, referenceMap);
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(property.Value) && ProjectInterchangeSemanticReferencePolicy.LooksLikeSemanticReferenceKey(property.Key))
                    {
                        opaque.Add(new ProjectInterchangeOpaqueReferenceWarning
                        {
                            OwnerElementSourceId = element.Id,
                            PropertyKey = property.Key,
                            PropertyValue = property.Value ?? string.Empty,
                            Reason = "Property looks like a semantic identity/reference but no explicit rewrite policy is registered for this key."
                        });
                    }
                }
            }

            return new ProjectInterchangeRemapPlan
            {
                SourceProjectId = source.Project.Id,
                ValidationWarnings = source.Validation.WarningCount,
                Items = items.OrderBy(x => x.Kind).ThenBy(x => x.SourceId, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly(),
                ReferenceRewrites = rewrites.OrderBy(x => x.OwnerElementSourceId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.ReferenceKind, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.PropertyKey, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.SourceReferenceId, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly(),
                OpaqueReferenceWarnings = opaque.OrderBy(x => x.OwnerElementSourceId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.PropertyKey, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly()
            };
        }

        private static IEnumerable<ProjectInterchangeRemapItem> PlanNamedIdentities(InterchangeRemapIdentityKind kind,
            IEnumerable<NamedIdentity> source, IEnumerable<NamedIdentity> target, int maxIdLength, int maxNameLength)
        {
            var incoming = source.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
            var existing = target.ToList();
            var occupiedIds = new HashSet<string>(existing.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var item in incoming) if (item.Id.Trim().Length <= maxIdLength) occupiedIds.Add(item.Id.Trim());
            var occupiedNames = new HashSet<string>(existing.Select(x => NameKey(x.NameScope, x.Name)), StringComparer.OrdinalIgnoreCase);
            foreach (var item in incoming) if (item.Name.Trim().Length <= maxNameLength) occupiedNames.Add(NameKey(item.NameScope, item.Name));
            var assignedIds = new HashSet<string>(existing.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var assignedNames = new HashSet<string>(existing.Select(x => NameKey(x.NameScope, x.Name)), StringComparer.OrdinalIgnoreCase);

            foreach (var sourceItem in incoming)
            {
                var sourceId = sourceItem.Id.Trim();
                var sourceName = sourceItem.Name.Trim();
                var sourceNameKey = NameKey(sourceItem.NameScope, sourceName);
                var idCollision = assignedIds.Contains(sourceId);
                var nameCollision = assignedNames.Contains(sourceNameKey);
                var idOverLimit = sourceId.Length > maxIdLength;
                var nameOverLimit = sourceName.Length > maxNameLength;
                var targetId = idCollision || idOverLimit ? NextId(sourceId, occupiedIds, maxIdLength) : sourceId;
                var targetName = nameCollision || nameOverLimit ? NextName(sourceName, sourceItem.NameScope, occupiedNames, maxNameLength) : sourceName;
                occupiedIds.Add(targetId);
                assignedIds.Add(targetId);
                var targetNameKey = NameKey(sourceItem.NameScope, targetName);
                occupiedNames.Add(targetNameKey);
                assignedNames.Add(targetNameKey);
                yield return new ProjectInterchangeRemapItem
                {
                    Kind = kind, SourceId = sourceItem.Id, TargetId = targetId, SourceName = sourceItem.Name, TargetName = targetName,
                    IdChanged = !string.Equals(sourceId, targetId, StringComparison.Ordinal),
                    NameChanged = !string.Equals(sourceName, targetName, StringComparison.Ordinal),
                    Reason = Reason(idCollision, nameCollision, idOverLimit, nameOverLimit, maxIdLength, maxNameLength)
                };
            }
        }

        private static IEnumerable<ProjectInterchangeRemapItem> PlanElements(ProjectInterchangeValidatedSnapshot source, ProjectState target)
        {
            var occupiedIds = new HashSet<string>(target.Elements.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var element in source.Elements) occupiedIds.Add(element.Id);
            foreach (var element in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var collision = target.FindElement(element.Id) != null;
                var targetId = collision ? NextId(element.Id, occupiedIds, ElementMaxIdLength) : element.Id;
                occupiedIds.Add(targetId);
                yield return new ProjectInterchangeRemapItem
                {
                    Kind = InterchangeRemapIdentityKind.Element, SourceId = element.Id, TargetId = targetId,
                    IdChanged = !string.Equals(element.Id, targetId, StringComparison.Ordinal), NameChanged = false,
                    Reason = collision ? "ID collision with target Element; import-as-new requires a new semantic Element ID." : "No target Element ID collision."
                };
            }
        }

        private static Dictionary<string, string> BuildMap(IEnumerable<ProjectInterchangeRemapItem> items, InterchangeRemapIdentityKind kind) =>
            items.Where(x => x.Kind == kind).ToDictionary(x => x.SourceId, x => x.TargetId, StringComparer.OrdinalIgnoreCase);

        private static IReadOnlyDictionary<string, string> MapFor(InterchangeRemapIdentityKind kind,
            IReadOnlyDictionary<string, string> zoneMap, IReadOnlyDictionary<string, string> floorMap,
            IReadOnlyDictionary<string, string> familyMap, IReadOnlyDictionary<string, string> elementMap)
        {
            switch (kind)
            {
                case InterchangeRemapIdentityKind.Zone: return zoneMap;
                case InterchangeRemapIdentityKind.Floor: return floorMap;
                case InterchangeRemapIdentityKind.Family: return familyMap;
                case InterchangeRemapIdentityKind.Element: return elementMap;
                default: throw new InvalidOperationException("Unsupported semantic reference kind: " + kind + ".");
            }
        }

        private static void AddTypedRewrite(ICollection<ProjectInterchangeReferenceRewrite> output, string owner,
            string referenceKind, string propertyKey, string sourceReference, IReadOnlyDictionary<string, string> map)
        {
            if (string.IsNullOrWhiteSpace(sourceReference)) return;
            var sourceId = sourceReference.Trim();
            if (!map.TryGetValue(sourceId, out var targetId))
                throw new InvalidOperationException("Strict remap planner could not resolve " + referenceKind + " source reference " + sourceId + " for Element " + owner + ".");
            if (string.Equals(sourceId, targetId, StringComparison.Ordinal)) return;
            output.Add(new ProjectInterchangeReferenceRewrite
            {
                OwnerElementSourceId = owner, ReferenceKind = referenceKind, PropertyKey = propertyKey ?? string.Empty,
                SourceReferenceId = sourceId, TargetReferenceId = targetId
            });
        }

        private static bool IsImportedOwnershipMetadata(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            var k = key.Trim();
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(k)) return true;
            if (k.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (k.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return true;
            return k.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NextId(string sourceId, ISet<string> occupied, int maxLength)
        {
            for (var suffix = 1; suffix < 1000000; suffix++)
            {
                var marker = suffix == 1 ? "-import" : "-import-" + suffix;
                var candidate = AppendBounded(sourceId, marker, maxLength);
                if (!occupied.Contains(candidate)) return candidate;
            }
            throw new InvalidOperationException("Unable to allocate a collision-free semantic import ID for " + sourceId + ".");
        }

        private static string NextName(string sourceName, string nameScope, ISet<string> occupied, int maxLength)
        {
            for (var suffix = 1; suffix < 1000000; suffix++)
            {
                var marker = suffix == 1 ? " (Imported)" : " (Imported " + suffix + ")";
                var candidate = AppendBounded(sourceName, marker, maxLength);
                if (!occupied.Contains(NameKey(nameScope, candidate))) return candidate;
            }
            throw new InvalidOperationException("Unable to allocate a collision-free semantic import name for " + sourceName + ".");
        }

        private static string NameKey(string nameScope, string name) =>
            (nameScope ?? string.Empty).Trim() + "\u001f" + (name ?? string.Empty).Trim();

        private static string AppendBounded(string value, string suffix, int maxLength)
        {
            var source = (value ?? string.Empty).Trim();
            if (!HasWellFormedUtf16(source) || !HasWellFormedUtf16(suffix))
                throw new InvalidOperationException("Remap identity/name contains malformed UTF-16.");
            if (suffix.Length >= maxLength) throw new InvalidOperationException("Remap suffix exceeds semantic identity/name limit.");
            var keep = Math.Min(source.Length, maxLength - suffix.Length);
            if (keep > 0 && keep < source.Length && char.IsHighSurrogate(source[keep - 1]) && char.IsLowSurrogate(source[keep]))
                keep--;
            return source.Substring(0, keep).TrimEnd() + suffix;
        }

        private static bool HasWellFormedUtf16(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (char.IsHighSurrogate(current))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1])) return false;
                    i++;
                    continue;
                }
                if (char.IsLowSurrogate(current)) return false;
            }
            return true;
        }

        private static string Reason(bool idCollision, bool nameCollision, bool idOverLimit, bool nameOverLimit, int maxIdLength, int maxNameLength)
        {
            var reasons = new List<string>();
            if (idCollision) reasons.Add("semantic ID collides with target/earlier incoming identity");
            if (nameCollision) reasons.Add("display name is already owned in the same semantic name scope");
            if (idOverLimit) reasons.Add("source ID exceeds target runtime limit " + maxIdLength);
            if (nameOverLimit) reasons.Add("source display name exceeds target runtime limit " + maxNameLength);
            if (reasons.Count == 0) return "No target ID/name collision or runtime-bound remap is required.";
            return "Import-as-new remap required: " + string.Join("; ", reasons) + ".";
        }

        private sealed class NamedIdentity
        {
            public NamedIdentity(string id, string name, string nameScope = "")
            {
                Id = id ?? string.Empty;
                Name = name ?? string.Empty;
                NameScope = nameScope ?? string.Empty;
            }
            public string Id { get; }
            public string Name { get; }
            public string NameScope { get; }
        }
    }
}
