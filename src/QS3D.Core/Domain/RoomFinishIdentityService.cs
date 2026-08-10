using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Domain
{
    public static class RoomFinishIdentityService
    {
        public static string CanonicalId(string roomId, ElementCategory category)
        {
            var normalizedRoomId = (roomId ?? string.Empty).Trim();
            if (normalizedRoomId.Length == 0) throw new ArgumentException("Room id is required.", nameof(roomId));
            EnsureFinishCategory(category);
            return normalizedRoomId + "-" + category;
        }

        public static ProjectElement? FindExisting(ProjectState project, ProjectElement room, ElementCategory category)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (room.Category != ElementCategory.Room)
                throw new ArgumentException("Source element must be a Room.", nameof(room));
            EnsureFinishCategory(category);

            var canonicalId = CanonicalId(room.Id, category);
            var canonical = project.FindElement(canonicalId);
            if (canonical != null && canonical.Category != category)
                throw new InvalidOperationException("Room finish id collision with category " + canonical.Category + ": " + canonicalId);

            var matches = new List<ProjectElement>();
            if (canonical != null)
            {
                var linkedRoomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, canonical);
                if (linkedRoomId.Length > 0 && !string.Equals(linkedRoomId, room.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Canonical room finish " + canonical.Id + " references another Room: " + linkedRoomId + ".");
                matches.Add(canonical);
            }

            foreach (var candidate in project.Elements
                .Where(x => x.Category == category && !string.Equals(x.Id, canonicalId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var linkedRoomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, candidate);
                if (string.Equals(linkedRoomId, room.Id, StringComparison.OrdinalIgnoreCase)) matches.Add(candidate);
            }

            var distinct = matches
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (distinct.Count > 1)
                throw new InvalidOperationException("Multiple " + category + " finishes reference Room " + room.Id + ": " + string.Join(", ", distinct.Select(x => x.Id)));
            return distinct.Count == 1 ? distinct[0] : null;
        }

        public static void ValidateProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            foreach (var finish in project.Elements
                .Where(x => AutoRoomLifecycle.IsRoomFinishCategory(x.Category))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var roomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, finish);
                if (roomId.Length == 0) continue;
                var room = project.FindElement(roomId);
                if (room == null || room.Category != ElementCategory.Room) continue;
                FindExisting(project, room, finish.Category);
            }
        }

        private static void EnsureFinishCategory(ElementCategory category)
        {
            if (!AutoRoomLifecycle.IsRoomFinishCategory(category))
                throw new ArgumentOutOfRangeException(nameof(category), category, "Category is not an HT_Phòng finish category.");
        }
    }
}
