using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeRemapAppendPlan
    {
        internal ProjectInterchangeRemapAppendPlan(ProjectInterchangeRemapPlan remap, int ownershipPropertiesToDiscard)
        {
            Remap = remap ?? throw new ArgumentNullException(nameof(remap));
            OwnershipPropertiesToDiscard = ownershipPropertiesToDiscard;
        }

        public ProjectInterchangeRemapPlan Remap { get; }
        public int OwnershipPropertiesToDiscard { get; }
        public int IdRemapCount => Remap.IdRemapCount;
        public int NameRemapCount => Remap.NameRemapCount;
        public int ReferenceRewriteCount => Remap.ReferenceRewrites.Count;
        public int SourceHandleCount { get; internal set; }
        public bool CanImport => Remap.CanAppendAsNew;
    }

    public sealed class ProjectInterchangeRemapAppendResult
    {
        public int ZonesAdded { get; internal set; }
        public int FloorsAdded { get; internal set; }
        public int FamiliesAdded { get; internal set; }
        public int ElementsAdded { get; internal set; }
        public int SourceHandlesDiscarded { get; internal set; }
        public int OwnershipPropertiesDiscarded { get; internal set; }
        public int IdsRemapped { get; internal set; }
        public int NamesRemapped { get; internal set; }
        public int ReferencesRewritten { get; internal set; }
    }

    /// <summary>
    /// Appends a validated snapshot as a new semantic namespace using a deterministic remap plan.
    /// Existing target identities are never replaced. Incoming drawing-local/native ownership is discarded.
    /// </summary>
    public static class ProjectInterchangeRemapAppendImporter
    {
        private const string ImportMode = "RemapAppendAsNew";
        private const string HostWallIdKey = "HostWallId";
        private const string LastModeKey = "Interchange.LastImport.Mode";
        private const string LastSourceProjectIdKey = "Interchange.LastImport.SourceProjectId";
        private const string LastImportedUtcKey = "Interchange.LastImport.ImportedUtc";
        private const string LastSourceHandlesDiscardedKey = "Interchange.LastImport.SourceHandlesDiscarded";
        private const string LastOwnershipPropertiesDiscardedKey = "Interchange.LastImport.OwnershipPropertiesDiscarded";
        private const string LastIdsRemappedKey = "Interchange.LastImport.IdsRemapped";
        private const string LastNamesRemappedKey = "Interchange.LastImport.NamesRemapped";
        private const string LastReferencesRewrittenKey = "Interchange.LastImport.ReferencesRewritten";

        public static ProjectInterchangeRemapAppendPlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var remap = ProjectInterchangeRemapPlanner.Plan(target, json);
            var ownershipProperties = checked(
                source.Families.Sum(x => x.Properties.Count(p => IsImportedOwnershipMetadata(p.Key))) +
                source.Elements.Sum(x => x.Properties.Count(p => IsImportedOwnershipMetadata(p.Key))));
            return new ProjectInterchangeRemapAppendPlan(remap, ownershipProperties)
            {
                SourceHandleCount = CountSourceHandles(source)
            };
        }

        public static ProjectInterchangeRemapAppendResult Import(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            // Re-plan against the current target immediately before mutation. Candidate IDs/names from
            // an earlier UI preview are never trusted after target state may have changed.
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var plan = Plan(target, json);
            ValidateExecutionSafety(source, plan);
            var rollback = ProjectStateSnapshot.Capture(target);

            try
            {
                var ownershipDiscarded = 0;
                var rewrites = 0;

                var zones = source.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var snapshot in zones)
                {
                    var item = Item(plan.Remap, InterchangeRemapIdentityKind.Zone, snapshot.Id);
                    ProjectZoneService.Create(target, item.TargetId, item.TargetName);
                }

                var floors = source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var snapshot in floors)
                {
                    var item = Item(plan.Remap, InterchangeRemapIdentityKind.Floor, snapshot.Id);
                    ProjectFloorService.Create(target, item.TargetId, item.TargetName, snapshot.ElevationM);
                }

                var families = source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var snapshot in families)
                {
                    var item = Item(plan.Remap, InterchangeRemapIdentityKind.Family, snapshot.Id);
                    var family = ProjectFamilyService.Create(target, item.TargetId, item.TargetName, snapshot.Category);
                    foreach (var property in snapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        if (IsImportedOwnershipMetadata(property.Key))
                        {
                            ownershipDiscarded = checked(ownershipDiscarded + 1);
                            continue;
                        }
                        if (LooksLikeUnregisteredSemanticReference(property.Key, property.Value))
                            throw new InvalidOperationException(
                                "Import As New found unregistered ID/ref-like Family property " + property.Key +
                                " on source Family " + snapshot.Id + ". Register an explicit rewrite policy before importing.");
                        ProjectFamilyService.SetProperty(target, family.Id, property.Key, property.Value ?? string.Empty);
                    }
                }

                var elements = source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var snapshot in elements)
                {
                    var targetId = plan.Remap.MapId(InterchangeRemapIdentityKind.Element, snapshot.Id);
                    var familyId = MapOptional(plan.Remap, InterchangeRemapIdentityKind.Family, snapshot.FamilyId, ref rewrites);
                    var floorId = MapOptional(plan.Remap, InterchangeRemapIdentityKind.Floor, snapshot.FloorId, ref rewrites);
                    var zoneId = MapOptional(plan.Remap, InterchangeRemapIdentityKind.Zone, snapshot.ZoneId, ref rewrites);
                    var added = new ProjectElement(targetId, snapshot.Category, familyId, floorId, zoneId)
                    {
                        // Import As New has no authoritative CAD source in the active drawing.
                        DrawingFingerprint = string.Empty
                    };

                    foreach (var dependency in snapshot.Dependencies)
                    {
                        var mapped = plan.Remap.MapId(InterchangeRemapIdentityKind.Element, dependency);
                        if (!string.Equals(mapped, dependency, StringComparison.Ordinal)) rewrites = checked(rewrites + 1);
                        added.DependsOn.Add(mapped);
                    }

                    foreach (var property in snapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        if (IsImportedOwnershipMetadata(property.Key))
                        {
                            ownershipDiscarded = checked(ownershipDiscarded + 1);
                            continue;
                        }

                        if (string.Equals(property.Key, HostWallIdKey, StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrWhiteSpace(property.Value))
                            {
                                added.Properties[property.Key] = string.Empty;
                                continue;
                            }
                            var sourceHost = property.Value.Trim();
                            var mappedHost = plan.Remap.MapId(InterchangeRemapIdentityKind.Element, sourceHost);
                            if (!string.Equals(mappedHost, sourceHost, StringComparison.Ordinal)) rewrites = checked(rewrites + 1);
                            added.Properties[property.Key] = mappedHost;
                            continue;
                        }

                        if (LooksLikeUnregisteredSemanticReference(property.Key, property.Value))
                            throw new InvalidOperationException(
                                "Import As New found unregistered ID/ref-like property " + property.Key +
                                " on source Element " + snapshot.Id + ". Register an explicit rewrite policy before importing.");

                        added.Properties[property.Key] = property.Value ?? string.Empty;
                    }

                    foreach (var quantity in snapshot.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        added.Quantities[quantity.Key] = quantity.Value;

                    added.MarkDirty(ElementDirtyFlags.All);
                    target.Elements.Add(added);
                }

                if (ownershipDiscarded != plan.OwnershipPropertiesToDiscard)
                    throw new InvalidOperationException(
                        "Import As New ownership discard count changed after planning. Planned " +
                        plan.OwnershipPropertiesToDiscard.ToString(CultureInfo.InvariantCulture) +
                        ", applied " + ownershipDiscarded.ToString(CultureInfo.InvariantCulture) + ". Refusing stale import authorization.");

                ValidateCombinedTarget(target);

                target.Metadata[LastModeKey] = ImportMode;
                target.Metadata[LastSourceProjectIdKey] = source.Project.Id;
                target.Metadata[LastImportedUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                target.Metadata[LastSourceHandlesDiscardedKey] = plan.SourceHandleCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastOwnershipPropertiesDiscardedKey] = ownershipDiscarded.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastIdsRemappedKey] = plan.IdRemapCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastNamesRemappedKey] = plan.NameRemapCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastReferencesRewrittenKey] = rewrites.ToString(CultureInfo.InvariantCulture);

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeRemapAppendAsNew",
                    string.Empty,
                    "Imported snapshot as new semantic identities from project " + source.Project.Id +
                    ": zones=" + zones.Count.ToString(CultureInfo.InvariantCulture) +
                    ", floors=" + floors.Count.ToString(CultureInfo.InvariantCulture) +
                    ", families=" + families.Count.ToString(CultureInfo.InvariantCulture) +
                    ", elements=" + elements.Count.ToString(CultureInfo.InvariantCulture) +
                    ", idsRemapped=" + plan.IdRemapCount.ToString(CultureInfo.InvariantCulture) +
                    ", namesRemapped=" + plan.NameRemapCount.ToString(CultureInfo.InvariantCulture) +
                    ", referencesRewritten=" + rewrites.ToString(CultureInfo.InvariantCulture) +
                    ", sourceHandlesDiscarded=" + plan.SourceHandleCount.ToString(CultureInfo.InvariantCulture) +
                    ", ownershipPropertiesDiscarded=" + ownershipDiscarded.ToString(CultureInfo.InvariantCulture) +
                    ". No imported handle/fingerprint became target DWG ownership.");
                target.Touch();

                return new ProjectInterchangeRemapAppendResult
                {
                    ZonesAdded = zones.Count,
                    FloorsAdded = floors.Count,
                    FamiliesAdded = families.Count,
                    ElementsAdded = elements.Count,
                    SourceHandlesDiscarded = plan.SourceHandleCount,
                    OwnershipPropertiesDiscarded = ownershipDiscarded,
                    IdsRemapped = plan.IdRemapCount,
                    NamesRemapped = plan.NameRemapCount,
                    ReferencesRewritten = rewrites
                };
            }
            catch
            {
                rollback.Restore(target);
                throw;
            }
        }

        private static void ValidateExecutionSafety(ProjectInterchangeValidatedSnapshot source, ProjectInterchangeRemapAppendPlan plan)
        {
            if (!plan.CanImport)
            {
                var first = plan.Remap.OpaqueReferenceWarnings.FirstOrDefault();
                throw new InvalidOperationException(
                    "Import As New is blocked by unresolved property-carried reference policy" +
                    (first == null ? "." : ": " + first.OwnerElementSourceId + " / " + first.PropertyKey + ". " + first.Reason));
            }

            foreach (var family in source.Families)
            {
                foreach (var property in family.Properties)
                {
                    if (IsImportedOwnershipMetadata(property.Key)) continue;
                    if (!LooksLikeUnregisteredSemanticReference(property.Key, property.Value)) continue;
                    throw new InvalidOperationException(
                        "Import As New is fail-closed because Family property " + property.Key + " on source Family " + family.Id +
                        " looks like a semantic ID/reference but has no explicit rewrite policy.");
                }
            }

            foreach (var element in source.Elements)
            {
                foreach (var property in element.Properties)
                {
                    if (IsImportedOwnershipMetadata(property.Key)) continue;
                    if (string.Equals(property.Key, HostWallIdKey, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!LooksLikeUnregisteredSemanticReference(property.Key, property.Value)) continue;
                    throw new InvalidOperationException(
                        "Import As New is fail-closed because property " + property.Key + " on source Element " + element.Id +
                        " looks like a semantic ID/reference but has no explicit rewrite policy.");
                }
            }
        }

        private static int CountSourceHandles(ProjectInterchangeValidatedSnapshot source)
        {
            var count = 0;
            foreach (var element in source.Elements)
                count = checked(count + element.SourceHandles.Count);
            return count;
        }

        private static bool LooksLikeUnregisteredSemanticReference(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) return false;
            var k = key.Trim();
            return k.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                   k.EndsWith("Ids", StringComparison.OrdinalIgnoreCase) ||
                   k.EndsWith("Ref", StringComparison.OrdinalIgnoreCase) ||
                   k.EndsWith("Refs", StringComparison.OrdinalIgnoreCase) ||
                   k.EndsWith("RefId", StringComparison.OrdinalIgnoreCase) ||
                   k.EndsWith("RefIds", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsImportedOwnershipMetadata(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            var k = key.Trim();
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(k)) return true;
            if (k.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (k.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return true;
            // Handle-bearing properties are drawing-local by construction. Keep descriptive CAD.*
            // properties such as CAD.Layer/CAD.EntityType, but never copy a handle owner/reference.
            return k.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string MapOptional(
            ProjectInterchangeRemapPlan plan,
            InterchangeRemapIdentityKind kind,
            string sourceId,
            ref int rewrites)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) return string.Empty;
            var trimmed = sourceId.Trim();
            var mapped = plan.MapId(kind, trimmed);
            if (!string.Equals(mapped, trimmed, StringComparison.Ordinal)) rewrites = checked(rewrites + 1);
            return mapped;
        }

        private static ProjectInterchangeRemapItem Item(ProjectInterchangeRemapPlan plan, InterchangeRemapIdentityKind kind, string sourceId) =>
            plan.Items.Single(x => x.Kind == kind && string.Equals(x.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));

        private static void ValidateCombinedTarget(ProjectState target)
        {
            foreach (var element in target.Elements)
            {
                if (!string.IsNullOrWhiteSpace(element.ZoneId) && target.FindZone(element.ZoneId) == null)
                    throw new InvalidOperationException("Combined target Element " + element.Id + " references missing Zone " + element.ZoneId + ".");
                if (!string.IsNullOrWhiteSpace(element.FloorId) && target.FindFloor(element.FloorId) == null)
                    throw new InvalidOperationException("Combined target Element " + element.Id + " references missing Floor " + element.FloorId + ".");
                if (!string.IsNullOrWhiteSpace(element.FamilyId))
                {
                    var family = target.FindFamily(element.FamilyId);
                    if (family == null)
                        throw new InvalidOperationException("Combined target Element " + element.Id + " references missing Family " + element.FamilyId + ".");
                    if (family.Category != element.Category)
                        throw new InvalidOperationException("Combined target Element " + element.Id + " references incompatible Family category " + family.Category + ".");
                }
            }

            var graph = new DependencyGraph();
            graph.Rebuild(target.Elements);
            graph.TopologicalDirtyOrder(target.Elements);
        }
    }
}
