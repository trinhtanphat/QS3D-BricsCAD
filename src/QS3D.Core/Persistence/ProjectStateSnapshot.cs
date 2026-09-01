using System;
using System.Collections.Generic;
using System.Xml;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectStateSnapshot
    {
        private const int MaximumSnapshotTopLevelEntries = 100000;
        private const int MaximumSnapshotNestedEntries = 10000;
        private readonly ProjectState _snapshot;
        private readonly ProjectState _capturedProject;
        private readonly IReadOnlyDictionary<string, ZoneDefinition> _capturedZones;
        private readonly IReadOnlyDictionary<string, FloorDefinition> _capturedFloors;
        private readonly IReadOnlyDictionary<string, ProjectFamily> _capturedFamilies;
        private readonly IReadOnlyDictionary<string, ProjectElement> _capturedElements;

        private ProjectStateSnapshot(
            ProjectState snapshot,
            ProjectState capturedProject,
            IReadOnlyDictionary<string, ZoneDefinition> capturedZones,
            IReadOnlyDictionary<string, FloorDefinition> capturedFloors,
            IReadOnlyDictionary<string, ProjectFamily> capturedFamilies,
            IReadOnlyDictionary<string, ProjectElement> capturedElements)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _capturedProject = capturedProject ?? throw new ArgumentNullException(nameof(capturedProject));
            _capturedZones = capturedZones ?? throw new ArgumentNullException(nameof(capturedZones));
            _capturedFloors = capturedFloors ?? throw new ArgumentNullException(nameof(capturedFloors));
            _capturedFamilies = capturedFamilies ?? throw new ArgumentNullException(nameof(capturedFamilies));
            _capturedElements = capturedElements ?? throw new ArgumentNullException(nameof(capturedElements));
        }

        public static ProjectStateSnapshot Capture(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidateCollectionEntries(project);
            var capturedZones = CaptureZoneReferences(project);
            var capturedFloors = CaptureFloorReferences(project);
            var capturedFamilies = CaptureFamilyReferences(project);
            var capturedElements = CaptureElementReferences(project);
            return new ProjectStateSnapshot(
                CreateDetachedCopy(project),
                project,
                capturedZones,
                capturedFloors,
                capturedFamilies,
                capturedElements);
        }

        public static ProjectState CreateDetachedCopy(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return Clone(project);
        }

        public void Restore(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!string.Equals(project.ProjectId, _snapshot.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Cannot restore a snapshot into a different project id.");
            var preservingIdentity = ReferenceEquals(project, _capturedProject);
            var preservedZones = preservingIdentity ? _capturedZones : null;
            var preservedFloors = preservingIdentity ? _capturedFloors : null;
            var preservedFamilies = preservingIdentity ? _capturedFamilies : null;
            var preservedElements = preservingIdentity ? _capturedElements : null;
            CopyInto(_snapshot, project, preservedZones, preservedFloors, preservedFamilies, preservedElements);
        }

        private static ProjectState Clone(ProjectState source)
        {
            var target = new ProjectState(source.ProjectId, source.Name);
            CopyInto(source, target, null, null, null, null);
            return target;
        }

        private static IReadOnlyDictionary<string, ZoneDefinition> CaptureZoneReferences(ProjectState project)
        {
            var result = new Dictionary<string, ZoneDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var zone in project.Zones)
            {
                if (zone == null || string.IsNullOrWhiteSpace(zone.Id))
                    throw new InvalidOperationException("Cannot capture a project containing a zone without id.");
                if (result.ContainsKey(zone.Id))
                    throw new InvalidOperationException("Cannot capture a project containing duplicate zone id: " + zone.Id + ".");
                result.Add(zone.Id, zone);
            }
            return result;
        }

        private static IReadOnlyDictionary<string, FloorDefinition> CaptureFloorReferences(ProjectState project)
        {
            var result = new Dictionary<string, FloorDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in project.Floors)
            {
                if (floor == null || string.IsNullOrWhiteSpace(floor.Id))
                    throw new InvalidOperationException("Cannot capture a project containing a floor without id.");
                if (result.ContainsKey(floor.Id))
                    throw new InvalidOperationException("Cannot capture a project containing duplicate floor id: " + floor.Id + ".");
                result.Add(floor.Id, floor);
            }
            return result;
        }

        private static IReadOnlyDictionary<string, ProjectFamily> CaptureFamilyReferences(ProjectState project)
        {
            var result = new Dictionary<string, ProjectFamily>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null || string.IsNullOrWhiteSpace(family.Id))
                    throw new InvalidOperationException("Cannot capture a project containing a family without id.");
                if (result.ContainsKey(family.Id))
                    throw new InvalidOperationException("Cannot capture a project containing duplicate family id: " + family.Id + ".");
                result.Add(family.Id, family);
            }
            return result;
        }

        private static IReadOnlyDictionary<string, ProjectElement> CaptureElementReferences(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || string.IsNullOrWhiteSpace(element.Id))
                    throw new InvalidOperationException("Cannot capture a project containing an element without id.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Cannot capture a project containing duplicate element id: " + element.Id + ".");
                result.Add(element.Id, element);
            }
            return result;
        }

        private static void CopyInto(
            ProjectState source,
            ProjectState target,
            IReadOnlyDictionary<string, ZoneDefinition>? preservedZones,
            IReadOnlyDictionary<string, FloorDefinition>? preservedFloors,
            IReadOnlyDictionary<string, ProjectFamily>? preservedFamilies,
            IReadOnlyDictionary<string, ProjectElement>? preservedElements)
        {
            ValidateCollectionEntries(source);

            target.SchemaVersion = source.SchemaVersion;
            target.RestoreSnapshotScalars(
                source.Name,
                source.DrawingPath,
                source.DrawingFingerprint,
                source.ActiveZoneId,
                source.ActiveFloorId);

            target.Zones.Clear();
            foreach (var zone in source.Zones)
            {
                ZoneDefinition copy;
                if (preservedZones != null && preservedZones.TryGetValue(zone.Id, out var preserved))
                {
                    copy = preserved;
                    CopyZoneInto(zone, copy);
                }
                else
                {
                    copy = CloneZone(zone);
                }
                target.Zones.Add(copy);
            }

            target.Floors.Clear();
            foreach (var floor in source.Floors)
            {
                FloorDefinition copy;
                if (preservedFloors != null && preservedFloors.TryGetValue(floor.Id, out var preserved))
                {
                    copy = preserved;
                    CopyFloorInto(floor, copy);
                }
                else
                {
                    copy = CloneFloor(floor);
                }
                target.Floors.Add(copy);
            }

            target.Families.Clear();
            foreach (var family in source.Families)
            {
                ProjectFamily copy;
                if (preservedFamilies != null && preservedFamilies.TryGetValue(family.Id, out var preserved))
                {
                    copy = preserved;
                    CopyFamilyInto(family, copy);
                }
                else
                {
                    copy = CloneFamily(family);
                }
                target.Families.Add(copy);
            }

            target.Elements.Clear();
            foreach (var element in source.Elements)
            {
                ProjectElement copy;
                if (preservedElements != null && preservedElements.TryGetValue(element.Id, out var preserved))
                {
                    copy = preserved;
                    CopyElementInto(element, copy);
                }
                else
                {
                    copy = CloneElement(element);
                }
                target.Elements.Add(copy);
            }

            target.QuantityRules.Clear();
            foreach (var rule in source.QuantityRules)
                target.QuantityRules.Add(new QuantityRule(rule.Id, rule.Category, rule.OutputName, rule.Expression, rule.Version));

            target.AuditEvents.Clear();
            foreach (var audit in source.AuditEvents)
            {
                target.AuditEvents.Add(new AuditEvent
                {
                    Utc = audit.Utc,
                    Action = audit.Action,
                    ElementId = audit.ElementId,
                    Detail = audit.Detail,
                    Actor = audit.Actor,
                    CorrelationId = audit.CorrelationId
                });
            }

            var targetMetadata = target.Metadata as ProjectMetadataDictionary
                ?? throw new InvalidOperationException("Project snapshot target does not expose the canonical project metadata store.");
            targetMetadata.ReplacePersistenceState(source.Metadata);
            target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);
        }

        private static void ValidateCollectionEntries(ProjectState source)
        {
            RequireSupportedCount(source.Metadata.Count, "metadata", MaximumSnapshotNestedEntries);
            RequireSupportedCount(source.Zones.Count, "zones", MaximumSnapshotTopLevelEntries);
            RequireSupportedCount(source.Floors.Count, "floors", MaximumSnapshotTopLevelEntries);
            RequireSupportedCount(source.Families.Count, "families", MaximumSnapshotTopLevelEntries);
            RequireSupportedCount(source.Elements.Count, "elements", MaximumSnapshotTopLevelEntries);
            RequireSupportedCount(source.QuantityRules.Count, "quantity rules", MaximumSnapshotTopLevelEntries);
            RequireSupportedCount(source.AuditEvents.Count, "audit events", MaximumSnapshotTopLevelEntries);

            RequireNoNullEntries(source.Zones, "zone");
            RequireNoNullEntries(source.Floors, "floor");
            RequireNoNullEntries(source.Families, "family");
            RequireNoNullEntries(source.Elements, "element");
            RequireNoNullEntries(source.QuantityRules, "quantity rule");
            RequireNoNullEntries(source.AuditEvents, "audit event");

            RequireUniqueIds(source.Zones, x => x.Id, "zone");
            RequireUniqueIds(source.Floors, x => x.Id, "floor");
            RequireUniqueIds(source.Families, x => x.Id, "family");
            RequireUniqueIds(source.Elements, x => x.Id, "element");
            RequireUniqueIds(source.QuantityRules, x => x.Id, "quantity rule");

            foreach (var family in source.Families)
            {
                RequireSupportedCount(family.Properties.Count, "family " + family.Id + " properties", MaximumSnapshotNestedEntries);
                RequireCanonicalFamilyProperties(family);
            }

            foreach (var element in source.Elements)
            {
                RequireSupportedCount(element.SourceHandles.Count, "element " + element.Id + " source handles", MaximumSnapshotNestedEntries);
                RequireSupportedCount(element.DependsOn.Count, "element " + element.Id + " dependencies", MaximumSnapshotNestedEntries);
                RequireSupportedCount(element.Properties.Count, "element " + element.Id + " properties", MaximumSnapshotNestedEntries);
                RequireSupportedCount(element.Quantities.Count, "element " + element.Id + " quantities", MaximumSnapshotNestedEntries);
                RequireCanonicalRelationIdentities(element.SourceHandles, element.Id, "source handle");
                RequireCanonicalRelationIdentities(element.DependsOn, element.Id, "dependency id");
                RequireCanonicalElementProperties(element);
                RequireCanonicalQuantities(element);
            }
        }

        private static void RequireCanonicalFamilyProperties(ProjectFamily family)
        {
            try
            {
                _ = ProjectFamilyService.SnapshotProperties(family, "Snapshot", "snapshot capture");
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Cannot snapshot Family " + family.Id + " with invalid property state.", ex);
            }
        }

        private static void RequireCanonicalRelationIdentities(IEnumerable<string> values, string elementId, string role)
        {
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Cannot snapshot element " + elementId + " with an empty " + role + ".");
                if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Cannot snapshot element " + elementId + " with a non-canonical " + role + ": '" + value + "'.");
                if (HasControlCharacter(value))
                    throw new InvalidOperationException("Cannot snapshot element " + elementId + " with a " + role + " containing control characters.");
                try
                {
                    XmlConvert.VerifyXmlChars(value);
                }
                catch (XmlException ex)
                {
                    throw new InvalidOperationException("Cannot snapshot element " + elementId + " with a " + role + " containing malformed XML text.", ex);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException("Cannot snapshot element " + elementId + " with a " + role + " containing malformed XML text.", ex);
                }
            }
        }

        private static void RequireCanonicalElementProperties(ProjectElement element)
        {
            var canonicalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.Properties)
            {
                if (string.IsNullOrWhiteSpace(property.Key))
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with an empty property name.");
                if (HasControlCharacter(property.Key))
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with a property name containing control characters.");

                var canonicalName = property.Key.Trim();
                if (!string.Equals(canonicalName, property.Key, StringComparison.Ordinal))
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with a non-canonical property name: '" + property.Key + "'.");
                RequireXmlPropertyText(canonicalName, element.Id, "name");
                RequireXmlPropertyText(property.Value ?? string.Empty, element.Id, "value");
                if (!canonicalNames.Add(canonicalName))
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with property names that collapse to the same canonical identity: " + canonicalName + ".");
            }
        }

        private static void RequireXmlPropertyText(string value, string elementId, string role)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("Cannot snapshot element " + elementId + " with a property " + role + " containing malformed XML text.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Cannot snapshot element " + elementId + " with a property " + role + " containing malformed XML text.", ex);
            }
        }

        private static void RequireCanonicalQuantities(ProjectElement element)
        {
            var canonicalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var quantity in element.Quantities)
            {
                if (string.IsNullOrWhiteSpace(quantity.Key))
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with an empty quantity name.");
                if (quantity.Key.IndexOfAny(new[] { '\t', '\r', '\n' }) >= 0 || HasControlCharacter(quantity.Key))
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with a quantity name containing control characters.");

                var canonicalName = quantity.Key.Trim();
                try
                {
                    XmlConvert.VerifyXmlChars(canonicalName);
                }
                catch (XmlException ex)
                {
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with a quantity name containing malformed XML text.", ex);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with a quantity name containing malformed XML text.", ex);
                }

                if (!canonicalNames.Add(canonicalName))
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with quantity names that collapse to the same canonical identity: " + canonicalName + ".");
                if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value) || quantity.Value < 0d)
                    throw new InvalidOperationException("Cannot snapshot element " + element.Id + " with a non-finite or negative quantity: " + canonicalName + ".");
            }
        }

        private static bool HasControlCharacter(string value)
        {
            foreach (var ch in value)
                if (char.IsControl(ch)) return true;
            return false;
        }

        private static void RequireSupportedCount(int count, string label, int maximum)
        {
            if (count <= maximum) return;
            throw new InvalidOperationException(
                "Cannot snapshot a project whose " + label + " contains more than " + maximum + " entries.");
        }

        private static void RequireNoNullEntries<T>(IEnumerable<T> values, string label) where T : class
        {
            var index = 0;
            foreach (var value in values)
            {
                if (value == null)
                    throw new InvalidOperationException("Cannot snapshot a project containing a null " + label + " entry at index " + index + ".");
                index++;
            }
        }

        private static void RequireUniqueIds<T>(IEnumerable<T> values, Func<T, string> idSelector, string label) where T : class
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                var id = idSelector(value);
                if (string.IsNullOrWhiteSpace(id))
                    throw new InvalidOperationException("Cannot snapshot a project containing a " + label + " without id.");
                if (!seen.Add(id))
                    throw new InvalidOperationException("Cannot snapshot a project containing duplicate " + label + " id: " + id + ".");
            }
        }

        private static ZoneDefinition CloneZone(ZoneDefinition source)
        {
            return new ZoneDefinition(source.Id, source.Name);
        }

        private static void CopyZoneInto(ZoneDefinition source, ZoneDefinition target)
        {
            if (!string.Equals(source.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot restore zone state into a different zone id.");
            target.Name = source.Name;
        }

        private static FloorDefinition CloneFloor(FloorDefinition source)
        {
            return new FloorDefinition(source.Id, source.Name, source.ElevationM);
        }

        private static void CopyFloorInto(FloorDefinition source, FloorDefinition target)
        {
            if (!string.Equals(source.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot restore floor state into a different floor id.");
            target.Name = source.Name;
            target.ElevationM = source.ElevationM;
        }

        private static ProjectFamily CloneFamily(ProjectFamily source)
        {
            var target = new ProjectFamily(source.Id, source.Name, source.Category);
            CopyFamilyInto(source, target);
            return target;
        }

        private static void CopyFamilyInto(ProjectFamily source, ProjectFamily target)
        {
            if (!string.Equals(source.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot restore family state into a different family id.");

            _ = ProjectFamilyService.SnapshotProperties(source, "Snapshot", "snapshot materialization");
            target.Name = source.Name;
            target.Category = source.Category;
            target.Properties.Clear();
            foreach (var property in source.Properties) target.Properties[property.Key] = property.Value;
        }

        private static ProjectElement CloneElement(ProjectElement source)
        {
            var target = new ProjectElement(source.Id, source.Category);
            CopyElementInto(source, target);
            return target;
        }

        private static void CopyElementInto(ProjectElement source, ProjectElement target)
        {
            if (!string.Equals(source.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot restore element state into a different element id.");

            target.Category = source.Category;
            target.FamilyId = source.FamilyId;
            target.FloorId = source.FloorId;
            target.ZoneId = source.ZoneId;
            target.DrawingFingerprint = source.DrawingFingerprint;

            target.SourceHandles.Clear();
            foreach (var handle in source.SourceHandles) target.SourceHandles.Add(handle);

            target.DependsOn.Clear();
            foreach (var dependency in source.DependsOn) target.DependsOn.Add(dependency);

            RequireCanonicalElementProperties(source);
            target.Properties.Clear();
            foreach (var property in source.Properties) target.Properties[property.Key] = property.Value;

            target.Quantities.Clear();
            foreach (var quantity in source.Quantities) target.SetQuantity(quantity.Key, quantity.Value);

            target.RestorePersistenceState(source.Dirty, source.UpdatedUtc);
        }
    }
}
