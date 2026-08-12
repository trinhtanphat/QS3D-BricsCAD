using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedFoundationMeshCountCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalCountRemainsHealthy();
            NonCanonicalAliasesFailVisible();
            NumericMismatchRemainsMismatch();
        }

        private static void CanonicalCountRemainsHealthy()
        {
            var setup = Create("CANON", "2");
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_MISMATCH");
        }

        private static void NonCanonicalAliasesFailVisible()
        {
            foreach (var count in new[] { "+2", "02", " 2 " })
            {
                var suffix = count.Replace("+", "PLUS").Replace(" ", "PAD");
                var setup = Create(suffix, count);
                var issues = Inspect(setup.Project);

                RequireIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Warning);
                ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_MISMATCH");
            }
        }

        private static void NumericMismatchRemainsMismatch()
        {
            var setup = Create("MISMATCH", "1");
            var issues = Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_NON_CANONICAL");
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            return new GeneratedFoundationMeshHealthService().Inspect(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "B" });
        }

        private static Setup Create(string suffix, string count)
        {
            var project = new ProjectState("P-FOUNDATION-MESH-COUNT-" + suffix, "Generated Foundation Mesh count canonicality");
            var element = new ProjectElement("E-FOUNDATION-MESH-COUNT-" + suffix, ElementCategory.Foundation);
            element.Properties["GeneratedFoundationMeshHandles"] = "A;B";
            element.Properties["GeneratedFoundationMeshCount"] = count;
            element.Properties["GeneratedFoundationMeshXDiameterMm"] = "12";
            element.Properties["GeneratedFoundationMeshYDiameterMm"] = "12";
            element.Properties["GeneratedFoundationMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshYActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshCoverM"] = "0.03";
            element.Properties["GeneratedFoundationMeshFaces"] = "Both";
            element.Properties["GeneratedFoundationMeshMode"] = "FoundationMeshXY";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(
            IReadOnlyList<ModelHealthIssue> issues,
            string elementId,
            string code,
            HealthSeverity severity)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == severity &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedFoundationMeshCountCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedFoundationMeshCountCanonicalitySmoke reported unexpected issue: " + code + ".");
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
