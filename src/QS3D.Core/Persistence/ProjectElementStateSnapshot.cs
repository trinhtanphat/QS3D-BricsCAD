using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectElementStateSnapshot
    {
        private readonly ProjectState _projectOwner;
        private readonly string _projectId;
        private readonly ProjectElement _elementOwner;
        private readonly string _elementId;
        private readonly ElementCategory _category;
        private readonly string _familyId;
        private readonly string _floorId;
        private readonly string _zoneId;
        private readonly string _drawingFingerprint;
        private readonly IReadOnlyList<string> _sourceHandles;
        private readonly IReadOnlyList<string> _dependsOn;
        private readonly IReadOnlyDictionary<string, string> _properties;
        private readonly IReadOnlyDictionary<string, double> _quantities;
        private readonly ElementDirtyFlags _dirty;
        private readonly DateTime _updatedUtc;

        private ProjectElementStateSnapshot(ProjectState project, ProjectElement element)
        {
            _projectOwner = project ?? throw new ArgumentNullException(nameof(project));
            _projectId = project.ProjectId;
            _elementOwner = element ?? throw new ArgumentNullException(nameof(element));
            _elementId = element.Id;
            _category = element.Category;
            _familyId = element.FamilyId;
            _floorId = element.FloorId;
            _zoneId = element.ZoneId;
            _drawingFingerprint = element.DrawingFingerprint;
            _sourceHandles = element.SourceHandles.Select(value => value ?? string.Empty).ToArray();
            _dependsOn = element.DependsOn.Select(value => value ?? string.Empty).ToArray();
            _properties = new Dictionary<string, string>(element.Properties, StringComparer.OrdinalIgnoreCase);
            _quantities = new Dictionary<string, double>(element.Quantities, StringComparer.OrdinalIgnoreCase);
            _dirty = element.Dirty;
            _updatedUtc = element.UpdatedUtc;
        }

        public string ElementId => _elementId;

        public static ProjectElementStateSnapshot Capture(ProjectState project, string elementId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(elementId))
                throw new ArgumentException("Element id is required.", nameof(elementId));
            var element = project.FindElement(elementId)
                ?? throw new InvalidOperationException("Cannot capture missing project element: " + elementId + ".");
            return new ProjectElementStateSnapshot(project, element);
        }

        public bool Matches(ProjectState project)
        {
            if (!TryResolve(project, out var element)) return false;
            if (element.Category != _category ||
                !string.Equals(element.FamilyId, _familyId, StringComparison.Ordinal) ||
                !string.Equals(element.FloorId, _floorId, StringComparison.Ordinal) ||
                !string.Equals(element.ZoneId, _zoneId, StringComparison.Ordinal) ||
                !string.Equals(element.DrawingFingerprint, _drawingFingerprint, StringComparison.Ordinal) ||
                element.Dirty != _dirty || element.UpdatedUtc != _updatedUtc)
                return false;
            if (!SameSequence(element.SourceHandles, _sourceHandles) ||
                !SameSequence(element.DependsOn, _dependsOn))
                return false;
            if (!SameMap(element.Properties, _properties)) return false;
            if (!SameMap(element.Quantities, _quantities)) return false;
            return true;
        }

        public void Restore(ProjectState project)
        {
            var element = ResolveExact(project);
            element.Category = _category;
            element.FamilyId = _familyId;
            element.FloorId = _floorId;
            element.ZoneId = _zoneId;
            element.DrawingFingerprint = _drawingFingerprint;

            element.ClearSourceHandlesPersistence();
            foreach (var value in _sourceHandles) element.AddSourceHandlePersistenceValue(value);
            element.ClearDependenciesPersistence();
            foreach (var value in _dependsOn) element.AddDependencyPersistenceValue(value);

            var properties = element.Properties as ProjectElementPropertyDictionary
                ?? throw new InvalidOperationException("Project element does not expose the canonical property store.");
            foreach (var key in properties.Keys.ToList()) properties.RemovePersistenceValue(key);
            foreach (var pair in _properties) properties.SetPersistenceValue(pair.Key, pair.Value);

            element.ClearQuantities();
            var quantities = element.Quantities as ProjectElementQuantityDictionary
                ?? throw new InvalidOperationException("Project element does not expose the canonical quantity store.");
            foreach (var pair in _quantities) quantities.SetPersistenceValue(pair.Key, pair.Value);

            element.RestorePersistenceState(_dirty, _updatedUtc);
        }

        private ProjectElement ResolveExact(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(project, _projectOwner) ||
                !string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Cannot restore an element snapshot across project generations.");
            var element = project.FindElement(_elementId)
                ?? throw new InvalidOperationException("Cannot restore missing project element: " + _elementId + ".");
            if (!ReferenceEquals(element, _elementOwner))
                throw new InvalidOperationException("Cannot restore an element snapshot across element generations.");
            return element;
        }

        private bool TryResolve(ProjectState project, out ProjectElement element)
        {
            element = null!;
            if (project == null || !ReferenceEquals(project, _projectOwner) ||
                !string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal))
                return false;
            var current = project.FindElement(_elementId);
            if (current == null || !ReferenceEquals(current, _elementOwner)) return false;
            element = current;
            return true;
        }

        private static bool SameSequence(IList<string> current, IReadOnlyList<string> expected)
        {
            if (current.Count != expected.Count) return false;
            for (var i = 0; i < expected.Count; i++)
                if (!string.Equals(current[i], expected[i], StringComparison.Ordinal)) return false;
            return true;
        }

        private static bool SameMap<TValue>(
            IDictionary<string, TValue> current,
            IReadOnlyDictionary<string, TValue> expected)
        {
            if (current.Count != expected.Count) return false;
            foreach (var pair in expected)
            {
                if (!current.TryGetValue(pair.Key, out var value) ||
                    !EqualityComparer<TValue>.Default.Equals(value, pair.Value))
                    return false;
            }
            return true;
        }
    }
}
