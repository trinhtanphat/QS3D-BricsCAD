using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCubicostDomainWorkflowsSmoke
    {
        internal static void Run()
        {
            SplitsCombinedQuantitiesWithoutLosingEvidence();
            CorrectedDimensionsFlowIntoBothDomains();
            ProposedRowsFailReviewedPublication();
            DuplicateDomainIdentityFailsClosed();
        }

        private static void SplitsCombinedQuantitiesWithoutLosingEvidence()
        {
            var evidence = new QuantityEvidence("D1", "A101.pdf#beam-1", "R4", "calibrated-recognition", 0.96d);
            var component = new RecognizedQsComponent("B1", "Beam", "STR.BEAM", "L01", 4d, 0.3d, 0.5d, ComponentSourceKind.DrawingRecognition, evidence);
            var review = new ComponentReviewDecision("B1", ComponentRecognitionStatus.Accepted, "qs", "verified", null, null, null);
            var bundle = new CubicostConcreteFormworkDomainOrchestrator().Quantify(new[] { component }, new[] { review }, true);

            Equal(1, bundle.Combined.Count, "combined count");
            Equal(1, bundle.Concrete.Count, "concrete count");
            Equal(1, bundle.Formwork.Count, "formwork count");
            Near(bundle.Combined[0].ConcreteVolume, bundle.Concrete[0].Quantity, 0d, "concrete projection");
            Near(bundle.Combined[0].FormworkArea, bundle.Formwork[0].Quantity, 0d, "formwork projection");
            Equal("m3", bundle.Concrete[0].Unit, "concrete unit");
            Equal("m2", bundle.Formwork[0].Unit, "formwork unit");
            True(ReferenceEquals(evidence, bundle.Concrete[0].Evidence), "concrete evidence identity");
            True(ReferenceEquals(evidence, bundle.Formwork[0].Evidence), "formwork evidence identity");
        }

        private static void CorrectedDimensionsFlowIntoBothDomains()
        {
            var evidence = new QuantityEvidence("D2", "model.ifc#column-2", "R8", "ifc-geometry", 1d);
            var component = new RecognizedQsComponent("C2", "Column", "STR.COLUMN", "L02", 3d, 0.4d, 0.4d, ComponentSourceKind.IfcModel, evidence);
            var corrected = new ComponentReviewDecision("C2", ComponentRecognitionStatus.Corrected, "qs", "field correction", 3.2d, 0.45d, 0.45d);
            var bundle = new CubicostConcreteFormworkDomainOrchestrator().Quantify(new[] { component }, new[] { corrected }, false);

            Equal(ComponentRecognitionStatus.Corrected, bundle.Concrete[0].ReviewStatus, "concrete corrected state");
            Equal(ComponentRecognitionStatus.Corrected, bundle.Formwork[0].ReviewStatus, "formwork corrected state");
            Near(bundle.Combined[0].ConcreteVolume, bundle.Concrete[0].Quantity, 0d, "corrected concrete quantity");
            Near(bundle.Combined[0].FormworkArea, bundle.Formwork[0].Quantity, 0d, "corrected formwork quantity");
            Equal("L02", bundle.Concrete[0].Storey, "storey preserved");
            Equal("STR.COLUMN", bundle.Formwork[0].Classification, "classification preserved");
        }

        private static void ProposedRowsFailReviewedPublication()
        {
            var evidence = new QuantityEvidence("D3", "A202.pdf#candidate", "R1", "component-recognition", 0.7d);
            var component = new RecognizedQsComponent("P1", "Beam", "STR.BEAM", "L03", 2d, 0.25d, 0.4d, ComponentSourceKind.DrawingRecognition, evidence);
            var bundle = new CubicostConcreteFormworkDomainOrchestrator().Quantify(new[] { component }, Array.Empty<ComponentReviewDecision>(), true);

            Equal(ComponentRecognitionStatus.Proposed, bundle.Concrete[0].ReviewStatus, "proposed concrete state");
            Throws<InvalidOperationException>(() => new CubicostConcreteDomainWorkflow().RequireReviewed(bundle.Concrete), "proposed concrete publication");
            Throws<InvalidOperationException>(() => new CubicostFormworkDomainWorkflow().RequireReviewed(bundle.Formwork), "proposed formwork publication");
        }

        private static void DuplicateDomainIdentityFailsClosed()
        {
            var evidence = new QuantityEvidence("D4", "manual#dup", "R1", "manual", 1d);
            var line = new CubicostQuantityLine("X1", "STR.SLAB", "L01", 1d, 2d, ComponentRecognitionStatus.Accepted, evidence);
            Throws<InvalidOperationException>(() => new CubicostConcreteDomainWorkflow().Project(new[] { line, line }), "duplicate concrete projection");

            var concrete = new CubicostDomainQuantityRow(CubicostQuantityDomain.Concrete, "X1", "STR.SLAB", "L01", "m3", 1d, ComponentRecognitionStatus.Accepted, evidence);
            var wrong = new CubicostDomainQuantityRow(CubicostQuantityDomain.Formwork, "X2", "STR.SLAB", "L01", "m2", 2d, ComponentRecognitionStatus.Accepted, evidence);
            Throws<InvalidOperationException>(() => new CubicostConcreteDomainWorkflow().RequireReviewed(new[] { concrete, wrong }), "wrong-domain publication");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
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
