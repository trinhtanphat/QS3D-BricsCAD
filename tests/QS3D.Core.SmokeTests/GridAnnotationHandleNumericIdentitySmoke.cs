using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridAnnotationHandleNumericIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NumericAliasesCollapseToOneGeneratedHandle();
            SourceNumericAliasFailsVisible();
            OptionalPrefixValidityRemainsUnchanged();
            DistinctCanonicalHandlesRemainClean();
        }

        private static void NumericAliasesCollapseToOneGeneratedHandle()
        {
            var project = Project("DUP");
            var grid = Grid(project, "A;0A;B;C;D;E");
            project.Elements.Add(grid);

            var issues = new GeneratedGridAnnotationHealthService().Inspect(project);
            Require(issues, "GRID_ANNOTATION_HANDLE_DUPLICATE");
            Require(issues, "GRID_ANNOTATION_HANDLE_COUNT");
        }

        private static void SourceNumericAliasFailsVisible()
        {
            var project = Project("SOURCE");
            var grid = Grid(project, "A;B;C;D;E;F");
            grid.SourceHandles.Add("00A");
            project.Elements.Add(grid);

            Require(new GeneratedGridAnnotationHealthService().Inspect(project), "GRID_ANNOTATION_HANDLE_IN_SOURCE");
        }

        private static void OptionalPrefixValidityRemainsUnchanged()
        {
            var project = Project("PREFIX");
            var grid = Grid(project, "0xA;B;C;D;E;F");
            project.Elements.Add(grid);

            var issues = new GeneratedGridAnnotationHealthService().Inspect(project);
            Require(issues, "GRID_ANNOTATION_HANDLE_INVALID");
            EnsureAbsent(issues, "GRID_ANNOTATION_HANDLE_DUPLICATE", "0x validity is outside this lane and must not be reclassified as a duplicate.");
        }

        private static void DistinctCanonicalHandlesRemainClean()
        {
            var project = Project("CLEAN");
            var grid = Grid(project, "A;B;C;D;E;F");
            project.Elements.Add(grid);

            var issues = new GeneratedGridAnnotationHealthService().Inspect(project);
            EnsureAbsent(issues, "GRID_ANNOTATION_HANDLE_DUPLICATE", "Distinct numeric handles must remain distinct.");
            EnsureAbsent(issues, "GRID_ANNOTATION_HANDLE_COUNT", "Six distinct valid handles must preserve the expected annotation count.");
            EnsureAbsent(issues, "GRID_ANNOTATION_HANDLE_INVALID", "Canonical valid handles must remain valid.");
            EnsureAbsent(issues, "GRID_ANNOTATION_HANDLE_IN_SOURCE", "Generated handles not present in SourceHandles must remain clean.");
        }

        private static ProjectState Project(string suffix) =>
            new ProjectState("P-GRID-ANNOTATION-HANDLE-" + suffix, "Grid annotation handle identity smoke");

        private static ProjectElement Grid(ProjectState project, string handles)
        {
            var element = new ProjectElement("G-1", ElementCategory.Grid);
            element.Properties["GeneratedGridAnnotationHandles"] = handles;
            element.Properties[GridNamingService.GridLabelKey] = "A";
            element.Properties["GeneratedGridAnnotationLabel"] = "A";
            element.Properties["GeneratedGridAnnotationOwnerProjectId"] = project.ProjectId;
            element.Properties["GeneratedGridAnnotationOwnerElementId"] = element.Id;
            element.Properties["GeneratedGridAnnotationOwnershipVersion"] = "1";
            element.Properties["GridBubbleRadiusM"] = "0.1";
            element.Properties["GridTextHeightM"] = "0.05";
            return element;
        }

        private static void Require(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException("Expected Grid Annotation numeric-handle issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
        }
    }
}
