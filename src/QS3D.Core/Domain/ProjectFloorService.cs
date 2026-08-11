using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Services;

namespace QS3D.Core.Domain
{
    public static class ProjectFloorService
    {
        public const string BottomLevelIdKey = "BottomLevelId";
        public const string BottomLevelOffsetKey = "BottomLevelOffsetM";
        public const string TopLevelIdKey = "TopLevelId";
        public const string TopLevelOffsetKey = "TopLevelOffsetM";

        private const int MaxFloors = 2000;
        private const int MaxNameLength = 120;

        public static FloorDefinition Create(ProjectState project, string id, string name, double elevationM)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedId = Required(id, nameof(id), 64);
            var normalizedName = Required(name, nameof(name), MaxNameLength);
            Finite(elevationM, nameof(elevationM));
            if (project.Floors.Count >= MaxFloors) throw new InvalidOperationException("Project supports at most " + MaxFloors + " floors.");
            if (project.Floors.Any(x => string.Equals(x.Id, normalizedId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Floor id already exists: " + normalizedId);
            EnsureUniqueName(project, normalizedName, string.Empty);
            var floor = new FloorDefinition(normalizedId, normalizedName, elevationM);
            project.Touch();
            project.Floors.Add(floor);
            if (string.IsNullOrWhiteSpace(project.ActiveFloorId)) project.ActiveFloorId = floor.Id;
            return floor;
        }

        public static FloorDefinition Update(ProjectState project, string id, string name, double elevationM)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = FindRequired(project, id);
            var normalizedName = Required(name, nameof(name), MaxNameLength);
            Finite(elevationM, nameof(elevationM));
            EnsureUniqueName(project, normalizedName, floor.Id);

            var nameChanged = !string.Equals(floor.Name, normalizedName, StringComparison.Ordinal);
            var elevationChanged = !NearlyEqual(floor.ElevationM, elevationM);
            if (!nameChanged && !elevationChanged) return floor;

            var projectElements = ResolveProjectElements(project);
            var referencedElements = projectElements
                .Where(x => ReferencesFloor(x, floor.Id))
                .ToList();
            var referencedIds = new HashSet<string>(referencedElements.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var dependencyGraph = new DependencyGraph();
            dependencyGraph.Rebuild(projectElements);
            var dependentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in referencedElements)
                dependentIds.UnionWith(dependencyGraph.GetDependentsTransitive(element.Id));
            dependentIds.ExceptWith(referencedIds);
            var dependentElements = projectElements.Where(x => dependentIds.Contains(x.Id)).ToList();

            project.Touch();
            floor.Name = normalizedName;
            floor.ElevationM = elevationM;
            foreach (var element in referencedElements)
            {
                var flags = ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
                if (elevationChanged) flags |= ElementDirtyFlags.Geometry;
                element.MarkDirty(flags);
            }
            foreach (var element in dependentElements)
                element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            return floor;
        }

        public static void SetActive(ProjectState project, string floorId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = FindRequired(project, floorId);
            if (string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase)) return;
            project.Touch();
            project.ActiveFloorId = floor.Id;
        }

        public static int Assign(ProjectState project, string floorId, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var floor = FindRequired(project, floorId);
            var targets = ResolveOwnedElements(project, elements);
            var changed = targets.Where(x => !string.Equals(x.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase)).ToList();
            if (changed.Count == 0) return 0;

            project.Touch();
            foreach (var element in changed)
            {
                element.FloorId = floor.Id;
                element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            }
            return changed.Count;
        }

        public static int AssignBottomLevel(ProjectState project, string floorId, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var floor = FindRequired(project, floorId);
            var targets = ResolveOwnedElements(project, elements);

            foreach (var element in targets)
            {
                var bottomOffset = LevelOffset(element, BottomLevelOffsetKey);
                if (!element.Properties.TryGetValue(TopLevelIdKey, out var topId) || string.IsNullOrWhiteSpace(topId)) continue;
                var top = FindRequired(project, topId);
                var topOffset = LevelOffset(element, TopLevelOffsetKey);
                if (top.ElevationM + topOffset <= floor.ElevationM + bottomOffset)
                    throw new InvalidOperationException("Cannot assign bottom level '" + floor.Name + "' because top level is not above it for element " + element.Id + ".");
            }

            var changed = targets.Where(element =>
            {
                var current = Property(element, BottomLevelIdKey);
                var addedOffset = !element.Properties.ContainsKey(BottomLevelOffsetKey);
                return !string.Equals(current, floor.Id, StringComparison.OrdinalIgnoreCase) || addedOffset;
            }).ToList();
            if (changed.Count == 0) return 0;

            project.Touch();
            foreach (var element in changed)
            {
                element.Properties[BottomLevelIdKey] = floor.Id;
                if (!element.Properties.ContainsKey(BottomLevelOffsetKey)) element.Properties[BottomLevelOffsetKey] = "0";
                element.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            }
            return changed.Count;
        }

