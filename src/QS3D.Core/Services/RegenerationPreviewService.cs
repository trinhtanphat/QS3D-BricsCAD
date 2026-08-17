using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Revisions;

namespace QS3D.Core.Services
{
    public sealed class RegenerationPreview
    {
        internal RegenerationPreview(
            string projectId,
            long sourceChangeVersion,
            IEnumerable<string> targetElementIds,
            int regeneratedElementCount,
            IEnumerable<RevisionDelta> deltas,
            ModelHealthBaselineDiff healthDiff)
        {
            ProjectId = projectId ?? string.Empty;
            SourceChangeVersion = sourceChangeVersion;
            TargetElementIds = (targetElementIds ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            RegeneratedElementCount = regeneratedElementCount;
            Deltas = (deltas ?? Enumerable.Empty<RevisionDelta>()).ToList().AsReadOnly();
            HealthDiff = healthDiff ?? throw new ArgumentNullException(nameof(healthDiff));
        }

        public string ProjectId { get; }
        public long SourceChangeVersion { get; }
        public IReadOnlyList<string> TargetElementIds { get; }
        public bool IsSubset => TargetElementIds.Count > 0;
        public int RegeneratedElementCount { get; }
        public IReadOnlyList<RevisionDelta> Deltas { get; }
        public ModelHealthBaselineDiff HealthDiff { get; }
        public int ChangedElementCount => Deltas.Count;
        public int ChangedFieldCount => Deltas.Sum(x => x.Fields.Count);
        public bool HasSemanticChanges => Deltas.Count > 0;
        public bool IntroducesHealthErrors => HealthDiff.NewErrorCount > 0;
    }

    public sealed class RegenerationGuardedApplyResult
    {
        internal RegenerationGuardedApplyResult(int regeneratedElementCount, ModelHealthBaselineDiff healthDiff)
        {
            RegeneratedElementCount = regeneratedElementCount;
            HealthDiff = healthDiff ?? throw new ArgumentNullException(nameof(healthDiff));
        }

        public int RegeneratedElementCount { get; }
        public ModelHealthBaselineDiff HealthDiff { get; }
    }

    public sealed class RegenerationPreviewService
    {
        private sealed class ElementFreshnessSnapshot
        {
            private readonly IReadOnlyList<string?> _sourceHandles;
            private readonly IReadOnlyList<string?> _dependsOn;
            private readonly IReadOnlyDictionary<string, string?> _properties;
            private readonly IReadOnlyDictionary<string, double> _quantities;

            internal ElementFreshnessSnapshot(ProjectElement element)
            {
                Owner = element ?? throw new ArgumentNullException(nameof(element));
                Category = element.Category;
                FamilyId = element.FamilyId;
                FloorId = element.FloorId;
                ZoneId = element.ZoneId;
                DrawingFingerprint = element.DrawingFingerprint;
                Dirty = element.Dirty;
                _sourceHandles = element.SourceHandles.Select(x => x).ToList().AsReadOnly();
                _dependsOn = element.DependsOn.Select(x => x).ToList().AsReadOnly();
                _properties = new Dictionary<string, string?>(element.Properties.ToDictionary(x => x.Key, x => (string?)x.Value), StringComparer.OrdinalIgnoreCase);
                _quantities = new Dictionary<string, double>(element.Quantities, StringComparer.OrdinalIgnoreCase);
            }

            internal ProjectElement Owner { get; }
            private ElementCategory Category { get; }
            private string FamilyId { get; }
            private string FloorId { get; }
            private string ZoneId { get; }
            private string DrawingFingerprint { get; }
            private ElementDirtyFlags Dirty { get; }

            internal bool Matches(ProjectElement element)
            {
                return element.Category == Category &&
                       string.Equals(element.FamilyId, FamilyId, StringComparison.Ordinal) &&
                       string.Equals(element.FloorId, FloorId, StringComparison.Ordinal) &&
                       string.Equals(element.ZoneId, ZoneId, StringComparison.Ordinal) &&
                       string.Equals(element.DrawingFingerprint, DrawingFingerprint, StringComparison.Ordinal) &&
                       element.Dirty == Dirty &&
                       StringListEquivalent(_sourceHandles, element.SourceHandles) &&
                       StringListEquivalent(_dependsOn, element.DependsOn) &&
                       StringMapEquivalent(_properties, element.Properties) &&
                       DoubleMapEquivalent(_quantities, element.Quantities);
            }
        }

        public RegenerationPreview Preview(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var sourceChangeVersion = project.ChangeVersion;
            return PreviewInternal(project, Array.Empty<string>(), sourceChangeVersion);
        }

