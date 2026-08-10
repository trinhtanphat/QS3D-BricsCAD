using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public enum InterchangeRemapIdentityKind
    {
        Zone = 0,
        Floor = 1,
        Family = 2,
        Element = 3
    }

    public sealed class ProjectInterchangeRemapIdentity
    {
        internal ProjectInterchangeRemapIdentity(InterchangeRemapIdentityKind kind, string sourceId, string targetId, string sourceName, string targetName)
        {
            Kind = kind;
            SourceId = sourceId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TargetName = targetName ?? string.Empty;
        }

        public InterchangeRemapIdentityKind Kind { get; }
        public string SourceId { get; }
        public string TargetId { get; }
        public string SourceName { get; }
        public string TargetName { get; }
    }

    public sealed class ProjectInterchangeRemapCopyPlan
    {
        internal ProjectInterchangeRemapCopyPlan(
            string sourceProjectId,
            int sourceSchemaVersion,
            string importNamespace,
            int zonesToAdd,
            int floorsToAdd,
            int familiesToAdd,
            int elementsToAdd,
            int sourceHandlesToDiscard,
            int propertyReferencesRemapped,
            int generatedOwnershipPropertiesDiscarded,
            int validationWarnings,
            IReadOnlyList<ProjectInterchangeRemapIdentity> mappings)
        {
            SourceProjectId = sourceProjectId ?? string.Empty;
            SourceSchemaVersion = sourceSchemaVersion;
            ImportNamespace = importNamespace ?? string.Empty;
            ZonesToAdd = zonesToAdd;
            FloorsToAdd = floorsToAdd;
            FamiliesToAdd = familiesToAdd;
            ElementsToAdd = elementsToAdd;
            SourceHandlesToDiscard = sourceHandlesToDiscard;
            PropertyReferencesRemapped = propertyReferencesRemapped;
            GeneratedOwnershipPropertiesDiscarded = generatedOwnershipPropertiesDiscarded;
            ValidationWarnings = validationWarnings;
            Mappings = new List<ProjectInterchangeRemapIdentity>(mappings ?? Array.Empty<ProjectInterchangeRemapIdentity>()).AsReadOnly();
        }

        public string SourceProjectId { get; }
        public int SourceSchemaVersion { get; }
        public string ImportNamespace { get; }
        public int ZonesToAdd { get; }
        public int FloorsToAdd { get; }
        public int FamiliesToAdd { get; }
        public int ElementsToAdd { get; }
        public int SourceHandlesToDiscard { get; }
        public int PropertyReferencesRemapped { get; }
        public int GeneratedOwnershipPropertiesDiscarded { get; }
        public int ValidationWarnings { get; }
        public IReadOnlyList<ProjectInterchangeRemapIdentity> Mappings { get; }
        public int TotalSemanticIdentitiesToAdd => checked(checked(ZonesToAdd + FloorsToAdd) + checked(FamiliesToAdd + ElementsToAdd));
    }

    public sealed class ProjectInterchangeRemapCopyResult
    {
        internal ProjectInterchangeRemapCopyResult(ProjectInterchangeRemapCopyPlan plan)
        {
            SourceProjectId = plan.SourceProjectId;
            ImportNamespace = plan.ImportNamespace;
            ZonesAdded = plan.ZonesToAdd;
            FloorsAdded = plan.FloorsToAdd;
            FamiliesAdded = plan.FamiliesToAdd;
            ElementsAdded = plan.ElementsToAdd;
            SourceHandlesDiscarded = plan.SourceHandlesToDiscard;
            PropertyReferencesRemapped = plan.PropertyReferencesRemapped;
            GeneratedOwnershipPropertiesDiscarded = plan.GeneratedOwnershipPropertiesDiscarded;
        }

        public string SourceProjectId { get; }
        public string ImportNamespace { get; }
        public int ZonesAdded { get; }
        public int FloorsAdded { get; }
        public int FamiliesAdded { get; }
        public int ElementsAdded { get; }
        public int SourceHandlesDiscarded { get; }
        public int PropertyReferencesRemapped { get; }
        public int GeneratedOwnershipPropertiesDiscarded { get; }
    }

    public static class ProjectInterchangeRemapCopyImporter
    {
        private sealed class PreparedZone
        {
            public string Id = string.Empty;
            public string Name = string.Empty;
        }

        private sealed class PreparedFloor
        {
            public string Id = string.Empty;
            public string Name = string.Empty;
            public double ElevationM;
        }

        private sealed class PreparedFamily
        {
            public string Id = string.Empty;
            public string Name = string.Empty;
            public ElementCategory Category;
            public Dictionary<string, string> Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class PreparedElement
        {
            public string Id = string.Empty;
            public ElementCategory Category;
            public string FamilyId = string.Empty;
            public string FloorId = string.Empty;
            public string ZoneId = string.Empty;
            public List<string> Dependencies = new List<string>();
            public Dictionary<string, string> Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, double> Quantities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class PreparationCounters
        {
            public int PropertyReferencesRemapped;
            public int GeneratedOwnershipPropertiesDiscarded;
        }

        private sealed class PreparedImport
        {
            public ProjectInterchangeValidatedSnapshot Source = null!;
            public ProjectInterchangeRemapCopyPlan Plan = null!;
            public List<PreparedZone> Zones = new List<PreparedZone>();
            public List<PreparedFloor> Floors = new List<PreparedFloor>();
            public List<PreparedFamily> Families = new List<PreparedFamily>();
            public List<PreparedElement> Elements = new List<PreparedElement>();
        }

        public const string ImportMode = "RemapCopy";
        public const string LastNamespaceKey = "Interchange.LastImport.Namespace";
        public const string LastPropertyReferencesRemappedKey = "Interchange.LastImport.PropertyReferencesRemapped";
        public const string LastGeneratedOwnershipPropertiesDiscardedKey = "Interchange.LastImport.GeneratedOwnershipPropertiesDiscarded";

        public static string SuggestNamespace(string sourceProjectId)
        {
            var raw = (sourceProjectId ?? string.Empty).Trim();
            var builder = new StringBuilder();
            foreach (var ch in raw)
            {
                if (builder.Length >= 24) break;
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') builder.Append(ch);
            }
            if (builder.Length == 0) builder.Append("source");
            return builder.ToString();
        }

        public static ProjectInterchangeRemapCopyPlan Plan(ProjectState target, string json, string importNamespace)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return Prepare(target, json, importNamespace).Plan;
        }

        public static ProjectInterchangeRemapCopyResult Import(ProjectState target, string json, string importNamespace)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var prepared = Prepare(target, json, importNamespace);
            var snapshot = ProjectStateSnapshot.Capture(target);
            var targetHadZones = target.Zones.Count > 0;
            var targetHadFloors = target.Floors.Count > 0;
            var previousActiveZoneId = target.ActiveZoneId ?? string.Empty;
            var previousActiveFloorId = target.ActiveFloorId ?? string.Empty;

            try
            {
                foreach (var zone in prepared.Zones)
                    ProjectZoneService.Create(target, zone.Id, zone.Name);

                foreach (var floor in prepared.Floors)
                    ProjectFloorService.Create(target, floor.Id, floor.Name, floor.ElevationM);

                foreach (var familySnapshot in prepared.Families)
                {
                    var family = ProjectFamilyService.Create(target, familySnapshot.Id, familySnapshot.Name, familySnapshot.Category);
                    foreach (var property in familySnapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        family.Properties[property.Key] = property.Value ?? string.Empty;
                }

                foreach (var elementSnapshot in prepared.Elements)
                {
                    var element = new ProjectElement(
                        elementSnapshot.Id,
                        elementSnapshot.Category,
                        elementSnapshot.FamilyId,
                        elementSnapshot.FloorId,
                        elementSnapshot.ZoneId)
                    {
                        DrawingFingerprint = string.Empty
                    };
                    foreach (var dependency in elementSnapshot.Dependencies) element.DependsOn.Add(dependency);
                    foreach (var property in elementSnapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        element.Properties[property.Key] = property.Value ?? string.Empty;
                    foreach (var quantity in elementSnapshot.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        element.Quantities[quantity.Key] = quantity.Value;
                    element.MarkDirty(ElementDirtyFlags.All);
                    target.Elements.Add(element);
                }

                if (targetHadZones) target.ActiveZoneId = previousActiveZoneId;
                if (targetHadFloors) target.ActiveFloorId = previousActiveFloorId;

                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey] = ImportMode;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceProjectIdKey] = prepared.Source.Project.Id;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceSchemaVersionKey] = prepared.Source.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture);
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceDrawingFingerprintKey] = prepared.Source.Project.DrawingFingerprint;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceUpdatedUtcKey] = prepared.Source.Project.UpdatedUtcRaw;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastImportedUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceHandlesDiscardedKey] = prepared.Plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastNamespaceKey] = prepared.Plan.ImportNamespace;
                target.Metadata[LastPropertyReferencesRemappedKey] = prepared.Plan.PropertyReferencesRemapped.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastGeneratedOwnershipPropertiesDiscardedKey] = prepared.Plan.GeneratedOwnershipPropertiesDiscarded.ToString(CultureInfo.InvariantCulture);

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeRemapCopy",
                    string.Empty,
                    "Imported isolated remapped semantic copy from project " + prepared.Source.Project.Id +
                    " namespace=" + prepared.Plan.ImportNamespace +
                    ", semanticAdded=" + prepared.Plan.TotalSemanticIdentitiesToAdd.ToString(CultureInfo.InvariantCulture) +
                    ", propertyReferencesRemapped=" + prepared.Plan.PropertyReferencesRemapped.ToString(CultureInfo.InvariantCulture) +
                    ", discardedDrawingHandles=" + prepared.Plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) +
                    ", discardedGeneratedOwnershipProperties=" + prepared.Plan.GeneratedOwnershipPropertiesDiscarded.ToString(CultureInfo.InvariantCulture) + ".");

                target.Touch();
                ValidateImportedReferences(target, prepared);
                return new ProjectInterchangeRemapCopyResult(prepared.Plan);
            }
            catch
            {
                snapshot.Restore(target);
                throw;
            }
        }

        private static PreparedImport Prepare(ProjectState target, string json, string importNamespace)
        {
            ValidateTargetIdentitySurface(target);
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var ns = NormalizeNamespace(importNamespace);
            var total = checked(checked(source.Zones.Count + source.Floors.Count) + checked(source.Families.Count + source.Elements.Count));
            if (total == 0) throw new InvalidOperationException("Remapped-copy import requires at least one source semantic identity.");
            if (checked(target.Zones.Count + source.Zones.Count) > 2000) throw new InvalidOperationException("Remapped-copy import would exceed the project Zone limit.");
            if (checked(target.Floors.Count + source.Floors.Count) > 2000) throw new InvalidOperationException("Remapped-copy import would exceed the project Floor limit.");
            if (checked(target.Families.Count + source.Families.Count) > 10000) throw new InvalidOperationException("Remapped-copy import would exceed the project Family limit.");

            var zoneMap = BuildIdMap(source.Zones.Select(x => x.Id), target.Zones.Select(x => x.Id), ns, InterchangeRemapIdentityKind.Zone, "RZ");
            var floorMap = BuildIdMap(source.Floors.Select(x => x.Id), target.Floors.Select(x => x.Id), ns, InterchangeRemapIdentityKind.Floor, "RL");
            var familyMap = BuildIdMap(source.Families.Select(x => x.Id), target.Families.Select(x => x.Id), ns, InterchangeRemapIdentityKind.Family, "RF");
            var elementMap = BuildIdMap(source.Elements.Select(x => x.Id), target.Elements.Select(x => x.Id), ns, InterchangeRemapIdentityKind.Element, "RE");
            var sourceIdentityIndex = BuildSourceIdentityIndex(source);
            var counters = new PreparationCounters();
            var prepared = new PreparedImport { Source = source };
            var mappings = new List<ProjectInterchangeRemapIdentity>(total);

            var zoneNames = new HashSet<string>(target.Zones.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var zone in source.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                var name = MapDisplayName(zone.Name, ns, zone.Id, 120, zoneNames);
                prepared.Zones.Add(new PreparedZone { Id = zoneMap[zone.Id], Name = name });
                mappings.Add(new ProjectInterchangeRemapIdentity(InterchangeRemapIdentityKind.Zone, zone.Id, zoneMap[zone.Id], zone.Name, name));
            }

            var floorNames = new HashSet<string>(target.Floors.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var floor in source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                var name = MapDisplayName(floor.Name, ns, floor.Id, 120, floorNames);
                prepared.Floors.Add(new PreparedFloor { Id = floorMap[floor.Id], Name = name, ElevationM = floor.ElevationM });
                mappings.Add(new ProjectInterchangeRemapIdentity(InterchangeRemapIdentityKind.Floor, floor.Id, floorMap[floor.Id], floor.Name, name));
            }

            var familyNames = new Dictionary<ElementCategory, HashSet<string>>();
            foreach (var category in Enum.GetValues(typeof(ElementCategory)).Cast<ElementCategory>())
                familyNames[category] = new HashSet<string>(target.Families.Where(x => x.Category == category).Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var family in source.Families.OrderBy(x => x.Category).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                var name = MapDisplayName(family.Name, ns, family.Id, 160, familyNames[family.Category]);
                var properties = RemapProperties(
                    family.Properties,
                    "Family " + family.Id,
                    sourceIdentityIndex,
                    zoneMap,
                    floorMap,
                    familyMap,
                    elementMap,
                    counters);
                prepared.Families.Add(new PreparedFamily { Id = familyMap[family.Id], Name = name, Category = family.Category, Properties = properties });
                mappings.Add(new ProjectInterchangeRemapIdentity(InterchangeRemapIdentityKind.Family, family.Id, familyMap[family.Id], family.Name, name));
            }

            foreach (var sourceElement in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                var element = new PreparedElement
                {
                    Id = elementMap[sourceElement.Id],
                    Category = sourceElement.Category,
                    FamilyId = RemapOptional(sourceElement.FamilyId, familyMap, "Family", sourceElement.Id),
                    FloorId = RemapOptional(sourceElement.FloorId, floorMap, "Floor", sourceElement.Id),
                    ZoneId = RemapOptional(sourceElement.ZoneId, zoneMap, "Zone", sourceElement.Id),
                    Properties = RemapProperties(
                        sourceElement.Properties,
                        "Element " + sourceElement.Id,
                        sourceIdentityIndex,
                        zoneMap,
                        floorMap,
                        familyMap,
                        elementMap,
                        counters),
                    Quantities = new Dictionary<string, double>(sourceElement.Quantities, StringComparer.OrdinalIgnoreCase)
                };
                foreach (var dependency in sourceElement.Dependencies)
                    element.Dependencies.Add(RemapRequired(dependency, elementMap, "Element dependency", sourceElement.Id));
                prepared.Elements.Add(element);
                mappings.Add(new ProjectInterchangeRemapIdentity(InterchangeRemapIdentityKind.Element, sourceElement.Id, element.Id, string.Empty, string.Empty));
            }

            var sourceHandles = 0;
            foreach (var element in source.Elements) sourceHandles = checked(sourceHandles + element.SourceHandles.Count);
            prepared.Plan = new ProjectInterchangeRemapCopyPlan(
                source.Project.Id,
                source.Project.SchemaVersion,
                ns,
                prepared.Zones.Count,
                prepared.Floors.Count,
                prepared.Families.Count,
                prepared.Elements.Count,
                sourceHandles,
                counters.PropertyReferencesRemapped,
                counters.GeneratedOwnershipPropertiesDiscarded,
                source.Validation.WarningCount,
                mappings.OrderBy(x => x.Kind).ThenBy(x => x.SourceId, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.SourceId, StringComparer.Ordinal).ToList().AsReadOnly());
            return prepared;
        }

        private static Dictionary<string, string> BuildIdMap(
            IEnumerable<string> sourceIds,
            IEnumerable<string> targetIds,
            string importNamespace,
            InterchangeRemapIdentityKind kind,
            string prefix)
        {
            var occupied = new HashSet<string>(targetIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceId in (sourceIds ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(sourceId)) throw new InvalidOperationException("Remapped-copy source contains a blank " + kind + " id.");
                if (result.ContainsKey(sourceId)) throw new InvalidOperationException("Remapped-copy source contains duplicate " + kind + " id: " + sourceId + ".");
                var mapped = prefix + "-" + Hash(importNamespace + "\n" + kind + "\n" + sourceId, 24);
                if (!occupied.Add(mapped))
                    throw new InvalidOperationException("Deterministic remapped " + kind + " id collides with target/import identity: " + mapped + ". Use a different import namespace.");
                result.Add(sourceId, mapped);
            }
            return result;
        }

        private static Dictionary<string, HashSet<InterchangeSemanticReferenceKind>> BuildSourceIdentityIndex(ProjectInterchangeValidatedSnapshot source)
        {
            var result = new Dictionary<string, HashSet<InterchangeSemanticReferenceKind>>(StringComparer.OrdinalIgnoreCase);
            AddSourceIds(result, source.Zones.Select(x => x.Id), InterchangeSemanticReferenceKind.Zone);
            AddSourceIds(result, source.Floors.Select(x => x.Id), InterchangeSemanticReferenceKind.Floor);
            AddSourceIds(result, source.Families.Select(x => x.Id), InterchangeSemanticReferenceKind.Family);
            AddSourceIds(result, source.Elements.Select(x => x.Id), InterchangeSemanticReferenceKind.Element);
            return result;
        }

        private static void AddSourceIds(Dictionary<string, HashSet<InterchangeSemanticReferenceKind>> index, IEnumerable<string> ids, InterchangeSemanticReferenceKind kind)
        {
            foreach (var id in ids)
            {
                if (!index.TryGetValue(id, out var kinds))
                {
                    kinds = new HashSet<InterchangeSemanticReferenceKind>();
                    index.Add(id, kinds);
                }
                kinds.Add(kind);
            }
        }

        private static Dictionary<string, string> RemapProperties(
            IReadOnlyDictionary<string, string> source,
            string ownerLabel,
            IReadOnlyDictionary<string, HashSet<InterchangeSemanticReferenceKind>> sourceIdentityIndex,
            IReadOnlyDictionary<string, string> zoneMap,
            IReadOnlyDictionary<string, string> floorMap,
            IReadOnlyDictionary<string, string> familyMap,
            IReadOnlyDictionary<string, string> elementMap,
            PreparationCounters counters)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.Ordinal))
            {
                var key = (pair.Key ?? string.Empty).Trim();
                if (key.Length == 0) throw new InvalidOperationException(ownerLabel + " contains a blank property key.");
                if (IsGeneratedOwnershipProperty(key))
                {
                    counters.GeneratedOwnershipPropertiesDiscarded = checked(counters.GeneratedOwnershipPropertiesDiscarded + 1);
                    continue;
                }

                var value = pair.Value ?? string.Empty;
                if (ProjectInterchangeSemanticReferencePolicy.TryGetPropertyReference(key, out var reference))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        result[key] = string.Empty;
                        continue;
                    }
                    result[key] = RemapByKind(value.Trim(), reference.Kind, zoneMap, floorMap, familyMap, elementMap, ownerLabel + "/" + key);
                    counters.PropertyReferencesRemapped = checked(counters.PropertyReferencesRemapped + 1);
                    continue;
                }

                var trimmed = value.Trim();
                if (trimmed.Length > 0 && ProjectInterchangeSemanticReferencePolicy.LooksLikeSemanticReferenceKey(key) && sourceIdentityIndex.ContainsKey(trimmed))
                    throw new InvalidOperationException(ownerLabel + "/" + key + " looks like a semantic identity reference to source id '" + trimmed + "' but has no registered remap policy. Add an explicit reference policy before importing this snapshot.");
                result[key] = value;
            }
            return result;
        }

        private static string RemapByKind(
            string sourceId,
            InterchangeSemanticReferenceKind kind,
            IReadOnlyDictionary<string, string> zoneMap,
            IReadOnlyDictionary<string, string> floorMap,
            IReadOnlyDictionary<string, string> familyMap,
            IReadOnlyDictionary<string, string> elementMap,
            string label)
        {
            switch (kind)
            {
                case InterchangeSemanticReferenceKind.Zone: return RemapRequired(sourceId, zoneMap, "Zone property reference", label);
                case InterchangeSemanticReferenceKind.Floor: return RemapRequired(sourceId, floorMap, "Floor property reference", label);
                case InterchangeSemanticReferenceKind.Family: return RemapRequired(sourceId, familyMap, "Family property reference", label);
                case InterchangeSemanticReferenceKind.Element: return RemapRequired(sourceId, elementMap, "Element property reference", label);
                default: throw new InvalidOperationException("Unsupported semantic property reference kind: " + kind + ".");
            }
        }

        private static string RemapOptional(string sourceId, IReadOnlyDictionary<string, string> map, string kind, string elementId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) return string.Empty;
            return RemapRequired(sourceId, map, kind, elementId);
        }

        private static string RemapRequired(string sourceId, IReadOnlyDictionary<string, string> map, string kind, string ownerId)
        {
            var id = (sourceId ?? string.Empty).Trim();
            if (id.Length == 0 || !map.TryGetValue(id, out var mapped))
                throw new InvalidOperationException(ownerId + " references source " + kind + " id that is not part of the remapped copy: " + id + ".");
            return mapped;
        }

        private static bool IsGeneratedOwnershipProperty(string key)
        {
            return key.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase);
        }

        private static string MapDisplayName(string sourceName, string importNamespace, string sourceId, int maxLength, ISet<string> occupied)
        {
            var baseName = (sourceName ?? string.Empty).Trim();
            if (baseName.Length == 0) throw new InvalidOperationException("Remapped-copy source display name is required.");
            var suffix = " [" + importNamespace + "]";
            var candidate = BoundedName(baseName, suffix, maxLength);
            if (occupied.Add(candidate)) return candidate;
            suffix = " [" + importNamespace + "~" + Hash(sourceId, 8) + "]";
            candidate = BoundedName(baseName, suffix, maxLength);
            if (occupied.Add(candidate)) return candidate;
            throw new InvalidOperationException("Deterministic remapped display name still collides: " + candidate + ". Use a different import namespace.");
        }

        private static string BoundedName(string baseName, string suffix, int maxLength)
        {
            if (suffix.Length >= maxLength) throw new InvalidOperationException("Import namespace is too long for remapped display names.");
            var allowed = maxLength - suffix.Length;
            var head = baseName.Length <= allowed ? baseName : baseName.Substring(0, allowed).TrimEnd();
            if (head.Length == 0) head = "Imported";
            if (head.Length + suffix.Length > maxLength) head = head.Substring(0, maxLength - suffix.Length);
            return head + suffix;
        }

        private static string NormalizeNamespace(string importNamespace)
        {
            var value = (importNamespace ?? string.Empty).Trim();
            if (value.Length < 1 || value.Length > 40) throw new ArgumentException("Import namespace must contain 1..40 characters.", nameof(importNamespace));
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
                throw new ArgumentException("Import namespace may contain only letters, digits, '-', '_' and '.'.", nameof(importNamespace));
            }
            return value;
        }

        private static string Hash(string value, int hexCharacters)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var text = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
                return text.Substring(0, Math.Min(hexCharacters, text.Length));
            }
        }

        private static void ValidateTargetIdentitySurface(ProjectState target)
        {
            if (string.IsNullOrWhiteSpace(target.ProjectId)) throw new InvalidOperationException("Target project id is required.");
            RequireUnique(target.Zones, x => x == null ? string.Empty : x.Id, "Zone id");
            RequireUnique(target.Zones, x => x == null ? string.Empty : x.Name, "Zone name");
            RequireUnique(target.Floors, x => x == null ? string.Empty : x.Id, "Floor id");
            RequireUnique(target.Floors, x => x == null ? string.Empty : x.Name, "Floor name");
            RequireUnique(target.Families, x => x == null ? string.Empty : x.Id, "Family id");
            var familyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in target.Families)
            {
                if (family == null) throw new InvalidOperationException("Target project contains a null Family entry.");
                var key = family.Category + "\n" + (family.Name ?? string.Empty).Trim();
                if (!familyNames.Add(key)) throw new InvalidOperationException("Target project contains duplicate category-scoped Family name: " + family.Name + ".");
            }
            RequireUnique(target.Elements, x => x == null ? string.Empty : x.Id, "Element id");
        }

        private static void RequireUnique<T>(IEnumerable<T> items, Func<T, string> selector, string label)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (item == null) throw new InvalidOperationException("Target project contains a null " + label + " entry.");
                var value = (selector(item) ?? string.Empty).Trim();
                if (value.Length == 0) throw new InvalidOperationException("Target project contains a blank " + label + ".");
                if (!ids.Add(value)) throw new InvalidOperationException("Target project contains duplicate " + label + ": " + value + ".");
            }
        }

        private static void ValidateImportedReferences(ProjectState target, PreparedImport prepared)
        {
            foreach (var element in prepared.Elements)
            {
                if (!string.IsNullOrWhiteSpace(element.FamilyId) && target.FindFamily(element.FamilyId) == null)
                    throw new InvalidOperationException("Remapped imported Element has missing Family after mutation: " + element.Id + ".");
                if (!string.IsNullOrWhiteSpace(element.FloorId) && target.FindFloor(element.FloorId) == null)
                    throw new InvalidOperationException("Remapped imported Element has missing Floor after mutation: " + element.Id + ".");
                if (!string.IsNullOrWhiteSpace(element.ZoneId) && target.FindZone(element.ZoneId) == null)
                    throw new InvalidOperationException("Remapped imported Element has missing Zone after mutation: " + element.Id + ".");
                foreach (var dependency in element.Dependencies)
                    if (target.FindElement(dependency) == null)
                        throw new InvalidOperationException("Remapped imported Element has missing dependency after mutation: " + element.Id + "/" + dependency + ".");
            }
        }
    }
}
