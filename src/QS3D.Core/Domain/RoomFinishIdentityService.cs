using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Domain
{
    public static class RoomFinishIdentityService
    {
        public static string CanonicalId(string roomId, ElementCategory category)
        {
            var rawRoomId = roomId ?? string.Empty;
            var normalizedRoomId = rawRoomId.Trim();
            if (normalizedRoomId.Length == 0) throw new ArgumentException("Room id is required.", nameof(roomId));
            if (!string.Equals(rawRoomId, normalizedRoomId, StringComparison.Ordinal))
                throw new ArgumentException("Room id must be canonical without surrounding whitespace.", nameof(roomId));
            if (rawRoomId.Any(char.IsControl))
                throw new ArgumentException("Room id cannot contain control characters.", nameof(roomId));
            EnsureFinishCategory(category);
            return rawRoomId + "-" + category;
        }

        public static ProjectElement? FindExisting(ProjectState project, ProjectElement room, ElementCategory category)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (room.Category != ElementCategory.Room)
                throw new ArgumentException("Source element must be a Room.", nameof(room));
            EnsureFinishCategory(category);

            var elements = ResolveProjectElements(project);
            if (!elements.TryGetValue(room.Id, out var ownedRoom))
                throw new InvalidOperationException("Room does not belong to the project: " + room.Id);
            if (!ReferenceEquals(ownedRoom, room))
                throw new InvalidOperationException("Room instance does not belong to the project: " + room.Id);
            return FindExistingCore(project, elements, ownedRoom, category);
        }

        public static void ValidateProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var elements = ResolveProjectElements(project);
            foreach (var finish in elements.Values
                .Where(x => AutoRoomLifecycle.IsRoomFinishCategory(x.Category))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var roomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, finish);
                if (roomId.Length == 0) continue;
                if (!elements.TryGetValue(roomId, out var room) || room.Category != ElementCategory.Room) continue;
                FindExistingCore(project, elements, room, finish.Category);
            }
        }

        private static ProjectElement? FindExistingCore(
            ProjectState project,
            IReadOnlyDictionary<string, ProjectElement> elements,
            ProjectElement room,
            ElementCategory category)
        {
            var canonicalId = CanonicalId(room.Id, category);
            elements.TryGetValue(canonicalId, out var canonical);
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

            foreach (var candidate in elements.Values
                .Where(x => x.Category == category && !string.Equals(x.Id, canonicalId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var linkedRoomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, candidate);
                if (string.Equals(linkedRoomId, room.Id, StringComparison.OrdinalIgnoreCase)) matches.Add(candidate);
            }

            if (matches.Count > 1)
                throw new InvalidOperationException("Multiple " + category + " finishes reference Room " + room.Id + ": " + string.Join(", ", matches.Select(x => x.Id)));
            return matches.Count == 1 ? matches[0] : null;
        }

        private static IReadOnlyDictionary<string, ProjectElement> ResolveProjectElements(ProjectState project)
        {
            var elements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                var elementId = element.Id ?? string.Empty;
                if (string.IsNullOrWhiteSpace(elementId))
                    throw new InvalidOperationException("Project contains an element with a blank semantic id.");
                if (!string.Equals(elementId, elementId.Trim(), StringComparison.Ordinal) || elementId.Any(char.IsControl))
                    throw new InvalidOperationException("Project contains an element with a non-canonical semantic id.");
                if (elements.ContainsKey(elementId))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + elementId);
                elements.Add(elementId, element);
            }
            return elements;
        }

        private static void EnsureFinishCategory(ElementCategory category)
        {
            if (!AutoRoomLifecycle.IsRoomFinishCategory(category))
                throw new ArgumentOutOfRangeException(nameof(category), category, "Category is not an HT_Phòng finish category.");
        }
    }
}