        public RegenerationPreview PreviewSubset(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            var sourceChangeVersion = project.ChangeVersion;
            var sourceElementOwnership = SnapshotElementOwnership(project);
            var sourceElementState = SnapshotElementFreshness(project);
            var targets = CanonicalPreviewTargets(elementIds, sourceElementOwnership.Count);
            if (targets.Count == 0) throw new ArgumentException("Subset regeneration preview requires at least one target element id.", nameof(elementIds));
            RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);
            RequireElementStateFresh(project, sourceElementState);
            var preview = PreviewInternal(project, targets, sourceChangeVersion);
            RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);
            RequireElementStateFresh(project, sourceElementState);
            return preview;
        }

        public RegenerationGuardedApplyResult Apply(ProjectState project, RegenerationPreview preview)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            if (!string.Equals(project.ProjectId, preview.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Regeneration preview belongs to a different project.");
            if (preview.SourceChangeVersion != project.ChangeVersion)
                throw new InvalidOperationException("Regeneration preview is stale because the project changed after preview; recompute before applying.");

            var current = preview.IsSubset ? PreviewSubset(project, preview.TargetElementIds) : Preview(project);
            if (!Equivalent(preview, current))
                throw new InvalidOperationException("Regeneration preview is stale; recompute preview before applying.");
            if (current.IntroducesHealthErrors)
                throw new InvalidOperationException("Regeneration preview introduces " + current.HealthDiff.NewErrorCount + " new Model Health error(s); apply is blocked.");

            var health = new ModelHealthBaselineService();
            var beforeHealth = health.CaptureSemantic(project);
            var snapshot = ProjectStateSnapshot.Capture(project);
            try
            {
                var engine = NewEngine();
                var count = preview.IsSubset
                    ? engine.RegenerateDirtySubset(project, preview.TargetElementIds)
                    : engine.RegenerateDirty(project);
                var afterHealth = health.CaptureSemantic(project);
                var diff = health.Compare(beforeHealth, afterHealth);
                if (diff.NewErrorCount > 0)
                    throw new InvalidOperationException("Regeneration introduced " + diff.NewErrorCount + " new Model Health error(s); project state was rolled back.");
                return new RegenerationGuardedApplyResult(count, diff);
            }
            catch (Exception applyError)
            {
                try
                {
                    snapshot.Restore(project);
                }
                catch (Exception rollbackError)
                {
                    throw new AggregateException("Regeneration preview apply failed and project rollback also failed.", applyError, rollbackError);
                }
                throw;
            }
        }

        private RegenerationPreview PreviewInternal(ProjectState project, IReadOnlyList<string> targets, long sourceChangeVersion)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (project.ChangeVersion != sourceChangeVersion)
                throw new InvalidOperationException("Project changed while regeneration preview scope was being established; recompute preview.");
            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var revisions = new RevisionService();
            var health = new ModelHealthBaselineService();
            var beforeRevision = revisions.Capture(detached, "regen-preview-before");
            var beforeHealth = health.CaptureSemantic(detached);

            var engine = NewEngine();
            var count = targets.Count == 0
                ? engine.RegenerateDirty(detached)
                : engine.RegenerateDirtySubset(detached, targets);

            var afterRevision = revisions.Capture(detached, "regen-preview-after");
            var afterHealth = health.CaptureSemantic(detached);
            return new RegenerationPreview(
                project.ProjectId,
                sourceChangeVersion,
                targets,
                count,
                revisions.Compare(beforeRevision, afterRevision),
                health.Compare(beforeHealth, afterHealth));
        }

