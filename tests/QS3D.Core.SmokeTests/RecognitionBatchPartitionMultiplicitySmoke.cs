using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionBatchPartitionMultiplicitySmoke
    {
        public static void Run()
        {
            DuplicateReviewEntriesRemainDistinctBatchEntries();
            MixedPartitionsPreserveMultiplicityAndOrder();
        }

        private static void DuplicateReviewEntriesRemainDistinctBatchEntries()
        {
            var review = Result("R", 0.50d);
            var batch = new RecognitionBatch(new[] { review, review });

            if (batch.Results.Count != 2 || batch.AutoAccepted.Count != 0 || batch.ReviewRequired.Count != 2)
                throw new InvalidOperationException("Recognition batch review partition lost duplicate input entries.");
            if (!ReferenceEquals(batch.ReviewRequired[0], review) || !ReferenceEquals(batch.ReviewRequired[1], review))
                throw new InvalidOperationException("Recognition batch review partition did not preserve duplicate result identity/order.");
        }

        private static void MixedPartitionsPreserveMultiplicityAndOrder()
        {
            var accepted = Result("A", 0.99d);
            var review = Result("R", 0.50d);
            var batch = new RecognitionBatch(new[] { accepted, review, accepted, review });

            var acceptedPartition = batch.AutoAccepted;
            var reviewPartition = batch.ReviewRequired;
            if (batch.Results.Count != 4 || acceptedPartition.Count != 2 || reviewPartition.Count != 2)
                throw new InvalidOperationException("Recognition batch partitions do not account for every input entry.");
            if (!ReferenceEquals(acceptedPartition[0], accepted) || !ReferenceEquals(acceptedPartition[1], accepted))
                throw new InvalidOperationException("Recognition batch auto-accepted partition lost duplicate accepted entries.");
            if (!ReferenceEquals(reviewPartition[0], review) || !ReferenceEquals(reviewPartition[1], review))
                throw new InvalidOperationException("Recognition batch review partition lost duplicate review entries.");
        }

        private static RecognitionResult Result(string handle, double confidence)
        {
            var candidate = new RecognitionCandidate
            {
                RuleId = "multiplicity",
                Category = ElementCategory.Beam,
                Confidence = confidence
            };
            return new RecognitionResult(new EntitySnapshot(handle, "Line", "BEAM"), new[] { candidate });
        }
    }

    internal static class RecognitionBatchPartitionMultiplicitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RecognitionBatchPartitionMultiplicitySmoke.Run();
        }
    }
}
