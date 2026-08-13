using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceContractSmoke
    {
        internal static void Run()
        {
            DeterministicCanonicalRepresentation();
            SnapshotIsolation();
            OptionalRulePair();
            InvalidStatesFailClosed();
        }

        private static void DeterministicCanonicalRepresentation()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
                var left = CreateTrace(
                    new[]
                    {
                        new MeasurementTraceFact("WidthM", 1.25d, "m", "SRC-WALL"),
                        new MeasurementTraceFact("OpeningAreaM2", 2.5d, "m2", "SRC-OPENING")
                    },
                    new[] { "geometry-reviewed", "source-present" },
                    new[] { "opening-is-deductible", "wall-face-is-planar" });

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                var right = CreateTrace(
                    new[]
                    {
                        new MeasurementTraceFact("OpeningAreaM2", 2.5d, "m2", "SRC-OPENING"),
                        new MeasurementTraceFact("WidthM", 1.25d, "m", "SRC-WALL")
                    },
                    new[] { "source-present", "geometry-reviewed" },
                    new[] { "wall-face-is-planar", "opening-is-deductible" });

                True(left.Equals(right), "Equivalent traces must compare equal after canonical ordering.");
                Equal(left.GetHashCode(), right.GetHashCode(), "Equivalent traces must have the same hash code.");
                Equal(left.ToCanonicalString(), right.ToCanonicalString(), "Canonical trace text must not depend on input order or current culture.");
                True(left.ToCanonicalString().Contains("4:1.25"), "Canonical numeric text must use invariant decimal formatting.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        private static void SnapshotIsolation()
        {
            var facts = new List<MeasurementTraceFact>
            {
                new MeasurementTraceFact("GrossAreaM2", 15.5d, "m2", "SRC-WALL")
            };
            var warnings = new List<string> { "source-present" };
            var trace = CreateTrace(facts, warnings, new[] { "wall-face-is-planar" });

            facts.Add(new MeasurementTraceFact("LateMutation", 99d, "m2", "SRC-WALL"));
            warnings.Add("late-warning");

            Equal(1, trace.InputFacts.Count, "Trace facts must be detached from caller collection mutation.");
            Equal(1, trace.Warnings.Count, "Trace warnings must be detached from caller collection mutation.");
        }

        private static void OptionalRulePair()
        {
            var trace = new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                Array.Empty<MeasurementTraceAdjustment>(),
                12d,
                "m2",
                "none");

            True(trace.RuleId == null && trace.RuleVersion == null, "Rule metadata may be omitted as a pair for non-rule quantity paths.");

            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                Array.Empty<MeasurementTraceAdjustment>(),
                12d,
                "m2",
                "none",
                ruleId: "wall-net-area"));
        }

        private static void InvalidStatesFailClosed()
        {
            Throws<ArgumentOutOfRangeException>(() => new MeasurementTraceFact("WidthM", double.NaN, "m"));
            Throws<ArgumentException>(() => new MeasurementTraceFact("WidthM", 1d, "M"));
            Throws<ArgumentOutOfRangeException>(() => new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                -1d,
                "m2",
                "opening",
                "SRC-OPENING"));
            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                " NetAreaM2 ",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                Array.Empty<MeasurementTraceAdjustment>(),
                12d,
                "m2",
                "none"));
            Throws<ArgumentOutOfRangeException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                double.PositiveInfinity,
                Array.Empty<MeasurementTraceAdjustment>(),
                12d,
                "m2",
                "none"));
            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                new[]
                {
                    new MeasurementTraceAdjustment(
                        MeasurementTraceAdjustmentKind.Deduction,
                        1d,
                        "m",
                        "wrong-unit",
                        "SRC-OPENING")
                },
                11d,
                "m2",
                "none"));
        }

        private static MeasurementTrace CreateTrace(
            IEnumerable<MeasurementTraceFact> facts,
            IEnumerable<string> warnings,
            IEnumerable<string> assumptions)
        {
            return new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                facts,
                15.5d,
                new[]
                {
                    new MeasurementTraceAdjustment(
                        MeasurementTraceAdjustmentKind.Deduction,
                        2.5d,
                        "m2",
                        "opening",
                        "SRC-OPENING")
                },
                13d,
                "m2",
                "none",
                warnings,
                assumptions,
                "wall-net-area",
                "2");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
