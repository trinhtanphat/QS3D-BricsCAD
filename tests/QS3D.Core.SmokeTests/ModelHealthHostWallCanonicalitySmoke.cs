using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthHostWallCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHostAliasFailsVisible();
            CaseVariantHostAliasFailsVisible();
            CanonicalHostDoesNotEmitCanonicalityError();
            MissingHostKeepsInvalidDiagnostic();
            DuplicateHostKeepsAmbiguityDiagnostic();
            CanonicalWrongCategoryKeepsCategoryDiagnostic();
        }

        private static void PaddedHostAliasFailsVisible()
        {
            var setup = Create("PAD", "Wall-A", ElementCategory.ArchitecturalWall);
            setup.Opening.Properties["HostWallId"] = " Wall-A ";
            RequireIssue(setup.Project, setup.Opening.Id, "HOST_REFERENCE_NON_CANONICAL", HealthSeverity.Error);
        }

        private static void CaseVariantHostAliasFailsVisible()
        {
            var setup = Create("CASE", "Wall-A", ElementCategory.ArchitecturalWall);
            setup.Opening.Properties["HostWallId"] = "wall-a";
            RequireIssue(setup.Project, setup.Opening.Id, "HOST_REFERENCE_NON_CANONICAL", HealthSeverity.Error);
        }

        private static void CanonicalHostDoesNotEmitCanonicalityError()
        {
            var setup = Create("CANONICAL", "Wall-A", ElementCategory.ArchitecturalWall);
            setup.Opening.Properties["HostWallId"] = "Wall-A";
            var issues = new ModelHealthService().Inspect(setup.Project);
            if (issues.Any(x => string.Equals(x.Code, "HOST_REFERENCE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Exact canonical HostWallId must not produce a canonicality error.");
        }

        private static void MissingHostKeepsInvalidDiagnostic()
        {
            var setup = Create("MISSING", "Wall-A", ElementCategory.ArchitecturalWall);
            setup.Opening.Properties["HostWallId"] = "missing-wall";
            var issues = new ModelHealthService().Inspect(setup.Project);
            RequireIssue(issues, setup.Opening.Id, "INVALID_HOST", HealthSeverity.Error);
            EnsureNoCanonicality(issues, "Missing HostWallId");
        }

        private static void DuplicateHostKeepsAmbiguityDiagnostic()
        {
            var project = new ProjectState("P-HOST-DUP", "HostWallId duplicate smoke");
            project.Elements.Add(new ProjectElement("Wall-A", ElementCategory.ArchitecturalWall));
            project.Elements.Add(new ProjectElement("wall-a", ElementCategory.StructuralWall));
            var opening = new ProjectElement("Opening-A", ElementCategory.WallOpening);
            opening.Properties["HostWallId"] = "Wall-A";
            project.Elements.Add(opening);

            var issues = new ModelHealthService().Inspect(project);
            RequireIssue(issues, opening.Id, "AMBIGUOUS_HOST", HealthSeverity.Error);
            EnsureNoCanonicality(issues, "Ambiguous HostWallId");
        }

        private static void CanonicalWrongCategoryKeepsCategoryDiagnostic()
        {
            var setup = Create("CATEGORY", "Beam-A", ElementCategory.Beam);
            setup.Opening.Properties["HostWallId"] = "Beam-A";
            var issues = new ModelHealthService().Inspect(setup.Project);
            RequireIssue(issues, setup.Opening.Id, "INVALID_HOST_CATEGORY", HealthSeverity.Error);
            EnsureNoCanonicality(issues, "Canonical wrong-category HostWallId");
        }

        private static Setup Create(string suffix, string hostId, ElementCategory hostCategory)
        {
            var project = new ProjectState("P-HOST-" + suffix, "HostWallId canonicality smoke");
            project.Elements.Add(new ProjectElement(hostId, hostCategory));
            var opening = new ProjectElement("Opening-" + suffix, ElementCategory.WallOpening);
            project.Elements.Add(opening);
            return new Setup(project, opening);
        }

        private static void RequireIssue(ProjectState project, string elementId, string code, HealthSeverity severity)
        {
            RequireIssue(new ModelHealthService().Inspect(project), elementId, code, severity);
        }

        private static void RequireIssue(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string elementId, string code, HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected HostWallId health issue was not reported: " + code + ".");
        }

        private static void EnsureNoCanonicality(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string label)
        {
            if (issues.Any(x => string.Equals(x.Code, "HOST_REFERENCE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException(label + " must preserve its existing diagnostic without adding HostWallId canonicality evidence.");
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement opening)
            {
                Project = project;
                Opening = opening;
            }

            public ProjectState Project { get; }
            public ProjectElement Opening { get; }
        }
    }
}
