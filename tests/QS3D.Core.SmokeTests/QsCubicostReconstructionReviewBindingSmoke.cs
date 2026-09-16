using System;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCubicostReconstructionReviewBindingSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            AcceptsCurrentReconstructionReview();
            RejectsStaleReconstructionReview();
        }

        private static void AcceptsCurrentReconstructionReview()
        {
            var component = Component(0.40d, "R1");
            var gate = new CubicostReconstructionReviewGate();
            var binding = gate.Bind(component);
            var verified = gate.Verify(new[] { component }, new[] { Accepted() }, new[] { binding });
            if (verified.Count != 1) throw new InvalidOperationException("current reconstruction review: expected one verified component.");
        }

        private static void RejectsStaleReconstructionReview()
        {
            var original = Component(0.40d, "R1");
            var reconstructed = Component(0.45d, "R2");
            var gate = new CubicostReconstructionReviewGate();
            var staleBinding = gate.Bind(original);
            try
            {
                gate.Verify(new[] { reconstructed }, new[] { Accepted() }, new[] { staleBinding });
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException("stale reconstruction review: expected InvalidOperationException.");
        }

        private static RecognizedQsComponent Component(double width, string revision)
        {
            return new RecognizedQsComponent(
                "COL-01", "Column", "STR.COLUMN", "L01", 3d, width, 0.40d,
                ComponentSourceKind.IfcModel,
                new QuantityEvidence("MODEL-01", "model.ifc#COL-01", revision, "ifc-component-reconstruction", 1d));
        }

        private static ComponentReviewDecision Accepted()
        {
            return new ComponentReviewDecision("COL-01", ComponentRecognitionStatus.Accepted, "qs-reviewer", "verified", null, null, null);
        }
    }
}
