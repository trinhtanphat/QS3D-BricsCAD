using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DQuantityAggregationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GroupsAndOrdersEvidenceDeterministically();
            UsesCompensatedSummation();
            AllowsEmbeddedIdentitySeparatorWithoutCollision();
            RejectsDuplicateEvidenceIdentity();
        }

        private static void GroupsAndOrdersEvidenceDeterministically()
        {
            var aggregate = new TakeoffQuantityAggregator2D().Aggregate(new[]
            {
                Evidence("M-2", "S-2", "R1", "h-2", "concrete", "Zone-A", "m", 3d),
                Evidence("M-1", "S-1", "R1", "h-1", "CONCRETE", "zone-a", "M", 2d)
            }).Single();

            Equal("concrete", aggregate.Classification, "classification preserves first group token");
            Equal(5d, aggregate.Quantity, "quantity");
            Equal(2, aggregate.EvidenceCount, "evidence count");
            Equal(2, aggregate.SheetCount, "sheet count");
            Equal("M-1", aggregate.Evidence[0].MarkupId, "deterministic evidence order first");
            Equal("M-2", aggregate.Evidence[1].MarkupId, "deterministic evidence order second");
        }

        private static void UsesCompensatedSummation()
        {
            var aggregate = new TakeoffQuantityAggregator2D().Aggregate(new[]
            {
                Evidence("M-1", "S-1", "R1", "h-1", "TEST", "", "ea", 1e16),
                Evidence("M-2", "S-1", "R1", "h-2", "TEST", "", "ea", 1d),
                Evidence("M-3", "S-1", "R1", "h-3", "TEST", "", "ea", -1e16)
            }).Single();

            Equal(1d, aggregate.Quantity, "compensated quantity");
        }

        private static void AllowsEmbeddedIdentitySeparatorWithoutCollision()
        {
            const string separator = "\u001f";
            var aggregate = new TakeoffQuantityAggregator2D().Aggregate(new[]
            {
                Evidence("M-1", "S-1" + separator + "R1", "R2", "h-1", "TEST", "", "ea", 1d),
                Evidence("M-1", "S-1", "R1" + separator + "R2", "h-2", "TEST", "", "ea", 2d)
            }).Single();

            Equal(3d, aggregate.Quantity, "separator-safe quantity");
            Equal(2, aggregate.EvidenceCount, "separator-safe evidence count");
        }

        private static void RejectsDuplicateEvidenceIdentity()
        {
            var aggregator = new TakeoffQuantityAggregator2D();
            try
            {
                aggregator.Aggregate(new[]
                {
                    Evidence("M-1", "S-1", "R1", "h-1", "TEST", "", "ea", 1d),
                    Evidence("m-1", "s-1", "r1", "h-2", "TEST", "", "ea", 2d)
                });
                throw new InvalidOperationException("Expected duplicate evidence identity rejection.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("Duplicate takeoff evidence identity", StringComparison.Ordinal) < 0)
                    throw;
            }
        }

        private static TakeoffQuantityEvidence2D Evidence(string markupId, string sheetId, string revision, string sourceHandle, string classification, string zone, string unit, double quantity)
        {
            return new TakeoffQuantityEvidence2D(markupId, sheetId, revision, "drawing-" + revision + ".pdf", sourceHandle, classification, zone, "QTO", quantity, unit);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Qs2DQuantityAggregationSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
