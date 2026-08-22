using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedWallMeshHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleFailsVisible();
            CanonicalHandlesStayCanonical();
            EmptyTokenKeepsInvalidPrecedence();
            PaddedDuplicateKeepsDuplicateVisible();
            LowercaseHexDoesNotEmitCanonicality();
        }

        private static void PaddedHandleFailsVisible()
        {
            var setup = Create("PAD", "A; B", "2");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "WALL_MESH_GENERATED_HANDLE_NON_CANONICAL");
            EnsureAbsent(issues, "INVALID_WALL_MESH_GENERATED_HANDLE", "Padded valid Wall Mesh handles must remain valid after normalization.");
        }

        private static void CanonicalHandlesStayCanonical()
        {
            var setup = Create("CANONICAL", "A;B", "2");
            EnsureAbsent(Inspect(setup), "WALL_MESH_GENERATED_HANDLE_NON_CANONICAL", "Canonical Wall Mesh handles must not produce canonicality evidence.");
        }

        private static void EmptyTokenKeepsInvalidPrecedence()
        {
            var setup = Create("EMPTY", "A;;B", "2");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "INVALID_WALL_MESH_GENERATED_HANDLE");
            EnsureAbsent(issues, "WALL_MESH_GENERATED_HANDLE_NON_CANONICAL", "Empty Wall Mesh handle tokens must keep existing invalid-token precedence.");
        }

        private static void PaddedDuplicateKeepsDuplicateVisible()
        {
            var setup = Create("DUP", "A; A", "1");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "WALL_MESH_GENERATED_HANDLE_NON_CANONICAL");
            RequireIssue(issues, setup.Element.Id, "DUPLICATE_WALL_MESH_GENERATED_HANDLE");
        }

        private static void LowercaseHexDoesNotEmitCanonicality()
        {
            var setup = Create("LOWER", "a;B", "2");
            EnsureAbsent(Inspect(setup), "WALL_MESH_GENERATED_HANDLE_NON_CANONICAL", "Wall Mesh handle canonicality must not impose hex-letter casing.");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-Wall-Mesh-" + suffix, "Wall Mesh handle canonicality smoke");
            var element = new ProjectElement("Wall-Mesh-" + suffix, ElementCategory.StructuralWall);
            element.Properties["GeneratedWallMeshHandles"] = handles;
            element.Properties["GeneratedWallMeshCount"] = count;
            element.Properties["GeneratedWallMeshHorizontalDiameterMm"] = "10";
            element.Properties["GeneratedWallMeshVerticalDiameterMm"] = "10";
            element.Properties["GeneratedWallMeshHorizontalActualSpacingM"] = "0.2";
            element.Properties["GeneratedWallMeshVerticalActualSpacingM"] = "0.2";
            element.Properties["GeneratedWallMeshCoverM"] = "0.03";
            element.Properties["GeneratedWallMeshFaces"] = "Both";
            element.Properties["GeneratedWallMeshMode"] = "StructuralWallMesh";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static System.Collections.Generic.IReadOnlyList<ModelHealthIssue> Inspect(Setup setup) =>
            new GeneratedWallMeshHealthService().Inspect(setup.Project);

        private static void RequireIssue(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Wall Mesh health issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
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
