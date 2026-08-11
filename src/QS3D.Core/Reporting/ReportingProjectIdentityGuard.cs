using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    internal static class ReportingProjectIdentityGuard
    {
        internal static void RequireUniqueElementIds(ProjectState project, string reportName)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(reportName)) throw new ArgumentException("Report name is required.", nameof(reportName));

            RequireUniqueIds(project.Elements, x => x.Id, "element", reportName);
            RequireUniqueIds(project.Floors, x => x.Id, "floor", reportName);
            RequireUniqueIds(project.Zones, x => x.Id, "zone", reportName);
            RequireUniqueIds(project.Families, x => x.Id, "family", reportName);
        }

        private static void RequireUniqueIds<T>(IEnumerable<T> items, Func<T, string> idSelector, string identityName, string reportName) where T : class
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var item in items)
            {
                if (item == null)
                    throw new InvalidOperationException(reportName + " cannot be built because project " + identityName + " index " + index + " is null.");

                var rawId = idSelector(item);
                if (string.IsNullOrWhiteSpace(rawId))
                    throw new InvalidOperationException(reportName + " cannot be built with a blank project " + identityName + " id.");

                var id = rawId.Trim();
                if (!seenIds.Add(id))
                    throw new InvalidOperationException(reportName + " cannot be built because project " + identityName + " id '" + id + "' is duplicated.");
                index++;
            }
        }
    }
}
