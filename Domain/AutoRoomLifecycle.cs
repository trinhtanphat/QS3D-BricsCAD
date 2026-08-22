using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Domain
{
    public static class AutoRoomLifecycle
    {
        public const string BoundaryModeKey = "BoundaryMode";
        public const string BoundaryModeAutoNetwork = "AutoNetwork";
        public const string BoundaryStateKey = "BoundaryState";
        public const string BoundaryStateActive = "Active";
        public const string BoundaryStateStale = "Stale";
        public const string BoundarySourceHandlesKey = "BoundarySourceHandles";
        public const string BoundarySourceSignatureKey = "BoundarySourceSignature";

        public static bool IsAutoRoom(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return element.Category == ElementCategory.Room &&
                   element.Properties.TryGetValue(BoundaryModeKey, out var mode) &&
                   string.Equals(mode, BoundaryModeAutoNetwork, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsStaleAutoRoom(ProjectElement element)
        {
            if (!IsAutoRoom(element)) return false;
            return element.Properties.TryGetValue(BoundaryStateKey, out var state) &&
                   string.Equals(state, BoundaryStateStale, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeSourceHandles(IEnumerable<string> handles)
        {
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            return string.Join(";", handles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        public static string SourceSignature(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Properties.TryGetValue(BoundarySourceSignatureKey, out var signature) && !string.IsNullOrWhiteSpace(signature))
                return NormalizeSourceHandles(signature.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            if (!element.Properties.TryGetValue(BoundarySourceHandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return NormalizeSourceHandles(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public static ProjectElement? FindBySourceSignature(ProjectState project, string signature, string floorId, string zoneId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = NormalizeSourceHandles((signature ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            if (normalized.Length == 0) return null;
            var matches = project.Elements
                .Where(IsAutoRoom)
                .Where(x => string.Equals(x.FloorId, floorId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.ZoneId, zoneId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(SourceSignature(x), normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (matches.Count > 1) throw new InvalidOperationException("Multiple auto rooms share the same boundary source signature: " + normalized);
            return matches.Count == 1 ? matches[0] : null;
        }

        public static IReadOnlyList<ProjectElement> MarkStaleForSelection(ProjectState project, ISet<string> activeRoomIds, ISet<string> selectedSourceHandles, string floorId, string zoneId, DateTime utcNow)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (activeRoomIds == null) throw new ArgumentNullException(nameof(activeRoomIds));
            if (selectedSourceHandles == null) throw new ArgumentNullException(nameof(selectedSourceHandles));
            var selected = new HashSet<string>(selectedSourceHandles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
            var stale = new List<ProjectElement>();
            foreach (var room in project.Elements.Where(IsAutoRoom).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (activeRoomIds.Contains(room.Id)) continue;
                if (!string.Equals(room.FloorId, floorId ?? string.Empty, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(room.ZoneId, zoneId ?? string.Empty, StringComparison.OrdinalIgnoreCase)) continue;
                var handles = SourceSignature(room).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (handles.Length == 0 || !handles.All(selected.Contains)) continue;
                room.Properties[BoundaryStateKey] = BoundaryStateStale;
                room.Properties["BoundaryStaleUtc"] = utcNow.ToUniversalTime().ToString("O");
                room.Properties["BoundaryStaleReason"] = "TopologyChanged";
                room.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
                stale.Add(room);
            }
            if (stale.Count > 0) project.Touch();
            return stale;
        }

        public static void MarkActive(ProjectElement room, string sourceSignature)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            room.Properties[BoundaryStateKey] = BoundaryStateActive;
            room.Properties[BoundarySourceSignatureKey] = NormalizeSourceHandles((sourceSignature ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            room.Properties.Remove("BoundaryStaleUtc");
            room.Properties.Remove("BoundaryStaleReason");
        }

        public static bool IsExcludedFromQuantity(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (IsStaleAutoRoom(element)) return true;
            foreach (var dependencyId in element.DependsOn.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var dependency = project.FindElement(dependencyId.Trim());
                if (dependency != null && IsStaleAutoRoom(dependency)) return true;
            }
            return false;
        }
    }
}
