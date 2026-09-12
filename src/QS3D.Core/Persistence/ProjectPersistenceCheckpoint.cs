using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Persistence
{
    /// <summary>
    /// Captures only the project persistence revision and the persistence state of
    /// an explicit element set. Restore is deliberately narrower than
    /// <see cref="ProjectStateSnapshot"/>: semantic content remains the caller's
    /// responsibility and unrelated elements/audit history are never replaced.
    /// </summary>
    public sealed class ProjectPersistenceCheckpoint
    {
        private const int MaximumElementCount = 10000;
        private readonly ProjectState _projectOwner;
        private readonly string _projectId;
        private readonly DateTime _projectUpdatedUtc;
        private readonly long _projectChangeVersion;
        private readonly Dictionary<string, ElementPersistenceState> _elements;
        private readonly IReadOnlyList<string> _elementIds;

        private ProjectPersistenceCheckpoint(
            ProjectState projectOwner,
            string projectId,
            DateTime projectUpdatedUtc,
            long projectChangeVersion,
            Dictionary<string, ElementPersistenceState> elements)
        {
            _projectOwner = projectOwner ?? throw new ArgumentNullException(nameof(projectOwner));
            _projectId = projectId;
            _projectUpdatedUtc = projectUpdatedUtc;
            _projectChangeVersion = projectChangeVersion;
            _elements = elements;
            _elementIds = elements.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        public IReadOnlyList<string> ElementIds => _elementIds;
        public DateTime ProjectUpdatedUtc => _projectUpdatedUtc;
        public long ProjectChangeVersion => _projectChangeVersion;

        public static ProjectPersistenceCheckpoint Capture(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));

            var projectId = project.ProjectId;
            var projectUpdatedUtc = project.UpdatedUtc;
            var projectChangeVersion = project.ChangeVersion;
            var expectedKnownCount = RejectMalformedKnownCounts(elementIds);

            var elements = new Dictionary<string, ElementPersistenceState>(StringComparer.OrdinalIgnoreCase);
            var observed = 0;
            using var enumerator = elementIds.GetEnumerator();
            if (expectedKnownCount.HasValue)
                RequireStableKnownCount(elementIds, expectedKnownCount.Value);

            while (true)
            {
                if (expectedKnownCount.HasValue)
                    RequireStableKnownCount(elementIds, expectedKnownCount.Value);

                var movedNext = enumerator.MoveNext();

                if (expectedKnownCount.HasValue)
                    RequireStableKnownCount(elementIds, expectedKnownCount.Value);
                if (!movedNext)
                    break;

                if (expectedKnownCount.HasValue && observed >= expectedKnownCount.Value)
                    throw new InvalidOperationException("Persistence checkpoint known element count does not match enumerated element count.");

                var rawId = enumerator.Current;
                if (expectedKnownCount.HasValue)
                    RequireStableKnownCount(elementIds, expectedKnownCount.Value);

                observed++;
                if (observed > MaximumElementCount)
                    throw new InvalidOperationException("Persistence checkpoint exceeds the supported " + MaximumElementCount + " element limit.");

                var id = rawId ?? string.Empty;
                if (id.Length == 0 || string.IsNullOrWhiteSpace(id))
                    throw new InvalidOperationException("Persistence checkpoint element id is required.");
                if (!string.Equals(id, id.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Persistence checkpoint element id must be canonical without leading or trailing whitespace: " + id + ".");
                if (elements.ContainsKey(id))
                    throw new InvalidOperationException("Persistence checkpoint contains duplicate element id: " + id + ".");
                var element = project.FindElement(id)
                    ?? throw new InvalidOperationException("Persistence checkpoint element is missing: " + id + ".");
                elements.Add(id, new ElementPersistenceState(element, element.Dirty, element.UpdatedUtc));
            }

            if (expectedKnownCount.HasValue && observed != expectedKnownCount.Value)
                throw new InvalidOperationException("Persistence checkpoint known element count does not match enumerated element count.");

            if (!string.Equals(project.ProjectId, projectId, StringComparison.Ordinal) ||
                project.UpdatedUtc != projectUpdatedUtc ||
                project.ChangeVersion != projectChangeVersion)
                throw new InvalidOperationException("Cannot capture a persistence checkpoint while the project revision is changing.");

            foreach (var pair in elements)
            {
                var element = project.FindElement(pair.Key);
                if (element == null || !ReferenceEquals(element, pair.Value.Owner) || !pair.Value.Matches(element))
                    throw new InvalidOperationException("Cannot capture a persistence checkpoint while captured element persistence or semantic state is changing.");
            }

            return new ProjectPersistenceCheckpoint(
                project,
                projectId,
                projectUpdatedUtc,
                projectChangeVersion,
                elements);
        }

        public bool Matches(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(project, _projectOwner))
                return false;
            if (!string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal) ||
                project.ChangeVersion != _projectChangeVersion ||
                project.UpdatedUtc != _projectUpdatedUtc)
                return false;

            foreach (var pair in _elements)
            {
                var element = project.FindElement(pair.Key);
                if (element == null || !ReferenceEquals(element, pair.Value.Owner) || !pair.Value.Matches(element)) return false;
            }
            return true;
        }

        public void Restore(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Cannot restore a persistence checkpoint into a different project id.");
            if (!ReferenceEquals(project, _projectOwner))
                throw new InvalidOperationException("Cannot restore a persistence checkpoint into a replacement project generation.");

            // Resolve and generation-fence the complete target set before the first mutation.
            // Logical ids are reusable domain identity; an in-memory persistence checkpoint
            // must never transplant stale persistence metadata onto a replacement object.
            var targets = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in _elementIds)
            {
                var element = project.FindElement(id)
                    ?? throw new InvalidOperationException("Cannot restore missing persistence checkpoint element: " + id + ".");
                var captured = _elements[id];
                if (!ReferenceEquals(element, captured.Owner))
                    throw new InvalidOperationException("Cannot restore persistence checkpoint because captured element generation changed: " + id + ".");
                targets.Add(id, element);
            }

            if (project.ChangeVersion != _projectChangeVersion ||
                project.UpdatedUtc != _projectUpdatedUtc)
                throw new InvalidOperationException("Cannot restore a persistence checkpoint because the project revision changed since checkpoint capture.");

            // Element semantic mutations do not necessarily advance the owning ProjectState
            // revision. Validate every captured semantic generation before restoring any
            // persistence metadata so a newer semantic generation cannot receive stale
            // Dirty/UpdatedUtc values from this checkpoint.
            foreach (var pair in _elements)
            {
                if (!pair.Value.SemanticMatches(targets[pair.Key]))
                    throw new InvalidOperationException(
                        "Cannot restore a persistence checkpoint because captured element semantic state changed: " + pair.Key + ".");
            }

            foreach (var pair in _elements)
                pair.Value.Restore(targets[pair.Key]);
            project.RestorePersistenceState(_projectUpdatedUtc, _projectChangeVersion);
        }

        private static void RequireStableKnownCount(IEnumerable<string> elementIds, int expectedKnownCount)
        {
            var currentKnownCount = RejectMalformedKnownCounts(elementIds);
            if (!currentKnownCount.HasValue || currentKnownCount.Value != expectedKnownCount)
                throw new InvalidOperationException("Persistence checkpoint known element count changed during enumeration.");
        }

        private static int? RejectMalformedKnownCounts(IEnumerable<string> elementIds)
        {
            var knownCounts = new List<int>(3);
            if (elementIds is ICollection<string> collection)
                knownCounts.Add(collection.Count);
            if (elementIds is IReadOnlyCollection<string> readOnlyCollection)
                knownCounts.Add(readOnlyCollection.Count);
            if (elementIds is ICollection nonGenericCollection)
                knownCounts.Add(nonGenericCollection.Count);

            if (knownCounts.Any(count => count > MaximumElementCount))
                throw new InvalidOperationException("Persistence checkpoint exceeds the supported " + MaximumElementCount + " element limit.");

            if (knownCounts.Any(count => count < 0))
                throw new InvalidOperationException("Persistence checkpoint collection reported an invalid negative element count.");

            if (knownCounts.Count > 1 && knownCounts.Any(count => count != knownCounts[0]))
                throw new InvalidOperationException("Persistence checkpoint collection reported conflicting element counts.");

            return knownCounts.Count == 0 ? (int?)null : knownCounts[0];
        }

        private sealed class ElementPersistenceState
        {
            private readonly ElementSemanticState _semanticState;

            public ElementPersistenceState(ProjectElement owner, ElementDirtyFlags dirty, DateTime updatedUtc)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                Dirty = dirty;
                UpdatedUtc = updatedUtc;
                _semanticState = ElementSemanticState.Capture(owner);
            }

            public ProjectElement Owner { get; }
            public ElementDirtyFlags Dirty { get; }
            public DateTime UpdatedUtc { get; }

            public bool Matches(ProjectElement element) =>
                element.Dirty == Dirty && element.UpdatedUtc == UpdatedUtc && SemanticMatches(element);

            public bool SemanticMatches(ProjectElement element) => _semanticState.Matches(element);

            public void Restore(ProjectElement element) =>
                element.RestorePersistenceState(Dirty, UpdatedUtc);
        }

        private sealed class ElementSemanticState
        {
            private readonly ElementCategory _category;
            private readonly string _familyId;
            private readonly string _floorId;
            private readonly string _zoneId;
            private readonly string _drawingFingerprint;
            private readonly string[] _sourceHandles;
            private readonly string[] _dependsOn;
            private readonly KeyValuePair<string, string>[] _properties;
            private readonly KeyValuePair<string, double>[] _quantities;

            private ElementSemanticState(
                ElementCategory category,
                string familyId,
                string floorId,
                string zoneId,
                string drawingFingerprint,
                string[] sourceHandles,
                string[] dependsOn,
                KeyValuePair<string, string>[] properties,
                KeyValuePair<string, double>[] quantities)
            {
                _category = category;
                _familyId = familyId;
                _floorId = floorId;
                _zoneId = zoneId;
                _drawingFingerprint = drawingFingerprint;
                _sourceHandles = sourceHandles;
                _dependsOn = dependsOn;
                _properties = properties;
                _quantities = quantities;
            }

            public static ElementSemanticState Capture(ProjectElement element)
            {
                if (element == null) throw new ArgumentNullException(nameof(element));

                return new ElementSemanticState(
                    element.Category,
                    element.FamilyId,
                    element.FloorId,
                    element.ZoneId,
                    element.DrawingFingerprint,
                    CaptureSequence(element.SourceHandles, "source handles"),
                    CaptureSequence(element.DependsOn, "dependencies"),
                    CaptureMap(element.Properties, "properties"),
                    CaptureMap(element.Quantities, "quantities"));
            }

            public bool Matches(ProjectElement element)
            {
                if (element == null) return false;
                if (element.Category != _category ||
                    !string.Equals(element.FamilyId, _familyId, StringComparison.Ordinal) ||
                    !string.Equals(element.FloorId, _floorId, StringComparison.Ordinal) ||
                    !string.Equals(element.ZoneId, _zoneId, StringComparison.Ordinal) ||
                    !string.Equals(element.DrawingFingerprint, _drawingFingerprint, StringComparison.Ordinal))
                    return false;

                return SequenceMatches(element.SourceHandles, _sourceHandles) &&
                       SequenceMatches(element.DependsOn, _dependsOn) &&
                       MapMatches(element.Properties, _properties, StringComparer.Ordinal) &&
                       MapMatches(element.Quantities, _quantities, EqualityComparer<double>.Default);
            }

            private static string[] CaptureSequence(IList<string> values, string label)
            {
                if (values == null) throw new InvalidOperationException("Persistence checkpoint element " + label + " collection is missing.");
                var expected = RequireSupportedNestedCount(values.Count, label);
                var snapshot = new List<string>(expected);
                foreach (var value in values)
                {
                    if (snapshot.Count >= expected)
                        throw new InvalidOperationException("Persistence checkpoint element " + label + " count changed during capture.");
                    snapshot.Add(value);
                }
                if (snapshot.Count != expected || values.Count != expected)
                    throw new InvalidOperationException("Persistence checkpoint element " + label + " count changed during capture.");
                return snapshot.ToArray();
            }

            private static KeyValuePair<TKey, TValue>[] CaptureMap<TKey, TValue>(IDictionary<TKey, TValue> values, string label)
            {
                if (values == null) throw new InvalidOperationException("Persistence checkpoint element " + label + " collection is missing.");
                var expected = RequireSupportedNestedCount(values.Count, label);
                var snapshot = new List<KeyValuePair<TKey, TValue>>(expected);
                foreach (var value in values)
                {
                    if (snapshot.Count >= expected)
                        throw new InvalidOperationException("Persistence checkpoint element " + label + " count changed during capture.");
                    snapshot.Add(value);
                }
                if (snapshot.Count != expected || values.Count != expected)
                    throw new InvalidOperationException("Persistence checkpoint element " + label + " count changed during capture.");
                return snapshot.ToArray();
            }

            private static int RequireSupportedNestedCount(int count, string label)
            {
                if (count < 0)
                    throw new InvalidOperationException("Persistence checkpoint element " + label + " collection reported a negative count.");
                if (count > MaximumElementCount)
                    throw new InvalidOperationException(
                        "Persistence checkpoint element " + label + " collection exceeds the supported " + MaximumElementCount + " entry limit.");
                return count;
            }

            private static bool SequenceMatches(IList<string> values, IReadOnlyList<string> expected)
            {
                if (values == null || values.Count != expected.Count) return false;
                for (var i = 0; i < expected.Count; i++)
                {
                    if (!string.Equals(values[i], expected[i], StringComparison.Ordinal)) return false;
                }
                return values.Count == expected.Count;
            }

            private static bool MapMatches<TValue>(
                IDictionary<string, TValue> values,
                IReadOnlyList<KeyValuePair<string, TValue>> expected,
                IEqualityComparer<TValue> valueComparer)
            {
                if (values == null || values.Count != expected.Count) return false;
                foreach (var pair in expected)
                {
                    if (!values.TryGetValue(pair.Key, out var actual) || !valueComparer.Equals(actual, pair.Value))
                        return false;
                }
                return values.Count == expected.Count;
            }
        }
    }
}