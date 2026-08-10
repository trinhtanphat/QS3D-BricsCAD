using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedRebarOwnershipHealthService
    {
        private static readonly string[] Keys =
        {
            "GeneratedRebarHandles",
            "GeneratedShapeRebarHandles",
            "GeneratedTieRebarHandles"
        };

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var key in Keys)
                {
                    if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        var token = element.Id + "/" + key;
                        if (owners.TryGetValue(handle, out var previous) && !string.Equals(previous, token, StringComparison.OrdinalIgnoreCase))
                        {
                            issues.Add(new ModelHealthIssue(
                                "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT",
                                HealthSeverity.Error,
                                "Generated rebar handle " + handle + " được khai báo bởi cả " + previous + " và " + token + ".",
                                element.Id));
                        }
                        else owners[handle] = token;
                    }
                }
            }
            return issues;
        }
    }
}
