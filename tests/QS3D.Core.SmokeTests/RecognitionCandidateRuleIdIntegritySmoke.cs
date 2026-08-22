using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionCandidateRuleIdIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ConstructorRejectsMalformedRuleIds();
            ConstructorRejectsDuplicateRuleIds();
            PostConstructionBlankRuleIdFailsClosed();
            PostConstructionDuplicateRuleIdFailsClosed();
        }

        private static void ConstructorRejectsMalformedRuleIds()
        {
            ExpectArgument(() => new RecognitionResult(Snapshot(), new[] { Candidate(string.Empty, 0.9d) }));
            ExpectArgument(() => new RecognitionResult(Snapshot(), new[] { Candidate("   ", 0.9d) }));
            ExpectArgument(() => new RecognitionResult(Snapshot(), new[] { Candidate(" first ", 0.9d) }));
        }

        private static void ConstructorRejectsDuplicateRuleIds()
        {
            ExpectArgument(() => new RecognitionResult(Snapshot(), new[]
            {
                Candidate("first", 0.9d),
                Candidate("FIRST", 0.8d)
            }));
        }

        private static void PostConstructionBlankRuleIdFailsClosed()
        {
            var first = Candidate("first", 0.9d);
            var result = new RecognitionResult(Snapshot(), new[] { first, Candidate("second", 0.7d) });
            var batch = new RecognitionBatch(new[] { result });

            first.RuleId = " ";

            ExpectArgument(() => _ = result.TopCandidate);
            ExpectArgument(() => _ = result.Margin);
            ExpectArgument(() => _ = batch.AutoAccepted);
            ExpectArgument(() => _ = batch.ReviewRequired);
        }

        private static void PostConstructionDuplicateRuleIdFailsClosed()
        {
            var first = Candidate("first", 0.8d);
            var second = Candidate("second", 0.8d);
            var result = new RecognitionResult(Snapshot(), new[] { first, second });
            var batch = new RecognitionBatch(new[] { result });

            second.RuleId = "FIRST";

            ExpectArgument(() => _ = result.TopCandidate);
            ExpectArgument(() => _ = result.SuggestedCategory);
            ExpectArgument(() => _ = batch.ReviewRequired);
        }

        private static RecognitionCandidate Candidate(string ruleId, double confidence) => new RecognitionCandidate
        {
            RuleId = ruleId,
            Category = ElementCategory.ArchitecturalWall,
            Confidence = confidence
        };

        private static EntitySnapshot Snapshot() => new EntitySnapshot("A1", "LINE", "A-WALL");

        private static void ExpectArgument(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Expected malformed recognition candidate RuleId to fail closed.");
        }
    }
}
