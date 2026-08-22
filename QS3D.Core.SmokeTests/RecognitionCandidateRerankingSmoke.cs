using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionCandidateRerankingSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var first = new RecognitionCandidate
            {
                RuleId = "arch-wall",
                Category = ElementCategory.ArchitecturalWall,
                Confidence = 0.75d
            };
            first.Evidence.Add("first");
            var promoted = new RecognitionCandidate
            {
                RuleId = "beam",
                Category = ElementCategory.Beam,
                Confidence = 0.60d
            };
            promoted.Evidence.Add("promoted");

            var result = new RecognitionResult(
                new EntitySnapshot("A1", "LINE", "A-WALL"),
                new[] { first, promoted });
            var batch = new RecognitionBatch(new[] { result }, autoAcceptConfidence: 0.90d, minimumMargin: 0.15d);

            if (!ReferenceEquals(result.TopCandidate, first))
                throw new InvalidOperationException("RecognitionResult changed the initial top candidate unexpectedly.");
            if (batch.ReviewRequired.Count != 1 || batch.AutoAccepted.Count != 0)
                throw new InvalidOperationException("RecognitionBatch initial partition fixture is invalid.");

            promoted.Confidence = 0.95d;

            if (!ReferenceEquals(result.Candidates[0], first) || !ReferenceEquals(result.Candidates[1], promoted))
                throw new InvalidOperationException("RecognitionResult reranking mutated the public Candidates snapshot order.");
            if (!ReferenceEquals(result.TopCandidate, promoted))
                throw new InvalidOperationException("RecognitionResult.TopCandidate did not follow the promoted runner-up.");
            if (Math.Abs(result.Confidence - 0.95d) > 1e-12)
                throw new InvalidOperationException("RecognitionResult.Confidence did not follow the promoted runner-up.");
            if (Math.Abs(result.Margin - 0.20d) > 1e-12)
                throw new InvalidOperationException("RecognitionResult.Margin stayed bound to the stale candidate order.");
            if (result.SuggestedCategory != ElementCategory.Beam.ToString())
                throw new InvalidOperationException("RecognitionResult.SuggestedCategory stayed bound to the stale top candidate.");
            if (result.Evidence != "promoted")
                throw new InvalidOperationException("RecognitionResult.Evidence stayed bound to the stale top candidate.");
            if (result.RequiresReview)
                throw new InvalidOperationException("RecognitionResult.RequiresReview stayed bound to the stale candidate ranking.");
            if (batch.AutoAccepted.Count != 1 || !ReferenceEquals(batch.AutoAccepted[0], result) || batch.ReviewRequired.Count != 0)
                throw new InvalidOperationException("RecognitionBatch did not refresh its partition from the current candidate ranking.");
        }
    }
}
