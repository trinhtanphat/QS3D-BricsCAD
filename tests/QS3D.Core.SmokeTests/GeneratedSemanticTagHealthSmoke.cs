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
