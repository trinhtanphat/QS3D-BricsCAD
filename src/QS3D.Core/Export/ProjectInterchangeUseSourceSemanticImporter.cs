using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeNativeCleanupRequirement
    {
        internal ProjectInterchangeNativeCleanupRequirement(string elementId, IEnumerable<string> ownerHandles)
        {
            ElementId = (elementId ?? string.Empty).Trim();
            if (ElementId.Length == 0) throw new ArgumentException("Native cleanup element id is required.", nameof(elementId));
            OwnerHandles = (ownerHandles ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
            if (OwnerHandles.Count == 0) throw new ArgumentException("Native cleanup owner handles are required.", nameof(ownerHandles));
        }

        public string ElementId { get; }
        public IReadOnlyList<string> OwnerHandles { get; }
    }

    public sealed class ProjectInterchangeUseSourceSemanticPlan
    {
        internal ProjectInterchangeUseSourceSemanticPlan(
            string targetProjectId,
            string targetDrawingFingerprint,
            long targetChangeVersion,
            string sourceProjectId,
            int sourceSchemaVersion,
            string sourceDrawingFingerprint,
            int zonesToAdd,
            int floorsToAdd,
            int familiesToAdd,
            int elementsToAdd,
            int zonesToReplace,
            int floorsToReplace,
            int familiesToReplace,
            int elementsToReplace,
            int sourceHandlesToDiscard,
            int validationWarnings,
            IEnumerable<string> affectedTargetElementIds,
            IEnumerable<ProjectInterchangeNativeCleanupRequirement> nativeCleanupRequirements)
        {
            TargetProjectId = (targetProjectId ?? string.Empty).Trim();
            if (TargetProjectId.Length == 0) throw new ArgumentException("Target project id is required.", nameof(targetProjectId));
            TargetDrawingFingerprint = (targetDrawingFingerprint ?? string.Empty).Trim();
            if (targetChangeVersion < 0L) throw new ArgumentOutOfRangeException(nameof(targetChangeVersion), "Target change version cannot be negative.");
            TargetChangeVersion = targetChangeVersion;
            SourceProjectId = sourceProjectId ?? string.Empty;
            SourceSchemaVersion = sourceSchemaVersion;
            SourceDrawingFingerprint = sourceDrawingFingerprint ?? string.Empty;
            ZonesToAdd = zonesToAdd;
            FloorsToAdd = floorsToAdd;
            FamiliesToAdd = familiesToAdd;
            ElementsToAdd = elementsToAdd;
            ZonesToReplace = zonesToReplace;
            FloorsToReplace = floorsToReplace;
            FamiliesToReplace = familiesToReplace;
            ElementsToReplace = elementsToReplace;
            SourceHandlesToDiscard = sourceHandlesToDiscard;
            ValidationWarnings = validationWarnings;
            AffectedTargetElementIds = ReadOnlyIds(affectedTargetElementIds);
            NativeCleanupRequirements = ReadOnlyRequirements(nativeCleanupRequirements);
            TargetElementIdsRequiringNativeCleanup = NativeCleanupRequirements
                .Select(x => x.ElementId)
                .ToList()
                .AsReadOnly();
            TargetGeneratedHandlesToClean = NativeCleanupRequirements.Sum(x => x.OwnerHandles.Count);
        }

        public string TargetProjectId { get; }
        public string TargetDrawingFingerprint { get; }
        public long TargetChangeVersion { get; }
        public string SourceProjectId { get; }
        public int SourceSchemaVersion { get; }
        public string SourceDrawingFingerprint { get; }
        public int ZonesToAdd { get; }
        public int FloorsToAdd { get; }
        public int FamiliesToAdd { get; }
        public int ElementsToAdd { get; }
        public int ZonesToReplace { get; }
        public int FloorsToReplace { get; }
        public int FamiliesToReplace { get; }
        public int ElementsToReplace { get; }
        public int SourceHandlesToDiscard { get; }
        public int ValidationWarnings { get; }
        public IReadOnlyList<string> AffectedTargetElementIds { get; }
        public IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> NativeCleanupRequirements { get; }
        public IReadOnlyList<string> TargetElementIdsRequiringNativeCleanup { get; }
        public int TargetGeneratedHandlesToClean { get; }
        public int TotalSemanticIdentitiesToAdd => checked(checked(ZonesToAdd + FloorsToAdd) + checked(FamiliesToAdd + ElementsToAdd));
        public int TotalSemanticIdentitiesToReplace => checked(checked(ZonesToReplace + FloorsToReplace) + checked(FamiliesToReplace + ElementsToReplace));
        public bool RequiresNativeCleanup => NativeCleanupRequirements.Count > 0;

        private static IReadOnlyList<string> ReadOnlyIds(IEnumerable<string> source)
        {
            return (source ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        private static IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> ReadOnlyRequirements(
            IEnumerable<ProjectInterchangeNativeCleanupRequirement> source)
        {
            var result = (source ?? Enumerable.Empty<ProjectInterchangeNativeCleanupRequirement>())
                .Where(x => x != null)
                .OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var duplicate = result
                .GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(x => x.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException("Duplicate native cleanup requirement for target element: " + duplicate.Key);
            return result.AsReadOnly();
        }
    }

    public sealed class ProjectInterchangeNativeCleanupAuthorization
    {
        private readonly HashSet<string> _elementIds;
        private readonly Dictionary<string, HashSet<string>> _ownerHandlesByElementId;
        private readonly bool _handleBound;
        private readonly bool _targetBound;
        private readonly string _targetProjectId;
        private readonly string _targetDrawingFingerprint;
        private readonly long _targetChangeVersion;

        private ProjectInterchangeNativeCleanupAuthorization(
            IEnumerable<string> elementIds,
            IEnumerable<ProjectInterchangeNativeCleanupRequirement> requirements,
            bool handleBound,
            bool targetBound,
            string targetProjectId,
            string targetDrawingFingerprint,
            long targetChangeVersion)
        {
            _elementIds = new HashSet<string>(
                (elementIds ?? Enumerable.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            _ownerHandlesByElementId = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var requirement in requirements ?? Enumerable.Empty<ProjectInterchangeNativeCleanupRequirement>())
            {
                if (requirement == null) continue;
                _ownerHandlesByElementId[requirement.ElementId] = new HashSet<string>(requirement.OwnerHandles, StringComparer.OrdinalIgnoreCase);
            }
            _handleBound = handleBound;
            _targetBound = targetBound;
            _targetProjectId = (targetProjectId ?? string.Empty).Trim();
            _targetDrawingFingerprint = (targetDrawingFingerprint ?? string.Empty).Trim();
            _targetChangeVersion = targetChangeVersion;
            ElementIds = _elementIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        public IReadOnlyList<string> ElementIds { get; }
        public bool IsHandleBound => _handleBound;
        public bool IsTargetBound => _targetBound;
        public static ProjectInterchangeNativeCleanupAuthorization None { get; } =
            new ProjectInterchangeNativeCleanupAuthorization(
                Array.Empty<string>(),
                Array.Empty<ProjectInterchangeNativeCleanupRequirement>(),
                handleBound: true,
                targetBound: false,
                targetProjectId: string.Empty,
                targetDrawingFingerprint: string.Empty,
                targetChangeVersion: 0L);

        public static ProjectInterchangeNativeCleanupAuthorization ForElementIds(IEnumerable<string> elementIds)
        {
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            return new ProjectInterchangeNativeCleanupAuthorization(
                elementIds,
                Array.Empty<ProjectInterchangeNativeCleanupRequirement>(),
                handleBound: false,
                targetBound: false,
                targetProjectId: string.Empty,
                targetDrawingFingerprint: string.Empty,
                targetChangeVersion: 0L);
        }

        public static ProjectInterchangeNativeCleanupAuthorization ForPlan(ProjectInterchangeUseSourceSemanticPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            return new ProjectInterchangeNativeCleanupAuthorization(
                plan.TargetElementIdsRequiringNativeCleanup,
                plan.NativeCleanupRequirements,
                handleBound: true,
                targetBound: true,
                targetProjectId: plan.TargetProjectId,
                targetDrawingFingerprint: plan.TargetDrawingFingerprint,
                targetChangeVersion: plan.TargetChangeVersion);
        }

        internal bool MatchesExactly(ProjectInterchangeUseSourceSemanticPlan plan)
        {
            if (plan == null) return false;
            var requirements = plan.NativeCleanupRequirements ?? Array.Empty<ProjectInterchangeNativeCleanupRequirement>();
            if (requirements.Count == 0) return _elementIds.Count == 0;
            if (!_handleBound || !_targetBound || _elementIds.Count != requirements.Count || _ownerHandlesByElementId.Count != requirements.Count)
                return false;
            if (!string.Equals(_targetProjectId, plan.TargetProjectId, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(_targetDrawingFingerprint, plan.TargetDrawingFingerprint, StringComparison.OrdinalIgnoreCase)) return false;
            if (_targetChangeVersion != plan.TargetChangeVersion) return false;

            foreach (var requirement in requirements)
            {
                if (requirement == null || !_elementIds.Contains(requirement.ElementId)) return false;
                if (!_ownerHandlesByElementId.TryGetValue(requirement.ElementId, out var authorizedHandles)) return false;
                if (authorizedHandles.Count != requirement.OwnerHandles.Count) return false;
                if (requirement.OwnerHandles.Any(x => !authorizedHandles.Contains(x))) return false;
            }
            return true;
        }
    }

    public sealed class ProjectInterchangeUseSourceSemanticResult
    {
        internal ProjectInterchangeUseSourceSemanticResult(ProjectInterchangeUseSourceSemanticPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            SourceProjectId = plan.SourceProjectId;
            ZonesAdded = plan.ZonesToAdd;
            FloorsAdded = plan.FloorsToAdd;
            FamiliesAdded = plan.FamiliesToAdd;
            ElementsAdded = plan.ElementsToAdd;
            ZonesReplaced = plan.ZonesToReplace;
            FloorsReplaced = plan.FloorsToReplace;
            FamiliesReplaced = plan.FamiliesToReplace;
            ElementsReplaced = plan.ElementsToReplace;
            SourceHandlesDiscarded = plan.SourceHandlesToDiscard;
            AffectedTargetElementsMarkedDirty = plan.AffectedTargetElementIds.Count;
            NativeCleanupElementsAuthorized = plan.TargetElementIdsRequiringNativeCleanup.Count;
            TargetGeneratedHandlesCleaned = plan.TargetGeneratedHandlesToClean;
        }

        public string SourceProjectId { get; }
        public int ZonesAdded { get; }
        public int FloorsAdded { get; }
        public int FamiliesAdded { get; }
        public int ElementsAdded { get; }
        public int ZonesReplaced { get; }
        public int FloorsReplaced { get; }
        public int FamiliesReplaced { get; }
        public int ElementsReplaced { get; }
        public int SourceHandlesDiscarded { get; }
        public int AffectedTargetElementsMarkedDirty { get; }
        public int NativeCleanupElementsAuthorized { get; }
        public int TargetGeneratedHandlesCleaned { get; }
    }

    public static class ProjectInterchangeUseSourceSemanticImporter
    {
        private sealed class PreparedImport
        {
            public PreparedImport(
                ProjectInterchangeValidatedSnapshot source,
                ProjectInterchangeImportResolutionPlan resolution,
                ProjectInterchangeUseSourceSemanticPlan plan)
            {
                Source = source;
                Resolution = resolution;
                Plan = plan;
            }

            public ProjectInterchangeValidatedSnapshot Source { get; }
            public ProjectInterchangeImportResolutionPlan Resolution { get; }
            public ProjectInterchangeUseSourceSemanticPlan Plan { get; }
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
                if (action == InterchangeImportResolutionAction.UseSourceSemanticData) return false;
                throw new InvalidOperationException("UseSource semantic mutation reached a non-executable resolution for " + kind + " " + id + ".");
            }
        }

        public const string ImportMode = "UseSourceSemanticData";
        public const string LastSemanticIdentitiesAddedKey = "Interchange.LastImport.SemanticIdentitiesAdded";
        public const string LastSemanticIdentitiesReplacedKey = "Interchange.LastImport.SemanticIdentitiesReplaced";
        public const string LastAffectedTargetElementsKey = "Interchange.LastImport.AffectedTargetElements";
        public const string LastNativeCleanupElementsKey = "Interchange.LastImport.NativeCleanupElements";
        public const string LastTargetGeneratedHandlesCleanedKey = "Interchange.LastImport.TargetGeneratedHandlesCleaned";

        public static ProjectInterchangeUseSourceSemanticPlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return Prepare(target, json).Plan;
        }

        public static ProjectInterchangeUseSourceSemanticResult Import(
            ProjectState target,
            string json,
            ProjectInterchangeNativeCleanupAuthorization nativeCleanupAuthorization)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (nativeCleanupAuthorization == null) throw new ArgumentNullException(nameof(nativeCleanupAuthorization));

            var prepared = Prepare(target, json);
            EnsureNativeCleanupAuthorized(prepared.Plan, nativeCleanupAuthorization);
            var source = prepared.Source;
            var plan = prepared.Plan;
            var resolutionActions = new ResolutionActionIndex(prepared.Resolution);
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
                foreach (var zoneSnapshot in source.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    if (resolutionActions.ShouldAdd(InterchangeIdentityKind.Zone, zoneSnapshot.Id))
                    {
                        ProjectZoneService.Create(target, zoneSnapshot.Id, zoneSnapshot.Name);
                    }
                    else
                    {
                        var zone = target.FindZone(zoneSnapshot.Id) ?? throw new InvalidOperationException("Target Zone disappeared before UseSource semantic apply: " + zoneSnapshot.Id);
                        zone.Name = zoneSnapshot.Name;
                    }
                }

                foreach (var floorSnapshot in source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    if (resolutionActions.ShouldAdd(InterchangeIdentityKind.Floor, floorSnapshot.Id))
                    {
                        ProjectFloorService.Create(target, floorSnapshot.Id, floorSnapshot.Name, floorSnapshot.ElevationM);
                    }
                    else
                    {
                        var floor = target.FindFloor(floorSnapshot.Id) ?? throw new InvalidOperationException("Target Floor disappeared before UseSource semantic apply: " + floorSnapshot.Id);
                        floor.Name = floorSnapshot.Name;
                        floor.ElevationM = floorSnapshot.ElevationM;
                    }
                }

                foreach (var familySnapshot in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    ProjectFamily family;
                    if (resolutionActions.ShouldAdd(InterchangeIdentityKind.Family, familySnapshot.Id))
                    {
                        family = ProjectFamilyService.Create(target, familySnapshot.Id, familySnapshot.Name, familySnapshot.Category);
                    }
                    else
                    {
                        family = target.FindFamily(familySnapshot.Id) ?? throw new InvalidOperationException("Target Family disappeared before UseSource semantic apply: " + familySnapshot.Id);
                        if (family.Category != familySnapshot.Category)
                            throw new InvalidOperationException("Target Family category changed after UseSource planning: " + familySnapshot.Id);
                        family = ProjectFamilyService.Rename(target, familySnapshot.Id, familySnapshot.Name);
                    }

                    ApplySourceFamilyProperties(target, family, familySnapshot);
                }

                foreach (var elementSnapshot in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    ProjectElement element;
                    if (resolutionActions.ShouldAdd(InterchangeIdentityKind.Element, elementSnapshot.Id))
                    {
                        element = new ProjectElement(
                            elementSnapshot.Id,
                            elementSnapshot.Category,
                            elementSnapshot.FamilyId,
                            elementSnapshot.FloorId,
                            elementSnapshot.ZoneId);
                        target.Elements.Add(element);
                    }
                    else
                    {
                        element = target.FindElement(elementSnapshot.Id) ?? throw new InvalidOperationException("Target element disappeared before UseSource semantic apply: " + elementSnapshot.Id);
                        if (element.Category != elementSnapshot.Category)
                            throw new InvalidOperationException("Target element category changed after UseSource planning: " + elementSnapshot.Id);
                    }

                    ApplySourceElementSemanticData(element, elementSnapshot);
                }

                foreach (var affectedId in plan.AffectedTargetElementIds)
                {
                    var element = target.FindElement(affectedId);
                    if (element == null) continue;
                    if (!source.Elements.Any(x => string.Equals(x.Id, element.Id, StringComparison.OrdinalIgnoreCase)))
                        ClearGeneratedOwnershipMetadata(element);
                    element.MarkDirty(ElementDirtyFlags.All);
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
                target.Metadata[LastSemanticIdentitiesReplacedKey] = plan.TotalSemanticIdentitiesToReplace.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastAffectedTargetElementsKey] = plan.AffectedTargetElementIds.Count.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastNativeCleanupElementsKey] = plan.TargetElementIdsRequiringNativeCleanup.Count.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastTargetGeneratedHandlesCleanedKey] = plan.TargetGeneratedHandlesToClean.ToString(CultureInfo.InvariantCulture);

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeUseSourceSemantic",
                    string.Empty,
                    "Imported semantic snapshot from project " + source.Project.Id +
                    " with UseSourceSemanticData policy: added=" + plan.TotalSemanticIdentitiesToAdd.ToString(CultureInfo.InvariantCulture) +
                    ", replaced=" + plan.TotalSemanticIdentitiesToReplace.ToString(CultureInfo.InvariantCulture) +
                    ", affectedTargetElements=" + plan.AffectedTargetElementIds.Count.ToString(CultureInfo.InvariantCulture) +
                    ", nativeCleanupElements=" + plan.TargetElementIdsRequiringNativeCleanup.Count.ToString(CultureInfo.InvariantCulture) +
                    ", discardedSourceHandles=" + plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) + ".");

                ValidateTarget(target);
                return new ProjectInterchangeUseSourceSemanticResult(plan);
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
                        "Interchange UseSource semantic import failed and project rollback also failed.",
                        new AggregateException(operationError, restoreError));
                }

                throw;
            }
        }

        private static PreparedImport Prepare(ProjectState target, string json)
        {
            ValidateTarget(target);
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var resolution = ProjectInterchangeImportResolutionPlanner.Plan(target, json, UseSourcePolicy());
            if (resolution.HasUnresolvedPolicy || resolution.HasBlocks || !resolution.CanProceedToMutationDesign)
            {
                var reasons = resolution.PolicyErrors
                    .Concat(resolution.GlobalBlocks)
                    .Concat(resolution.Items
                        .Where(x => x.Action == InterchangeImportResolutionAction.BlockedIncompatible || x.Action == InterchangeImportResolutionAction.Unresolved)
                        .Select(x => x.Kind + " " + x.Id + ": " + x.Reason))
                    .Take(8)
                    .ToArray();
                throw new InvalidOperationException("UseSource semantic interchange import is blocked" + (reasons.Length == 0 ? "." : ": " + string.Join("; ", reasons)));
            }

            if (resolution.Items.Any(x =>
                x.Action != InterchangeImportResolutionAction.AddSourceSemanticData &&
                x.Action != InterchangeImportResolutionAction.UseSourceSemanticData))
                throw new InvalidOperationException("UseSource semantic importer received a non Add/UseSource resolution action.");

            var sourceHandlesToDiscard = source.Elements.Sum(x => x.SourceHandles.Count);
            var affected = BuildAffectedTargetElementIds(target, source, resolution);
            var cleanup = new List<ProjectInterchangeNativeCleanupRequirement>();
            foreach (var id in affected)
            {
                var element = target.FindElement(id);
                if (element == null) continue;
                var handles = GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)
                    .Select(x => x.Key)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (handles.Length == 0) continue;

                foreach (var handle in handles)
                {
                    try
                    {
                        if (!GeneratedHandleOwnershipPolicy.TryFindOwner(target, handle, out var owner, out _) ||
                            owner == null ||
                            !ReferenceEquals(owner, element))
                            throw new InvalidOperationException(
                                "UseSource native cleanup handle " + handle + " is not exclusively owned by affected target element " + element.Id + ".");
                    }
                    catch (InvalidOperationException error)
                    {
                        throw new InvalidOperationException(
                            "UseSource native cleanup ownership is ambiguous or unsafe for handle " + handle + "/" + element.Id + ": " + error.Message,
                            error);
                    }
                }

                cleanup.Add(new ProjectInterchangeNativeCleanupRequirement(element.Id, handles));
            }

            if (cleanup.Count > 0 && string.IsNullOrWhiteSpace(target.DrawingFingerprint))
                throw new InvalidOperationException(
                    "UseSource native cleanup requires a non-empty target drawing fingerprint before cleanup authorization can be created.");

            var plan = new ProjectInterchangeUseSourceSemanticPlan(
                target.ProjectId,
                target.DrawingFingerprint,
                target.ChangeVersion,
                source.Project.Id,
                source.Project.SchemaVersion,
                source.Project.DrawingFingerprint,
                Count(resolution, InterchangeIdentityKind.Zone, InterchangeImportResolutionAction.AddSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Floor, InterchangeImportResolutionAction.AddSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Family, InterchangeImportResolutionAction.AddSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Element, InterchangeImportResolutionAction.AddSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Zone, InterchangeImportResolutionAction.UseSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Floor, InterchangeImportResolutionAction.UseSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Family, InterchangeImportResolutionAction.UseSourceSemanticData),
                Count(resolution, InterchangeIdentityKind.Element, InterchangeImportResolutionAction.UseSourceSemanticData),
                sourceHandlesToDiscard,
                source.Validation.WarningCount,
                affected,
                cleanup);

            var sourceIdentityCount = checked(checked(source.Zones.Count + source.Floors.Count) + checked(source.Families.Count + source.Elements.Count));
            if (checked(plan.TotalSemanticIdentitiesToAdd + plan.TotalSemanticIdentitiesToReplace) != sourceIdentityCount)
                throw new InvalidOperationException("UseSource semantic resolution did not account for every source semantic identity.");

            return new PreparedImport(source, resolution, plan);
        }

        private static ProjectInterchangeImportPolicy UseSourcePolicy()
        {
            return new ProjectInterchangeImportPolicy
            {
                ZoneCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
                FloorCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
                FamilyCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
                ElementCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
                ProjectId = InterchangeProjectIdPolicy.AllowDifferent,
                DrawingFingerprint = InterchangeDrawingFingerprintPolicy.AllowDifferentOrUnknown,
                SourceHandles = InterchangeSourceHandlePolicy.Discard,
                GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild
            };
        }

        private static IReadOnlyList<string> BuildAffectedTargetElementIds(
            ProjectState target,
            ProjectInterchangeValidatedSnapshot source,
            ProjectInterchangeImportResolutionPlan resolution)
        {
            var affected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var replacedZones = new HashSet<string>(
                resolution.Items.Where(x => x.Kind == InterchangeIdentityKind.Zone && x.Action == InterchangeImportResolutionAction.UseSourceSemanticData).Select(x => x.Id),
                StringComparer.OrdinalIgnoreCase);
            var replacedFloors = new HashSet<string>(
                resolution.Items.Where(x => x.Kind == InterchangeIdentityKind.Floor && x.Action == InterchangeImportResolutionAction.UseSourceSemanticData).Select(x => x.Id),
                StringComparer.OrdinalIgnoreCase);
            var replacedFamilies = new HashSet<string>(
                resolution.Items.Where(x => x.Kind == InterchangeIdentityKind.Family && x.Action == InterchangeImportResolutionAction.UseSourceSemanticData).Select(x => x.Id),
                StringComparer.OrdinalIgnoreCase);

            foreach (var item in resolution.Items.Where(x => x.Kind == InterchangeIdentityKind.Element && x.Action == InterchangeImportResolutionAction.UseSourceSemanticData))
                affected.Add(item.Id);

            foreach (var element in target.Elements)
            {
                if (element == null) continue;
                if ((!string.IsNullOrWhiteSpace(element.ZoneId) && replacedZones.Contains(element.ZoneId.Trim())) ||
                    ReferencesReplacedFloor(element, replacedFloors) ||
                    (!string.IsNullOrWhiteSpace(element.FamilyId) && replacedFamilies.Contains(element.FamilyId)))
                    affected.Add(element.Id);
            }

            var sourceElementIds = new HashSet<string>(source.Elements.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var element in target.Elements)
                {
                    if (element == null || affected.Contains(element.Id)) continue;
                    if (element.DependsOn.Any(affected.Contains) ||
                        ReferencesAffectedHost(element, affected))
                    {
                        affected.Add(element.Id);
                        changed = true;
                    }
                }
            }

            foreach (var id in sourceElementIds)
                if (target.FindElement(id) != null && resolution.Items.Any(x => x.Kind == InterchangeIdentityKind.Element && string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase) && x.Action == InterchangeImportResolutionAction.UseSourceSemanticData))
                    affected.Add(id);

            return affected.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static bool ReferencesReplacedFloor(ProjectElement element, ISet<string> replacedFloors)
        {
            if (element == null || replacedFloors == null || replacedFloors.Count == 0) return false;
            if (!string.IsNullOrWhiteSpace(element.FloorId) && replacedFloors.Contains(element.FloorId.Trim())) return true;
            if (ReferencesFloorProperty(element, ProjectFloorService.BottomLevelIdKey, replacedFloors)) return true;
            return ReferencesFloorProperty(element, ProjectFloorService.TopLevelIdKey, replacedFloors);
        }

        private static bool ReferencesFloorProperty(ProjectElement element, string key, ISet<string> replacedFloors)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return false;
            return replacedFloors.Contains(raw.Trim());
        }

        private static bool ReferencesAffectedHost(ProjectElement element, ISet<string> affected)
        {
            if (element.Properties.TryGetValue("HostWallId", out var hostWallId) &&
                !string.IsNullOrWhiteSpace(hostWallId) &&
                affected.Contains(hostWallId.Trim()))
                return true;
            return false;
        }

        private static void EnsureNativeCleanupAuthorized(
            ProjectInterchangeUseSourceSemanticPlan plan,
            ProjectInterchangeNativeCleanupAuthorization authorization)
        {
            if (authorization.MatchesExactly(plan)) return;
            var required = plan.TargetElementIdsRequiringNativeCleanup
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Take(16)
                .ToArray();
            throw new InvalidOperationException(
                "UseSource semantic import requires native cleanup authorization bound to the exact target project, drawing fingerprint, semantic revision and generated-handle set for target element(s): " +
                string.Join(", ", required) +
                ". The Core importer re-plans before mutation and rejects cross-project, cross-drawing, stale-revision or element-id-only cleanup authorization; native cleanup must be completed by a guarded adapter transaction/recovery workflow first.");
        }

        private static void ApplySourceFamilyProperties(
            ProjectState target,
            ProjectFamily family,
            InterchangeFamilySnapshot source)
        {
            var sourceProperties = source.Properties.ToDictionary(
                x => x.Key,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase);
            var removedKeys = family.Properties.Keys
                .Where(x => !sourceProperties.ContainsKey(x))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (var key in removedKeys)
                ProjectFamilyService.RemoveProperty(target, family.Id, key);

            foreach (var property in source.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                ProjectFamilyService.SetProperty(target, family.Id, property.Key, property.Value ?? string.Empty);
        }

        private static void ApplySourceElementSemanticData(ProjectElement element, InterchangeElementSnapshot source)
        {
            element.Category = source.Category;
            element.FamilyId = source.FamilyId;
            element.FloorId = source.FloorId;
            element.ZoneId = source.ZoneId;
            element.DrawingFingerprint = string.Empty;
            element.SourceHandles.Clear();
            element.DependsOn.Clear();
            foreach (var dependency in source.Dependencies)
                element.DependsOn.Add(dependency);

            element.Properties.Clear();
            foreach (var property in source.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                if (IsPortableElementProperty(property.Key))
                    element.Properties[property.Key] = property.Value ?? string.Empty;

            element.Quantities.Clear();
            foreach (var quantity in source.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                element.Quantities[quantity.Key] = quantity.Value;
            element.MarkDirty(ElementDirtyFlags.All);
        }

        private static void ClearGeneratedOwnershipMetadata(ProjectElement element)
        {
            element.ClearGeneratedGeometryStale();
            var remove = element.Properties.Keys
                .Where(IsGeneratedOwnershipMetadata)
                .ToArray();
            foreach (var key in remove)
                element.Properties.Remove(key);
        }

        private static bool IsGeneratedOwnershipMetadata(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)) return true;
            if (normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return true;
            return normalized.StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPortableElementProperty(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)) return false;
            if (normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return false;
            return normalized.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) < 0;
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
                var firstFamily = source.Families
                    .Where(x => target.Families.Any(y => string.Equals(y.Id, x.Id, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (firstFamily != null) ProjectFamilyActivationService.SetActive(target, firstFamily.Id);
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
