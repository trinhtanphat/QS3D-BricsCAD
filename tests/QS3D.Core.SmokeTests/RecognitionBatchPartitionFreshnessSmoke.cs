using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionBatchPartitionFreshnessSmoke
    {
        public static void Run()
        {
            CurrentConfidenceReclassifiesBatch();
            CorruptCurrentConfidenceFailsClosed();
        }

        private static void CurrentConfidenceReclassifiesBatch()
        {
            var candidate = Candidate(0.99d);
            var result = Result(candidate);
            var batch = new RecognitionBatch(new[] { result }, autoAcceptConfidence: 0.92d, minimumMargin: 0.15d);

            AssertPartition(batch, 1, 0, "initial high confidence");

            candidate.Confidence = 0.91d;
            AssertPartition(batch, 0, 1, "confidence below original batch threshold");

            candidate.Confidence = 0.95d;
            AssertPartition(batch, 1, 0, "restored high confidence");
        }

        private static void CorruptCurrentConfidenceFailsClosed()
        {
            var candidate = Candidate(0.99d);
            var batch = new RecognitionBatch(new[] { Result(candidate) });
            candidate.Confidence = double.NaN;

            Throws<ArgumentOutOfRangeException>(() => { var ignored = batch.AutoAccepted.Count; });
            Throws<ArgumentOutOfRangeException>(() => { var ignored = batch.ReviewRequired.Count; });
        }

        private static RecognitionCandidate Candidate(double confidence)
        {
            return new RecognitionCandidate
            {
                RuleId = "freshness",
                Category = ElementCategory.Beam,
                Confidence = confidence
            };
        }

        private static RecognitionResult Result(RecognitionCandidate candidate)
        {
            var snapshot = new EntitySnapshot("H1", "Line", "BEAM");
            return new RecognitionResult(snapshot, new[] { candidate });
        }

        private static void AssertPartition(RecognitionBatch batch, int accepted, int review, string label)
        {
            if (batch.AutoAccepted.Count != accepted || batch.ReviewRequired.Count != review)
                throw new InvalidOperationException("Recognition batch partition is stale for " + label + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class RecognitionBatchPartitionFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RecognitionBatchPartitionFreshnessSmoke.Run();
        }
    }
}