        private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry while regeneration preview scope is being established.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id while regeneration preview scope is being established: " + element.Id + ".");
                result.Add(element.Id, element);
            }
            return result;
        }

        private static IReadOnlyDictionary<string, ElementFreshnessSnapshot> SnapshotElementFreshness(ProjectState project)
        {
            var result = new Dictionary<string, ElementFreshnessSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry while regeneration preview scope is being established.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id while regeneration preview scope is being established: " + element.Id + ".");
                result.Add(element.Id, new ElementFreshnessSnapshot(element));
            }
            return result;
        }

        private static void RequireProjectFresh(
            ProjectState project,
            long expectedChangeVersion,
            IReadOnlyDictionary<string, ProjectElement> expectedOwnership)
        {
            if (project.ChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException("Project changed while regeneration preview scope was being established; recompute preview.");
            if (project.Elements.Count != expectedOwnership.Count)
                throw StructuralFreshnessError();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || !seen.Add(element.Id) ||
                    !expectedOwnership.TryGetValue(element.Id, out var original) ||
                    !ReferenceEquals(original, element))
                    throw StructuralFreshnessError();
            }
        }

        private static void RequireElementStateFresh(
            ProjectState project,
            IReadOnlyDictionary<string, ElementFreshnessSnapshot> expectedElements)
        {
            if (project.Elements.Count != expectedElements.Count)
                throw StructuralFreshnessError();

            foreach (var element in project.Elements)
            {
                if (element == null ||
                    !expectedElements.TryGetValue(element.Id, out var snapshot) ||
                    !ReferenceEquals(snapshot.Owner, element))
                    throw StructuralFreshnessError();
                if (!snapshot.Matches(element))
                    throw ElementStateFreshnessError(element.Id);
            }
        }

        private static InvalidOperationException StructuralFreshnessError()
        {
            return new InvalidOperationException(
                "Project element ownership changed while regeneration preview scope was being established; recompute preview.");
        }

        private static InvalidOperationException ElementStateFreshnessError(string elementId)
        {
            return new InvalidOperationException(
                "Project element state changed while regeneration preview scope was being established: " + elementId + ". Recompute preview.");
        }

        private static bool StringListEquivalent(IReadOnlyList<string?> expected, IList<string> actual)
        {
            if (expected.Count != actual.Count) return false;
            for (var i = 0; i < expected.Count; i++)
                if (!string.Equals(expected[i], actual[i], StringComparison.Ordinal)) return false;
            return true;
        }

        private static bool StringMapEquivalent(IReadOnlyDictionary<string, string?> expected, IDictionary<string, string> actual)
        {
            if (expected.Count != actual.Count) return false;
            foreach (var pair in expected)
                if (!actual.TryGetValue(pair.Key, out var value) || !string.Equals(pair.Value, value, StringComparison.Ordinal))
                    return false;
            return true;
        }

        private static bool DoubleMapEquivalent(IReadOnlyDictionary<string, double> expected, IDictionary<string, double> actual)
        {
            if (expected.Count != actual.Count) return false;
            foreach (var pair in expected)
                if (!actual.TryGetValue(pair.Key, out var value) || !pair.Value.Equals(value))
                    return false;
            return true;
        }

        private static IReadOnlyList<string> CanonicalPreviewTargets(IEnumerable<string> elementIds, int maxCount)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var value in elementIds)
            {
                var raw = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new ArgumentException("Regeneration preview target cannot be blank at index " + index.ToString(CultureInfo.InvariantCulture) + ".", nameof(elementIds));
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new ArgumentException("Regeneration preview target must be canonical without surrounding whitespace: " + raw + ".", nameof(elementIds));
                if (seen.Contains(raw))
                    throw new ArgumentException("Duplicate regeneration preview target: " + raw + ".", nameof(elementIds));
                if (result.Count >= maxCount)
                    throw new ArgumentException("Regeneration preview target set cannot exceed project element count of " + maxCount.ToString(CultureInfo.InvariantCulture) + ".", nameof(elementIds));
                seen.Add(raw);
                result.Add(raw);
                index++;
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.AsReadOnly();
        }

        private static RegenerationEngine NewEngine()
        {
            return new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
        }

        private static bool Equivalent(RegenerationPreview left, RegenerationPreview right)
        {
            if (!string.Equals(left.ProjectId, right.ProjectId, StringComparison.Ordinal) ||
                left.SourceChangeVersion != right.SourceChangeVersion ||
                !TargetScopeEquivalent(left.TargetElementIds, right.TargetElementIds) ||
                left.RegeneratedElementCount != right.RegeneratedElementCount ||
                left.Deltas.Count != right.Deltas.Count ||
                !HealthEquivalent(left.HealthDiff, right.HealthDiff))
                return false;

            for (var i = 0; i < left.Deltas.Count; i++)
            {
                var a = left.Deltas[i];
                var b = right.Deltas[i];
                if (!string.Equals(a.ElementId, b.ElementId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(a.Change, b.Change, StringComparison.Ordinal) ||
                    a.Fields.Count != b.Fields.Count)
                    return false;
                for (var f = 0; f < a.Fields.Count; f++)
                {
                    var af = a.Fields[f];
                    var bf = b.Fields[f];
                    if (!string.Equals(af.Field, bf.Field, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(af.Before, bf.Before, StringComparison.Ordinal) ||
                        !string.Equals(af.After, bf.After, StringComparison.Ordinal))
                        return false;
                }
            }
            return true;
        }

        private static bool TargetScopeEquivalent(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count) return false;
            for (var i = 0; i < left.Count; i++)
                if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static bool HealthEquivalent(ModelHealthBaselineDiff left, ModelHealthBaselineDiff right)
        {
            return IssueListEquivalent(left.NewIssues, right.NewIssues) &&
                   IssueListEquivalent(left.ResolvedIssues, right.ResolvedIssues) &&
                   IssueListEquivalent(left.PersistentIssues, right.PersistentIssues);
        }

        private static bool IssueListEquivalent(IReadOnlyList<ModelHealthIssue> left, IReadOnlyList<ModelHealthIssue> right)
        {
            if (left.Count != right.Count) return false;
            for (var i = 0; i < left.Count; i++)
            {
                var a = left[i];
                var b = right[i];
                if (a.Severity != b.Severity ||
                    !string.Equals(a.Code, b.Code, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(a.ElementId, b.ElementId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(a.Message, b.Message, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }
}
