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
        private readonly string _projectId;
        private readonly DateTime _projectUpdatedUtc;
        private readonly long _projectChangeVersion;
        private readonly Dictionary<string, ElementPersistenceState> _elements;
        private readonly IReadOnlyList<string> _elementIds;

        private ProjectPersistenceCheckpoint(
            string projectId,
            DateTime projectUpdatedUtc,
            long projectChangeVersion,
            Dictionary<string, ElementPersistenceState> elements)
        {
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
            RejectKnownOversize(elementIds);

            var elements = new Dictionary<string, ElementPersistenceState>(StringComparer.OrdinalIgnoreCase);
            var observed = 0;
            foreach (var rawId in elementIds)
            {
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
                elements.Add(id, new ElementPersistenceState(element.Dirty, element.UpdatedUtc));
            }

            return new ProjectPersistenceCheckpoint(
                project.ProjectId,
                project.UpdatedUtc,
                project.ChangeVersion,
                elements);
        }

        public bool Matches(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal) ||
                project.ChangeVersion != _projectChangeVersion ||
                project.UpdatedUtc != _projectUpdatedUtc)
                return false;

            foreach (var pair in _elements)
            {
                var element = project.FindElement(pair.Key);
                if (element == null || !pair.Value.Matches(element)) return false;
            }
            return true;
        }

        public void Restore(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Cannot restore a persistence checkpoint into a different project id.");

            // Resolve the complete target set before the first mutation. Captured
            // values came from valid domain objects, so the internal exact-state
            // restores below cannot overflow or partially advance revision state.
            var targets = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in _elementIds)
            {
                var element = project.FindElement(id)
                    ?? throw new InvalidOperationException("Cannot restore missing persistence checkpoint element: " + id + ".");
                targets.Add(id, element);
            }

            foreach (var pair in _elements)
                pair.Value.Restore(targets[pair.Key]);
            project.RestorePersistenceState(_projectUpdatedUtc, _projectChangeVersion);
        }

        private static void RejectKnownOversize(IEnumerable<string> elementIds)
        {
            if (elementIds is ICollection<string> collection && collection.Count > MaximumElementCount)
                throw new InvalidOperationException("Persistence checkpoint exceeds the supported " + MaximumElementCount + " element limit.");
            if (elementIds is IReadOnlyCollection<string> readOnlyCollection && readOnlyCollection.Count > MaximumElementCount)
                throw new InvalidOperationException("Persistence checkpoint exceeds the supported " + MaximumElementCount + " element limit.");
            if (elementIds is ICollection nonGenericCollection && nonGenericCollection.Count > MaximumElementCount)
                throw new InvalidOperationException("Persistence checkpoint exceeds the supported " + MaximumElementCount + " element limit.");
        }

        private sealed class ElementPersistenceState
        {
            public ElementPersistenceState(ElementDirtyFlags dirty, DateTime updatedUtc)
            {
                Dirty = dirty;
                UpdatedUtc = updatedUtc;
            }

            public ElementDirtyFlags Dirty { get; }
            public DateTime UpdatedUtc { get; }

            public bool Matches(ProjectElement element) =>
                element.Dirty == Dirty && element.UpdatedUtc == UpdatedUtc;

            public void Restore(ProjectElement element) =>
                element.RestorePersistenceState(Dirty, UpdatedUtc);
        }
    }
}
