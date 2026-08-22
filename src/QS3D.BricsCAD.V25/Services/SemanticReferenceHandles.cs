using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class SemanticReferenceHandles
    {
        public static IReadOnlyList<string> Get(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in element.SourceHandles) Add(handle, result, seen);

            if (result.Count == 0 && element.Properties.TryGetValue(AutoRoomLifecycle.BoundarySourceHandlesKey, out var boundaryHandles))
                foreach (var handle in (boundaryHandles ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    Add(handle, result, seen);

            if (result.Count == 0 && element.Properties.TryGetValue("GeneratedSolidHandle", out var generated)) Add(generated, result, seen);
            return result.AsReadOnly();
        }

        public static bool Intersects(ProjectElement element, ISet<string> handles) => MatchesSelection(element, handles);

        public static bool MatchesSelection(ProjectElement element, ISet<string> handles)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            var owned = element.SourceHandles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (owned.Count > 0) return owned.Any(handles.Contains);

            if (element.Properties.TryGetValue(AutoRoomLifecycle.BoundarySourceHandlesKey, out var rawBoundary))
            {
                var boundary = (rawBoundary ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (boundary.Count > 0) return boundary.All(handles.Contains);
            }

            return Get(element).Any(handles.Contains);
        }

        private static void Add(string? value, ICollection<string> target, ISet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var handle = value!.Trim();
            if (seen.Add(handle)) target.Add(handle);
        }
    }
}
