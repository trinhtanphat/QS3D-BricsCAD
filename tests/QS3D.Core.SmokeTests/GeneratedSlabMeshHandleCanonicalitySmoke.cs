using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedSlabMeshHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleFailsVisibleButKeepsLiveLookup();
            LowercaseCanonicalHandleRemainsAccepted();
            EmptyDelimiterTokenRemainsInvalid();
        }

        private static void PaddedHandleFailsVisibleButKeepsLiveLookup()
        {
            var setup = Create("PAD", " A ", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedSlabMeshHealthService().Inspect(setup.Project, live);

            RequireIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "INVALID_SLAB_MESH_GENERATED_HANDLE");
        }

        private static void LowercaseCanonicalHandleRemainsAccepted()
        {
            var setup = Create("LOWER", "a", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedSlabMeshHealthService().Inspect(setup.Project, live);

            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "INVALID_SLAB_MESH_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_GENERATED_SOLID_MISSING");
        }

        private static void EmptyDelimiterTokenRemainsInvalid()
        {
            var setup = Create("EMPTY", "A;;B", "2");
            var issues = new GeneratedSlabMeshHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "INVALID_SLAB_MESH_GENERATED_HANDLE");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-SLAB-MESH-CANON-" + suffix, "Generated Slab Mesh handle canonicality");
            var element = new ProjectElement("E-SLAB-MESH-CANON-" + suffix, ElementCategory.Slab);
            element.Properties["GeneratedSlabMeshHandles"] = handles;
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

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedSlabMeshHandleCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedSlabMeshHandleCanonicalitySmoke unexpected issue was reported: " + code + ".");
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
