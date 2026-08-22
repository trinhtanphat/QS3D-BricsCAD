using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
            project.Floors.Add(floor);
            if (string.IsNullOrWhiteSpace(project.ActiveFloorId)) project.ActiveFloorId = floor.Id;
            project.Touch();
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
            floor.Name = normalizedName;
            floor.ElevationM = elevationM;
            foreach (var element in project.Elements.Where(x => ReferencesFloor(x, floor.Id)))
            {
                var flags = ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
                if (elevationChanged) flags |= ElementDirtyFlags.Geometry;
                element.MarkDirty(flags);
            }
            project.Touch();
            return floor;
        }

        public static void SetActive(ProjectState project, string floorId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = FindRequired(project, floorId);
            if (string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase)) return;
            project.ActiveFloorId = floor.Id;
            project.Touch();
        }

        public static int Assign(ProjectState project, string floorId, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var floor = FindRequired(project, floorId);
            var targets = ResolveOwnedElements(project, elements);
            var changed = 0;
            foreach (var element in targets)
            {
                if (string.Equals(element.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase)) continue;
                element.FloorId = floor.Id;
                element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                changed++;
            }
            if (changed > 0) project.Touch();
            return changed;
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

            var changed = 0;
            foreach (var element in targets)
            {
                var current = Property(element, BottomLevelIdKey);
                var addedOffset = !element.Properties.ContainsKey(BottomLevelOffsetKey);
                if (string.Equals(current, floor.Id, StringComparison.OrdinalIgnoreCase) && !addedOffset) continue;
                element.Properties[BottomLevelIdKey] = floor.Id;
                if (addedOffset) element.Properties[BottomLevelOffsetKey] = "0";
                element.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                changed++;
            }
            if (changed > 0) project.Touch();
            return changed;
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

            var changed = 0;
            foreach (var element in targets)
            {
                var current = Property(element, TopLevelIdKey);
                var addedOffset = !element.Properties.ContainsKey(TopLevelOffsetKey);
                if (string.Equals(current, top.Id, StringComparison.OrdinalIgnoreCase) && !addedOffset) continue;
                element.Properties[TopLevelIdKey] = top.Id;
                if (addedOffset) element.Properties[TopLevelOffsetKey] = "0";
                element.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                changed++;
            }
            if (changed > 0) project.Touch();
            return changed;
        }

        public static int ClearVerticalLevels(ProjectState project, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var targets = ResolveOwnedElements(project, elements);
            var changed = 0;
            foreach (var element in targets)
            {
                var removed = element.Properties.Remove(BottomLevelIdKey);
                removed |= element.Properties.Remove(BottomLevelOffsetKey);
                removed |= element.Properties.Remove(TopLevelIdKey);
                removed |= element.Properties.Remove(TopLevelOffsetKey);
                if (!removed) continue;
                element.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                changed++;
            }
            if (changed > 0) project.Touch();
            return changed;
        }

        public static bool Delete(ProjectState project, string floorId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = FindRequired(project, floorId);
            if (string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot delete the active floor. Activate another floor first.");
            var references = project.Elements.Count(x => ReferencesFloor(x, floor.Id));
            if (references > 0)
                throw new InvalidOperationException("Floor '" + floor.Name + "' is referenced by " + references + " semantic element(s). Reassign or clear Floor/Level references before deletion.");
            var removed = project.Floors.Remove(floor);
            if (removed) project.Touch();
            return removed;
        }

        public static int ReferenceCount(ProjectState project, string floorId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = FindRequired(project, floorId);
            return project.Elements.Count(x => ReferencesFloor(x, floor.Id));
        }

        public static bool ReferencesFloor(ProjectElement element, string floorId)
        {
            if (element == null || string.IsNullOrWhiteSpace(floorId)) return false;
            return string.Equals(element.FloorId, floorId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Property(element, BottomLevelIdKey), floorId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Property(element, TopLevelIdKey), floorId, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ReferencesVerticalLevel(ProjectElement element, string floorId)
        {
            if (element == null || string.IsNullOrWhiteSpace(floorId)) return false;
            return string.Equals(Property(element, BottomLevelIdKey), floorId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Property(element, TopLevelIdKey), floorId, StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<ProjectElement> ResolveOwnedElements(ProjectState project, IEnumerable<ProjectElement> elements)
        {
            var projectElements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var projectElement in project.Elements)
            {
                if (projectElement == null) continue;
                if (projectElements.ContainsKey(projectElement.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + projectElement.Id);
                projectElements[projectElement.Id] = projectElement;
            }

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
