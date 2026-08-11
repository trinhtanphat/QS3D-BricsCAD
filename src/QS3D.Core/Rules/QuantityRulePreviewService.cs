using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.Core.Rules
{
    public enum QuantityRulePreviewChangeKind
    {
        Added = 0,
        Changed = 1,
        Removed = 2
    }

    public sealed class QuantityRulePreviewChange
    {
        internal QuantityRulePreviewChange(
            string outputName,
            QuantityRulePreviewChangeKind kind,
            double? beforeValue,
            double? afterValue,
            string beforeProvenance,
            string afterProvenance)
        {
            OutputName = outputName ?? string.Empty;
            Kind = kind;
            BeforeValue = beforeValue;
            AfterValue = afterValue;
            BeforeProvenance = beforeProvenance ?? string.Empty;
            AfterProvenance = afterProvenance ?? string.Empty;
        }

        public string OutputName { get; }
        public QuantityRulePreviewChangeKind Kind { get; }
        public double? BeforeValue { get; }
        public double? AfterValue { get; }
        public string BeforeProvenance { get; }
        public string AfterProvenance { get; }
    }

    public sealed class QuantityRuleElementPreview
    {
        internal QuantityRuleElementPreview(string projectId, long sourceChangeVersion, string elementId, ElementCategory category, IEnumerable<QuantityRulePreviewChange> changes)
        {
            ProjectId = projectId ?? string.Empty;
            SourceChangeVersion = sourceChangeVersion;
            ElementId = elementId ?? string.Empty;
            Category = category;
            Changes = (changes ?? Enumerable.Empty<QuantityRulePreviewChange>()).ToList().AsReadOnly();
        }

        public string ProjectId { get; }
        public long SourceChangeVersion { get; }
        public string ElementId { get; }
        public ElementCategory Category { get; }
        public IReadOnlyList<QuantityRulePreviewChange> Changes { get; }
        public bool HasChanges => Changes.Count > 0;
    }

    public sealed class QuantityRuleProjectPreview
    {
        internal QuantityRuleProjectPreview(string projectId, long sourceChangeVersion, IEnumerable<QuantityRuleElementPreview> elements)
        {
            ProjectId = projectId ?? string.Empty;
            SourceChangeVersion = sourceChangeVersion;
            Elements = (elements ?? Enumerable.Empty<QuantityRuleElementPreview>()).ToList().AsReadOnly();
        }

        public string ProjectId { get; }
        public long SourceChangeVersion { get; }
        public IReadOnlyList<QuantityRuleElementPreview> Elements { get; }
        public int ChangedElementCount => Elements.Count(x => x.HasChanges);
        public int ChangeCount => Elements.Sum(x => x.Changes.Count);
        public bool HasChanges => ChangeCount > 0;
    }

    public sealed class QuantityRuleGuardedApplyResult
    {
        internal QuantityRuleGuardedApplyResult(int appliedOperationCount, ModelHealthBaselineDiff healthDiff)
        {
            AppliedOperationCount = appliedOperationCount;
            HealthDiff = healthDiff ?? throw new ArgumentNullException(nameof(healthDiff));
        }

        public int AppliedOperationCount { get; }
        public ModelHealthBaselineDiff HealthDiff { get; }
    }

    public sealed class QuantityRulePreviewService
    {
        private const string ProvenancePrefix = "Rule:";
        private readonly QuantityRuleEngine _engine = new QuantityRuleEngine();

        public QuantityRuleElementPreview PreviewElement(ProjectState project, ProjectElement element)
        {
            RequireOwnedElement(project, element);
            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var detachedElement = detached.FindElement(element.Id)
                ?? throw new InvalidOperationException("Detached quantity-rule preview lost element " + element.Id + ".");
            return PreviewDetached(detached, detachedElement, project.ChangeVersion);
        }

        public QuantityRuleProjectPreview PreviewProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var sourceChangeVersion = project.ChangeVersion;
            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var previews = detached.Elements
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => PreviewDetached(detached, x, sourceChangeVersion))
                .ToList();
            return new QuantityRuleProjectPreview(project.ProjectId, sourceChangeVersion, previews);
        }

        public int ApplyElement(ProjectState project, ProjectElement element, QuantityRuleElementPreview preview)
        {
            RequireOwnedElement(project, element);
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            RequirePreviewIdentity(project, element, preview);
            if (preview.SourceChangeVersion != project.ChangeVersion)
                throw new InvalidOperationException("Quantity-rule preview is stale because the project changed after preview; recompute before applying.");
            var current = PreviewElement(project, element);
            if (!Equivalent(preview, current))
                throw new InvalidOperationException("Quantity-rule preview is stale for element " + element.Id + "; recompute preview before applying.");
            if (!preview.HasChanges) return 0;

            return ProjectSemanticMutationExecutor.Execute(project, "quantity-rule-preview.apply-element", () =>
            {
                var applied = _engine.ApplyMatching(project, element);
                if (applied > 0) project.Touch();
                return applied;
            });
        }

        public int ApplyProject(ProjectState project, QuantityRuleProjectPreview preview)
        {
            ValidateFreshProjectPreview(project, preview);
            var snapshot = ProjectStateSnapshot.Capture(project);
            try
            {
                return ApplyFreshProjectPreview(project, preview);
            }
            catch
            {
                snapshot.Restore(project);
                throw;
            }
        }

        public QuantityRuleGuardedApplyResult ApplyProjectWithHealthGuard(ProjectState project, QuantityRuleProjectPreview preview)
        {
            ValidateFreshProjectPreview(project, preview);
            var health = new ModelHealthBaselineService();
            var before = health.CaptureSemantic(project);
            var snapshot = ProjectStateSnapshot.Capture(project);
            try
            {
                var applied = ApplyFreshProjectPreview(project, preview);
                var after = health.CaptureSemantic(project);
                var diff = health.Compare(before, after);
                if (diff.NewErrorCount > 0)
                    throw new InvalidOperationException("Quantity-rule apply introduced " + diff.NewErrorCount + " new Model Health error(s); project state was rolled back.");
                return new QuantityRuleGuardedApplyResult(applied, diff);
            }
            catch
            {
                snapshot.Restore(project);
                throw;
            }
        }

        private void ValidateFreshProjectPreview(ProjectState project, QuantityRuleProjectPreview preview)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            if (!string.Equals(preview.ProjectId, project.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity-rule project preview belongs to a different project.");
            if (preview.SourceChangeVersion != project.ChangeVersion)
                throw new InvalidOperationException("Quantity-rule project preview is stale because the project changed after preview; recompute before applying.");

            var current = PreviewProject(project);
            if (!Equivalent(preview, current))
                throw new InvalidOperationException("Quantity-rule project preview is stale; recompute preview before applying.");
        }

        private int ApplyFreshProjectPreview(ProjectState project, QuantityRuleProjectPreview preview)
        {
            var applied = 0;
            foreach (var item in preview.Elements.Where(x => x.HasChanges).OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase))
            {
                var element = project.FindElement(item.ElementId)
                    ?? throw new InvalidOperationException("Quantity-rule apply lost element " + item.ElementId + ".");
                applied = checked(applied + _engine.ApplyMatching(project, element));
            }
            if (applied > 0) project.Touch();
            return applied;
        }

        private QuantityRuleElementPreview PreviewDetached(ProjectState detached, ProjectElement element, long sourceChangeVersion)
        {
            var beforeQuantities = new Dictionary<string, double>(element.Quantities, StringComparer.OrdinalIgnoreCase);
            var beforeProvenance = ManagedProvenance(element);

            _engine.ApplyMatching(detached, element);

            var afterQuantities = new Dictionary<string, double>(element.Quantities, StringComparer.OrdinalIgnoreCase);
            var afterProvenance = ManagedProvenance(element);
            var outputs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in beforeProvenance.Keys) outputs.Add(key);
            foreach (var key in afterProvenance.Keys) outputs.Add(key);
            foreach (var rule in detached.QuantityRules.Where(x => x.Category == element.Category)) outputs.Add(rule.OutputName);

            var changes = new List<QuantityRulePreviewChange>();
            foreach (var output in outputs)
            {
                var beforeHasValue = beforeQuantities.TryGetValue(output, out var beforeValue);
                var afterHasValue = afterQuantities.TryGetValue(output, out var afterValue);
                beforeProvenance.TryGetValue(output, out var beforeRule);
                afterProvenance.TryGetValue(output, out var afterRule);
                beforeRule = beforeRule ?? string.Empty;
                afterRule = afterRule ?? string.Empty;

                if (beforeHasValue == afterHasValue &&
                    (!beforeHasValue || beforeValue.Equals(afterValue)) &&
                    string.Equals(beforeRule, afterRule, StringComparison.Ordinal))
                    continue;

                var beforeManaged = beforeHasValue || beforeRule.Length > 0;
                var afterManaged = afterHasValue || afterRule.Length > 0;
                var kind = !beforeManaged && afterManaged
                    ? QuantityRulePreviewChangeKind.Added
                    : beforeManaged && !afterManaged
                        ? QuantityRulePreviewChangeKind.Removed
                        : QuantityRulePreviewChangeKind.Changed;
                changes.Add(new QuantityRulePreviewChange(
                    output,
                    kind,
                    beforeHasValue ? beforeValue : (double?)null,
                    afterHasValue ? afterValue : (double?)null,
                    beforeRule,
                    afterRule));
            }

            return new QuantityRuleElementPreview(detached.ProjectId, sourceChangeVersion, element.Id, element.Category, changes);
        }

        private static Dictionary<string, string> ManagedProvenance(ProjectElement element)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in element.Properties)
            {
                if (!pair.Key.StartsWith(ProvenancePrefix, StringComparison.OrdinalIgnoreCase)) continue;
                var output = pair.Key.Substring(ProvenancePrefix.Length);
                if (string.IsNullOrWhiteSpace(output) || !string.Equals(output, output.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Element " + element.Id + " contains malformed quantity-rule provenance key: " + pair.Key + ".");
                if (result.ContainsKey(output))
                    throw new InvalidOperationException("Element " + element.Id + " contains ambiguous quantity-rule provenance for output " + output + ".");
                result[output] = pair.Value ?? string.Empty;
            }
            return result;
        }

        private static void RequireOwnedElement(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            var owned = project.FindElement(element.Id);
            if (owned == null || !ReferenceEquals(owned, element))
                throw new InvalidOperationException("Quantity-rule preview/apply requires the exact element instance owned by the project.");
        }

        private static void RequirePreviewIdentity(ProjectState project, ProjectElement element, QuantityRuleElementPreview preview)
        {
            if (!string.Equals(preview.ProjectId, project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(preview.ElementId, element.Id, StringComparison.OrdinalIgnoreCase) ||
                preview.Category != element.Category)
                throw new InvalidOperationException("Quantity-rule preview identity does not match the target element.");
        }

        private static bool Equivalent(QuantityRuleElementPreview left, QuantityRuleElementPreview right)
        {
            if (!string.Equals(left.ProjectId, right.ProjectId, StringComparison.Ordinal) ||
                left.SourceChangeVersion != right.SourceChangeVersion ||
                !string.Equals(left.ElementId, right.ElementId, StringComparison.OrdinalIgnoreCase) ||
                left.Category != right.Category ||
                left.Changes.Count != right.Changes.Count)
                return false;

            for (var i = 0; i < left.Changes.Count; i++)
            {
                var a = left.Changes[i];
                var b = right.Changes[i];
                if (!string.Equals(a.OutputName, b.OutputName, StringComparison.OrdinalIgnoreCase) ||
                    a.Kind != b.Kind ||
                    !Nullable.Equals(a.BeforeValue, b.BeforeValue) ||
                    !Nullable.Equals(a.AfterValue, b.AfterValue) ||
                    !string.Equals(a.BeforeProvenance, b.BeforeProvenance, StringComparison.Ordinal) ||
                    !string.Equals(a.AfterProvenance, b.AfterProvenance, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static bool Equivalent(QuantityRuleProjectPreview left, QuantityRuleProjectPreview right)
        {
            if (!string.Equals(left.ProjectId, right.ProjectId, StringComparison.Ordinal) ||
                left.SourceChangeVersion != right.SourceChangeVersion ||
                left.Elements.Count != right.Elements.Count)
                return false;
            for (var i = 0; i < left.Elements.Count; i++)
                if (!Equivalent(left.Elements[i], right.Elements[i])) return false;
            return true;
        }
    }
}