        public static int AssignTopLevel(ProjectState project, string floorId, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var top = FindRequired(project, floorId);
            var targets = ResolveOwnedElements(project, elements);

            foreach (var element in targets)
            {
                var bottomId = Property(element, BottomLevelIdKey);
                if (bottomId.Length == 0)
                    throw new InvalidOperationException("Assign Bottom Level before Top Level for element " + element.Id + ".");
                var bottom = FindRequired(project, bottomId);
                var bottomOffset = LevelOffset(element, BottomLevelOffsetKey);
                var topOffset = LevelOffset(element, TopLevelOffsetKey);
                if (top.ElevationM + topOffset <= bottom.ElevationM + bottomOffset)
                    throw new InvalidOperationException("Top level '" + top.Name + "' must be above bottom level for element " + element.Id + ".");
            }

            var changed = targets.Where(element =>
            {
                var current = Property(element, TopLevelIdKey);
                var addedOffset = !element.Properties.ContainsKey(TopLevelOffsetKey);
                return !string.Equals(current, top.Id, StringComparison.OrdinalIgnoreCase) || addedOffset;
            }).ToList();
            if (changed.Count == 0) return 0;

            project.Touch();
            foreach (var element in changed)
            {
                element.Properties[TopLevelIdKey] = top.Id;
                if (!element.Properties.ContainsKey(TopLevelOffsetKey)) element.Properties[TopLevelOffsetKey] = "0";
                element.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            }
            return changed.Count;
        }

        public static int ClearVerticalLevels(ProjectState project, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var targets = ResolveOwnedElements(project, elements);
            var changed = targets.Where(element =>
                element.Properties.ContainsKey(BottomLevelIdKey) ||
                element.Properties.ContainsKey(BottomLevelOffsetKey) ||
                element.Properties.ContainsKey(TopLevelIdKey) ||
                element.Properties.ContainsKey(TopLevelOffsetKey)).ToList();
            if (changed.Count == 0) return 0;

            project.Touch();
            foreach (var element in changed)
            {
                element.Properties.Remove(BottomLevelIdKey);
                element.Properties.Remove(BottomLevelOffsetKey);
                element.Properties.Remove(TopLevelIdKey);
                element.Properties.Remove(TopLevelOffsetKey);
                element.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            }
            return changed.Count;
        }

        public static bool Delete(ProjectState project, string floorId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = FindRequired(project, floorId);
            if (string.Equals((project.ActiveFloorId ?? string.Empty).Trim(), floor.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot delete the active floor. Activate another floor first.");
            var references = ResolveProjectElements(project).Count(x => ReferencesFloor(x, floor.Id));
            if (references > 0)
                throw new InvalidOperationException("Floor '" + floor.Name + "' is referenced by " + references + " semantic element(s). Reassign or clear Floor/Level references before deletion.");
            project.Touch();
            return project.Floors.Remove(floor);
        }

        public static int ReferenceCount(ProjectState project, string floorId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = FindRequired(project, floorId);
            return ResolveProjectElements(project).Count(x => ReferencesFloor(x, floor.Id));
        }

        public static bool ReferencesFloor(ProjectElement element, string floorId)
        {
            if (element == null || string.IsNullOrWhiteSpace(floorId)) return false;
            var normalizedFloorId = floorId.Trim();
            return string.Equals((element.FloorId ?? string.Empty).Trim(), normalizedFloorId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Property(element, BottomLevelIdKey), normalizedFloorId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Property(element, TopLevelIdKey), normalizedFloorId, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ReferencesVerticalLevel(ProjectElement element, string floorId)
        {
            if (element == null || string.IsNullOrWhiteSpace(floorId)) return false;
            var normalizedFloorId = floorId.Trim();
            return string.Equals(Property(element, BottomLevelIdKey), normalizedFloorId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Property(element, TopLevelIdKey), normalizedFloorId, StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<ProjectElement> ResolveOwnedElements(ProjectState project, IEnumerable<ProjectElement> elements)
        {
            var projectElements = ResolveProjectElements(project)
                .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            var unique = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (element == null) continue;
                if (!projectElements.TryGetValue(element.Id, out var owned) || !ReferenceEquals(owned, element))
                    throw new InvalidOperationException("Element does not belong to the project instance: " + element.Id);
                unique[element.Id] = owned;
            }
            return unique.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static IReadOnlyList<ProjectElement> ResolveProjectElements(ProjectState project)
        {
            var resolved = new List<ProjectElement>(project.Elements.Count);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("Project contains an element with a blank semantic id.");
                if (!ids.Add(elementId))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + elementId);
                resolved.Add(element);
            }
            return resolved.AsReadOnly();
        }

        private static double LevelOffset(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return 0d;
            if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(element.Id + "/" + key + " must be a finite invariant number.");
            return value;
        }

        private static string Property(ProjectElement element, string key)
        {
            return element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;
        }

        private static FloorDefinition FindRequired(ProjectState project, string id)
        {
            var normalized = Required(id, nameof(id), 64);
            return project.FindFloor(normalized) ?? throw new InvalidOperationException("Floor not found: " + normalized);
        }

        private static void EnsureUniqueName(ProjectState project, string name, string exceptId)
        {
            if (project.Floors.Any(x => !string.Equals(x.Id, exceptId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Another floor already uses the name '" + name + "'.");
        }

        private static string Required(string value, string parameterName, int maxLength)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0 || text.Length > maxLength)
                throw new ArgumentException(parameterName + " must contain 1.." + maxLength + " characters.", parameterName);
            return text;
        }

        private static double Finite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            return value;
        }

        private static bool NearlyEqual(double left, double right)
        {
            var scale = Math.Max(1d, Math.Max(Math.Abs(left), Math.Abs(right)));
            return Math.Abs(left - right) <= scale * 1e-12d;
        }
    }
}
