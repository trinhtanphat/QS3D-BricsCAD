using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeFieldMergeExecutionPlan
    {
        internal ProjectInterchangeFieldMergeExecutionPlan(
            ProjectInterchangeFieldMergePlan fieldPlan,
            string targetProjectId,
            string targetDrawingFingerprint,
            long targetChangeVersion,
            string sourceSnapshotHash,
            string decisionStamp,
            int validationWarnings,
            IEnumerable<string> executionBlockers,
            IEnumerable<string> affectedTargetElementIds,
            IEnumerable<ProjectInterchangeNativeCleanupRequirement> nativeCleanupRequirements)
        {
            FieldPlan = fieldPlan ?? throw new ArgumentNullException(nameof(fieldPlan));
            TargetProjectId = (targetProjectId ?? string.Empty).Trim();
            if (TargetProjectId.Length == 0) throw new ArgumentException("Target project id is required.", nameof(targetProjectId));
            TargetDrawingFingerprint = (targetDrawingFingerprint ?? string.Empty).Trim();
            if (targetChangeVersion < 0L) throw new ArgumentOutOfRangeException(nameof(targetChangeVersion));
            TargetChangeVersion = targetChangeVersion;
            SourceSnapshotHash = sourceSnapshotHash ?? string.Empty;
            DecisionStamp = decisionStamp ?? string.Empty;
            ValidationWarnings = validationWarnings;
            ExecutionBlockers = ReadOnlyStrings(executionBlockers);
            AffectedTargetElementIds = ReadOnlyIds(affectedTargetElementIds);
            NativeCleanupRequirements = ReadOnlyRequirements(nativeCleanupRequirements);
            NativeCleanupElementIds = NativeCleanupRequirements.Select(x => x.ElementId).ToList().AsReadOnly();
            TargetGeneratedHandlesToClean = NativeCleanupRequirements.Sum(x => x.OwnerHandles.Count);
        }

        public ProjectInterchangeFieldMergePlan FieldPlan { get; }
        public string TargetProjectId { get; }
        public string TargetDrawingFingerprint { get; }
        public long TargetChangeVersion { get; }
        public string SourceSnapshotHash { get; }
        public string DecisionStamp { get; }
        public int ValidationWarnings { get; }
        public IReadOnlyList<string> ExecutionBlockers { get; }
        public IReadOnlyList<string> AffectedTargetElementIds { get; }
        public IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> NativeCleanupRequirements { get; }
        public IReadOnlyList<string> NativeCleanupElementIds { get; }
        public int TargetGeneratedHandlesToClean { get; }
        public bool RequiresNativeCleanup => NativeCleanupRequirements.Count > 0;
        public bool HasBlocks => FieldPlan.HasBlocks || ExecutionBlockers.Count > 0;
        public bool CanExecute => FieldPlan.CanProceedToMutationDesign && !HasBlocks && FieldPlan.SourceOnlyIdentityCount == 0;

        public ProjectInterchangeFieldMergeAuthorization CreateAuthorization() =>
            ProjectInterchangeFieldMergeAuthorization.ForPlan(this);

        private static IReadOnlyList<string> ReadOnlyStrings(IEnumerable<string> source) =>
            (source ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();

        private static IReadOnlyList<string> ReadOnlyIds(IEnumerable<string> source) =>
            (source ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

        private static IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> ReadOnlyRequirements(
            IEnumerable<ProjectInterchangeNativeCleanupRequirement> source)
        {
            var result = (source ?? Enumerable.Empty<ProjectInterchangeNativeCleanupRequirement>())
                .Where(x => x != null)
                .OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var duplicate = result.GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException("Duplicate field-merge native cleanup requirement for target element: " + duplicate.Key);
            return result.AsReadOnly();
        }
    }

    public sealed class ProjectInterchangeFieldMergeAuthorization
    {
        private readonly string _targetProjectId;
        private readonly string _targetDrawingFingerprint;
        private readonly long _targetChangeVersion;
        private readonly string _sourceSnapshotHash;
        private readonly string _decisionStamp;
        private readonly Dictionary<string, HashSet<string>> _ownerHandlesByElementId;

        private ProjectInterchangeFieldMergeAuthorization(ProjectInterchangeFieldMergeExecutionPlan plan)
        {
            _targetProjectId = plan.TargetProjectId;
            _targetDrawingFingerprint = plan.TargetDrawingFingerprint;
            _targetChangeVersion = plan.TargetChangeVersion;
            _sourceSnapshotHash = plan.SourceSnapshotHash;
            _decisionStamp = plan.DecisionStamp;
            _ownerHandlesByElementId = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var requirement in plan.NativeCleanupRequirements)
                _ownerHandlesByElementId[requirement.ElementId] = new HashSet<string>(requirement.OwnerHandles, StringComparer.OrdinalIgnoreCase);
        }

        public static ProjectInterchangeFieldMergeAuthorization ForPlan(ProjectInterchangeFieldMergeExecutionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (!plan.CanExecute)
                throw new InvalidOperationException("Cannot authorize a blocked or unresolved field-merge plan.");
            return new ProjectInterchangeFieldMergeAuthorization(plan);
        }

        internal bool MatchesExactly(ProjectInterchangeFieldMergeExecutionPlan plan)
        {
            if (plan == null || !plan.CanExecute) return false;
            if (!string.Equals(_targetProjectId, plan.TargetProjectId, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(_targetDrawingFingerprint, plan.TargetDrawingFingerprint, StringComparison.Ordinal)) return false;
            if (_targetChangeVersion != plan.TargetChangeVersion) return false;
            if (!string.Equals(_sourceSnapshotHash, plan.SourceSnapshotHash, StringComparison.Ordinal)) return false;
            if (!string.Equals(_decisionStamp, plan.DecisionStamp, StringComparison.Ordinal)) return false;
            if (_ownerHandlesByElementId.Count != plan.NativeCleanupRequirements.Count) return false;

            foreach (var requirement in plan.NativeCleanupRequirements)
            {
                if (!_ownerHandlesByElementId.TryGetValue(requirement.ElementId, out var handles)) return false;
                if (handles.Count != requirement.OwnerHandles.Count) return false;
                if (requirement.OwnerHandles.Any(x => !handles.Contains(x))) return false;
            }
            return true;
        }
    }

    public sealed class ProjectInterchangeFieldMergeResult
    {
        internal ProjectInterchangeFieldMergeResult(ProjectInterchangeFieldMergeExecutionPlan plan)
        {
            SourceProjectId = plan.FieldPlan.SourceProjectId;
            SourceFieldsApplied = plan.FieldPlan.SourceChoiceCount;
            TargetFieldsKept = plan.FieldPlan.TargetChoiceCount;
            AffectedTargetElementsMarkedDirty = plan.AffectedTargetElementIds.Count;
            NativeCleanupElementsAuthorized = plan.NativeCleanupRequirements.Count;
            NativeCleanupHandlesRequired = plan.TargetGeneratedHandlesToClean;
        }

        public string SourceProjectId { get; }
        public int SourceFieldsApplied { get; }
        public int TargetFieldsKept { get; }
        public int AffectedTargetElementsMarkedDirty { get; }
        public int NativeCleanupElementsAuthorized { get; }
        public int NativeCleanupHandlesRequired { get; }

        [Obsolete("Use NativeCleanupHandlesRequired. Core authorizes/requires native cleanup but does not erase BricsCAD entities.")]
        public int TargetGeneratedHandlesCleaned => NativeCleanupHandlesRequired;
    }

    public static class ProjectInterchangeFieldMergeImporter
    {
        private sealed class PreparedImport
        {
            public PreparedImport(
                ProjectInterchangeValidatedSnapshot source,
                ProjectInterchangeFieldMergeExecutionPlan plan)
            {
                Source = source;
                Plan = plan;
            }

            public ProjectInterchangeValidatedSnapshot Source { get; }
            public ProjectInterchangeFieldMergeExecutionPlan Plan { get; }
        }

        public const string ImportMode = "FieldMerge";
        public const string LastSourceFieldsAppliedKey = "Interchange.LastImport.SourceFieldsApplied";
        public const string LastTargetFieldsKeptKey = "Interchange.LastImport.TargetFieldsKept";
        public const string LastAffectedTargetElementsKey = "Interchange.LastImport.AffectedTargetElements";
        public const string LastNativeCleanupElementsKey = "Interchange.LastImport.NativeCleanupElements";
        public const string LastNativeCleanupHandlesRequiredKey = "Interchange.LastImport.NativeCleanupHandlesRequired";

        [Obsolete("Use LastNativeCleanupHandlesRequiredKey. Core does not prove native CAD cleanup.")]
        public const string LastTargetGeneratedHandlesCleanedKey = "Interchange.LastImport.TargetGeneratedHandlesCleaned";

        public static ProjectInterchangeFieldMergeExecutionPlan Plan(
            ProjectState target,
            string json,
            ProjectInterchangeFieldMergePolicy policy)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            return Prepare(target, json, policy).Plan;
        }

        public static ProjectInterchangeFieldMergeResult Import(
            ProjectState target,
            string json,
            ProjectInterchangeFieldMergePolicy policy,
            ProjectInterchangeFieldMergeAuthorization authorization)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (authorization == null) throw new ArgumentNullException(nameof(authorization));

            var prepared = Prepare(target, json, policy);
            var plan = prepared.Plan;
            EnsureExecutable(plan);
            if (!authorization.MatchesExactly(plan))
                throw new InvalidOperationException(
                    "Field merge authorization is stale or belongs to a different target/source/decision/native-cleanup set. Re-plan and review the merge before mutation.");

            if (plan.FieldPlan.SourceChoiceCount == 0)
                return new ProjectInterchangeFieldMergeResult(plan);

            var snapshot = ProjectStateSnapshot.Capture(target);
            var beforeVersion = target.ChangeVersion;
            var originalElementProperties = CaptureElementProperties(target, prepared.Source);
            try
            {
                ApplyCatalogChoices(target, prepared.Source, plan.FieldPlan);
                ApplyElementChoices(target, prepared.Source, plan.FieldPlan, policy, originalElementProperties);

                foreach (var id in plan.AffectedTargetElementIds)
                {
                    var element = target.FindElement(id);
                    if (element == null) continue;
                    ClearGeneratedOwnershipMetadata(element);
                    element.MarkDirty(ElementDirtyFlags.All);
                }

                if (target.ChangeVersion == beforeVersion) target.Touch();

                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey] = ImportMode;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceProjectIdKey] = prepared.Source.Project.Id;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceSchemaVersionKey] = prepared.Source.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture);
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceDrawingFingerprintKey] = prepared.Source.Project.DrawingFingerprint;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceUpdatedUtcKey] = prepared.Source.Project.UpdatedUtcRaw;
                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastImportedUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                target.Metadata[LastSourceFieldsAppliedKey] = plan.FieldPlan.SourceChoiceCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastTargetFieldsKeptKey] = plan.FieldPlan.TargetChoiceCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastAffectedTargetElementsKey] = plan.AffectedTargetElementIds.Count.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastNativeCleanupElementsKey] = plan.NativeCleanupRequirements.Count.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastNativeCleanupHandlesRequiredKey] = plan.TargetGeneratedHandlesToClean.ToString(CultureInfo.InvariantCulture);
                target.Metadata.Remove("Interchange.LastImport.TargetGeneratedHandlesCleaned");

                ValidateCombinedTarget(target);
                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeFieldMerge",
                    string.Empty,
                    "Applied reviewed field-level semantic precedence from project " + prepared.Source.Project.Id +
                    ": sourceFields=" + plan.FieldPlan.SourceChoiceCount.ToString(CultureInfo.InvariantCulture) +
                    ", targetFields=" + plan.FieldPlan.TargetChoiceCount.ToString(CultureInfo.InvariantCulture) +
                    ", affectedTargetElements=" + plan.AffectedTargetElementIds.Count.ToString(CultureInfo.InvariantCulture) +
                    ", nativeCleanupElements=" + plan.NativeCleanupRequirements.Count.ToString(CultureInfo.InvariantCulture) +
                    ", nativeCleanupHandlesRequired=" + plan.TargetGeneratedHandlesToClean.ToString(CultureInfo.InvariantCulture) + ".");

                return new ProjectInterchangeFieldMergeResult(plan);
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
                        "Interchange field merge failed and project rollback also failed.",
                        new AggregateException(operationError, restoreError));
                }
                throw;
            }
        }

        private static PreparedImport Prepare(ProjectState target, string json, ProjectInterchangeFieldMergePolicy policy)
        {
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var fieldPlan = ProjectInterchangeFieldMergePlanner.Plan(target, json, policy);
            var blockers = new List<string>();
            if (fieldPlan.SourceOnlyIdentityCount > 0)
                blockers.Add(
                    "Field merge handles same-ID collisions only; source contains " + fieldPlan.SourceOnlyIdentityCount.ToString(CultureInfo.InvariantCulture) +
                    " source-only semantic identity/identities. Use AppendOnly or ImportAsNew for retain/add behavior.");

            AddRuntimeCompatibilityBlocks(target, source, fieldPlan, blockers);
            var affected = BuildAffectedTargetElementIds(target, fieldPlan);
            var cleanup = BuildNativeCleanupRequirements(target, affected, blockers);
            if (cleanup.Count > 0 && string.IsNullOrWhiteSpace(target.DrawingFingerprint))
                blockers.Add("Field merge native cleanup requires a non-empty target drawing fingerprint before cleanup authorization can be created.");
            var plan = new ProjectInterchangeFieldMergeExecutionPlan(
                fieldPlan,
                target.ProjectId,
                target.DrawingFingerprint,
                target.ChangeVersion,
                Hash(json),
                DecisionStamp(fieldPlan),
                source.Validation.WarningCount,
                blockers,
                affected,
                cleanup);
            return new PreparedImport(source, plan);
        }

        private static void EnsureExecutable(ProjectInterchangeFieldMergeExecutionPlan plan)
        {
            if (plan.CanExecute) return;
            var reasons = plan.FieldPlan.Blockers
                .Concat(plan.ExecutionBlockers)
                .Concat(plan.FieldPlan.Decisions.Where(x => !x.IsResolved).Select(x => x.Kind + " " + x.Id + " " + x.Field + ": precedence is unresolved."))
                .Take(12)
                .ToArray();
            throw new InvalidOperationException(
                "Field merge is blocked" + (reasons.Length == 0 ? "." : ": " + string.Join("; ", reasons)));
        }

        private static void AddRuntimeCompatibilityBlocks(
            ProjectState target,
            ProjectInterchangeValidatedSnapshot source,
            ProjectInterchangeFieldMergePlan plan,
            ICollection<string> blockers)
        {
            foreach (var decision in plan.Decisions.Where(x => x.Choice == InterchangeFieldPrecedenceChoice.UseSource))
            {
                if (decision.Kind == InterchangeIdentityKind.Zone && string.Equals(decision.Field, "name", StringComparison.OrdinalIgnoreCase))
                    RequireLength(blockers, "Zone " + decision.Id + " source name", decision.SourceValue, 1, 120);
                else if (decision.Kind == InterchangeIdentityKind.Floor && string.Equals(decision.Field, "name", StringComparison.OrdinalIgnoreCase))
                    RequireLength(blockers, "Floor " + decision.Id + " source name", decision.SourceValue, 1, 120);
                else if (decision.Kind == InterchangeIdentityKind.Family && string.Equals(decision.Field, "name", StringComparison.OrdinalIgnoreCase))
                    RequireLength(blockers, "Family " + decision.Id + " source name", decision.SourceValue, 1, 160);
                else if (decision.Kind == InterchangeIdentityKind.Family && decision.Field.StartsWith("properties.", StringComparison.OrdinalIgnoreCase))
                {
                    var key = decision.Field.Substring("properties.".Length);
                    RequireLength(blockers, "Family " + decision.Id + " property key", key, 1, 120);
                    if (decision.SourceHasValue && decision.SourceValue.Length > 1000)
                        blockers.Add("Family " + decision.Id + " source property '" + key + "' exceeds target runtime value limit 1000.");
                }
                else if (decision.Kind == InterchangeIdentityKind.Element && string.Equals(decision.Field, "familyId", StringComparison.OrdinalIgnoreCase) && decision.SourceHasValue && !string.IsNullOrWhiteSpace(decision.SourceValue))
                {
                    RequireLength(blockers, "Element " + decision.Id + " source Family id", decision.SourceValue, 1, 80);
                    var family = target.FindFamily(decision.SourceValue);
                    if (family != null && !FamilyDefaultsFitRuntime(family, out var reason))
                        blockers.Add("Element " + decision.Id + " cannot select source Family " + decision.SourceValue + ": " + reason);
                }
                else if (decision.Kind == InterchangeIdentityKind.Element && string.Equals(decision.Field, "floorId", StringComparison.OrdinalIgnoreCase) && decision.SourceHasValue && !string.IsNullOrWhiteSpace(decision.SourceValue))
                    RequireLength(blockers, "Element " + decision.Id + " source Floor id", decision.SourceValue, 1, 64);
                else if (decision.Kind == InterchangeIdentityKind.Element && string.Equals(decision.Field, "zoneId", StringComparison.OrdinalIgnoreCase) && decision.SourceHasValue && !string.IsNullOrWhiteSpace(decision.SourceValue))
                    RequireLength(blockers, "Element " + decision.Id + " source Zone id", decision.SourceValue, 1, 64);
            }

            foreach (var family in source.Families)
            {
                if (!plan.Decisions.Any(x => x.Kind == InterchangeIdentityKind.Family && string.Equals(x.Id, family.Id, StringComparison.OrdinalIgnoreCase) && x.Choice == InterchangeFieldPrecedenceChoice.UseSource && x.Field.StartsWith("properties.", StringComparison.OrdinalIgnoreCase)))
                    continue;
                foreach (var property in family.Properties)
                {
                    if (property.Key.Length > 120)
                        blockers.Add("Family " + family.Id + " source property key exceeds target runtime limit 120: " + property.Key + ".");
                    if ((property.Value ?? string.Empty).Length > 1000)
                        blockers.Add("Family " + family.Id + " source property value exceeds target runtime limit 1000: " + property.Key + ".");
                }
            }
        }

        private static bool FamilyDefaultsFitRuntime(ProjectFamily family, out string reason)
        {
            foreach (var property in family.Properties)
            {
                if ((property.Key ?? string.Empty).Trim().Length == 0 || property.Key.Trim().Length > 120)
                {
                    reason = "target Family contains a property key outside the canonical 1..120 character runtime contract.";
                    return false;
                }
                if ((property.Value ?? string.Empty).Length > 1000)
                {
                    reason = "target Family contains a property value outside the canonical 1000 character runtime contract.";
                    return false;
                }
            }
            reason = string.Empty;
            return true;
        }

        private static void RequireLength(ICollection<string> blockers, string label, string value, int min, int max)
        {
            var length = (value ?? string.Empty).Trim().Length;
            if (length < min || length > max)
                blockers.Add(label + " length " + length.ToString(CultureInfo.InvariantCulture) + " is outside target runtime limit " + min + ".." + max + ".");
        }

        private static IReadOnlyList<string> BuildAffectedTargetElementIds(ProjectState target, ProjectInterchangeFieldMergePlan plan)
        {
            var affected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reset = plan.Decisions
                .Where(x => x.Choice == InterchangeFieldPrecedenceChoice.UseSource && x.RequiresGeneratedOutputReset)
                .ToArray();

            foreach (var decision in reset)
            {
                if (decision.Kind == InterchangeIdentityKind.Element)
                {
                    affected.Add(decision.Id);
                    continue;
                }
                if (decision.Kind == InterchangeIdentityKind.Floor && string.Equals(decision.Field, "elevationM", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var element in target.Elements.Where(x => x != null && ProjectFloorService.ReferencesFloor(x, decision.Id)))
                        affected.Add(element.Id);
                    continue;
                }
                if (decision.Kind == InterchangeIdentityKind.Family && decision.Field.StartsWith("properties.", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var element in target.Elements.Where(x => x != null && string.Equals(x.FamilyId, decision.Id, StringComparison.OrdinalIgnoreCase)))
                        affected.Add(element.Id);
                }
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var element in target.Elements)
                {
                    if (element == null || affected.Contains(element.Id)) continue;
                    if (element.DependsOn.Any(affected.Contains) || ReferencesAffectedHost(element, affected))
                    {
                        affected.Add(element.Id);
                        changed = true;
                    }
                }
            }

            return affected.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static bool ReferencesAffectedHost(ProjectElement element, ISet<string> affected)
        {
            if (!element.Properties.TryGetValue("HostWallId", out var hostWallId) || string.IsNullOrWhiteSpace(hostWallId)) return false;
            return affected.Contains(hostWallId.Trim());
        }

        private static IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> BuildNativeCleanupRequirements(
            ProjectState target,
            IEnumerable<string> affectedIds,
            ICollection<string> blockers)
        {
            if (blockers == null) throw new ArgumentNullException(nameof(blockers));
            var result = new List<ProjectInterchangeNativeCleanupRequirement>();
            foreach (var id in affectedIds)
            {
                var element = target.FindElement(id);
                if (element == null) continue;
                var handles = GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)
                    .Select(x => x.Key)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (handles.Length == 0) continue;

                var ownershipSafe = true;
                foreach (var handle in handles)
                {
                    try
                    {
                        if (!GeneratedHandleOwnershipPolicy.TryFindOwner(target, handle, out var owner, out _) ||
                            owner == null ||
                            !ReferenceEquals(owner, element))
                        {
                            blockers.Add(
                                "Field merge native cleanup handle " + handle + " is not exclusively owned by affected target element " + element.Id + ".");
                            ownershipSafe = false;
                        }
                    }
                    catch (InvalidOperationException error)
                    {
                        blockers.Add(
                            "Field merge native cleanup ownership is ambiguous for handle " + handle + "/" + element.Id + ": " + error.Message);
                        ownershipSafe = false;
                    }
                }

                if (ownershipSafe)
                    result.Add(new ProjectInterchangeNativeCleanupRequirement(element.Id, handles));
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CaptureElementProperties(
            ProjectState target,
            ProjectInterchangeValidatedSnapshot source)
        {
            var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceElement in source.Elements)
            {
                var element = target.FindElement(sourceElement.Id) ??
                    throw new InvalidOperationException("Target element disappeared before field merge property snapshot: " + sourceElement.Id);
                result[element.Id] = new Dictionary<string, string>(element.Properties, StringComparer.OrdinalIgnoreCase);
            }
            return result;
        }

        private static void ApplyCatalogChoices(
            ProjectState target,
            ProjectInterchangeValidatedSnapshot source,
            ProjectInterchangeFieldMergePlan plan)
        {
            foreach (var zone in source.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!UseSource(plan, InterchangeIdentityKind.Zone, zone.Id, "name")) continue;
                ProjectZoneService.Update(target, zone.Id, zone.Name);
            }

            foreach (var floor in source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var targetFloor = target.FindFloor(floor.Id) ?? throw new InvalidOperationException("Target Floor disappeared before field merge: " + floor.Id);
                var useName = UseSource(plan, InterchangeIdentityKind.Floor, floor.Id, "name");
                var useElevation = UseSource(plan, InterchangeIdentityKind.Floor, floor.Id, "elevationM");
                if (!useName && !useElevation) continue;
                ProjectFloorService.Update(
                    target,
                    floor.Id,
                    useName ? floor.Name : targetFloor.Name,
                    useElevation ? floor.ElevationM : targetFloor.ElevationM);
            }

            foreach (var family in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (UseSource(plan, InterchangeIdentityKind.Family, family.Id, "name"))
                    ProjectFamilyService.Rename(target, family.Id, family.Name);

                if (!plan.Decisions.Any(x =>
                    x.Kind == InterchangeIdentityKind.Family &&
                    string.Equals(x.Id, family.Id, StringComparison.OrdinalIgnoreCase) &&
                    x.Choice == InterchangeFieldPrecedenceChoice.UseSource &&
                    x.Field.StartsWith("properties.", StringComparison.OrdinalIgnoreCase)))
                    continue;

                var targetFamily = target.FindFamily(family.Id) ?? throw new InvalidOperationException("Target Family disappeared before field merge: " + family.Id);
                var sourceProperties = family.Properties.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
                var removed = targetFamily.Properties.Keys.Where(x => !sourceProperties.ContainsKey(x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
                foreach (var key in removed) ProjectFamilyService.RemoveProperty(target, family.Id, key);
                foreach (var property in family.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    ProjectFamilyService.SetProperty(target, family.Id, property.Key, property.Value ?? string.Empty);
            }
        }

        private static void ApplyElementChoices(
            ProjectState target,
            ProjectInterchangeValidatedSnapshot source,
            ProjectInterchangeFieldMergePlan plan,
            ProjectInterchangeFieldMergePolicy policy,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> originalElementProperties)
        {
            foreach (var sourceElement in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var element = target.FindElement(sourceElement.Id) ?? throw new InvalidOperationException("Target element disappeared before field merge: " + sourceElement.Id);
                if (!originalElementProperties.TryGetValue(element.Id, out var originalProperties))
                    throw new InvalidOperationException("Reviewed target property snapshot is missing for field merge element: " + element.Id);

                if (UseSource(plan, InterchangeIdentityKind.Element, element.Id, "familyId"))
                    ApplyFamilyReference(target, element, sourceElement.FamilyId);
                if (UseSource(plan, InterchangeIdentityKind.Element, element.Id, "floorId"))
                    ApplyFloorReference(target, element, sourceElement.FloorId);
                if (UseSource(plan, InterchangeIdentityKind.Element, element.Id, "zoneId"))
                    ApplyZoneReference(target, element, sourceElement.ZoneId);

                if (UseSource(plan, InterchangeIdentityKind.Element, element.Id, "dependencies"))
                {
                    element.DependsOn.Clear();
                    foreach (var dependency in sourceElement.Dependencies) element.DependsOn.Add(dependency);
                    element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity | ElementDirtyFlags.Geometry);
                }

                if (policy.ElementProperties == InterchangeFieldPrecedenceChoice.UseSource)
                    ApplySourcePortableProperties(element, sourceElement.Properties);
                else
                    RestoreProperties(element, originalProperties);

                var hasQuantityDecision = plan.Decisions.Any(x =>
                    x.Kind == InterchangeIdentityKind.Element &&
                    string.Equals(x.Id, element.Id, StringComparison.OrdinalIgnoreCase) &&
                    x.Field.StartsWith("quantities.", StringComparison.OrdinalIgnoreCase));
                if (policy.ElementQuantities == InterchangeFieldPrecedenceChoice.UseSource && hasQuantityDecision)
                {
                    element.Quantities.Clear();
                    foreach (var quantity in sourceElement.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        element.SetQuantity(quantity.Key, quantity.Value);
                }
            }
        }

        private static void ApplyFamilyReference(ProjectState target, ProjectElement element, string familyId)
        {
            var normalized = (familyId ?? string.Empty).Trim();
            if (normalized.Length > 0)
            {
                ProjectFamilyService.Assign(target, normalized, new[] { element });
                return;
            }

            var previous = string.IsNullOrWhiteSpace(element.FamilyId) ? null : target.FindFamily(element.FamilyId);
            if (previous != null)
            {
                foreach (var property in previous.Properties)
                    if (element.Properties.TryGetValue(property.Key, out var value) && string.Equals(value, property.Value, StringComparison.Ordinal))
                        element.Properties.Remove(property.Key);
            }
            if (string.IsNullOrWhiteSpace(element.FamilyId)) return;
            element.FamilyId = string.Empty;
            element.MarkDirty(ElementDirtyFlags.All);
        }

        private static void ApplyFloorReference(ProjectState target, ProjectElement element, string floorId)
        {
            var normalized = (floorId ?? string.Empty).Trim();
            if (normalized.Length > 0)
            {
                ProjectFloorService.Assign(target, normalized, new[] { element });
                return;
            }
            if (string.IsNullOrWhiteSpace(element.FloorId)) return;
            element.FloorId = string.Empty;
            element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity | ElementDirtyFlags.Geometry);
        }

        private static void ApplyZoneReference(ProjectState target, ProjectElement element, string zoneId)
        {
            var normalized = (zoneId ?? string.Empty).Trim();
            if (normalized.Length > 0)
            {
                ProjectZoneService.Assign(target, normalized, new[] { element });
                return;
            }
            if (string.IsNullOrWhiteSpace(element.ZoneId)) return;
            element.ZoneId = string.Empty;
            element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
        }

        private static void ApplySourcePortableProperties(ProjectElement element, IReadOnlyDictionary<string, string> sourceProperties)
        {
            var desired = element.Properties
                .Where(x => IsGeneratedOwnershipMetadata(x.Key))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            foreach (var property in sourceProperties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                desired[property.Key] = property.Value ?? string.Empty;
            if (PropertiesEqual(element.Properties, desired)) return;

            element.Properties.Clear();
            foreach (var property in desired) element.Properties[property.Key] = property.Value;
            element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity | ElementDirtyFlags.Geometry);
        }

        private static void RestoreProperties(ProjectElement element, IReadOnlyDictionary<string, string> properties)
        {
            if (PropertiesEqual(element.Properties, properties)) return;
            element.Properties.Clear();
            foreach (var property in properties) element.Properties[property.Key] = property.Value;
            element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity | ElementDirtyFlags.Geometry);
        }

        private static bool PropertiesEqual(IDictionary<string, string> current, IReadOnlyDictionary<string, string> expected)
        {
            if (current.Count != expected.Count) return false;
            foreach (var property in expected)
            {
                if (!current.TryGetValue(property.Key, out var value) || !string.Equals(value ?? string.Empty, property.Value ?? string.Empty, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static bool UseSource(ProjectInterchangeFieldMergePlan plan, InterchangeIdentityKind kind, string id, string field)
        {
            var decision = plan.Decisions.FirstOrDefault(x =>
                x.Kind == kind &&
                string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Field, field, StringComparison.OrdinalIgnoreCase));
            return decision != null && decision.Choice == InterchangeFieldPrecedenceChoice.UseSource;
        }

        private static void ClearGeneratedOwnershipMetadata(ProjectElement element)
        {
            element.ClearGeneratedGeometryStale();
            var remove = element.Properties.Keys.Where(IsGeneratedOwnershipMetadata).ToArray();
            foreach (var key in remove) element.Properties.Remove(key);
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

        private static void ValidateCombinedTarget(ProjectState target)
        {
            var json = ProjectInterchangeJsonExporter.Build(target);
            var validation = ProjectInterchangeJsonValidator.Validate(json);
            if (validation.IsValid) return;
            var reasons = validation.Issues
                .Where(x => x.Severity == InterchangeValidationSeverity.Error)
                .Take(8)
                .Select(x => x.Code + " " + x.Path + ": " + x.Message)
                .ToArray();
            throw new InvalidOperationException("Field merge produced an invalid portable semantic target: " + string.Join("; ", reasons));
        }

        private static string DecisionStamp(ProjectInterchangeFieldMergePlan plan)
        {
            var text = new StringBuilder();
            text.Append(plan.SourceProjectId).Append('|').Append(plan.TargetProjectId).Append('|')
                .Append(plan.SourceOnlyIdentityCount.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(plan.CollidingIdentityCount.ToString(CultureInfo.InvariantCulture));
            foreach (var decision in plan.Decisions)
            {
                text.Append('\n').Append((int)decision.Kind).Append('|').Append(decision.Id).Append('|').Append(decision.Field).Append('|')
                    .Append((int)decision.Choice).Append('|').Append(decision.TargetHasValue ? '1' : '0').Append('|').Append(decision.TargetValue).Append('|')
                    .Append(decision.SourceHasValue ? '1' : '0').Append('|').Append(decision.SourceValue).Append('|').Append(decision.RequiresGeneratedOutputReset ? '1' : '0');
            }
            foreach (var blocker in plan.Blockers) text.Append("\nB|").Append(blocker);
            return Hash(text.ToString());
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var text = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) text.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }
    }
}
