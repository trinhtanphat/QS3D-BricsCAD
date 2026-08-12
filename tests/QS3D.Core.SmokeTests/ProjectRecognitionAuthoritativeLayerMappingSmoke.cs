using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectRecognitionAuthoritativeLayerMappingSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactProjectMappingOverridesIncompatibleFallbackHeuristic();
            MissingProjectMappingStillUsesFallback();
        }

        private static void ExactProjectMappingOverridesIncompatibleFallbackHeuristic()
        {
            var project = new ProjectState("P-RECOGNITION-AUTH", "Recognition authoritative mapping");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-BEAM"] = ElementCategory.Door.ToString();
            var snapshot = BeamLikeSnapshot("H-AUTH");
            var service = new ProjectRecognitionService();

            var result = service.Suggest(project, snapshot);
            Equal(1, result.Candidates.Count, "mapped candidate count");
            if (result.TopCandidate == null) throw new InvalidOperationException("Expected an authoritative project layer candidate.");
            Equal(ElementCategory.Door, result.TopCandidate.Category, "mapped category");
            Equal("project-layer:A-BEAM", result.TopCandidate.RuleId, "mapped rule id");
            Near(0.99d, result.TopCandidate.Confidence, "mapped confidence");

            var batch = service.SuggestBatch(project, new[] { snapshot });
            Equal(1, batch.Results.Count, "mapped batch result count");
            if (batch.Results[0].TopCandidate == null) throw new InvalidOperationException("Expected mapped batch candidate.");
            Equal(ElementCategory.Door, batch.Results[0].TopCandidate!.Category, "mapped batch category");
        }

        private static void MissingProjectMappingStillUsesFallback()
        {
            var project = new ProjectState("P-RECOGNITION-FALLBACK", "Recognition fallback control");
            var result = new ProjectRecognitionService().Suggest(project, BeamLikeSnapshot("H-FALLBACK"));
            if (result.TopCandidate == null) throw new InvalidOperationException("Expected fallback recognition candidate.");
            Equal(ElementCategory.Beam, result.TopCandidate.Category, "fallback category");
        }

        private static EntitySnapshot BeamLikeSnapshot(string handle)
        {
            var snapshot = new EntitySnapshot(handle, "Line", "A-BEAM");
            snapshot.Metadata["Text"] = "beam dam";
            return snapshot;
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }
    }
}
