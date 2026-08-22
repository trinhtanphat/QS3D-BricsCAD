using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Domain
{
    public static class ProjectFloorService
    {
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
            foreach (var element in project.Elements.Where(x => string.Equals(x.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase)))
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
            var changed = 0;
            foreach (var element in unique.Values)
            {
                if (string.Equals(element.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase)) continue;
                element.FloorId = floor.Id;
                element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
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
            var references = project.Elements.Count(x => string.Equals(x.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase));
            if (references > 0)
                throw new InvalidOperationException("Floor '" + floor.Name + "' is referenced by " + references + " semantic element(s). Reassign them before deletion.");
            var removed = project.Floors.Remove(floor);
            if (removed) project.Touch();
            return removed;
        }

        public static int ReferenceCount(ProjectState project, string floorId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = FindRequired(project, floorId);
            return project.Elements.Count(x => string.Equals(x.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase));
        }

        private static FloorDefinition FindRequired(ProjectState project, string id)
        {
            var normalized = Required(id, nameof(id), 64);
            return project.Floors.FirstOrDefault(x => string.Equals(x.Id, normalized, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Floor not found: " + normalized);
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
