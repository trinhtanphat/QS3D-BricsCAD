using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsBenchmarkQuantityAggregationSmoke
    {
        internal static void Run()
        {
            TakeoffInventoryPreservesResidualAndOrder();
            IfcInventoryPreservesResidualAndOrder();
            QuantBimEvidencePreservesResidualAndProvenance();
            AggregateOverflowFailsClosed();
        }

        private static void TakeoffInventoryPreservesResidualAndOrder()
        {
            var calibration = new DrawingCalibration(1d, 1d, "m");
            var forward = new TakeoffPackage("P-F", "Forward", "R1");
            forward.Add(new TakeoffMeasurement2D("M1", "S1", TakeoffMeasurementKind.Count, 1e16, calibration, "Z"));
            forward.Add(new TakeoffMeasurement2D("M2", "S1", TakeoffMeasurementKind.Count, 1d, calibration, "Z"));
            forward.Add(new TakeoffMeasurement2D("M3", "S1", TakeoffMeasurementKind.Count, 1d, calibration, "Z"));
            forward.Add(new TakeoffMeasurement2D("M4", "S1", TakeoffMeasurementKind.Count, 2d, calibration, "A"));

            var reverse = new TakeoffPackage("P-R", "Reverse", "R1");
            reverse.Add(new TakeoffMeasurement2D("M4", "S1", TakeoffMeasurementKind.Count, 2d, calibration, "A"));
            reverse.Add(new TakeoffMeasurement2D("M3", "S1", TakeoffMeasurementKind.Count, 1d, calibration, "Z"));
            reverse.Add(new TakeoffMeasurement2D("M2", "S1", TakeoffMeasurementKind.Count, 1d, calibration, "Z"));
            reverse.Add(new TakeoffMeasurement2D("M1", "S1", TakeoffMeasurementKind.Count, 1e16, calibration, "Z"));

            AssertInventory(forward.BuildInventory(), 10000000000000002d, "takeoff forward");
            AssertInventory(reverse.BuildInventory(), 10000000000000002d, "takeoff reverse");
        }

        private static void IfcInventoryPreservesResidualAndOrder()
        {
            var forward = new[]
            {
                new IfcQtoItem("G1", "IfcWall", "L1", "Z", "Count", 1e16, "ea"),
                new IfcQtoItem("G2", "IfcWall", "L1", "Z", "Count", 1d, "ea"),
                new IfcQtoItem("G3", "IfcWall", "L1", "Z", "Count", 1d, "ea"),
                new IfcQtoItem("G4", "IfcSlab", "L1", "A", "Count", 2d, "ea")
            };
            var reverse = forward.Reverse().ToArray();
            var workbench = new IfcQtoWorkbench();
            AssertInventory(workbench.Aggregate(forward), 10000000000000002d, "ifc forward");
            AssertInventory(workbench.Aggregate(reverse), 10000000000000002d, "ifc reverse");
        }

        private static void QuantBimEvidencePreservesResidualAndProvenance()
        {
            var engine = new QuantBimTraceableTakeoffEngine();
            var forward = new[]
            {
                Evidence("G1", "Z", 1e16),
                Evidence("G2", "Z", 1d),
                Evidence("G3", "Z", 1d),
                Evidence("G4", "A", 2d)
            };
            var reverse = forward.Reverse().ToArray();
            var a = engine.BuildBoq(forward);
            var b = engine.BuildBoq(reverse);
            AssertInventory(a, 10000000000000002d, "quantbim forward");
            AssertInventory(b, 10000000000000002d, "quantbim reverse");
            var z = a.Single(x => x.Classification == "Z");
            if (z.SourceCount != 3) throw new InvalidOperationException("QuantBIM source provenance count drifted.");
        }

        private static void AggregateOverflowFailsClosed()
        {
            var calibration = new DrawingCalibration(1d, 1d, "m");
            var package = new TakeoffPackage("P-O", "Overflow", "R1");
            package.Add(new TakeoffMeasurement2D("O1", "S1", TakeoffMeasurementKind.Count, double.MaxValue, calibration, "Z"));
            package.Add(new TakeoffMeasurement2D("O2", "S1", TakeoffMeasurementKind.Count, double.MaxValue, calibration, "Z"));
            ExpectOverflow(() => package.BuildInventory(), "takeoff overflow");

            var workbench = new IfcQtoWorkbench();
            ExpectOverflow(() => workbench.Aggregate(new[]
            {
                new IfcQtoItem("O1", "IfcWall", "L1", "Z", "Count", double.MaxValue, "ea"),
                new IfcQtoItem("O2", "IfcWall", "L1", "Z", "Count", double.MaxValue, "ea")
            }), "ifc overflow");

            var engine = new QuantBimTraceableTakeoffEngine();
            ExpectOverflow(() => engine.BuildBoq(new[] { Evidence("O1", "Z", double.MaxValue), Evidence("O2", "Z", double.MaxValue) }), "quantbim overflow");
        }

        private static QuantBimEvidenceLine Evidence(string guid, string classification, double quantity)
        {
            return new QuantBimEvidenceLine("model.ifc", "R1", guid, "IfcWall", "L1", classification, "Count", quantity, "ea", "geom:" + guid);
        }

        private static void AssertInventory(System.Collections.Generic.IReadOnlyList<TakeoffInventoryLine> inventory, double expectedZ, string label)
        {
            if (inventory.Count != 2) throw new InvalidOperationException(label + " inventory cardinality drifted.");
            if (inventory[0].Classification != "A" || inventory[1].Classification != "Z") throw new InvalidOperationException(label + " inventory ordering is not deterministic.");
            if (inventory[1].Quantity != expectedZ) throw new InvalidOperationException(label + " lost a representable quantity residual.");
        }

        private static void ExpectOverflow(Action action, string label)
        {
            try { action(); }
            catch (OverflowException) { return; }
            throw new InvalidOperationException(label + " did not fail closed.");
        }
    }

    internal static class QsBenchmarkQuantityAggregationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsBenchmarkQuantityAggregationSmoke.Run();
    }
}
