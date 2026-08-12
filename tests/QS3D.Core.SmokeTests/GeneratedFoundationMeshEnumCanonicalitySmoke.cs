using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedFoundationMeshEnumCanonicalitySmoke
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
            var setup = Create("CANON", "Both", "FoundationMeshXY", "RectangleLocalXY");
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FACES_INVALID");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_MODE_INVALID");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID");
        }

        private static void LegacyMissingFootprintRemainsHealthy()
        {
            var setup = Create("LEGACY", "Top", "FoundationMeshXY", null);
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FACES_INVALID");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_MODE_INVALID");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID");
        }

        private static void NonCanonicalFacesFailVisible()
        {
            var setup = Create("FACES", "bottom", "FoundationMeshXY", "PolygonGlobalXY");
            var issues = Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FACES_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_MODE_INVALID");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID");
        }

        private static void NonCanonicalModeFailsVisible()
        {
            var setup = Create("MODE", "Bottom", "foundationmeshxy", "PolygonGlobalXY");
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FACES_INVALID");
            RequireIssue(issues, setup.Element.Id, "FOUNDATION_MESH_MODE_INVALID", HealthSeverity.Warning);
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID");
        }

        private static void NonCanonicalFootprintFailsVisible()
        {
            var setup = Create("FOOTPRINT", "Bottom", "FoundationMeshXY", "rectanglelocalxy");
            var issues = Inspect(setup.Project);

            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FACES_INVALID");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_MODE_INVALID");
            RequireIssue(issues, setup.Element.Id, "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID", HealthSeverity.Warning);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            return new GeneratedFoundationMeshHealthService().Inspect(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "B" });
        }

        private static Setup Create(string suffix, string faces, string mode, string? footprintMode)
        {
            var project = new ProjectState("P-FOUNDATION-MESH-ENUM-" + suffix, "Generated Foundation Mesh enum canonicality");
            var element = new ProjectElement("E-FOUNDATION-MESH-ENUM-" + suffix, ElementCategory.Foundation);
            element.Properties["GeneratedFoundationMeshHandles"] = "A;B";
            element.Properties["GeneratedFoundationMeshCount"] = "2";
            element.Properties["GeneratedFoundationMeshXDiameterMm"] = "12";
            element.Properties["GeneratedFoundationMeshYDiameterMm"] = "12";
            element.Properties["GeneratedFoundationMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshYActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshCoverM"] = "0.03";
            element.Properties["GeneratedFoundationMeshFaces"] = faces;
            element.Properties["GeneratedFoundationMeshMode"] = mode;
            if (footprintMode != null)
                element.Properties["GeneratedFoundationMeshFootprintMode"] = footprintMode;
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

            throw new InvalidOperationException("GeneratedFoundationMeshEnumCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;

            throw new InvalidOperationException("GeneratedFoundationMeshEnumCanonicalitySmoke reported unexpected issue: " + code + ".");
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
