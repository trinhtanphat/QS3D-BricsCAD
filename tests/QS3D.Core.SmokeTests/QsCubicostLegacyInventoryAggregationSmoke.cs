using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCubicostLegacyInventoryAggregationSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var evidence = new QuantityEvidence("AGG", "aggregation-fixture", "R1", "numeric-regression", 1d);
            var lines = new[]
            {
                new CubicostQuantityLine("C1", "STR.HOSTILE", "L01", 1e16, 1e16, ComponentRecognitionStatus.Accepted, evidence),
                new CubicostQuantityLine("C2", "STR.HOSTILE", "L01", 1d, 1d, ComponentRecognitionStatus.Accepted, evidence),
                new CubicostQuantityLine("C3", "STR.HOSTILE", "L01", 1d, 1d, ComponentRecognitionStatus.Corrected, evidence)
            };

            var workflow = new CubicostConcreteFormworkWorkflow();
            var forward = workflow.BuildInventory(lines);
            var reverse = workflow.BuildInventory(lines.Reverse());
            var expected = 10000000000000002d;
            Equal(expected, Quantity(forward, "STR.HOSTILE.CONCRETE"), "compensated concrete total");
            Equal(expected, Quantity(forward, "STR.HOSTILE.FORMWORK"), "compensated formwork total");
            Equal(Quantity(forward, "STR.HOSTILE.CONCRETE"), Quantity(reverse, "STR.HOSTILE.CONCRETE"), "concrete order determinism");
            Equal(Quantity(forward, "STR.HOSTILE.FORMWORK"), Quantity(reverse, "STR.HOSTILE.FORMWORK"), "formwork order determinism");
            Equal(3, forward.Single(x => x.Classification == "STR.HOSTILE.CONCRETE").SourceCount, "concrete source count");
            Equal(3, forward.Single(x => x.Classification == "STR.HOSTILE.FORMWORK").SourceCount, "formwork source count");

            var zero = workflow.BuildInventory(new[]
            {
                new CubicostQuantityLine("Z1", "STR.ZERO", "L01", -0d, -0d, ComponentRecognitionStatus.Accepted, evidence)
            });
            Equal(0L, BitConverter.DoubleToInt64Bits(Quantity(zero, "STR.ZERO.CONCRETE")), "concrete canonical positive zero");
            Equal(0L, BitConverter.DoubleToInt64Bits(Quantity(zero, "STR.ZERO.FORMWORK")), "formwork canonical positive zero");

            RejectsInvalidOperation(() => workflow.BuildInventory(new[]
            {
                new CubicostQuantityLine("O1", "STR.OVERFLOW", "L01", double.MaxValue, double.MaxValue, ComponentRecognitionStatus.Accepted, evidence),
                new CubicostQuantityLine("O2", "STR.OVERFLOW", "L01", double.MaxValue, double.MaxValue, ComponentRecognitionStatus.Accepted, evidence)
            }), "aggregate overflow fails closed");
        }

        private static double Quantity(IEnumerable<TakeoffInventoryLine> inventory, string classification)
        {
            return inventory.Single(x => x.Classification == classification).Quantity;
        }

        private static void RejectsInvalidOperation(Action action, string label)
        {
            var rejected = false;
            try { action(); }
            catch (InvalidOperationException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException(label + ": expected rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
