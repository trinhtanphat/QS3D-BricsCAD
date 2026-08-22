using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Measurement;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WallQuantityMeasurementTraceSmoke
    {
        public static void Run()
        {
            TraceProjectionUsesCanonicalClampedResultWithoutReenumeratingOpenings();
        }

        private static void TraceProjectionUsesCanonicalClampedResultWithoutReenumeratingOpenings()
        {
            var openings = new SingleUseOpenings(
                new OpeningCut { WidthM = 10d, HeightM = 2d });

            var result = WallQuantityCalculator.CalculateWithTrace(
                "wall-1",
                "source-handle-1",
                5d,
                3d,
                0.2d,
                openings);

            Require(openings.EnumerationCount == 1, "Wall trace projection re-enumerated opening inputs instead of reusing the canonical quantity result.");

            Near(15d, result.Quantities.GrossAreaM2, "Canonical gross wall area changed.");
            Near(15d, result.Quantities.OpeningAreaM2, "Canonical opening deduction was not clamped to gross wall area.");
            Near(0d, result.Quantities.NetAreaM2, "Canonical net wall area changed after a fully clamped opening deduction.");
            Near(3d, result.Quantities.GrossVolumeM3, "Canonical gross wall volume changed.");
            Near(3d, result.Quantities.DeductionVolumeM3, "Canonical wall volume deduction changed.");
            Near(0d, result.Quantities.NetVolumeM3, "Canonical net wall volume changed after a fully clamped opening deduction.");

            AssertTrace(
                result.NetAreaTrace,
                "NetAreaM2",
                "m2",
                15d,
                15d,
                0d,
                "Wall opening area deduction");
            AssertTrace(
                result.NetVolumeTrace,
                "NetVolumeM3",
                "m3",
                3d,
                3d,
                0d,
                "Wall opening volume deduction");

            AssertFact(result.NetAreaTrace, "LengthM", 5d);
            AssertFact(result.NetAreaTrace, "HeightM", 3d);
            AssertFact(result.NetAreaTrace, "ThicknessM", 0.2d);
            AssertFact(result.NetVolumeTrace, "LengthM", 5d);
            AssertFact(result.NetVolumeTrace, "HeightM", 3d);
            AssertFact(result.NetVolumeTrace, "ThicknessM", 0.2d);
        }

        private static void AssertTrace(
            MeasurementTrace trace,
            string quantityKey,
            string unit,
            double grossValue,
            double deduction,
            double netValue,
            string reason)
        {
            Require(trace.SemanticIdentity == "wall-1", "Wall measurement trace lost semantic identity.");
            Require(trace.SourceIdentity == "source-handle-1", "Wall measurement trace lost source identity.");
            Require(trace.QuantityKey == quantityKey, "Wall measurement trace quantity key changed.");
            Require(trace.Unit == unit, "Wall measurement trace unit changed.");
            Require(trace.RoundingPolicy == "none", "Wall measurement trace unexpectedly introduced rounding.");
            Near(grossValue, trace.GrossValue, "Wall measurement trace gross value diverged from canonical quantity output.");
            Near(netValue, trace.NetValue, "Wall measurement trace net value diverged from canonical quantity output.");
            Require(trace.Adjustments.Count == 1, "Wall measurement trace must contain exactly one aggregate opening deduction for this input.");
            var adjustment = trace.Adjustments[0];
            Require(adjustment.Kind == MeasurementTraceAdjustmentKind.Deduction, "Wall measurement trace opening adjustment kind changed.");
            Require(adjustment.Unit == unit, "Wall measurement trace deduction unit changed.");
            Require(adjustment.SourceIdentity == "source-handle-1", "Wall measurement trace deduction lost source identity.");
            Require(adjustment.Reason == reason, "Wall measurement trace deduction reason changed.");
            Near(deduction, adjustment.Amount, "Wall measurement trace deduction diverged from canonical clamped quantity output.");
        }

        private static void AssertFact(MeasurementTrace trace, string name, double expectedValue)
        {
            for (var index = 0; index < trace.InputFacts.Count; index++)
            {
                var fact = trace.InputFacts[index];
                if (fact.Name != name) continue;
                Require(fact.Unit == "m", "Wall measurement trace input fact unit changed for " + name + ".");
                Require(fact.SourceIdentity == "source-handle-1", "Wall measurement trace input fact lost source identity for " + name + ".");
                Near(expectedValue, fact.Value, "Wall measurement trace input fact changed for " + name + ".");
                return;
            }
            throw new Exception("Wall measurement trace is missing input fact " + name + ".");
        }

        private sealed class SingleUseOpenings : IEnumerable<OpeningCut>
        {
            private readonly OpeningCut _opening;

            public SingleUseOpenings(OpeningCut opening)
            {
                _opening = opening;
            }

            public int EnumerationCount { get; private set; }

            public IEnumerator<OpeningCut> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new Exception("Wall opening sequence was enumerated more than once.");
                yield return _opening;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}