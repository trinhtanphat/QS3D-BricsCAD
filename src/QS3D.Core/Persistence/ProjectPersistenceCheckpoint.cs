using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
                    throw new InvalidOperationException("Cannot capture a persistence checkpoint while captured element persistence state is changing or semantic state changed.");
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
            private readonly byte[] _signature;

            private ElementSemanticState(byte[] signature)
            {
                _signature = signature ?? throw new ArgumentNullException(nameof(signature));
            }

            public static ElementSemanticState Capture(ProjectElement element) =>
                new ElementSemanticState(ComputeSignature(element));

            public bool Matches(ProjectElement element)
            {
                byte[] candidate;
                try
                {
                    candidate = ComputeSignature(element);
                }
                catch (InvalidOperationException)
                {
                    return false;
                }

                if (candidate.Length != _signature.Length) return false;
                for (var i = 0; i < _signature.Length; i++)
                {
                    if (candidate[i] != _signature[i]) return false;
                }
                return true;
            }

            private static byte[] ComputeSignature(ProjectElement element)
            {
                if (element == null) throw new ArgumentNullException(nameof(element));

                using var hash = SHA256.Create();
                using var crypto = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
                using var writer = new BinaryWriter(crypto, Encoding.UTF8, leaveOpen: true);

                writer.Write((int)element.Category);
                writer.Write(element.FamilyId ?? string.Empty);
                writer.Write(element.FloorId ?? string.Empty);
                writer.Write(element.ZoneId ?? string.Empty);
                writer.Write(element.DrawingFingerprint ?? string.Empty);
                WriteSequence(writer, element.SourceHandles, "source handles");
                WriteSequence(writer, element.DependsOn, "dependencies");
                WriteMap(writer, element.Properties, "properties", (output, value) => output.Write(value ?? string.Empty));
                WriteMap(writer, element.Quantities, "quantities", (output, value) => output.Write(value));

                writer.Flush();
                crypto.FlushFinalBlock();
                var signature = hash.Hash;
                if (signature == null || signature.Length == 0)
                    throw new InvalidOperationException("Persistence checkpoint could not finalize the element semantic signature.");
                return (byte[])signature.Clone();
            }

            private static void WriteSequence(BinaryWriter writer, IList<string> values, string label)
            {
                if (values == null) throw new InvalidOperationException("Persistence checkpoint element " + label + " collection is missing.");
                var expected = RequireSupportedNestedCount(values.Count, label);
                writer.Write(expected);
                var observed = 0;
                foreach (var value in values)
                {
                    if (observed >= expected)
                        throw new InvalidOperationException("Persistence checkpoint element " + label + " count changed during capture.");
                    writer.Write(CanonicalizeOrdinalIgnoreCaseIdentity(value));
                    observed++;
                }
                if (observed != expected || values.Count != expected)
                    throw new InvalidOperationException("Persistence checkpoint element " + label + " count changed during capture.");
            }

            private static void WriteMap<TValue>(
                BinaryWriter writer,
                IDictionary<string, TValue> values,
                string label,
                Action<BinaryWriter, TValue> writeValue)
            {
                if (values == null) throw new InvalidOperationException("Persistence checkpoint element " + label + " collection is missing.");
                var expected = RequireSupportedNestedCount(values.Count, label);
                var snapshot = new List<KeyValuePair<string, TValue>>(expected);
                foreach (var pair in values)
                {
                    if (snapshot.Count >= expected)
                        throw new InvalidOperationException("Persistence checkpoint element " + label + " count changed during capture.");
                    snapshot.Add(pair);
                }
                if (snapshot.Count != expected || values.Count != expected)
                    throw new InvalidOperationException("Persistence checkpoint element " + label + " count changed during capture.");

                snapshot.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key));
                writer.Write(expected);
                foreach (var pair in snapshot)
                {
                    writer.Write(CanonicalizeOrdinalIgnoreCaseIdentity(pair.Key));
                    writeValue(writer, pair.Value);
                }
            }

            private static string CanonicalizeOrdinalIgnoreCaseIdentity(string value) =>
                (value ?? string.Empty).ToUpperInvariant();

            private static int RequireSupportedNestedCount(int count, string label)
            {
                if (count < 0)
                    throw new InvalidOperationException("Persistence checkpoint element " + label + " collection reported a negative count.");
                if (count > MaximumElementCount)
                    throw new InvalidOperationException(
                        "Persistence checkpoint element " + label + " collection exceeds the supported " + MaximumElementCount + " entry limit.");
                return count;
            }
        }
    }
}
