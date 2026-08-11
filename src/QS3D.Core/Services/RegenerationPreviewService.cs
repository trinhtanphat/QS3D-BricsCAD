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
        public RegenerationPreview Preview(ProjectState project)
        {
            return PreviewInternal(project, Array.Empty<string>());
        }

        public RegenerationPreview PreviewSubset(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            var targets = CanonicalPreviewTargets(elementIds, project.Elements.Count);
            if (targets.Count == 0) throw new ArgumentException("Subset regeneration preview requires at least one target element id.", nameof(elementIds));
            return PreviewInternal(project, targets);
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

        private RegenerationPreview PreviewInternal(ProjectState project, IReadOnlyList<string> targets)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var sourceChangeVersion = project.ChangeVersion;
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
