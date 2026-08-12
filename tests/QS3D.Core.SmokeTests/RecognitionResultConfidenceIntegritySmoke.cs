using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionResultConfidenceIntegritySmoke
    {
        public static void Run()
        {
            MutatedNaNConfidenceFailsClosed();
            MutatedOutOfRangeConfidenceFailsClosed();
            ValidConfidenceReadinessSemanticsRemainStable();
        }

        private static void MutatedNaNConfidenceFailsClosed()
        {
            var candidate = Candidate(0.95d);
            var result = Result(candidate);
            candidate.Confidence = double.NaN;

            Throws<ArgumentOutOfRangeException>(() => ReadRequiresReview(result));
        }

        private static void MutatedOutOfRangeConfidenceFailsClosed()
        {
            var candidate = Candidate(0.95d);
            var result = Result(candidate);
            candidate.Confidence = 1.01d;

            Throws<ArgumentOutOfRangeException>(() => ReadRequiresReview(result));
        }

        private static void ValidConfidenceReadinessSemanticsRemainStable()
        {
            var high = Result(Candidate(0.95d));
            if (high.RequiresReview)
                throw new InvalidOperationException("Valid high-confidence capture-ready recognition result unexpectedly requires review.");

            var low = Result(Candidate(0.50d));
            if (!low.RequiresReview)
                throw new InvalidOperationException("Valid low-confidence recognition result must require review.");
        }

        private static RecognitionCandidate Candidate(double confidence)
        {
            return new RecognitionCandidate
            {
                RuleId = "beam",
                Category = ElementCategory.Beam,
                Confidence = confidence
            };
        }

        private static RecognitionResult Result(RecognitionCandidate candidate)
        {
            var snapshot = new EntitySnapshot("ABCD", "Line", "BEAM");
            return new RecognitionResult(snapshot, new[] { candidate });
        }

        private static void ReadRequiresReview(RecognitionResult result)
        {
            _ = result.RequiresReview;
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

    internal static class RecognitionResultConfidenceIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RecognitionResultConfidenceIntegritySmoke.Run();
        }
    }
}
