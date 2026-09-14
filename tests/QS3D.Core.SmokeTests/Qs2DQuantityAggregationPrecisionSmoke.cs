using System;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DQuantityAggregationPrecisionSmoke
    {
        internal static void Run()
        {
            PreservesPositiveHighDynamicResidual();
            WorkflowPreservesPositiveHighDynamicResidual();
            LiveWorkbookPreservesPositiveHighDynamicResidual();
            CubicostInventoryPreservesPositiveHighDynamicResidual();
            AggregatorsPreserveOverflowExceptionContract();
        }

        private static void PreservesPositiveHighDynamicResidual()
        {
            var aggregate = new TakeoffQuantityAggregator2D().Aggregate(new[]
            {
                Evidence("M-1", 1e16),
                Evidence("M-2", 3d),
                Evidence("M-3", 2d),
                Evidence("M-4", 1e-16)
            }).Single();

            Equal(10000000000000004d, aggregate.Quantity, "canonical positive residual");
        }


        private static void WorkflowPreservesPositiveHighDynamicResidual()
        {
            var values = new[] { 1e16, 3d, 2d, 1e-16 };
            var evidence = values.Select((quantity, index) =>
                new TakeoffQuantityEvidence2D("W-" + index, "S-W", "R1", "workflow.pdf", "h-w-" + index,
                    "PRECISION", "ZONE-P", "QTO", quantity, "m")).ToArray();
            var line = new AutodeskTakeoffWorkflow().BuildInventoryAndEstimate(
                evidence,
                Array.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 1d).Single();

            Equal(10000000000000004d, line.MeasuredQuantity, "workflow canonical positive residual");
            Equal(10000000000000004d, line.FormulaQuantity, "workflow formula receives canonical quantity");
            Equal(10000000000000004d, line.EstimatedCost, "workflow commercial total uses canonical quantity");
            if (line.EvidenceCount != 4) throw new InvalidOperationException("Workflow precision evidence cardinality drifted.");
        }

        private static void LiveWorkbookPreservesPositiveHighDynamicResidual()
        {
            var bindings = new[]
            {
                Binding("D1", "S1", Array.Empty<string>()),
                Binding("D2", "S2", Array.Empty<string>()),
                Binding("D3", "S3", Array.Empty<string>()),
                Binding("TARGET", "S0", new[] { "D1", "D2", "D3" })
            };
            var sources = new[]
            {
                Source("S0", 1e16), Source("S1", 3d), Source("S2", 2d), Source("S3", 1e-16)
            };
            var result = new LiveWorkbookRefreshEngine2().Refresh(bindings, sources, "R1")
                .Results.Single(x => x.Binding.BindingId == "TARGET");

            Equal(10000000000000004d, result.Value, "live workbook canonical positive residual");
            if (!result.IsUsable) throw new InvalidOperationException("Live workbook precision result became unusable.");
            if (result.Trace.Count != 4) throw new InvalidOperationException("Live workbook evidence trace cardinality drifted.");
        }

        private static LiveWorkbookBinding Binding(string id, string sourceId, string[] dependencies)
        {
            return new LiveWorkbookBinding(id, "WB", "Sheet1", "A" + id, "BOQ-" + id,
                LiveWorkbookSourceKind.BimElement, sourceId, "R1", dependencies, 1d, 0d, 0d);
        }

        private static LiveWorkbookSourceSnapshot Source(string id, double quantity)
        {
            return new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, id, "R1", quantity, "evidence:" + id);
        }

        private static void CubicostInventoryPreservesPositiveHighDynamicResidual()
        {
            var evidence = new QuantityEvidence("SRC", "model.ifc", "R1", "QTO", 1d);
            var values = new[] { 1e16, 3d, 2d, 1e-16 };
            var lines = values.Select((quantity, index) => new CubicostDownstreamLine(
                "C-" + index, "WALL", "WALL.CONCRETE", "F-CONCRETE", "m3", quantity,
                ComponentRecognitionStatus.Accepted, evidence)).ToArray();
            var inventory = new CubicostReviewedQuantityDownstreamBridge().BuildInventory(lines).Single();

            Equal(10000000000000004d, inventory.Quantity, "Cubicost canonical positive residual");
            if (inventory.SourceCount != 4) throw new InvalidOperationException("Cubicost precision component cardinality drifted.");
        }

        private static void AggregatorsPreserveOverflowExceptionContract()
        {
            try
            {
                new TakeoffQuantityAggregator2D().Aggregate(new[]
                {
                    Evidence("O-1", double.MaxValue), Evidence("O-2", double.MaxValue)
                }).ToArray();
                throw new InvalidOperationException("2D overflow was accepted.");
            }
            catch (ArgumentOutOfRangeException) { }

            var evidence = new[]
            {
                new TakeoffQuantityEvidence2D("WO-1", "S-W", "R1", "overflow.pdf", "h-wo-1", "OVERFLOW", "ZONE", "QTO", double.MaxValue, "m"),
                new TakeoffQuantityEvidence2D("WO-2", "S-W", "R1", "overflow.pdf", "h-wo-2", "OVERFLOW", "ZONE", "QTO", double.MaxValue, "m")
            };
            try
            {
                new AutodeskTakeoffWorkflow().BuildInventoryAndEstimate(evidence, Array.Empty<IfcQtoItem>(), (c, q) => q, (c, u) => 1d).ToArray();
                throw new InvalidOperationException("Workflow overflow was accepted.");
            }
            catch (ArgumentOutOfRangeException) { }
        }

        private static TakeoffQuantityEvidence2D Evidence(string markupId, double quantity)
        {
            return new TakeoffQuantityEvidence2D(markupId, "S-1", "R1", "sheet.pdf", "h-" + markupId,
                "TEST", "", "QTO", quantity, "ea");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(
                    "Qs2DQuantityAggregationPrecisionSmoke " + label + ": expected=" + expected.ToString("R") +
                    ", actual=" + actual.ToString("R") + ".");
        }
    }
}
