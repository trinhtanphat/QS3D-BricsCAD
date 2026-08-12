using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionResultConfidenceProjectionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ValidProjectionsRemainStable();
            MutatedTopConfidenceFailsClosed();
            MutatedRunnerUpConfidenceFailsClosed();
        }

        private static void ValidProjectionsRemainStable()
        {
            var result = Result(0.8d, 0.6d);
            if (Math.Abs(result.Confidence - 0.8d) > 1e-12)
                throw new InvalidOperationException("RecognitionResult.Confidence changed valid projection semantics.");
            if (Math.Abs(result.Margin - 0.2d) > 1e-12)
                throw new InvalidOperationException("RecognitionResult.Margin changed valid projection semantics.");
        }

        private static void MutatedTopConfidenceFailsClosed()
        {
            var result = Result(0.8d, 0.6d);
            result.Candidates[0].Confidence = double.NaN;
            ExpectInvalid(() => _ = result.Confidence, "Confidence accepted a post-construction NaN candidate.");
            ExpectInvalid(() => _ = result.Margin, "Margin accepted a post-construction NaN candidate.");
        }

        private static void MutatedRunnerUpConfidenceFailsClosed()
        {
            var result = Result(0.8d, 0.6d);
            result.Candidates[1].Confidence = double.PositiveInfinity;
            ExpectInvalid(() => _ = result.Margin, "Margin accepted a post-construction infinite runner-up candidate.");
        }

        private static RecognitionResult Result(double first, double second)
        {
            var snapshot = new EntitySnapshot("A1", "LINE", "A-WALL");
            return new RecognitionResult(snapshot, new[]
            {
                new RecognitionCandidate { RuleId = "first", Category = ElementCategory.ArchitecturalWall, Confidence = first },
                new RecognitionCandidate { RuleId = "second", Category = ElementCategory.Beam, Confidence = second }
            });
        }

        private static void ExpectInvalid(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }
    }
}
