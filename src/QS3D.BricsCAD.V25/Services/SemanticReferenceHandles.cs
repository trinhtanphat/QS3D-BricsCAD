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

        public static IReadOnlyList<string> GetSelectionAliases(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in element.SourceHandles) Add(handle, result, seen);

            // Auto Room boundary provenance only represents the semantic source when the Room
            // does not own an explicit source handle. MatchesSelection still requires the whole
            // boundary set, while this method returns every allowed handle for final validation.
            if (result.Count == 0 && element.Properties.TryGetValue(AutoRoomLifecycle.BoundarySourceHandlesKey, out var boundaryHandles))
                foreach (var handle in (boundaryHandles ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    Add(handle, result, seen);

            // Generated host aliases are valid rebuild entry points even when stable sources exist.
            // Deliberately exclude generated rebar/mesh/detail handles: QS3DBUILD3D owns host solids.
            if (element.Properties.TryGetValue("GeneratedSolidHandle", out var generated)) Add(generated, result, seen);
            if (element.Properties.TryGetValue("PhysicalOpeningCutSolidHandle", out var cutSolid)) Add(cutSolid, result, seen);
            return result.AsReadOnly();
        }

        public static bool Intersects(ProjectElement element, ISet<string> handles) => MatchesSelection(element, handles);

        public static bool MatchesSelection(ProjectElement element, ISet<string> handles)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (handles == null) throw new ArgumentNullException(nameof(handles));

            var owned = element.SourceHandles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (owned.Any(handles.Contains)) return true;

            // Auto Room boundary provenance is intentionally all-or-nothing and only acts as the
            // semantic reference when the Room has no explicit source handle of its own.
            if (owned.Count == 0 && element.Properties.TryGetValue(AutoRoomLifecycle.BoundarySourceHandlesKey, out var rawBoundary))
            {
                var boundary = (rawBoundary ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (boundary.Count > 0 && boundary.All(handles.Contains)) return true;
            }

            // BLT-style rebuild must also work when the user selects the generated host solid.
            // Do not broaden this to generated rebar/mesh/detail families: QS3DBUILD3D rebuilds the
            // semantic host and should only resolve its host-solid aliases back to stable sources.
            return MatchesPropertyHandle(element, "GeneratedSolidHandle", handles) ||
                   MatchesPropertyHandle(element, "PhysicalOpeningCutSolidHandle", handles);
        }

        private static bool MatchesPropertyHandle(ProjectElement element, string key, ISet<string> handles)
        {
            return element.Properties.TryGetValue(key, out var raw) &&
                   !string.IsNullOrWhiteSpace(raw) &&
                   handles.Contains(raw.Trim());
        }

        private static void Add(string? value, ICollection<string> target, ISet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var handle = value!.Trim();
            if (seen.Add(handle)) target.Add(handle);
        }
    }
}
