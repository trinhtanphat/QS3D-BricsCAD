using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedFoundationMeshNumericCanonicalitySmoke
    {
        private static readonly string[] NumericIssueCodes =
        {
            "FOUNDATION_MESH_X_DIAMETER_INVALID",
            "FOUNDATION_MESH_Y_DIAMETER_INVALID",
            "FOUNDATION_MESH_X_SPACING_INVALID",
            "FOUNDATION_MESH_Y_SPACING_INVALID",
            "FOUNDATION_MESH_COVER_INVALID"
        };

        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalWriterTextRemainsHealthy();
            NonCanonicalAliasesFailVisible();
        }

        private static void CanonicalWriterTextRemainsHealthy()
        {
            var setup = Create("CANON");
            var issues = Inspect(setup.Project);

            foreach (var code in NumericIssueCodes)
                ForbidIssue(issues, setup.Element.Id, code);
        }

        private static void NonCanonicalAliasesFailVisible()
        {
            AssertAlias("XDIAM-PLUS", "GeneratedFoundationMeshXDiameterMm", "+12", "FOUNDATION_MESH_X_DIAMETER_INVALID");
            AssertAlias("YDIAM-DECIMAL", "GeneratedFoundationMeshYDiameterMm", "12.0", "FOUNDATION_MESH_Y_DIAMETER_INVALID");
            AssertAlias("XSPACE-PLUS", "GeneratedFoundationMeshXActualSpacingM", "+0.2", "FOUNDATION_MESH_X_SPACING_INVALID");
            AssertAlias("YSPACE-EXP", "GeneratedFoundationMeshYActualSpacingM", "2E-1", "FOUNDATION_MESH_Y_SPACING_INVALID");
            AssertAlias("COVER-EXP", "GeneratedFoundationMeshCoverM", "3E-2", "FOUNDATION_MESH_COVER_INVALID");
        }

        private static void AssertAlias(string suffix, string key, string value, string expectedCode)
        {
            var setup = Create(suffix);
            setup.Element.Properties[key] = value;
            var issues = Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, expectedCode, HealthSeverity.Warning);
            foreach (var code in NumericIssueCodes.Where(x => !string.Equals(x, expectedCode, StringComparison.Ordinal)))
                ForbidIssue(issues, setup.Element.Id, code);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            return new GeneratedFoundationMeshHealthService().Inspect(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "B" });
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-FOUNDATION-MESH-NUMERIC-" + suffix, "Generated Foundation Mesh numeric canonicality");
            var element = new ProjectElement("E-FOUNDATION-MESH-NUMERIC-" + suffix, ElementCategory.Foundation);
            element.Properties["GeneratedFoundationMeshHandles"] = "A;B";
            element.Properties["GeneratedFoundationMeshCount"] = "2";
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

            throw new InvalidOperationException("GeneratedFoundationMeshNumericCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;

            throw new InvalidOperationException("GeneratedFoundationMeshNumericCanonicalitySmoke reported unexpected issue: " + code + ".");
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
