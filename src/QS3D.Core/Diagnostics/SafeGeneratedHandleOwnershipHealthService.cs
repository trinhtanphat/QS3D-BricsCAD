using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class SafeGeneratedHandleOwnershipHealthService
    {
        private const string InvalidProjectIssueCode = "GENERATED_HANDLE_OWNERSHIP_INVALID_PROJECT";

        private sealed class Claim
        {
            public string ElementId { get; set; } = string.Empty;
            public string Slot { get; set; } = string.Empty;
            public string DisplayToken => ElementId + "/" + Slot;
        }

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            try
            {
                GeneratedHandleOwnershipIndex.Build(project);
            }
            catch (InvalidOperationException)
            {
                return new[]
                {
                    new ModelHealthIssue(
                        InvalidProjectIssueCode,
                        HealthSeverity.Error,
                        "Generated handle ownership cannot be inspected safely because the semantic project is invalid.")
                };
            }

            var claims = new Dictionary<string, List<Claim>>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                AddClaims(claims, element, "SourceHandles", element.SourceHandles);
                foreach (var group in GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element).GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase))
                    AddClaims(claims, element, group.Key, group.Select(x => x.Key));
            }

            var issues = new List<ModelHealthIssue>();
            foreach (var pair in claims.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var distinct = pair.Value
                    .OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Slot, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (distinct.Count <= 1) continue;

                foreach (var claim in distinct)
                {
                    var others = string.Join(", ", distinct
                        .Where(x => !ReferenceEquals(x, claim))
                        .Select(x => x.DisplayToken));
                    issues.Add(new ModelHealthIssue(
                        "GENERATED_HANDLE_OWNERSHIP_CONFLICT",
                        HealthSeverity.Error,
                        "CAD handle " + pair.Key + " đang được nhiều semantic/generated owner slot cùng claim. Slot hiện tại: " + claim.Slot + "; xung đột: " + others + ".",
                        claim.ElementId));
                }
            }
            return issues.AsReadOnly();
        }

        private static void AddClaims(Dictionary<string, List<Claim>> claims, ProjectElement element, string slot, IEnumerable<string> handles)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in handles)
            {
                var handle = (value ?? string.Empty).Trim();
                if (handle.Length == 0 || !seen.Add(handle)) continue;
                if (!claims.TryGetValue(handle, out var list))
                {
                    list = new List<Claim>();
                    claims[handle] = list;
                }
                if (list.Any(x =>
                    string.Equals(x.ElementId, element.Id, StringComparison.OrdinalIgnoreCase) &&
                    GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots(x.Slot, slot)))
                    continue;
                list.Add(new Claim { ElementId = element.Id, Slot = slot });
            }
        }
    }
}
