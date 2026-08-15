using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeKeepTargetImportPlan
    {
        internal ProjectInterchangeKeepTargetImportPlan(
            string sourceProjectId,
            int sourceSchemaVersion,
            string sourceDrawingFingerprint,
            int zonesToAdd,
            int floorsToAdd,
            int familiesToAdd,
            int elementsToAdd,
            int zonesToKeep,
            int floorsToKeep,
            int familiesToKeep,
            int elementsToKeep,
            int sourceHandlesToDiscard,
            int validationWarnings)
        {
            SourceProjectId = sourceProjectId ?? string.Empty;
            SourceSchemaVersion = sourceSchemaVersion;
            SourceDrawingFingerprint = sourceDrawingFingerprint ?? string.Empty;
            ZonesToAdd = zonesToAdd;
            FloorsToAdd = floorsToAdd;
            FamiliesToAdd = familiesToAdd;
            ElementsToAdd = elementsToAdd;
            ZonesToKeep = zonesToKeep;
            FloorsToKeep = floorsToKeep;
            FamiliesToKeep = familiesToKeep;
            ElementsToKeep = elementsToKeep;
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
        public int ZonesToKeep { get; }
        public int FloorsToKeep { get; }
        public int FamiliesToKeep { get; }
        public int ElementsToKeep { get; }
        public int SourceHandlesToDiscard { get; }
        public int ValidationWarnings { get; }
        public int TotalSemanticIdentitiesToAdd => checked(checked(ZonesToAdd + FloorsToAdd) + checked(FamiliesToAdd + ElementsToAdd));
        public int TotalSemanticIdentitiesToKeep => checked(checked(ZonesToKeep + FloorsToKeep) + checked(FamiliesToKeep + ElementsToKeep));
    }

    public sealed class ProjectInterchangeKeepTargetImportResult
    {
        internal ProjectInterchangeKeepTargetImportResult(ProjectInterchangeKeepTargetImportPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            SourceProjectId = plan.SourceProjectId;
            ZonesAdded = plan.ZonesToAdd;
            FloorsAdded = plan.FloorsToAdd;
            FamiliesAdded = plan.FamiliesToAdd;
            ElementsAdded = plan.ElementsToAdd;
            TargetIdentitiesKept = plan.TotalSemanticIdentitiesToKeep;
            SourceHandlesDiscarded = plan.SourceHandlesToDiscard;
        }

        public string SourceProjectId { get; }
        public int ZonesAdded { get; }
        public int FloorsAdded { get; }
        public int FamiliesAdded { get; }
        public int ElementsAdded { get; }
        public int TargetIdentitiesKept { get; }
        public int SourceHandlesDiscarded { get; }
    }

    public static class ProjectInterchangeKeepTargetImporter
    {
        private sealed class PreparedImport
        {
            public PreparedImport(
                ProjectInterchangeValidatedSnapshot source,
                ProjectInterchangeImportResolutionPlan resolution,
                ProjectInterchangeKeepTargetImportPlan plan)
            {
                Source = source;
                Resolution = resolution;
                Plan = plan;
            }

            public ProjectInterchangeValidatedSnapshot Source { get; }
            public ProjectInterchangeImportResolutionPlan Resolution { get; }
            public ProjectInterchangeKeepTargetImportPlan Plan { get; }
        }

        private sealed class ResolutionActionIndex
        {
            private readonly Dictionary<InterchangeIdentityKind, Dictionary<string, InterchangeImportResolutionAction>> _actionsByKind =
                new Dictionary<InterchangeIdentityKind, Dictionary<string, InterchangeImportResolutionAction>>();

            public ResolutionActionIndex(ProjectInterchangeImportResolutionPlan plan)
            {
                if (plan == null) throw new ArgumentNullException(nameof(plan));
                foreach (var item in plan.Items)
                {
                    if (!_actionsByKind.TryGetValue(item.Kind, out var actionsById))
                    {
                        actionsById = new Dictionary<string, InterchangeImportResolutionAction>(StringComparer.OrdinalIgnoreCase);
                        _actionsByKind.Add(item.Kind, actionsById);
                    }

                    if (actionsById.ContainsKey(item.Id))
                        throw new InvalidOperationException("Sequence contains more than one matching element");
                    actionsById.Add(item.Id, item.Action);
                }
            }

            public bool ShouldAdd(InterchangeIdentityKind kind, string id)
            {
                if (!_actionsByKind.TryGetValue(kind, out var actionsById) ||
                    !actionsById.TryGetValue(id ?? string.Empty, out var action))
                    throw new InvalidOperationException("Sequence contains no matching element");
                if (action == InterchangeImportResolutionAction.AddSourceSemanticData) return true;
                if (action == InterchangeImportResolutionAction.KeepTarget) return false;
                throw new InvalidOperationException("KeepTarget interchange mutation reached a non-executable resolution for " + kind + " " + id + ".");
            }
        }

        public const string ImportMode = "KeepTarget";
        public const string LastSemanticIdentitiesAddedKey = "Interchange.LastImport.SemanticIdentitiesAdded";
        public const string LastTargetIdentitiesKeptKey = "Interchange.LastImport.TargetIdentitiesKept";

        public static ProjectInterchangeKeepTargetImportPlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return Prepare(target, json).Plan;
        }

        public static ProjectInterchangeKeepTargetImportResult Import(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var prepared = Prepare(target, json);
            var source = prepared.Source;
            var resolutionActions = new ResolutionActionIndex(prepared.Resolution);
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
                    if (resolutionActions.ShouldAdd(InterchangeIdentityKind.Zone, zone.Id))
                        ProjectZoneService.Create(target, zone.Id, zone.Name);

                foreach (var floor in source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                    if (resolutionActions.ShouldAdd(InterchangeIdentityKind.Floor, floor.Id))
                        ProjectFloorService.Create(target, floor.Id, floor.Name, floor.ElevationM);

                foreach (var familySnapshot in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    if (!resolutionActions.ShouldAdd(InterchangeIdentityKind.Family, familySnapshot.Id)) continue;
                    var family = ProjectFamilyService.Create(target, familySnapshot.Id, familySnapshot.Name, familySnapshot.Category);
                    foreach (var property in familySnapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        family.Properties[property.Key] = property.Value ?? string.Empty;
                }

                foreach (var elementSnapshot in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    if (!resolutionActions.ShouldAdd(InterchangeIdentityKind.Element, elementSnapshot.Id)) continue;
                    var element = new ProjectElement(
                        elementSnapshot.Id,
                        elementSnapshot.Category,
                        elementSnapshot.FamilyId,
                        elementSnapshot.FloorId,
                        elementSnapshot.ZoneId)
                    {
                        DrawingFingerprint = string.Empty
                    };

                    foreach (var dependency in elementSnapshot.Dependencies)
                        element.DependsOn.Add(dependency);
                    foreach (var property in elementSnapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        element.Properties[property.Key] = property.Value ?? string.Empty;
                    foreach (var quantity in elementSnapshot.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        element.Quantities[quantity.Key] = quantity.Value;
                    element.MarkDirty(ElementDirtyFlags.All);
                    target.Elements.Add(element);
                }

                RestoreExistingActiveContext(
                    target,
                    source,
                    targetHadZones,
                    targetHadFloors,
                    targetHadFamilies,
                    previousActiveZoneId,
                    previousActiveFloorId,
                    hadActiveFamilyMetadata,
                    previousActiveFamilyId);

                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey] = ImportMode;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceProjectIdKey] = source.Project.Id;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceSchemaVersionKey] = source.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture);
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceDrawingFingerprintKey] = source.Project.DrawingFingerprint;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceUpdatedUtcKey] = source.Project.UpdatedUtcRaw;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastImportedUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceHandlesDiscardedKey] = plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastSemanticIdentitiesAddedKey] = plan.TotalSemanticIdentitiesToAdd.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastTargetIdentitiesKeptKey] = plan.TotalSemanticIdentitiesToKeep.ToString(CultureInfo.InvariantCulture);

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeKeepTarget",
                    string.Empty,
                    "Imported semantic snapshot from project " + source.Project.Id +
                    " with KeepTarget collision policy: added=" + plan.TotalSemanticIdentitiesToAdd.ToString(CultureInfo.InvariantCulture) +
                    ", keptTarget=" + plan.TotalSemanticIdentitiesToKeep.ToString(CultureInfo.InvariantCulture) +
                    ", discardedDrawingHandles=" + plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) + ".");

                ValidateTarget(target);
                return new ProjectInterchangeKeepTargetImportResult(plan);
            }
            catch (Exception operationError)
            {
                try
                {
                    snapshot.Restore(target);
                }
                catch (Exception restoreError)
                {
                    throw new InvalidOperationException(
                        "Interchange KeepTarget import failed and project rollback also failed.",
                        new AggregateException(operationError, restoreError));
                }

                throw;
            }
        }

        private static PreparedImport Prepare(ProjectState target, string json)
        {
            ValidateTarget(target);
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var resolution = ProjectInterchangeImportResolutionPlanner.Plan(target, json, KeepTargetPolicy());
            if (resolution.HasUnresolvedPolicy || resolution.HasBlocks || !resolution.CanProceedToMutationDesign)
            {
                var reasons = resolution.PolicyErrors
                    .Concat(resolution.GlobalBlocks)
                    .Concat(resolution.Items.Where(x => x.Action == InterchangeImportResolutionAction.BlockedIncompatible || x.Action == InterchangeImportResolutionAction.Unresolved).Select(x => x.Kind + " " + x.Id + ": " + x.Reason))
                    .Take(8)
                    .ToArray();
                throw new InvalidOperationException("KeepTarget interchange import is blocked" + (reasons.Length == 0 ? "." : ": " + string.Join("; ", reasons)));
            }

            if (resolution.Items.Any(x => x.Action != InterchangeImportResolutionAction.AddSourceSemanticData && x.Action != InterchangeImportResolutionAction.KeepTarget))
                throw new InvalidOperationException("KeepTarget interchange importer received a non KeepTarget/Add resolution action.");

            var sourceHandlesToDiscard = 0;
            foreach (var element in source.Elements)
                sourceHandlesToDiscard = checked(sourceHandlesToDiscard + element.SourceHandles.Count);

            var plan = new ProjectInterchangeKeepTargetImportPlan(
                source.Project.Id,
                source.Project.SchemaVersion,
                source.Project.DrawingFingerprint,
                Count(resolution, InterchangeIdentityKind.Zone, InterchangeImportResolutionAction.AddSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Floor, InterchangeImportResolutionAction.AddSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Family, InterchangeImportResolutionAction.AddSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Element, InterchangeImportResolutionAction.AddSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Zone, InterchangeImportResolutionAction.KeepTarget),
                Count(resolution, InterchangeIdentityKind.Floor, InterchangeImportResolutionAction.KeepTarget),
                Count(resolution, InterchangeIdentityKind.Family, InterchangeImportResolutionAction.KeepTarget),
                Count(resolution, InterchangeIdentityKind.Element, InterchangeImportResolutionAction.KeepTarget),
                sourceHandlesToDiscard,
                source.Validation.WarningCount);

            var sourceIdentityCount = checked(checked(source.Zones.Count + source.Floors.Count) + checked(source.Families.Count + source.Elements.Count));
            if (checked(plan.TotalSemanticIdentitiesToAdd + plan.TotalSemanticIdentitiesToKeep) != sourceIdentityCount)
                throw new InvalidOperationException("KeepTarget interchange resolution did not account for every source semantic identity.");

            return new PreparedImport(source, resolution, plan);
        }

        private static ProjectInterchangeImportPolicy KeepTargetPolicy()
        {
            return new ProjectInterchangeImportPolicy
            {
                ZoneCollision = InterchangeExistingIdentityAction.KeepTarget,
                FloorCollision = InterchangeExistingIdentityAction.KeepTarget,
                FamilyCollision = InterchangeExistingIdentityAction.KeepTarget,
                ElementCollision = InterchangeExistingIdentityAction.KeepTarget,
                ProjectId = InterchangeProjectIdPolicy.AllowDifferent,
                DrawingFingerprint = InterchangeDrawingFingerprintPolicy.AllowDifferentOrUnknown,
                SourceHandles = InterchangeSourceHandlePolicy.Discard,
                GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.Unspecified
            };
        }

        private static int Count(ProjectInterchangeImportResolutionPlan plan, InterchangeIdentityKind kind, InterchangeImportResolutionAction action)
        {
            return plan.Items.Count(x => x.Kind == kind && x.Action == action);
        }

        private static void RestoreExistingActiveContext(
            ProjectState target,
            ProjectInterchangeValidatedSnapshot source,
            bool targetHadZones,
            bool targetHadFloors,
            bool targetHadFamilies,
            string previousActiveZoneId,
            string previousActiveFloorId,
            bool hadActiveFamilyMetadata,
            string previousActiveFamilyId)
        {
            if (targetHadZones) target.ActiveZoneId = previousActiveZoneId;
            if (targetHadFloors) target.ActiveFloorId = previousActiveFloorId;
            if (targetHadFamilies)
            {
                if (hadActiveFamilyMetadata) target.Metadata["ActiveFamilyId"] = previousActiveFamilyId;
                else target.Metadata.Remove("ActiveFamilyId");
            }
            else if (source.Families.Count > 0 && (!hadActiveFamilyMetadata || string.IsNullOrWhiteSpace(previousActiveFamilyId)))
            {
                var firstAddedFamily = source.Families
                    .Where(x => target.Families.Any(y => string.Equals(y.Id, x.Id, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (firstAddedFamily != null) ProjectFamilyActivationService.SetActive(target, firstAddedFamily.Id);
            }
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
            if (target.Metadata.TryGetValue("ActiveFamilyId", out var activeFamilyId) && !string.IsNullOrWhiteSpace(activeFamilyId) && !familyIds.Contains(activeFamilyId))
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

        private static void RequireExistingUnique(ISet<string> values, string value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidOperationException("Target project contains a blank " + label + ".");
            if (!values.Add(normalized)) throw new InvalidOperationException("Target project contains duplicate " + label + ": " + normalized);
        }
    }
}
