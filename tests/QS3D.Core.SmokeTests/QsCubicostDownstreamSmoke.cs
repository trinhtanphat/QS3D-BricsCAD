using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCubicostDownstreamSmoke
    {
        internal static void Run()
        {
            AcceptedAndCorrectedEvidenceFlowsDeterministically();
            UnreviewedAndMissingMappingsFailClosed();
            InventorySemanticKeysAndNumericTotalsStayExact();
        }

        private static void AcceptedAndCorrectedEvidenceFlowsDeterministically()
        {
            var evidenceA = new QuantityEvidence("A", "A101.pdf#beam-01", "R7", "calibrated-recognition", 0.98d);
            var evidenceB = new QuantityEvidence("B", "model.ifc#column-02", "R7", "ifc-geometry", 1d);
            var quantities = new[]
            {
                new CubicostQuantityLine("C2", "STR.COLUMN", "L01", 0.54d, 5.1d, ComponentRecognitionStatus.Corrected, evidenceB),
                new CubicostQuantityLine("C1", "STR.BEAM", "L01", 0.90d, 6.6d, ComponentRecognitionStatus.Accepted, evidenceA)
            };
            var bindings = new[]
            {
                new CubicostDownstreamBinding("STR.COLUMN", "FORMULA.CONCRETE.COLUMN", "FORMULA.FORMWORK.COLUMN"),
                new CubicostDownstreamBinding("STR.BEAM", "FORMULA.CONCRETE.BEAM", "FORMULA.FORMWORK.BEAM")
            };

            var bridge = new CubicostReviewedQuantityDownstreamBridge();
            var admitted = bridge.Admit(quantities, bindings, false);
            Equal(4, admitted.Count, "two quantity channels per reviewed component");
            Equal("STR.BEAM.CONCRETE", admitted[0].InventoryClassification, "deterministic classification order");
            Equal("A101.pdf#beam-01", admitted[0].Evidence.SourceReference, "drawing evidence preserved");
            Equal("R7", admitted.Single(x => x.ComponentId == "C2" && x.Unit == "m3").Evidence.Revision, "revision evidence preserved");
            Equal(ComponentRecognitionStatus.Corrected, admitted.Single(x => x.ComponentId == "C2" && x.Unit == "m3").ReviewStatus, "corrected state preserved");
            Equal("FORMULA.CONCRETE.COLUMN", admitted.Single(x => x.ComponentId == "C2" && x.Unit == "m3").FormulaId, "formula binding preserved");

            var inventory = bridge.BuildInventory(admitted);
            Near(0.90d, inventory.Single(x => x.Classification == "STR.BEAM.CONCRETE").Quantity, 1e-12, "concrete inventory quantity");
            Near(5.1d, inventory.Single(x => x.Classification == "STR.COLUMN.FORMWORK").Quantity, 1e-12, "formwork inventory quantity");

            var commercial = bridge.BuildCommercialHandoffs(admitted);
            Equal(12, commercial.Count, "estimate/tender/procurement handoffs");
            Equal(4, commercial.Count(x => x.Destination == "Estimate"), "estimate lines");
            Equal(4, commercial.Count(x => x.Destination == "Tender"), "tender lines");
            Equal(4, commercial.Count(x => x.Destination == "Procurement"), "procurement lines");
            True(commercial.All(x => x.Line.Evidence.Revision == "R7"), "commercial traceability preserved");
        }

        private static void UnreviewedAndMissingMappingsFailClosed()
        {
            var evidence = new QuantityEvidence("P", "A201.pdf#candidate", "R3", "component-recognition", 0.72d);
            var proposed = new CubicostQuantityLine("P1", "STR.BEAM", "L02", 1d, 2d, ComponentRecognitionStatus.Proposed, evidence);
            var binding = new CubicostDownstreamBinding("STR.BEAM", "F.CONC", "F.FORM");
            var bridge = new CubicostReviewedQuantityDownstreamBridge();

            Throws<InvalidOperationException>(() => bridge.Admit(new[] { proposed }, new[] { binding }, false), "proposed blocked by default");

            var policyAdmitted = bridge.Admit(new[] { proposed }, new[] { binding }, true);
            Equal(2, policyAdmitted.Count, "explicit proposed admission policy");
            Throws<InvalidOperationException>(() => bridge.BuildInventory(policyAdmitted), "proposed cannot be commercially published without review");

            var accepted = new CubicostQuantityLine("A1", "STR.SLAB", "L01", 3d, 8d, ComponentRecognitionStatus.Accepted, evidence);
            Throws<InvalidOperationException>(() => bridge.Admit(new[] { accepted }, Array.Empty<CubicostDownstreamBinding>(), false), "missing classification/formula mapping");
            Throws<InvalidOperationException>(() => bridge.Admit(new[] { accepted, accepted }, new[] { new CubicostDownstreamBinding("STR.SLAB", "F1", "F2") }, false), "duplicate component identity");
        }

        private static void InventorySemanticKeysAndNumericTotalsStayExact()
        {
            var evidence = new QuantityEvidence("N", "model.ifc#numeric", "R9", "ifc-geometry", 1d);
            var bridge = new CubicostReviewedQuantityDownstreamBridge();
            var highDynamicRange = new[]
            {
                new CubicostDownstreamLine("N1", "SRC", "STR.NUMERIC", "F.NUM", "m3", 1e16, ComponentRecognitionStatus.Accepted, evidence),
                new CubicostDownstreamLine("N2", "SRC", "STR.NUMERIC", "F.NUM", "m3", 1d, ComponentRecognitionStatus.Accepted, evidence),
                new CubicostDownstreamLine("N3", "SRC", "STR.NUMERIC", "F.NUM", "m3", 1d, ComponentRecognitionStatus.Accepted, evidence)
            };
            var numericInventory = bridge.BuildInventory(highDynamicRange);
            Equal(1, numericInventory.Count, "numeric inventory group count");
            Near(1e16 + 2d, numericInventory[0].Quantity, 0d, "high dynamic range inventory quantity");
            Equal(3, numericInventory[0].Count, "numeric inventory component count");

            const string separator = "\u001f";
            var groupingCollision = new[]
            {
                new CubicostDownstreamLine("G1", "SRC", "A" + separator + "B", "F1", "C", 2d, ComponentRecognitionStatus.Accepted, evidence),
                new CubicostDownstreamLine("G2", "SRC", "A", "F2", "B" + separator + "C", 3d, ComponentRecognitionStatus.Accepted, evidence)
            };
            var grouped = bridge.BuildInventory(groupingCollision);
            Equal(2, grouped.Count, "embedded separator must not alias inventory grouping identity");

            var validationCollision = new[]
            {
                new CubicostDownstreamLine("A" + separator + "B", "SRC", "C", "F3", "D", 4d, ComponentRecognitionStatus.Accepted, evidence),
                new CubicostDownstreamLine("A", "SRC", "B" + separator + "C", "F4", "D", 5d, ComponentRecognitionStatus.Accepted, evidence)
            };
            var commercial = bridge.BuildCommercialHandoffs(validationCollision);
            Equal(6, commercial.Count, "embedded separator must not alias downstream validation identity");
            True(commercial.All(x => ReferenceEquals(evidence, x.Line.Evidence)), "hostile-token evidence preserved");
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
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }
    }
}
