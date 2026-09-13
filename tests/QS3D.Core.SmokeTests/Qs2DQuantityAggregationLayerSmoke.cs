using System;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DQuantityAggregationLayerSmoke
    {
        internal static void Run()
        {
            SeparatesLayersWithoutLosingEvidence();
            KeepsLegacyEmptyLayerCompatibility();
        }

        private static void SeparatesLayersWithoutLosingEvidence()
        {
            var evidence = new[]
            {
                new TakeoffQuantityEvidence2D("M1", "A101", "R1", "A101.pdf", "H1", "ARC.WALL", "L01", "A-WALL", 10d, "m"),
                new TakeoffQuantityEvidence2D("M2", "A101", "R1", "A101.pdf", "H2", "ARC.WALL", "L01", "A-DOOR", 3d, "m"),
                new TakeoffQuantityEvidence2D("M3", "A102", "R1", "A102.pdf", "H3", "ARC.WALL", "L01", "A-WALL", 5d, "m")
            };

            var aggregates = new TakeoffQuantityAggregator2D().Aggregate(evidence);
            Equal(2, aggregates.Count, "layer-aware aggregate count");

            var wall = aggregates.Single(x => x.Layer == "A-WALL");
            Equal("ARC.WALL", wall.Classification, "classification preserved");
            Equal("L01", wall.Zone, "zone preserved");
            Equal("m", wall.Unit, "unit preserved");
            Near(15d, wall.Quantity, "wall layer quantity");
            Equal(2, wall.EvidenceCount, "wall layer evidence count");
            Equal(2, wall.SheetCount, "wall layer sheet count");

            var door = aggregates.Single(x => x.Layer == "A-DOOR");
            Near(3d, door.Quantity, "door layer quantity");
            Equal(1, door.EvidenceCount, "door layer evidence count");
            Equal("M2", door.Evidence[0].MarkupId, "door layer evidence provenance");
        }

        private static void KeepsLegacyEmptyLayerCompatibility()
        {
            var evidence = new[]
            {
                new TakeoffQuantityEvidence2D("M10", "B101", "R2", "B101.png", "HB1", "STR.SLAB", "", "", 4d, "m2"),
                new TakeoffQuantityEvidence2D("M11", "B102", "R2", "B102.png", "HB2", "STR.SLAB", "", "", 6d, "m2")
            };

            var aggregate = new TakeoffQuantityAggregator2D().Aggregate(evidence).Single();
            Equal(string.Empty, aggregate.Layer, "legacy empty layer preserved");
            Near(10d, aggregate.Quantity, "legacy empty-layer aggregate quantity");
            Equal(2, aggregate.EvidenceCount, "legacy empty-layer evidence count");

            var legacy = new TakeoffEvidenceAggregate2D("STR.SLAB", "", "m2", 10d, evidence);
            Equal(string.Empty, legacy.Layer, "legacy aggregate constructor maps to empty layer");
            Near(10d, legacy.Quantity, "legacy aggregate constructor quantity");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
