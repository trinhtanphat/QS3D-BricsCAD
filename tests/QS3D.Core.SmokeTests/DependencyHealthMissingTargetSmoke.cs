using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyHealthMissingTargetSmoke
    {
        internal static void Run()
        {
            var project = new ProjectState("P-DEPENDENCY-HEALTH", "Dependency health");
            var existing = Element("EXISTING");
            var source = Element("SOURCE", " missing-target ", "MISSING-TARGET", " existing ");
            project.Elements.Add(source);
            project.Elements.Add(existing);

            var issues = new DependencyHealthService().Inspect(project);
            var missing = issues
                .Where(x => string.Equals(x.Code, "DEPENDENCY_TARGET_MISSING", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (missing.Count != 1)
                throw new InvalidOperationException("Equivalent missing dependency tokens must produce exactly one health issue per referencing element.");

            var issue = missing[0];
            if (issue.Severity != HealthSeverity.Error)
                throw new InvalidOperationException("A missing semantic dependency must be a Health error.");
            if (!string.Equals(issue.ElementId, "SOURCE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Missing dependency health issue must identify the referencing element.");
            if (issue.Message.IndexOf("missing-target", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Missing dependency health issue must identify the missing semantic target.");
            if (issues.Any(x => string.Equals(x.Code, "DEPENDENCY_TARGET_MISSING", StringComparison.OrdinalIgnoreCase) &&
                                x.Message.IndexOf("EXISTING", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("An existing semantic dependency must not be reported as missing.");
        }

        private static ProjectElement Element(string id, params string[] dependencies)
        {
            var element = new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            foreach (var dependency in dependencies) AddPersistedDependency(element, dependency);
            return element;
        }

        private static void AddPersistedDependency(ProjectElement element, string dependency)
        {
            var valuesField = element.DependsOn.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to seed malformed persisted dependency health state.");
            var values = valuesField.GetValue(element.DependsOn) as List<string>
                ?? throw new InvalidOperationException("Unexpected ProjectElement.DependsOn backing collection.");
            values.Add(dependency);
        }
    }
}