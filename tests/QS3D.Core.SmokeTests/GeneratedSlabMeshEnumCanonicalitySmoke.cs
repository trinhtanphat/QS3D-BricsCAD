using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedSlabMeshEnumCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalValuesRemainHealthy();
            LegacyMissingFootprintRemainsHealthy();
            NonCanonicalFacesFailVisible();
            NonCanonicalModeFailsVisible();
            NonCanonicalFootprintFailsVisible();
        }

        private static void CanonicalValuesRemainHealthy()
        {
            var setup = Create("CANON", "Both", "SlabMeshXY", "RectangleLocalXY");
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_FACES_INVALID");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_MODE_INVALID");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_FOOTPRINT_MODE_INVALID");
        }

        private static void LegacyMissingFootprintRemainsHealthy()
        {
            var setup = Create("LEGACY", "Top", "SlabMeshXY", null);
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_FACES_INVALID");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_MODE_INVALID");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_FOOTPRINT_MODE_INVALID");
        }

        private static void NonCanonicalFacesFailVisible()
        {
            var setup = Create("FACES", "bottom", "SlabMeshXY", "PolygonGlobalXY");
            var issues = Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "SLAB_MESH_FACES_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_MODE_INVALID");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_FOOTPRINT_MODE_INVALID");
        }

        private static void NonCanonicalModeFailsVisible()
        {
            var setup = Create("MODE", "Bottom", "slabmeshxy", "PolygonGlobalXY");
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_FACES_INVALID");
            RequireIssue(issues, setup.Element.Id, "SLAB_MESH_MODE_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_FOOTPRINT_MODE_INVALID");
        }

        private static void NonCanonicalFootprintFailsVisible()
        {
            var setup = Create("FOOTPRINT", "Bottom", "SlabMeshXY", " RectangleLocalXY ");
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_FACES_INVALID");
            ForbidIssue(issues, setup.Element.Id, "SLAB_MESH_MODE_INVALID");
            RequireIssue(issues, setup.Element.Id, "SLAB_MESH_FOOTPRINT_MODE_INVALID", HealthSeverity.Error);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            return new GeneratedSlabMeshHealthService().Inspect(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "B" });
        }

        private static Setup Create(string suffix, string faces, string mode, string? footprintMode)
        {
            var project = new ProjectState("P-SLAB-MESH-ENUM-" + suffix, "Generated Slab Mesh enum canonicality");
            var element = new ProjectElement("E-SLAB-MESH-ENUM-" + suffix, ElementCategory.Slab);
            element.Properties["GeneratedSlabMeshHandles"] = "A;B";
            element.Properties["GeneratedSlabMeshCount"] = "2";
            element.Properties["GeneratedSlabMeshXDiameterMm"] = "12";
            element.Properties["GeneratedSlabMeshYDiameterMm"] = "12";
            element.Properties["GeneratedSlabMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedSlabMeshYActualSpacingM"] = "0.2";
            element.Properties["GeneratedSlabMeshCoverM"] = "0.03";
            element.Properties["GeneratedSlabMeshFaces"] = faces;
            element.Properties["GeneratedSlabMeshMode"] = mode;
            if (footprintMode != null)
                element.Properties["GeneratedSlabMeshFootprintMode"] = footprintMode;
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

            throw new InvalidOperationException("GeneratedSlabMeshEnumCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;

            throw new InvalidOperationException("GeneratedSlabMeshEnumCanonicalitySmoke reported unexpected issue: " + code + ".");
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
