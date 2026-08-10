using System;
using System.Collections.Generic;
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
            int regeneratedElementCount,
            IEnumerable<RevisionDelta> deltas,
            ModelHealthBaselineDiff healthDiff)
        {
            ProjectId = projectId ?? string.Empty;
            SourceChangeVersion = sourceChangeVersion;
            RegeneratedElementCount = regeneratedElementCount;
            Deltas = (deltas ?? Enumerable.Empty<RevisionDelta>()).ToList().AsReadOnly();
            HealthDiff = healthDiff ?? throw new ArgumentNullException(nameof(healthDiff));
        }

        public string ProjectId { get; }
        public long SourceChangeVersion { get; }
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
            if (project == null) throw new ArgumentNullException(nameof(project));
            var sourceChangeVersion = project.ChangeVersion;
            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var revisions = new RevisionService();
            var health = new ModelHealthBaselineService();
            var beforeRevision = revisions.Capture(detached, "regen-preview-before");
            var beforeHealth = health.CaptureSemantic(detached);

            var count = NewEngine().RegenerateDirty(detached);

            var afterRevision = revisions.Capture(detached, "regen-preview-after");
            var afterHealth = health.CaptureSemantic(detached);
            return new RegenerationPreview(
                project.ProjectId,
                sourceChangeVersion,
                count,
                revisions.Compare(beforeRevision, afterRevision),
                health.Compare(beforeHealth, afterHealth));
        }

        public RegenerationGuardedApplyResult Apply(ProjectState project, RegenerationPreview preview)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            if (!string.Equals(project.ProjectId, preview.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Regeneration preview belongs to a different project.");
            if (preview.SourceChangeVersion != project.ChangeVersion)
                throw new InvalidOperationException("Regeneration preview is stale because the project changed after preview; recompute before applying.");

            var current = Preview(project);
            if (!Equivalent(preview, current))
                throw new InvalidOperationException("Regeneration preview is stale; recompute preview before applying.");
            if (current.IntroducesHealthErrors)
                throw new InvalidOperationException("Regeneration preview introduces " + current.HealthDiff.NewErrorCount + " new Model Health error(s); apply is blocked.");

            var health = new ModelHealthBaselineService();
            var beforeHealth = health.CaptureSemantic(project);
            var snapshot = ProjectStateSnapshot.Capture(project);
            try
            {
                var count = NewEngine().RegenerateDirty(project);
                var afterHealth = health.CaptureSemantic(project);
                var diff = health.Compare(beforeHealth, afterHealth);
                if (diff.NewErrorCount > 0)
                    throw new InvalidOperationException("Regeneration introduced " + diff.NewErrorCount + " new Model Health error(s); project state was rolled back.");
                return new RegenerationGuardedApplyResult(count, diff);
            }
            catch
            {
                snapshot.Restore(project);
                throw;
            }
        }

        private static RegenerationEngine NewEngine()
        {
            return new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
        }

        private static bool Equivalent(RegenerationPreview left, RegenerationPreview right)
        {
            if (!string.Equals(left.ProjectId, right.ProjectId, StringComparison.Ordinal) ||
                left.SourceChangeVersion != right.SourceChangeVersion ||
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
