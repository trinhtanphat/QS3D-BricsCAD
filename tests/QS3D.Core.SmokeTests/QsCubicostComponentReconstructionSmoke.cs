using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCubicostComponentReconstructionSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            AppliesCorrectionBeforeConcreteFormworkQuantity();
            RejectsMissingReview();
            RejectsDuplicateReview();
            RejectsOrphanReview();
            RejectsUnreviewedProposal();
        }

        private static void AppliesCorrectionBeforeConcreteFormworkQuantity()
        {
            var evidence = new QuantityEvidence("REC-1", "A101.pdf#B12", "R4", "drawing-component-reconstruction", 0.97d);
            var source = new RecognizedQsComponent("B12", "Beam", "STR.BEAM", "L02", 4d, 0.25d, 0.45d, ComponentSourceKind.DrawingRecognition, evidence);
            var review = new ComponentReviewDecision("B12", ComponentRecognitionStatus.Corrected, "qs-reviewer", "width verified from dimension string", 4d, 0.30d, 0.45d);
            var verifier = new CubicostComponentReconstructionVerifier();

            var verified = verifier.Verify(new[] { source }, new[] { review });
            var bundle = verifier.QuantifyVerified(new[] { source }, new[] { review }, false);

            Equal(1, verified.Count, "verified reconstruction count");
            Equal(ComponentRecognitionStatus.Corrected, verified[0].Status, "review status");
            Equal(0.30d, verified[0].Width, "corrected width");
            True(ReferenceEquals(evidence, verified[0].Evidence), "evidence identity preserved");
            Equal(1, bundle.Concrete.Count, "concrete row count");
            Equal(1, bundle.Formwork.Count, "formwork row count");
            Equal(0.54d, bundle.Concrete[0].Quantity, "corrected concrete volume");
            Equal(3.6d, bundle.Formwork[0].Quantity, "corrected formwork area");
            Equal(ComponentRecognitionStatus.Corrected, bundle.Concrete[0].ReviewStatus, "downstream review status");
            True(ReferenceEquals(evidence, bundle.Concrete[0].Evidence), "downstream evidence identity preserved");
        }

        private static void RejectsMissingReview()
        {
            var component = Component("C1");
            Throws<InvalidOperationException>(() => new CubicostComponentReconstructionVerifier().Verify(new[] { component }, Array.Empty<ComponentReviewDecision>()), "missing review");
        }

        private static void RejectsDuplicateReview()
        {
            var component = Component("C2");
            var first = Accepted("C2");
            var second = Accepted("C2");
            Throws<InvalidOperationException>(() => new CubicostComponentReconstructionVerifier().Verify(new[] { component }, new[] { first, second }), "duplicate review");
        }

        private static void RejectsOrphanReview()
        {
            var component = Component("C3");
            Throws<InvalidOperationException>(() => new CubicostComponentReconstructionVerifier().Verify(new[] { component }, new[] { Accepted("C3"), Accepted("UNKNOWN") }), "orphan review");
        }

        private static void RejectsUnreviewedProposal()
        {
            var component = Component("C4");
            var proposal = new ComponentReviewDecision("C4", ComponentRecognitionStatus.Proposed, "qs-reviewer", "pending verification", null, null, null);
            Throws<InvalidOperationException>(() => new CubicostComponentReconstructionVerifier().Verify(new[] { component }, new[] { proposal }), "proposed reconstruction");
        }

        private static RecognizedQsComponent Component(string id)
        {
            return new RecognizedQsComponent(id, "Column", "STR.COLUMN", "L01", 3d, 0.4d, 0.4d, ComponentSourceKind.IfcModel,
                new QuantityEvidence("MODEL-R1", "model.ifc#" + id, "R1", "ifc-component-reconstruction", 1d));
        }

        private static ComponentReviewDecision Accepted(string id)
        {
            return new ComponentReviewDecision(id, ComponentRecognitionStatus.Accepted, "qs-reviewer", "verified", null, null, null);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }
    }
}
