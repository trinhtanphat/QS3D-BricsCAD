using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedSemanticTagHealthSmoke
    {
        public static void Run()
        {
            NoMetadataIsOptional();
            HealthyTagPasses();
            SemanticChangeMarksRenderedTextStale();
            GeneratedRuntimeTemplateFailsClosed();
            OwnerAndPositionCorruptionAreDetected();
            OwnerCanonicalityIsDetected();
            MLeaderTargetHandleCanonicalityIsDetected();
        }

        private static void NoMetadataIsOptional()
        {
            var project = new ProjectState("P", "P");
            project.Elements.Add(new ProjectElement("E", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));
            if (new GeneratedSemanticTagHealthService().Inspect(project).Count != 0)
                throw new Exception("Semantic tag health must not require every semantic element to have a generated tag.");
        }

        private static void HealthyTagPasses()
        {
            var project = Fixture(out _);
            var issues = new GeneratedSemanticTagHealthService().Inspect(project);
            if (issues.Count != 0)
                throw new Exception("Healthy semantic tag produced health issues: " + string.Join(",", issues.Select(x => x.Code)));
        }

        private static void SemanticChangeMarksRenderedTextStale()
        {
            var project = Fixture(out var element);
            element.Properties["Mark"] = "B-02";
            var issues = new GeneratedSemanticTagHealthService().Inspect(project);
            if (!issues.Any(x => x.Code == "SEMANTIC_TAG_TEXT_STALE" && x.Severity == HealthSeverity.Warning))
                throw new Exception("Semantic tag health did not detect rendered text stale after semantic property change.");
        }

        private static void GeneratedRuntimeTemplateFailsClosed()
        {
            var project = Fixture(out var element);
            element.Properties[GeneratedSemanticTagHealthService.TemplateKey] = "{P:GeneratedSolidHandle}";
            var issues = new GeneratedSemanticTagHealthService().Inspect(project);
            if (!issues.Any(x => x.Code == "SEMANTIC_TAG_RENDER_INVALID" && x.Severity == HealthSeverity.Error))
                throw new Exception("Semantic tag health must reject templates that expose generated/native runtime properties.");
        }

        private static void OwnerAndPositionCorruptionAreDetected()
        {
            var project = Fixture(out var element);
            element.Properties[GeneratedSemanticTagHealthService.OwnerProjectKey] = "OTHER";
            element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = "PortableWorld";
            element.Properties[GeneratedSemanticTagHealthService.PositionXKey] = "NaN";
            var issues = new GeneratedSemanticTagHealthService().Inspect(project);
            Require(issues.Any(x => x.Code == "SEMANTIC_TAG_PROJECT_MISMATCH"), "project mismatch missing");
            Require(issues.Any(x => x.Code == "SEMANTIC_TAG_POSITION_SCOPE_INVALID"), "position scope mismatch missing");
            Require(issues.Any(x => x.Code == "SEMANTIC_TAG_POSITION_INVALID"), "invalid drawing-local position missing");
        }

        private static void OwnerCanonicalityIsDetected()
        {
            var project = Fixture(out var element);
            element.Properties[GeneratedSemanticTagHealthService.OwnerProjectKey] = " p-tag ";
            element.Properties[GeneratedSemanticTagHealthService.OwnerElementKey] = "e-1";
            var issues = new GeneratedSemanticTagHealthService().Inspect(project);
            Require(issues.Any(x => x.Code == "SEMANTIC_TAG_PROJECT_NON_CANONICAL"), "padded/case-drift project owner must be non-canonical");
            Require(issues.Any(x => x.Code == "SEMANTIC_TAG_ELEMENT_NON_CANONICAL"), "case-drift element owner must be non-canonical");
            Require(!issues.Any(x => x.Code == "SEMANTIC_TAG_PROJECT_MISMATCH"), "semantic-equal project owner must not be reported as mismatch");
            Require(!issues.Any(x => x.Code == "SEMANTIC_TAG_ELEMENT_MISMATCH"), "semantic-equal element owner must not be reported as mismatch");
        }

        private static void MLeaderTargetHandleCanonicalityIsDetected()
        {
            var project = MLeaderFixture(out var element);
            var healthy = new GeneratedSemanticTagHealthService().Inspect(project);
            Require(healthy.Count == 0, "canonical MLeader metadata must be healthy: " + string.Join(",", healthy.Select(x => x.Code)));

            foreach (var nonCanonical in new[] { "ab", "00AB", " AB " })
            {
                element.Properties[GeneratedSemanticTagHealthService.LeaderTargetHandleKey] = nonCanonical;
                var issues = new GeneratedSemanticTagHealthService().Inspect(project);
                Require(issues.Any(x => x.Code == "SEMANTIC_TAG_MLEADER_TARGET_HANDLE_NON_CANONICAL"), "MLeader target alias must be diagnosed as non-canonical: " + nonCanonical);
                Require(!issues.Any(x => x.Code == "SEMANTIC_TAG_MLEADER_TARGET_HANDLE_MISMATCH"), "alias-safe source ownership matching must remain intact: " + nonCanonical);
            }

            element.Properties[GeneratedSemanticTagHealthService.LeaderTargetHandleKey] = "0";
            var zeroIssues = new GeneratedSemanticTagHealthService().Inspect(project);
            Require(zeroIssues.Any(x => x.Code == "SEMANTIC_TAG_MLEADER_TARGET_HANDLE_INVALID"), "zero MLeader target handle must be invalid");

            element.Properties[GeneratedSemanticTagHealthService.LeaderTargetHandleKey] = "AC";
            var mismatchIssues = new GeneratedSemanticTagHealthService().Inspect(project);
            Require(mismatchIssues.Any(x => x.Code == "SEMANTIC_TAG_MLEADER_TARGET_HANDLE_MISMATCH"), "valid canonical but foreign target handle must mismatch source ownership");
        }

        private static ProjectState MLeaderFixture(out ProjectElement element)
        {
            var project = Fixture(out element);
            element.SourceHandles.Clear();
            element.SourceHandles.Add("AB");
            element.Properties[GeneratedSemanticTagHealthService.ArtifactKindKey] = GeneratedSemanticTagHealthService.MLeaderArtifactKind;
            element.Properties[GeneratedSemanticTagHealthService.LeaderTargetHandleKey] = "AB";
            element.Properties[GeneratedSemanticTagHealthService.LeaderTargetXKey] = "1000";
            element.Properties[GeneratedSemanticTagHealthService.LeaderTargetYKey] = "2000";
            element.Properties[GeneratedSemanticTagHealthService.LeaderTargetZKey] = "0";
            element.Properties[GeneratedSemanticTagHealthService.LeaderTextXKey] = "1100";
            element.Properties[GeneratedSemanticTagHealthService.LeaderTextYKey] = "2100";
            element.Properties[GeneratedSemanticTagHealthService.LeaderTextZKey] = "0";
            return project;
        }

        private static ProjectState Fixture(out ProjectElement element)
        {
            var project = new ProjectState("P-TAG", "Semantic Tag");
            element = new ProjectElement("E-1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.SourceHandles.Add("10");
            element.Properties["Mark"] = "B-01";
            element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = "AA";
            element.Properties[GeneratedSemanticTagHealthService.TemplateKey] = "{Id}-{P:Mark}";
            element.Properties[GeneratedSemanticTagHealthService.TextKey] = "E-1-B-01";
            element.Properties[GeneratedSemanticTagHealthService.OwnerProjectKey] = project.ProjectId;
            element.Properties[GeneratedSemanticTagHealthService.OwnerElementKey] = element.Id;
            element.Properties[GeneratedSemanticTagHealthService.OwnershipVersionKey] = GeneratedSemanticTagHealthService.OwnershipVersion;
            element.Properties[GeneratedSemanticTagHealthService.TextHeightKey] = "0.18";
            element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = GeneratedSemanticTagHealthService.DrawingLocalWcs;
            element.Properties[GeneratedSemanticTagHealthService.PositionXKey] = "1000";
            element.Properties[GeneratedSemanticTagHealthService.PositionYKey] = "2000";
            element.Properties[GeneratedSemanticTagHealthService.PositionZKey] = "0";
            element.Properties[GeneratedSemanticTagHealthService.RotationKey] = "0";
            project.Elements.Add(element);
            return project;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception("GeneratedSemanticTagHealthSmoke: " + message);
        }
    }
}
