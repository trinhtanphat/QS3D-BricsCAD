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

            var seenElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (string.IsNullOrWhiteSpace(element.Id))
                    throw new InvalidOperationException(reportName + " cannot be built with a blank project element id.");

                var elementId = element.Id.Trim();
                if (!seenElementIds.Add(elementId))
                    throw new InvalidOperationException(reportName + " cannot be built because project element id '" + elementId + "' is duplicated.");
            }
        }
    }
}
