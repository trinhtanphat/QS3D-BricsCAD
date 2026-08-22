using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedSlabMeshCountCanonicalitySmoke
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
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_COUNT_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_COUNT_MISMATCH");
        }

        private static void NonCanonicalAliasesFailVisible()
        {
            foreach (var count in new[] { "+2", "02", " 2 " })
            {
                var suffix = count.Replace("+", "PLUS").Replace(" ", "PAD");
                var setup = Create(suffix, count);
                var issues = Inspect(setup.Project);
                RequireIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Warning);
                ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_COUNT_MISMATCH");
            }
        }

        private static void NumericMismatchRemainsMismatch()
        {
            var setup = Create("MISMATCH", "1");
            var issues = Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_COUNT_NON_CANONICAL");
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            return new GeneratedSlabMeshHealthService().Inspect(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "B" });
        }

        private static Setup Create(string suffix, string count)
        {
            var project = new ProjectState("P-SLAB-MESH-COUNT-" + suffix, "Generated Slab Mesh count canonicality");
            var element = new ProjectElement("E-SLAB-MESH-COUNT-" + suffix, ElementCategory.Slab);
            element.Properties["GeneratedSlabMeshHandles"] = "A;B";
            element.Properties["GeneratedSlabMeshCount"] = count;
            element.Properties["GeneratedSlabMeshXDiameterMm"] = "12";
            element.Properties["GeneratedSlabMeshYDiameterMm"] = "12";
            element.Properties["GeneratedSlabMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedSlabMeshYActualSpacingM"] = "0.2";
            element.Properties["GeneratedSlabMeshCoverM"] = "0.03";
            element.Properties["GeneratedSlabMeshFaces"] = "Both";
            element.Properties["GeneratedSlabMeshMode"] = "SlabMeshXY";
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
            throw new InvalidOperationException("GeneratedSlabMeshCountCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedSlabMeshCountCanonicalitySmoke reported unexpected issue: " + code + ".");
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
