using System;
using System.Collections.Generic;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsTakeoffPackageRevisionNumericPrecisionSmoke
    {
        internal static void Run()
        {
            PreservesCanonicalHighDynamicResidual();
        }

        private static void PreservesCanonicalHighDynamicResidual()
        {
            var deltas = new[]
            {
                Added("A", 1e16),
                Added("B", 1d),
                Added("C", 2d),
                Removed("D", 1e-16)
            };
            var sheet = new TakeoffPackageRevisionDelta("S1", "R1", "R2", deltas);
            var package = new TakeoffPackageRevisionComparison("PKG-HD", "R1", "R2", new[] { sheet });

            Equal(10000000000000004d, sheet.QuantityDelta, "sheet high-dynamic revision residual");
            Equal(10000000000000004d, package.QuantityDelta, "package high-dynamic revision residual");
            Equal(3, sheet.AddedCount, "sheet added evidence count");
            Equal(1, sheet.RemovedCount, "sheet removed evidence count");
        }
        private static RevisionMarkupDelta2D Added(string id, double quantity)
        {
            return new RevisionMarkupDelta2D(id, RevisionMarkupChangeKind.Added, null, Evidence(id, "R2", quantity));
        }

        private static RevisionMarkupDelta2D Removed(string id, double quantity)
        {
            return new RevisionMarkupDelta2D(id, RevisionMarkupChangeKind.Removed, Evidence(id, "R1", quantity), null);
        }

        private static TakeoffQuantityEvidence2D Evidence(string id, string revision, double quantity)
        {
            return new TakeoffQuantityEvidence2D(
                id, "S1", revision, "S1-" + revision + ".pdf", "H-" + id,
                "ARC.ITEM", "L01", "A-ITEM", quantity, "ea");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
