using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyDuplicateElementIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var duplicateProject = new ProjectState("P-DEP-DUP-ID", "Dependency duplicate identity");
            duplicateProject.Elements.Add(new ProjectElement("E-DUP", ElementCategory.Beam));
            duplicateProject.Elements.Add(new ProjectElement("E-DUP", ElementCategory.Column));

            var issues = new DependencyHealthService().Inspect(duplicateProject);
            var duplicateIssues = issues
                .Where(x => string.Equals(x.Code, "DEPENDENCY_ELEMENT_ID_DUPLICATE", StringComparison.Ordinal))
                .ToList();

            if (duplicateIssues.Count != 1)
                throw new InvalidOperationException("Duplicate dependency graph identity must produce exactly one deterministic issue per duplicated semantic ID.");
            if (issues.Count != 1)
                throw new InvalidOperationException("Duplicate semantic IDs without relations should not require an incoming dependency to become fail-visible.");

            var issue = duplicateIssues[0];
            if (issue.Severity != HealthSeverity.Error || !string.Equals(issue.ElementId, "E-DUP", StringComparison.Ordinal))
                throw new InvalidOperationException("Duplicate dependency graph identity issue has unexpected severity or element identity.");

            var uniqueProject = new ProjectState("P-DEP-UNIQUE-ID", "Dependency unique identity");
            uniqueProject.Elements.Add(new ProjectElement("E-ONE", ElementCategory.Beam));
            uniqueProject.Elements.Add(new ProjectElement("E-TWO", ElementCategory.Column));
            if (new DependencyHealthService().Inspect(uniqueProject).Any(x => string.Equals(x.Code, "DEPENDENCY_ELEMENT_ID_DUPLICATE", StringComparison.Ordinal)))
                throw new InvalidOperationException("Unique semantic element IDs must not produce duplicate dependency identity issues.");
        }
    }
}
