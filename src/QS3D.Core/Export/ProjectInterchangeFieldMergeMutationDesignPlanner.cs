using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeFieldMergeMutationDesign
    {
        internal ProjectInterchangeFieldMergeMutationDesign(
            ProjectInterchangeFieldMergePlan fieldPlan,
            string targetProjectId,
            string targetDrawingFingerprint,
            long targetChangeVersion,
            IEnumerable<string> affectedTargetElementIds,
            IEnumerable<ProjectInterchangeNativeCleanupRequirement> nativeCleanupRequirements)
        {
            FieldPlan = fieldPlan ?? throw new ArgumentNullException(nameof(fieldPlan));
            TargetProjectId = (targetProjectId ?? string.Empty).Trim();
            if (TargetProjectId.Length == 0) throw new ArgumentException("Target project id is required.", nameof(targetProjectId));
            TargetDrawingFingerprint = (targetDrawingFingerprint ?? string.Empty).Trim();
            if (targetChangeVersion < 0L) throw new ArgumentOutOfRangeException(nameof(targetChangeVersion));
            TargetChangeVersion = targetChangeVersion;
            AffectedTargetElementIds = ReadOnlyIds(affectedTargetElementIds);
            NativeCleanupRequirements = ReadOnlyRequirements(nativeCleanupRequirements);
            TargetElementIdsRequiringNativeCleanup = NativeCleanupRequirements
                .Select(x => x.ElementId)
                .ToList()
                .AsReadOnly();
            TargetGeneratedHandlesToClean = NativeCleanupRequirements.Sum(x => x.OwnerHandles.Count);
        }

        public ProjectInterchangeFieldMergePlan FieldPlan { get; }
        public string SourceProjectId => FieldPlan.SourceProjectId;
        public string TargetProjectId { get; }
        public string TargetDrawingFingerprint { get; }
        public long TargetChangeVersion { get; }
        public IReadOnlyList<string> AffectedTargetElementIds { get; }
        public IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> NativeCleanupRequirements { get; }
        public IReadOnlyList<string> TargetElementIdsRequiringNativeCleanup { get; }
        public int TargetGeneratedHandlesToClean { get; }
        public bool RequiresNativeCleanup => NativeCleanupRequirements.Count > 0;
        public bool IsPreviewOnly => true;
        public bool CanProceedToGuardedAdapterDesign => FieldPlan.CanProceedToMutationDesign;

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
                throw new InvalidOperationException("Duplicate field-merge native cleanup requirement for target element: " + duplicate.Key + ".");
            return result.AsReadOnly();
        }
    }

    /// <summary>
    /// Converts a fully resolved field-precedence preview into a target-bound, still non-mutating design envelope.
    /// It identifies the exact semantic target revision, dependent element closure, and generated owner handles that
    /// a future guarded BricsCAD adapter would have to clean transactionally before any field-level mutation.
    /// </summary>
    public static class ProjectInterchangeFieldMergeMutationDesignPlanner
    {
        public static ProjectInterchangeFieldMergeMutationDesign Plan(
            ProjectState target,
            string json,
            ProjectInterchangeFieldMergePolicy policy)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var targetProjectId = (target.ProjectId ?? string.Empty).Trim();
            if (targetProjectId.Length == 0)
                throw new InvalidOperationException("Field merge mutation design requires a target project id.");
            var targetDrawingFingerprint = (target.DrawingFingerprint ?? string.Empty).Trim();
            var targetChangeVersion = target.ChangeVersion;
            if (targetChangeVersion < 0L)
                throw new InvalidOperationException("Field merge mutation design requires a non-negative target ChangeVersion.");

            var fieldPlan = ProjectInterchangeFieldMergePlanner.Plan(target, json, policy);
            EnsureTargetStillMatches(target, targetProjectId, targetDrawingFingerprint, targetChangeVersion, "after field precedence planning");
            if (!fieldPlan.CanProceedToMutationDesign)
            {
                var reasons = fieldPlan.Blockers.Take(8).ToList();
                if (fieldPlan.UnresolvedDecisionCount > 0)
                    reasons.Add(fieldPlan.UnresolvedDecisionCount + " field precedence decision(s) remain unresolved");
                throw new InvalidOperationException(
                    "Field merge mutation design is blocked" +
                    (reasons.Count == 0 ? "." : ": " + string.Join("; ", reasons) + "."));
            }
            if (!string.Equals(fieldPlan.TargetProjectId, targetProjectId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Field merge preview target ProjectId no longer matches the project being designed.");

            var affected = BuildAffectedTargetElementIds(target, fieldPlan.Decisions);
            var cleanup = BuildNativeCleanupRequirements(target, affected);
            EnsureTargetStillMatches(target, targetProjectId, targetDrawingFingerprint, targetChangeVersion, "after affected/native cleanup planning");

            if (cleanup.Count > 0 && targetDrawingFingerprint.Length == 0)
                throw new InvalidOperationException(
                    "Field merge mutation design requires a non-empty target drawing fingerprint before generated CAD cleanup can be authorized.");

            return new ProjectInterchangeFieldMergeMutationDesign(
                fieldPlan,
                targetProjectId,
                targetDrawingFingerprint,
                targetChangeVersion,
                affected,
                cleanup);
        }

        private static IReadOnlyList<string> BuildAffectedTargetElementIds(
            ProjectState target,
            IEnumerable<InterchangeFieldMergeDecision> decisions)
        {
            var affected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var decision in decisions ?? Enumerable.Empty<InterchangeFieldMergeDecision>())
            {
                if (decision == null ||
                    decision.Choice != InterchangeFieldPrecedenceChoice.UseSource ||
                    !decision.RequiresGeneratedOutputReset)
                    continue;

                switch (decision.Kind)
                {
                    case InterchangeIdentityKind.Element:
                        RequireTargetElement(target, decision.Id);
                        affected.Add(decision.Id);
                        break;
                    case InterchangeIdentityKind.Family:
                        foreach (var element in target.Elements.Where(x =>
                            x != null &&
                            !string.IsNullOrWhiteSpace(x.FamilyId) &&
                            string.Equals(x.FamilyId.Trim(), decision.Id, StringComparison.OrdinalIgnoreCase)))
                            affected.Add(element.Id);
                        break;
                    case InterchangeIdentityKind.Floor:
                        foreach (var element in target.Elements.Where(x => x != null && ReferencesFloor(x, decision.Id)))
                            affected.Add(element.Id);
                        break;
                    case InterchangeIdentityKind.Zone:
                        foreach (var element in target.Elements.Where(x =>
                            x != null &&
                            !string.IsNullOrWhiteSpace(x.ZoneId) &&
                            string.Equals(x.ZoneId.Trim(), decision.Id, StringComparison.OrdinalIgnoreCase)))
                            affected.Add(element.Id);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Field merge generated-output reset is not defined for semantic identity kind " + decision.Kind + ".");
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

            return affected
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        private static IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> BuildNativeCleanupRequirements(
            ProjectState target,
            IEnumerable<string> affectedTargetElementIds)
        {
            var result = new List<ProjectInterchangeNativeCleanupRequirement>();
            foreach (var id in (affectedTargetElementIds ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var element = RequireTargetElement(target, id);
                var handles = GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)
                    .Select(x => x.Key)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (handles.Length == 0) continue;

                foreach (var handle in handles)
                {
                    if (!GeneratedHandleOwnershipPolicy.TryFindOwner(target, handle, out var owner, out _) ||
                        owner == null ||
                        !ReferenceEquals(owner, element))
                        throw new InvalidOperationException(
                            "Field merge cleanup handle " + handle + " is not exclusively owned by affected target element " + element.Id + ".");
                }

                result.Add(new ProjectInterchangeNativeCleanupRequirement(element.Id, handles));
            }
            return result.AsReadOnly();
        }

        private static ProjectElement RequireTargetElement(ProjectState target, string id)
        {
            var normalized = (id ?? string.Empty).Trim();
            if (normalized.Length == 0)
                throw new InvalidOperationException("Field merge mutation design contains an empty affected target element id.");
            var matches = target.Elements
                .Where(x => x != null && string.Equals(x.Id, normalized, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException(
                    "Field merge mutation design requires exactly one target element for id " + normalized + "; found " + matches.Count + ".");
            return matches[0];
        }

        private static bool ReferencesFloor(ProjectElement element, string floorId)
        {
            var normalized = (floorId ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (!string.IsNullOrWhiteSpace(element.FloorId) &&
                string.Equals(element.FloorId.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                return true;
            return ReferencesProperty(element, ProjectFloorService.BottomLevelIdKey, normalized) ||
                   ReferencesProperty(element, ProjectFloorService.TopLevelIdKey, normalized);
        }

        private static bool ReferencesProperty(ProjectElement element, string key, string expectedId)
        {
            return element.Properties.TryGetValue(key, out var raw) &&
                   !string.IsNullOrWhiteSpace(raw) &&
                   string.Equals(raw.Trim(), expectedId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ReferencesAffectedHost(ProjectElement element, ISet<string> affected)
        {
            return element.Properties.TryGetValue("HostWallId", out var hostWallId) &&
                   !string.IsNullOrWhiteSpace(hostWallId) &&
                   affected.Contains(hostWallId.Trim());
        }

        private static void EnsureTargetStillMatches(
            ProjectState target,
            string expectedProjectId,
            string expectedDrawingFingerprint,
            long expectedChangeVersion,
            string phase)
        {
            if (!string.Equals((target.ProjectId ?? string.Empty).Trim(), expectedProjectId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Field merge target ProjectId changed " + phase + ". Refusing stale mutation design.");
            if (!string.Equals((target.DrawingFingerprint ?? string.Empty).Trim(), expectedDrawingFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Field merge target drawing fingerprint changed " + phase + ". Refusing stale mutation design.");
            if (target.ChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException("Field merge target ChangeVersion changed " + phase + ". Refusing stale mutation design.");
        }
    }
}
