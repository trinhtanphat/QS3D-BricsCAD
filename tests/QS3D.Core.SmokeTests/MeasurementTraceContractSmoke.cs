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
            DuplicateEvidenceFailsClosed();
            NoneRoundingRequiresReconciliation();
            BalancedFiniteAdjustmentsDoNotOverflow();
            OptionalMetadataNullability();
            AdjustmentRuleIdentity();
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
                True(left.ToCanonicalString().StartsWith("4:MTR1", StringComparison.Ordinal), "Legacy traces without adjustment rule metadata must remain on the MTR1 schema.");
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

        private static void DuplicateEvidenceFailsClosed()
        {
            var duplicateFact = new MeasurementTraceFact("GrossAreaM2", 12d, "m2", "SRC-WALL");
            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                new[]
                {
                    duplicateFact,
                    new MeasurementTraceFact("GrossAreaM2", 12d, "m2", "SRC-WALL")
                },
                12d,
                Array.Empty<MeasurementTraceAdjustment>(),
                12d,
                "m2",
                "none"));

            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                new[]
                {
                    new MeasurementTraceFact("GrossAreaM2", 12d, "m2", "SRC-WALL"),
                    new MeasurementTraceFact("GrossAreaM2", 13d, "m2", "SRC-WALL")
                },
                12d,
                Array.Empty<MeasurementTraceAdjustment>(),
                12d,
                "m2",
                "none"));

            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                new[]
                {
                    new MeasurementTraceFact("GrossAreaM2", 12d, "m2", "SRC-WALL"),
                    new MeasurementTraceFact("GrossAreaM2", 12d, "m", "SRC-WALL")
                },
                12d,
                Array.Empty<MeasurementTraceAdjustment>(),
                12d,
                "m2",
                "none"));

            var distinctFacts = new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                new[]
                {
                    new MeasurementTraceFact("GrossAreaM2", 12d, "m2", "SRC-WALL"),
                    new MeasurementTraceFact("LengthM", 4d, "m", "SRC-WALL"),
                    new MeasurementTraceFact("GrossAreaM2", 12d, "m2", "SRC-WALL-ALT")
                },
                12d,
                Array.Empty<MeasurementTraceAdjustment>(),
                12d,
                "m2",
                "none");
            Equal(3, distinctFacts.InputFacts.Count, "Fact evidence must remain distinct when name or source identity differs.");

            var duplicateAdjustment = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING",
                "opening-deduction",
                "3");
            Throws<ArgumentException>(() => CreateAdjustmentTrace(
                duplicateAdjustment,
                new MeasurementTraceAdjustment(
                    MeasurementTraceAdjustmentKind.Deduction,
                    1d,
                    "m2",
                    "opening",
                    "SRC-OPENING",
                    "opening-deduction",
                    "3")));
        }

        private static void NoneRoundingRequiresReconciliation()
        {
            var deduction = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING");
            var addition = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Addition,
                1d,
                "m2",
                "allowance",
                "SRC-ALLOWANCE");

            var reconciled = new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                new[] { addition, deduction },
                12d,
                "m2",
                "none");
            Equal(12d, reconciled.NetValue, "No-rounding traces must accept reconciled deduction/addition evidence.");

            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                new[] { addition, deduction },
                12d,
                "m2",
                "NONE"));
            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                new[] { addition, deduction },
                12d,
                "m2",
                "None"));

            Throws<ArgumentException>(() => new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                new[] { deduction },
                12d,
                "m2",
                "none"));

            var explicitRounding = new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                new[] { deduction },
                12d,
                "m2",
                "nearest-cent");
            Equal(12d, explicitRounding.NetValue, "Non-none rounding policies remain outside this reconciliation contract.");
        }

        private static void BalancedFiniteAdjustmentsDoNotOverflow()
        {
            var deductions = new[]
            {
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, double.MaxValue, "m2", "large-deduction-a", "SRC-D1"),
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, double.MaxValue, "m2", "large-deduction-b", "SRC-D2")
            };
            var additions = new[]
            {
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Addition, double.MaxValue, "m2", "large-addition-a", "SRC-A1"),
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Addition, double.MaxValue, "m2", "large-addition-b", "SRC-A2")
            };

            var trace = new MeasurementTrace(
                "SEM-WALL-MAX",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                0d,
                new[] { additions[0], deductions[0], additions[1], deductions[1] },
                0d,
                "m2",
                "none");

            Equal(0d, trace.NetValue, "Balanced finite adjustments with a representable net must not fail on intermediate overflow.");
        }

        private static void OptionalMetadataNullability()
        {
            var fact = new MeasurementTraceFact("Count", 1d, "ea");
            True(fact.SourceIdentity == null, "Optional fact source identity must remain nullable.");
            True(!fact.Equals((MeasurementTraceFact?)null), "Fact equality must reject null without throwing.");

            var adjustment = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING");
            True(!adjustment.Equals((MeasurementTraceAdjustment?)null), "Adjustment equality must reject null without throwing.");

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
            True(!trace.Equals((MeasurementTrace?)null), "Trace equality must reject null without throwing.");
            True(!object.Equals(trace, null), "Object equality must reject null without throwing.");
        }

        private static void AdjustmentRuleIdentity()
        {
            var legacyConstructor = typeof(MeasurementTraceAdjustment).GetConstructor(new[]
            {
                typeof(MeasurementTraceAdjustmentKind),
                typeof(double),
                typeof(string),
                typeof(string),
                typeof(string)
            });
            True(legacyConstructor != null, "The legacy five-argument adjustment constructor must remain available for binary compatibility.");

            var legacy = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING");
            True(legacy.RuleId == null && legacy.RuleVersion == null, "Adjustment rule metadata must remain optional as a pair.");

            var ruleAware = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING",
                "opening-deduction",
                "3");
            Equal("opening-deduction", ruleAware.RuleId, "Adjustment rule id mismatch.");
            Equal("3", ruleAware.RuleVersion, "Adjustment rule version mismatch.");
            True(!legacy.Equals(ruleAware), "Adjustment rule identity must participate in structural equality.");

            var ruleAwareClone = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING",
                "opening-deduction",
                "3");
            True(ruleAware.Equals(ruleAwareClone), "Equivalent rule-aware adjustments must compare equal.");
            Equal(ruleAware.GetHashCode(), ruleAwareClone.GetHashCode(), "Equivalent rule-aware adjustments must have the same hash code.");

            Throws<ArgumentException>(() => new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING",
                ruleId: "opening-deduction"));
            Throws<ArgumentException>(() => new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING",
                ruleVersion: "3"));

            var legacyTrace = CreateAdjustmentTrace(legacy);
            Equal(
                "4:MTR110:SEM-WALL-18:SRC-WALL9:NetAreaM22:122:112:m24:none-;-;1:01:11:01:12:m27:opening11:SRC-OPENING1:01:0",
                legacyTrace.ToCanonicalString(),
                "Legacy adjustment traces must preserve the existing MTR1 canonical bytes.");

            var ruleAwareTrace = CreateAdjustmentTrace(ruleAware);
            True(ruleAwareTrace.ToCanonicalString().StartsWith("4:MTR2", StringComparison.Ordinal), "Rule-aware adjustment traces must use the MTR2 canonical schema.");
            True(ruleAwareTrace.ToCanonicalString().Contains("opening-deduction"), "MTR2 canonical text must include adjustment rule identity.");
            True(!legacyTrace.Equals(ruleAwareTrace), "Trace equality must distinguish adjustment rule provenance.");

            var ruleA = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING",
                "rule-a",
                "1");
            var ruleB = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                1d,
                "m2",
                "opening",
                "SRC-OPENING",
                "rule-b",
                "1");
            var left = CreateAdjustmentTrace(10d, ruleB, ruleA);
            var right = CreateAdjustmentTrace(10d, ruleA, ruleB);
            Equal(left.ToCanonicalString(), right.ToCanonicalString(), "MTR2 adjustment ordering must include rule identity and remain independent of input order.");
            True(left.Equals(right), "Rule-aware traces must compare equal after deterministic adjustment ordering.");
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

        private static MeasurementTrace CreateAdjustmentTrace(params MeasurementTraceAdjustment[] adjustments)
        {
            return CreateAdjustmentTrace(11d, adjustments);
        }

        private static MeasurementTrace CreateAdjustmentTrace(
            double netValue,
            params MeasurementTraceAdjustment[] adjustments)
        {
            return new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                12d,
                adjustments,
                netValue,
                "m2",
                "none");
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
