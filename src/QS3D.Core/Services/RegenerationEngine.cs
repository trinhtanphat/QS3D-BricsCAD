using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.Services
{
    public interface IElementRegenerator
    {
        bool CanRegenerate(ElementCategory category);
        void Regenerate(ProjectState project, ProjectElement element);
    }

    public static class RegeneratorCatalog
    {
        public static IReadOnlyList<IElementRegenerator> CreateDefault() => Array.AsReadOnly(new IElementRegenerator[]
        {
            new OpeningRegenerator(),
            new WallRegenerator(),
            new StructuralRegenerator(),
            new RoomRegenerator(),
            new GenericTakeoffRegenerator()
        });
    }

    public sealed class RegenerationEngine
    {
        private readonly DependencyGraph _graph;
        private readonly IList<IElementRegenerator> _regenerators;
        private readonly QuantityRuleEngine _ruleEngine;

        public RegenerationEngine(DependencyGraph graph, IEnumerable<IElementRegenerator> regenerators)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            if (regenerators == null) throw new ArgumentNullException(nameof(regenerators));
            _regenerators = MaterializeRegenerators(regenerators);
            _ruleEngine = new QuantityRuleEngine();
        }

        private static List<IElementRegenerator> MaterializeRegenerators(IEnumerable<IElementRegenerator> regenerators)
        {
            var knownCount = ReadKnownRegeneratorCount(regenerators);
            var materialized = new List<IElementRegenerator>(knownCount ?? 0);
            using (var enumerator = regenerators.GetEnumerator())
            {
                EnsureKnownRegeneratorCountStable(regenerators, knownCount);
                while (true)
                {
                    EnsureKnownRegeneratorCountStable(regenerators, knownCount);
                    var moved = enumerator.MoveNext();
                    EnsureKnownRegeneratorCountStable(regenerators, knownCount);
                    if (!moved) break;
                    if (knownCount.HasValue && materialized.Count >= knownCount.Value)
                        throw new InvalidOperationException("Regenerator collection enumerated more entries than its reported Count " + knownCount.Value.ToString(CultureInfo.InvariantCulture) + ".");
                    var current = enumerator.Current;
                    EnsureKnownRegeneratorCountStable(regenerators, knownCount);
                    if (current == null)
                        throw new ArgumentException("Regenerator collection cannot contain null entries.", nameof(regenerators));
                    materialized.Add(current);
                }
            }
            EnsureKnownRegeneratorCountStable(regenerators, knownCount);
            if (knownCount.HasValue && materialized.Count != knownCount.Value)
                throw new InvalidOperationException("Regenerator collection reported Count " + knownCount.Value.ToString(CultureInfo.InvariantCulture) + " but enumerated " + materialized.Count.ToString(CultureInfo.InvariantCulture) + " entries.");
            return materialized;
        }

        private static void EnsureKnownRegeneratorCountStable(IEnumerable<IElementRegenerator> regenerators, int? admittedCount)
        {
            var observedCount = ReadKnownRegeneratorCount(regenerators);
            if (observedCount != admittedCount)
                throw new InvalidOperationException("Regenerator collection changed its reported Count during enumeration.");
        }

        private static int? ReadKnownRegeneratorCount(IEnumerable<IElementRegenerator> regenerators)
        {
            var genericCount = regenerators is ICollection<IElementRegenerator> genericCollection ? (int?)genericCollection.Count : null;
            var readOnlyCount = regenerators is IReadOnlyCollection<IElementRegenerator> readOnlyCollection ? (int?)readOnlyCollection.Count : null;
            var nonGenericCount = regenerators is ICollection nonGenericCollection ? (int?)nonGenericCollection.Count : null;
            if ((genericCount.HasValue && genericCount.Value < 0) ||
                (readOnlyCount.HasValue && readOnlyCount.Value < 0) ||
                (nonGenericCount.HasValue && nonGenericCount.Value < 0))
                throw new ArgumentException("Regenerator collection reported a negative Count.", nameof(regenerators));
            var expected = genericCount ?? readOnlyCount ?? nonGenericCount;
            if (!expected.HasValue) return null;
            if ((genericCount.HasValue && genericCount.Value != expected.Value) ||
                (readOnlyCount.HasValue && readOnlyCount.Value != expected.Value) ||
                (nonGenericCount.HasValue && nonGenericCount.Value != expected.Value))
                throw new ArgumentException("Regenerator collection reported conflicting Count values.", nameof(regenerators));
            return expected;
        }

        public void MarkChanged(ProjectState project, string elementId, ElementDirtyFlags flags)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedId = elementId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedId))
                throw new ArgumentException("Regeneration changed element id cannot be blank.", nameof(elementId));
            if (!string.Equals(normalizedId, normalizedId.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Regeneration changed element id must be canonical without surrounding whitespace: " + normalizedId + ".", nameof(elementId));

            _graph.Rebuild(project.Elements);
            if (!_graph.TryGetElement(normalizedId, out var source) || source == null)
                throw new KeyNotFoundException("Unknown element: " + elementId);
            if (flags == ElementDirtyFlags.None) return;

            var dependents = new List<ProjectElement>();
            foreach (var dependentId in _graph.GetDependentsTransitive(source.Id))
            {
                if (!_graph.TryGetElement(dependentId, out var dependent) || dependent == null)
                    throw new InvalidOperationException("Dependency graph returned missing semantic element: " + dependentId);
                dependents.Add(dependent);
            }

            ProjectSemanticMutationExecutor.Execute(project, "regeneration.mark-changed", () =>
            {
                source.MarkDirty(flags);
                foreach (var dependent in dependents)
                    dependent.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                project.Touch();
                return true;
            });
        }

        public int RegenerateDirty(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidateProjectElements(project.Elements);
            _graph.Rebuild(project.Elements);
            return RegenerateTransactional(project, project.Elements, project.Elements.Count);
        }

        public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));

            var inputVersion = project.ChangeVersion;
            var sourceElements = project.Elements.ToArray();
            var unresolved = CanonicalTargetIds(elementIds, sourceElements.Length);
            if (project.ChangeVersion != inputVersion)
                throw new InvalidOperationException("Project state changed while materializing regeneration target ids.");
            RequireElementStructureFresh(project, sourceElements);
            if (unresolved.Count == 0) return 0;

            var targets = new List<ProjectElement>(unresolved.Count);
            var seenProjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in sourceElements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (!seenProjectIds.Add(element.Id))
                    throw new InvalidOperationException("Project contains duplicate element id: " + element.Id);
                if (unresolved.Remove(element.Id)) targets.Add(element);
            }
            if (unresolved.Count > 0)
            {
                var missing = unresolved.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First();
                throw new KeyNotFoundException("Unknown regeneration target: " + missing);
            }
            ValidateSubsetDependencyExistence(targets, seenProjectIds);
            if (project.ChangeVersion != inputVersion)
                throw new InvalidOperationException("Project state changed while materializing regeneration target ids.");
            RequireElementStructureFresh(project, sourceElements);

            return RegenerateTransactional(project, targets, targets.Count);
        }

        private static void ValidateProjectElements(IEnumerable<ProjectElement> elements)
        {
            var seenProjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (!seenProjectIds.Add(element.Id))
                    throw new InvalidOperationException("Project contains duplicate element id: " + element.Id);
            }
        }

        private static void RequireElementStructureFresh(ProjectState project, IReadOnlyList<ProjectElement> sourceElements)
        {
            if (project.Elements.Count != sourceElements.Count)
                throw StructuralFreshnessError();
            for (var index = 0; index < sourceElements.Count; index++)
                if (!ReferenceEquals(project.Elements[index], sourceElements[index]))
                    throw StructuralFreshnessError();
        }

        private static InvalidOperationException StructuralFreshnessError()
        {
            return new InvalidOperationException(
                "Project element structure changed while materializing regeneration target ids. Retry targeted regeneration against the current project state.");
        }

        private static void ValidateSubsetDependencyExistence(
            IEnumerable<ProjectElement> targets,
            ISet<string> projectIds)
        {
            foreach (var target in targets)
            {
                var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var canonicalUnique = true;
                foreach (var dependencyRaw in target.DependsOn)
                {
                    var dependency = dependencyRaw ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(dependency) ||
                        !string.Equals(dependency, dependency.Trim(), StringComparison.Ordinal) ||
                        !dependencies.Add(dependency))
                    {
                        canonicalUnique = false;
                        break;
                    }
                }
                if (!canonicalUnique) continue;

                foreach (var dependency in dependencies)
                {
                    if (projectIds.Contains(dependency)) continue;
                    throw new InvalidOperationException(
                        "Semantic element " + target.Id + " depends on missing semantic element: " + dependency + ". Repair semantic relations before graph evaluation.");
                }
            }
        }

        private static HashSet<string> CanonicalTargetIds(IEnumerable<string> elementIds, int maxCount)
        {
            var knownCount = ValidateKnownTargetIdCounts(elementIds);
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            using (var enumerator = elementIds.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownTargetIdCounts(elementIds, knownCount);
                    if (!enumerator.MoveNext()) break;
                    RequireStableKnownTargetIdCounts(elementIds, knownCount);

                    if (knownCount.HasValue && index >= knownCount.Value)
                        throw new InvalidOperationException("Regeneration target id count changed during enumeration.");

                    var value = enumerator.Current;
                    RequireStableKnownTargetIdCounts(elementIds, knownCount);
                    var raw = value ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(raw))
                        throw new ArgumentException("Regeneration target id cannot be blank at index " + index.ToString(CultureInfo.InvariantCulture) + ".", nameof(elementIds));
                    if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                        throw new ArgumentException("Regeneration target id must be canonical without surrounding whitespace: " + raw + ".", nameof(elementIds));
                    if (result.Contains(raw))
                        throw new ArgumentException("Duplicate regeneration target id: " + raw + ".", nameof(elementIds));
                    if (result.Count >= maxCount)
                        throw new ArgumentException("Regeneration target set cannot exceed project element count of " + maxCount.ToString(CultureInfo.InvariantCulture) + ".", nameof(elementIds));
                    result.Add(raw);
                    index++;
                }
            }

            if (knownCount.HasValue && knownCount.Value != index)
                throw new InvalidOperationException("Regeneration target id count changed during enumeration.");
            RequireStableKnownTargetIdCounts(elementIds, knownCount);
            return result;
        }

        private static void RequireStableKnownTargetIdCounts(IEnumerable<string> elementIds, int? expectedCount)
        {
            var observedCount = ValidateKnownTargetIdCounts(elementIds);
            if (observedCount != expectedCount)
                throw new InvalidOperationException("Regeneration target id count changed during enumeration.");
        }

        private static int? ValidateKnownTargetIdCounts(IEnumerable<string> elementIds)
        {
            var genericCount = elementIds is ICollection<string> collection ? (int?)collection.Count : null;
            var readOnlyCount = elementIds is IReadOnlyCollection<string> readOnlyCollection ? (int?)readOnlyCollection.Count : null;
            var nonGenericCount = elementIds is System.Collections.ICollection nonGenericCollection ? (int?)nonGenericCollection.Count : null;

            ValidateKnownTargetIdCount(genericCount, nameof(elementIds));
            ValidateKnownTargetIdCount(readOnlyCount, nameof(elementIds));
            ValidateKnownTargetIdCount(nonGenericCount, nameof(elementIds));

            var expected = genericCount ?? readOnlyCount ?? nonGenericCount;
            if (!expected.HasValue) return null;
            if ((genericCount.HasValue && genericCount.Value != expected.Value) ||
                (readOnlyCount.HasValue && readOnlyCount.Value != expected.Value) ||
                (nonGenericCount.HasValue && nonGenericCount.Value != expected.Value))
                throw new ArgumentException("Regeneration target ids report conflicting known counts.", nameof(elementIds));
            return expected;
        }

        private static void ValidateKnownTargetIdCount(int? count, string parameterName)
        {
            if (!count.HasValue) return;
            if (count.Value < 0)
                throw new ArgumentException("Regeneration target ids report an invalid negative known count.", parameterName);
        }

        private int RegenerateTransactional(ProjectState project, IEnumerable<ProjectElement> candidates, int passBasis)
        {
            var snapshot = ProjectStateSnapshot.Capture(project);
            try
            {
                return Regenerate(project, candidates, passBasis);
            }
            catch (Exception regenerationError)
            {
                try
                {
                    snapshot.Restore(project);
                }
                catch (Exception rollbackError)
                {
                    throw new AggregateException("Semantic regeneration failed and project rollback also failed.", regenerationError, rollbackError);
                }
                throw;
            }
        }

        private int Regenerate(ProjectState project, IEnumerable<ProjectElement> candidates, int passBasis)
        {
            var candidateList = candidates?.ToList() ?? throw new ArgumentNullException(nameof(candidates));
            var total = 0;
            var maxPasses = Math.Max(2, passBasis * 2 + 2);

            for (var pass = 0; pass < maxPasses; pass++)
            {
                var dirty = _graph.TopologicalDirtyOrder(candidateList);
                if (dirty.Count == 0) break;
                var progress = 0;

                foreach (var element in dirty)
                {
                    var semanticDirty = element.Dirty & (ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                    if (semanticDirty == ElementDirtyFlags.None) continue;

                    IElementRegenerator? selected = null;
                    foreach (var regenerator in _regenerators)
                    {
                        if (!regenerator.CanRegenerate(element.Category)) continue;
                        selected = regenerator;
                        break;
                    }

                    var handled = false;
                    if (selected != null)
                    {
                        selected.Regenerate(project, element);
                        handled = true;
                    }
                    if (MeasuredSolidQuantityPolicy.Apply(element)) handled = true;
                    if (_ruleEngine.ApplyMatching(project, element) > 0) handled = true;
                    if (!handled) continue;

                    element.MarkClean(ElementGeometryPolicy.SemanticCleanFlags(element.Category));
                    progress++;
                    total++;
                }

                if (progress == 0) break;
            }

            if (total > 0) project.Touch();
            return total;
        }
    }
}