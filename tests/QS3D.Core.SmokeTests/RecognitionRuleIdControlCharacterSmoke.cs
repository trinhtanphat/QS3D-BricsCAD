using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionRuleIdControlCharacterSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RuleConstructorRejectsInternalControlCharacters();
            RuleConstructorPreservesExistingTrimBehavior();
            ResultRejectsCandidateControlCharacters();
            PostConstructionCandidateMutationFailsClosed();
        }

        private static void RuleConstructorRejectsInternalControlCharacters()
        {
            ExpectArgument(() => new RecognitionRule("first\nsecond", ElementCategory.ArchitecturalWall));
            ExpectArgument(() => new RecognitionRule("first\tsecond", ElementCategory.ArchitecturalWall));
        }

        private static void RuleConstructorPreservesExistingTrimBehavior()
        {
            var rule = new RecognitionRule("  canonical-rule  ", ElementCategory.ArchitecturalWall);
            Equal("canonical-rule", rule.Id, "RecognitionRule must preserve its existing surrounding-whitespace trim behavior.");
        }

        private static void ResultRejectsCandidateControlCharacters()
        {
            ExpectArgument(() => new RecognitionResult(
                Snapshot(),
                new[] { Candidate("first\nsecond", 0.9d) }));
        }

        private static void PostConstructionCandidateMutationFailsClosed()
        {
            var first = Candidate("first", 0.9d);
            var result = new RecognitionResult(
                Snapshot(),
                new[] { first, Candidate("second", 0.7d) });
            var batch = new RecognitionBatch(new[] { result });

            first.RuleId = "first\u001fmutated";

            ExpectArgument(() => _ = result.TopCandidate);
            ExpectArgument(() => _ = result.Margin);
            ExpectArgument(() => _ = result.RequiresReview);
            ExpectArgument(() => _ = batch.AutoAccepted);
            ExpectArgument(() => _ = batch.ReviewRequired);
        }

        private static RecognitionCandidate Candidate(string ruleId, double confidence) => new RecognitionCandidate
        {
            RuleId = ruleId,
            Category = ElementCategory.ArchitecturalWall,
            Confidence = confidence
        };

        private static EntitySnapshot Snapshot() => new EntitySnapshot("A1", "LINE", "A-WALL");

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

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

            throw new InvalidOperationException("Expected malformed Recognition RuleId to fail closed.");
        }
    }
}
