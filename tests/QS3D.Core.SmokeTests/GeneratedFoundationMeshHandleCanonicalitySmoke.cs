using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedFoundationMeshHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleFailsVisibleButKeepsLiveLookup();
            LowercaseCanonicalHandleRemainsAccepted();
            NumericEquivalentLiveHandleIsAccepted();
            NumericEquivalentDuplicateSpellingsAreRejected();
            NumericEquivalentSourceHandleIsRejected();
            NumericEquivalentCrossOwnerConflictIsRejected();
            EmptyDelimiterTokenRemainsInvalid();
        }

        private static void PaddedHandleFailsVisibleButKeepsLiveLookup()
        {
            var setup = Create("PAD", " A ", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedFoundationMeshHealthService().Inspect(setup.Project, live);

            RequireIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "INVALID_FOUNDATION_MESH_GENERATED_HANDLE");
        }

        private static void LowercaseCanonicalHandleRemainsAccepted()
        {
            var setup = Create("LOWER", "a", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedFoundationMeshHealthService().Inspect(setup.Project, live);

            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_HANDLE_NON_CANONICAL");
            ForbidIssue(issues, setup.Element.Id, "INVALID_FOUNDATION_MESH_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_SOLID_MISSING");
        }

        private static void NumericEquivalentLiveHandleIsAccepted()
        {
            var setup = Create("LIVE-ALIAS", "000A", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedFoundationMeshHealthService().Inspect(setup.Project, live);

            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_MISMATCH");
        }

        private static void NumericEquivalentDuplicateSpellingsAreRejected()
        {
            var setup = Create("DUP-ALIAS", "A;000A", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedFoundationMeshHealthService().Inspect(setup.Project, live);

            RequireIssue(issues, setup.Element.Id, "DUPLICATE_FOUNDATION_MESH_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_MISMATCH");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_SOLID_MISSING");
        }

        private static void NumericEquivalentSourceHandleIsRejected()
        {
            var setup = Create("SOURCE-ALIAS", "A", "1");
            setup.Element.SourceHandles.Add("0A");
            var issues = new GeneratedFoundationMeshHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_HANDLE_IN_SOURCE");
        }

        private static void NumericEquivalentCrossOwnerConflictIsRejected()
        {
            var setup = Create("OWNER-ALIAS", "A", "1");
            var other = new ProjectElement("E-FOUNDATION-MESH-OTHER", ElementCategory.Foundation);
            other.Properties["GeneratedFutureMeshHandles"] = "0A";
            setup.Project.Elements.Add(other);

            var issues = new GeneratedFoundationMeshHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT");
        }

        private static void EmptyDelimiterTokenRemainsInvalid()
        {
            var setup = Create("EMPTY", "A;;B", "2");
            var issues = new GeneratedFoundationMeshHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "INVALID_FOUNDATION_MESH_GENERATED_HANDLE");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-FOUNDATION-MESH-CANON-" + suffix, "Generated Foundation Mesh handle canonicality");
            var element = new ProjectElement("E-FOUNDATION-MESH-CANON-" + suffix, ElementCategory.Foundation);
            element.Properties["GeneratedFoundationMeshHandles"] = handles;
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

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedFoundationMeshHandleCanonicalitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedFoundationMeshHandleCanonicalitySmoke reported unexpected issue: " + code + ".");
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
