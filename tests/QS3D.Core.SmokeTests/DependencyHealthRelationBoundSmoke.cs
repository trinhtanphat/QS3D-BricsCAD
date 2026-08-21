using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyHealthRelationBoundSmoke
    {
        private const int MaxRelations = 10000;

        internal static void Run()
        {
            RejectsOversizedRelationList();
            AcceptsExactRelationBoundary();
            PreservesExistingDependencyDiagnostics();
        }

        private static void RejectsOversizedRelationList()
        {
            var project = ProjectWithSourceAndTarget(out var source);
            for (var index = 0; index <= MaxRelations; index++)
                source.DependsOn.Add("TARGET");

            try
            {
                new DependencyHealthService().Inspect(project);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(MaxRelations.ToString(), StringComparison.Ordinal) < 0 ||
                    ex.Message.IndexOf("SOURCE", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    throw new InvalidOperationException(
                        "Oversized dependency relation rejection must identify the relation ceiling and owning element.",
                        ex);
                }
                return;
            }

            throw new InvalidOperationException(
                "Dependency health must reject a per-element dependency list above 10,000 relations before materialization.");
        }

        private static void AcceptsExactRelationBoundary()
        {
            var project = ProjectWithSourceAndTarget(out var source);
            for (var index = 0; index < MaxRelations; index++)
                source.DependsOn.Add("TARGET");

            var issues = new DependencyHealthService().Inspect(project);
            var duplicateIssues = issues
                .Where(x => string.Equals(x.Code, "DEPENDENCY_TARGET_DUPLICATE", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (duplicateIssues.Count != 1)
                throw new InvalidOperationException(
                    "Exactly 10,000 dependency relations must remain accepted and preserve deterministic duplicate diagnostics.");
        }

        private static void PreservesExistingDependencyDiagnostics()
        {
            var project = ProjectWithSourceAndTarget(out var source);
            source.DependsOn.Add("TARGET");
            source.DependsOn.Add("TARGET");
            source.DependsOn.Add(string.Empty);
            source.DependsOn.Add(" MISSING ");
            source.DependsOn.Add("MISSING");
            source.DependsOn.Add("SOURCE");
            source.DependsOn.Add("BAD\u0001TOKEN");

            var issues = new DependencyHealthService().Inspect(project);
            RequireCode(issues, "DEPENDENCY_TARGET_DUPLICATE");
            RequireCode(issues, "DEPENDENCY_TARGET_BLANK");
            RequireCode(issues, "DEPENDENCY_TARGET_NON_CANONICAL");
            RequireCode(issues, "DEPENDENCY_TARGET_MISSING");
            RequireCode(issues, "DEPENDENCY_SELF_REFERENCE");
            RequireCode(issues, "DEPENDENCY_TARGET_CONTROL_CHARACTER");
        }

        private static ProjectState ProjectWithSourceAndTarget(out ProjectElement source)
        {
            var project = new ProjectState("P-DEPENDENCY-RELATION-BOUND", "Dependency relation bound");
            source = Element("SOURCE");
            project.Elements.Add(source);
            project.Elements.Add(Element("TARGET"));
            return project;
        }

        private static ProjectElement Element(string id)
        {
            return new ProjectElement(
                id,
                ElementCategory.ArchitecturalWall,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        private static void RequireCode(System.Collections.Generic.IEnumerable<ModelHealthIssue> issues, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Dependency health regression lost expected diagnostic: " + code + ".");
        }
    }
}
