using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeAppendOnlyImportPlan
    {
        internal ProjectInterchangeAppendOnlyImportPlan(
            string sourceProjectId,
            int sourceSchemaVersion,
            string sourceDrawingFingerprint,
            int zonesToAdd,
            int floorsToAdd,
            int familiesToAdd,
            int elementsToAdd,
            int sourceHandlesToDiscard,
            int validationWarnings)
        {
            SourceProjectId = sourceProjectId;
            SourceSchemaVersion = sourceSchemaVersion;
            SourceDrawingFingerprint = sourceDrawingFingerprint;
            ZonesToAdd = zonesToAdd;
            FloorsToAdd = floorsToAdd;
            FamiliesToAdd = familiesToAdd;
            ElementsToAdd = elementsToAdd;
            SourceHandlesToDiscard = sourceHandlesToDiscard;
            ValidationWarnings = validationWarnings;
        }

        public string SourceProjectId { get; }
        public int SourceSchemaVersion { get; }
        public string SourceDrawingFingerprint { get; }
        public int ZonesToAdd { get; }
        public int FloorsToAdd { get; }
        public int FamiliesToAdd { get; }
        public int ElementsToAdd { get; }
        public int SourceHandlesToDiscard { get; }
        public int ValidationWarnings { get; }
        public int TotalSemanticIdentitiesToAdd => checked(ZonesToAdd + FloorsToAdd + FamiliesToAdd + ElementsToAdd);
    }

    public sealed class ProjectInterchangeAppendOnlyImportResult
    {
        internal ProjectInterchangeAppendOnlyImportResult(ProjectInterchangeAppendOnlyImportPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            SourceProjectId = plan.SourceProjectId;
            SourceSchemaVersion = plan.SourceSchemaVersion;
            SourceDrawingFingerprint = plan.SourceDrawingFingerprint;
            ZonesAdded = plan.ZonesToAdd;
            FloorsAdded = plan.FloorsToAdd;
            FamiliesAdded = plan.FamiliesToAdd;
            ElementsAdded = plan.ElementsToAdd;
            SourceHandlesDiscarded = plan.SourceHandlesToDiscard;
            ValidationWarnings = plan.ValidationWarnings;
        }

        public string SourceProjectId { get; }
        public int SourceSchemaVersion { get; }
        public string SourceDrawingFingerprint { get; }
        public int ZonesAdded { get; }
        public int FloorsAdded { get; }
        public int FamiliesAdded { get; }
        public int ElementsAdded { get; }
        public int SourceHandlesDiscarded { get; }
        public int ValidationWarnings { get; }
    }

    public static class ProjectInterchangeAppendOnlyImporter
    {
        private sealed class PreparedImport
        {
            public PreparedImport(ProjectInterchangeValidatedSnapshot source, ProjectInterchangeAppendOnlyImportPlan plan)
            {
                Source = source;
                Plan = plan;
            }

            public ProjectInterchangeValidatedSnapshot Source { get; }
            public ProjectInterchangeAppendOnlyImportPlan Plan { get; }
        }

        public const string ImportMode = "AppendOnly";
        public const string LastModeKey = "Interchange.LastImport.Mode";
        public const string LastSourceProjectIdKey = "Interchange.LastImport.SourceProjectId";
        public const string LastSourceSchemaVersionKey = "Interchange.LastImport.SourceSchemaVersion";
        public const string LastSourceDrawingFingerprintKey = "Interchange.LastImport.SourceDrawingFingerprint";
        public const string LastSourceUpdatedUtcKey = "Interchange.LastImport.SourceUpdatedUtc";
        public const string LastImportedUtcKey = "Interchange.LastImport.Utc";
        public const string LastSourceHandlesDiscardedKey = "Interchange.LastImport.SourceHandlesDiscarded";

        public static ProjectInterchangeAppendOnlyImportPlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return Prepare(target, json).Plan;
        }

        public static ProjectInterchangeAppendOnlyImportResult Import(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var prepared = Prepare(target, json);
            var source = prepared.Source;
            var plan = prepared.Plan;
            var snapshot = ProjectStateSnapshot.Capture(target);
            var targetHadZones = target.Zones.Count > 0;
            var targetHadFloors = target.Floors.Count > 0;
            var targetHadFamilies = target.Families.Count > 0;
            var previousActiveZoneId = target.ActiveZoneId ?? string.Empty;
            var previousActiveFloorId = target.ActiveFloorId ?? string.Empty;
            var hadActiveFamilyMetadata = target.Metadata.TryGetValue("ActiveFamilyId", out var previousActiveFamilyId);
            previousActiveFamilyId = previousActiveFamilyId ?? string.Empty;

            try
            {
                foreach (var zone in source.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                    ProjectZoneService.Create(target, zone.Id, zone.Name);

                foreach (var floor in source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                    ProjectFloorService.Create(target, floor.Id, floor.Name, floor.ElevationM);

                foreach (var familySnapshot in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var family = ProjectFamilyService.Create(target, familySnapshot.Id, familySnapshot.Name, familySnapshot.Category);
                    foreach (var property in familySnapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        family.Properties[property.Key] = property.Value ?? string.Empty;
                }

                foreach (var elementSnapshot in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var element = new ProjectElement(
                        elementSnapshot.Id,
                        elementSnapshot.Category,
                        elementSnapshot.FamilyId,
                        elementSnapshot.FloorId,
                        elementSnapshot.ZoneId);

                    // Source CAD identity is drawing-local provenance only. Import never turns it into
                    // ownership in the target DWG and never reconstructs generated/native ownership.
                    element.DrawingFingerprint = string.Empty;
                    foreach (var dependency in elementSnapshot.Dependencies)
                        element.DependsOn.Add(dependency);
                    foreach (var property in elementSnapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        element.Properties[property.Key] = property.Value ?? string.Empty;
                    foreach (var quantity in elementSnapshot.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        element.Quantities[quantity.Key] = quantity.Value;
                    element.MarkDirty(ElementDirtyFlags.All);
                    target.Elements.Add(element);
                }

                // Existing target context wins. Only a previously empty catalog receives the first
                // imported item as its active default, matching normal create behavior.
                if (targetHadZones) target.ActiveZoneId = previousActiveZoneId;
                if (targetHadFloors) target.ActiveFloorId = previousActiveFloorId;
                if (targetHadFamilies)
                {
                    if (hadActiveFamilyMetadata) target.Metadata["ActiveFamilyId"] = previousActiveFamilyId;
                    else target.Metadata.Remove("ActiveFamilyId");
                }
                else if (source.Families.Count > 0 && (!hadActiveFamilyMetadata || string.IsNullOrWhiteSpace(previousActiveFamilyId)))
                {
                    ProjectFamilyActivationService.SetActive(
                        target,
                        source.Families.OrderBy(x => x.Category).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).First().Id);
                }

                target.Metadata[LastModeKey] = ImportMode;
                target.Metadata[LastSourceProjectIdKey] = source.Project.Id;
                target.Metadata[LastSourceSchemaVersionKey] = source.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastSourceDrawingFingerprintKey] = source.Project.DrawingFingerprint;
                target.Metadata[LastSourceUpdatedUtcKey] = source.Project.UpdatedUtcRaw;
                target.Metadata[LastImportedUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                target.Metadata[LastSourceHandlesDiscardedKey] = plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture);

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeAppendOnly",
                    string.Empty,
                    "Imported semantic snapshot from project " + source.Project.Id +
                    ": zones=" + plan.ZonesToAdd.ToString(CultureInfo.InvariantCulture) +
                    ", floors=" + plan.FloorsToAdd.ToString(CultureInfo.InvariantCulture) +
                    ", families=" + plan.FamiliesToAdd.ToString(CultureInfo.InvariantCulture) +
                    ", elements=" + plan.ElementsToAdd.ToString(CultureInfo.InvariantCulture) +
                    ", discardedDrawingHandles=" + plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) + ".");

                ValidateTarget(target);
                return new ProjectInterchangeAppendOnlyImportResult(plan);
            }
            catch
            {
                snapshot.Restore(target);
                throw;
            }
        }

        private static PreparedImport Prepare(ProjectState target, string json)
        {
            ValidateTarget(target);
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            PreflightCollisions(target, source);

            var sourceHandlesToDiscard = 0;
            foreach (var element in source.Elements)
                sourceHandlesToDiscard = checked(sourceHandlesToDiscard + element.SourceHandles.Count);

            var plan = new ProjectInterchangeAppendOnlyImportPlan(
                source.Project.Id,
                source.Project.SchemaVersion,
                source.Project.DrawingFingerprint,
                source.Zones.Count,
                source.Floors.Count,
                source.Families.Count,
                source.Elements.Count,
                sourceHandlesToDiscard,
                source.Validation.WarningCount);
            return new PreparedImport(source, plan);
        }

        private static void PreflightCollisions(ProjectState target, ProjectInterchangeValidatedSnapshot source)
        {
            var zoneIds = new HashSet<string>(target.Zones.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var zoneNames = new HashSet<string>(target.Zones.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var zone in source.Zones)
            {
                RequireNew(zoneIds, zone.Id, "Zone id");
                RequireNew(zoneNames, zone.Name, "Zone name");
            }

            var floorIds = new HashSet<string>(target.Floors.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var floorNames = new HashSet<string>(target.Floors.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var floor in source.Floors)
            {
                RequireNew(floorIds, floor.Id, "Floor id");
                RequireNew(floorNames, floor.Name, "Floor name");
            }

            var familyIds = new HashSet<string>(target.Families.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var familyNames = new HashSet<string>(
                target.Families.Select(x => FamilyNameKey(x.Category, x.Name)),
                StringComparer.OrdinalIgnoreCase);
            foreach (var family in source.Families)
            {
                RequireNew(familyIds, family.Id, "Family id");
                RequireNew(familyNames, FamilyNameKey(family.Category, family.Name), family.Category + " Family name");
            }

            var elementIds = new HashSet<string>(target.Elements.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var element in source.Elements)
                RequireNew(elementIds, element.Id, "Element id");
        }

        private static void ValidateTarget(ProjectState target)
        {
            if (string.IsNullOrWhiteSpace(target.ProjectId)) throw new InvalidOperationException("Target project id is required.");

            var zoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var zoneNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var zone in target.Zones)
            {
                if (zone == null) throw new InvalidOperationException("Target project contains a null Zone entry.");
                RequireExistingUnique(zoneIds, zone.Id, "Zone id");
                RequireExistingUnique(zoneNames, zone.Name, "Zone name");
            }
            if (!string.IsNullOrWhiteSpace(target.ActiveZoneId) && !zoneIds.Contains(target.ActiveZoneId))
                throw new InvalidOperationException("Target project has a stale active Zone id: " + target.ActiveZoneId);

            var floorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var floorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var floor in target.Floors)
            {
                if (floor == null) throw new InvalidOperationException("Target project contains a null Floor entry.");
                if (double.IsNaN(floor.ElevationM) || double.IsInfinity(floor.ElevationM))
                    throw new InvalidOperationException("Target project contains a non-finite Floor elevation: " + floor.Id);
                RequireExistingUnique(floorIds, floor.Id, "Floor id");
                RequireExistingUnique(floorNames, floor.Name, "Floor name");
            }
            if (!string.IsNullOrWhiteSpace(target.ActiveFloorId) && !floorIds.Contains(target.ActiveFloorId))
                throw new InvalidOperationException("Target project has a stale active Floor id: " + target.ActiveFloorId);

            var familyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var familyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in target.Families)
            {
                if (family == null) throw new InvalidOperationException("Target project contains a null Family entry.");
                RequireExistingUnique(familyIds, family.Id, "Family id");
                RequireExistingUnique(familyNames, FamilyNameKey(family.Category, family.Name), family.Category + " Family name");
            }
            if (target.Metadata.TryGetValue("ActiveFamilyId", out var activeFamilyId) &&
                !string.IsNullOrWhiteSpace(activeFamilyId) && !familyIds.Contains(activeFamilyId))
                throw new InvalidOperationException("Target project has a stale active Family id: " + activeFamilyId);

            var elementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in target.Elements)
            {
                if (element == null) throw new InvalidOperationException("Target project contains a null semantic element entry.");
                RequireExistingUnique(elementIds, element.Id, "Element id");
                if (!string.IsNullOrWhiteSpace(element.FamilyId) && !familyIds.Contains(element.FamilyId))
                    throw new InvalidOperationException("Target element " + element.Id + " references missing Family " + element.FamilyId + ".");
                if (!string.IsNullOrWhiteSpace(element.FloorId) && !floorIds.Contains(element.FloorId))
                    throw new InvalidOperationException("Target element " + element.Id + " references missing Floor " + element.FloorId + ".");
                if (!string.IsNullOrWhiteSpace(element.ZoneId) && !zoneIds.Contains(element.ZoneId))
                    throw new InvalidOperationException("Target element " + element.Id + " references missing Zone " + element.ZoneId + ".");
                foreach (var quantity in element.Quantities)
                    if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value))
                        throw new InvalidOperationException("Target element " + element.Id + " contains non-finite quantity " + quantity.Key + ".");
            }

            foreach (var element in target.Elements)
                foreach (var dependency in element.DependsOn)
                    if (!elementIds.Contains(dependency))
                        throw new InvalidOperationException("Target element " + element.Id + " references missing dependency " + dependency + ".");
        }

        private static string FamilyNameKey(ElementCategory category, string name) => category + "\u001f" + (name ?? string.Empty).Trim();

        private static void RequireNew(ISet<string> values, string value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (!values.Add(normalized)) throw new InvalidOperationException("Append-only interchange import collision on " + label + ": " + normalized);
        }

        private static void RequireExistingUnique(ISet<string> values, string value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidOperationException("Target project contains a blank " + label + ".");
            if (!values.Add(normalized)) throw new InvalidOperationException("Target project contains duplicate " + label + ": " + normalized);
        }
    }
}
