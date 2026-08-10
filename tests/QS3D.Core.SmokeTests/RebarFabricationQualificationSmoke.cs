using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarFabricationQualificationSmoke
    {
        public static void Run()
        {
            DisabledQualificationDoesNotBlockOrdinaryProjects();
            EnabledQualificationFailsClosedWithoutEvidence();
            GeneratedRebarRequiresElementApprovalAndBinding();
            MismatchedElementEvidenceIsRejected();
            ApprovedMatchingEvidencePasses();
            NonRebarGeneratedOutputDoesNotSatisfyFabricationGate();
            MeshOutputUsesCanonicalRebarOwnershipKeys();
        }

        private static void DisabledQualificationDoesNotBlockOrdinaryProjects()
        {
            var project = NewProject("FAB0");
            Equal(0, Inspect(project).Count);
        }

        private static void EnabledQualificationFailsClosedWithoutEvidence()
        {
            var project = NewProject("FAB1");
            project.Metadata[RebarFabricationQualificationHealthService.RequireQualificationMetadataKey] = "true";
            var issues = Inspect(project);
            HasCode(issues, "REBAR_FAB_STANDARD_MISSING");
            HasCode(issues, "REBAR_FAB_REVISION_MISSING");
            HasCode(issues, "REBAR_FAB_OUTPUT_MISSING");
        }

        private static void GeneratedRebarRequiresElementApprovalAndBinding()
        {
            var project = QualifiedProject("FAB2", "STANDARD-X:2026", "R1");
            var element = NewRebarElement("COL1", "GeneratedRebarHandles");
            project.Elements.Add(element);

            var issues = Inspect(project);
            HasCode(issues, "REBAR_FAB_NOT_APPROVED");
            HasCode(issues, "REBAR_FAB_ELEMENT_STANDARD_MISSING");
            HasCode(issues, "REBAR_FAB_ELEMENT_REVISION_MISSING");
        }

        private static void MismatchedElementEvidenceIsRejected()
        {
            var project = QualifiedProject("FAB3", "STANDARD-X:2026", "R2");
            var element = NewRebarElement("BEAM1", "GeneratedBeamStirrupHandles");
            Bind(element, "Approved", "STANDARD-Y:2025", "R1");
            project.Elements.Add(element);

            var issues = Inspect(project);
            HasCode(issues, "REBAR_FAB_STANDARD_MISMATCH");
            HasCode(issues, "REBAR_FAB_REVISION_MISMATCH");
        }

        private static void ApprovedMatchingEvidencePasses()
        {
            var project = QualifiedProject("FAB4", "STANDARD-X:2026", "R3");
            var element = NewRebarElement("SLAB1", "GeneratedSlabMeshHandles");
            Bind(element, "Approved", "STANDARD-X:2026", "R3");
            project.Elements.Add(element);
            Equal(0, Inspect(project).Count);
        }

        private static void NonRebarGeneratedOutputDoesNotSatisfyFabricationGate()
        {
            var project = QualifiedProject("FAB5", "STANDARD-X:2026", "R4");
            var curtain = new ProjectElement("GW1", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            curtain.Properties["GeneratedCurtainFrameHandles"] = "CF1";
            Bind(curtain, "Approved", "STANDARD-X:2026", "R4");
            project.Elements.Add(curtain);

            var issues = Inspect(project);
            HasCode(issues, "REBAR_FAB_OUTPUT_MISSING");
        }

        private static void MeshOutputUsesCanonicalRebarOwnershipKeys()
        {
            var project = QualifiedProject("FAB6", "STANDARD-X:2026", "R5");
            var foundation = NewRebarElement("FD1", "GeneratedFoundationMeshHandles");
            Bind(foundation, "Approved", "STANDARD-X:2026", "R5");
            project.Elements.Add(foundation);
            Equal(0, Inspect(project).Count);
        }

        private static ProjectState NewProject(string id)
        {
            return new ProjectState(id, "Fabrication qualification smoke");
        }

        private static ProjectState QualifiedProject(string id, string standard, string revision)
        {
            var project = NewProject(id);
            project.Metadata[RebarFabricationQualificationHealthService.RequireQualificationMetadataKey] = "true";
            project.Metadata[RebarFabricationQualificationHealthService.StandardCodeMetadataKey] = standard;
            project.Metadata[RebarFabricationQualificationHealthService.DetailingRevisionMetadataKey] = revision;
            return project;
        }

        private static ProjectElement NewRebarElement(string id, string handleKey)
        {
            var element = new ProjectElement(id, ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            element.Properties[handleKey] = id + "-H";
            return element;
        }

        private static void Bind(ProjectElement element, string status, string standard, string revision)
        {
            element.Properties[RebarFabricationQualificationHealthService.StatusPropertyKey] = status;
            element.Properties[RebarFabricationQualificationHealthService.StandardCodePropertyKey] = standard;
            element.Properties[RebarFabricationQualificationHealthService.DetailingRevisionPropertyKey] = revision;
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            return new RebarFabricationQualificationHealthService().Inspect(project);
        }

        private static void HasCode(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new Exception("Expected fabrication issue code " + code + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
