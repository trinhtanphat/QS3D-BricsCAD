using System;
using System.Collections.Generic;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceInspectorSmoke
    {
        internal static void Run()
        {
            ProjectsCanonicalTraceWithoutRecomputation();
            PreservesOptionalRuleMetadata();
            ProjectionCollectionsAreReadOnly();
            NullTraceFailsClosed();
        }

        private static void ProjectsCanonicalTraceWithoutRecomputation()
        {
            var trace = new MeasurementTrace(
                "SEM-WALL-42",
                "SRC-WALL-42",
                "NetAreaM2",
                new[]
                {
                    new MeasurementTraceFact("WidthM", 5d, "m", "SRC-WALL-42"),
                    new MeasurementTraceFact("HeightM", 4d, "m", "SRC-WALL-42")
                },
                20d,
                new[]
                {
                    new MeasurementTraceAdjustment(
                        MeasurementTraceAdjustmentKind.Addition,
                        1d,
                        "m2",
                        "return-face",
                        "SRC-RETURN",
                        "return-addition",
                        "2"),
                    new MeasurementTraceAdjustment(
                        MeasurementTraceAdjustmentKind.Deduction,
                        3d,
                        "m2",
                        "opening",
                        "SRC-OPENING",
                        "opening-deduction",
                        "7")
                },
                99d,
                "m2",
                "project-rule",
                new[] { "source-shape-approximate", "manual-review-required" },
                new[] { "wall-face-is-planar", "opening-is-deductible" },
                "wall-net-area",
                "12");

            var inspector = MeasurementTraceInspector.FromTrace(trace);

            Equal(trace.SemanticIdentity, inspector.SemanticIdentity, "Semantic identity must be projected exactly.");
            Equal(trace.SourceIdentity, inspector.SourceIdentity, "Source identity must be projected exactly.");
            Equal(trace.QuantityKey, inspector.QuantityKey, "Quantity key must be projected exactly.");
            Equal(trace.GrossValue, inspector.GrossValue, "Gross value must be projected exactly.");
            Equal(trace.NetValue, inspector.NetValue, "Net value must be projected exactly instead of being recalculated from adjustments.");
            Equal(99d, inspector.NetValue, "Inspector must preserve the canonical trace net even when it does not reconcile arithmetically.");
            Equal(trace.Unit, inspector.Unit, "Unit must be projected exactly.");
            Equal(trace.RoundingPolicy, inspector.RoundingPolicy, "Rounding policy must be projected exactly.");
            Equal(trace.RuleId, inspector.RuleId, "Rule id must be projected exactly.");
            Equal(trace.RuleVersion, inspector.RuleVersion, "Rule version must be projected exactly.");

            Equal(trace.InputFacts.Count, inspector.InputFacts.Count, "Input fact count mismatch.");
            for (var i = 0; i < trace.InputFacts.Count; i++)
            {
                var source = trace.InputFacts[i];
                var projected = inspector.InputFacts[i];
                Equal(source.Name, projected.Name, "Input fact name/order mismatch.");
                Equal(source.Value, projected.Value, "Input fact value mismatch.");
                Equal(source.Unit, projected.Unit, "Input fact unit mismatch.");
                Equal(source.SourceIdentity, projected.SourceIdentity, "Input fact source mismatch.");
            }

            Equal(trace.Adjustments.Count, inspector.Adjustments.Count, "Adjustment count mismatch.");
            for (var i = 0; i < trace.Adjustments.Count; i++)
            {
                var source = trace.Adjustments[i];
                var projected = inspector.Adjustments[i];
                Equal(source.Kind, projected.Kind, "Adjustment kind/order mismatch.");
                Equal(source.Amount, projected.Amount, "Adjustment amount mismatch.");
                Equal(source.Unit, projected.Unit, "Adjustment unit mismatch.");
                Equal(source.Reason, projected.Reason, "Adjustment reason mismatch.");
                Equal(source.SourceIdentity, projected.SourceIdentity, "Adjustment source mismatch.");
                Equal(source.RuleId, projected.RuleId, "Adjustment rule id mismatch.");
                Equal(source.RuleVersion, projected.RuleVersion, "Adjustment rule version mismatch.");
            }

            SequenceEqual(trace.Warnings, inspector.Warnings, "Warnings must preserve canonical trace ordering/content.");
            SequenceEqual(trace.Assumptions, inspector.Assumptions, "Assumptions must preserve canonical trace ordering/content.");
        }

        private static void PreservesOptionalRuleMetadata()
        {
            var trace = new MeasurementTrace(
                "SEM-COUNT-1",
                "SRC-COUNT-1",
                "Count",
                Array.Empty<MeasurementTraceFact>(),
                1d,
                Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                "ea",
                "none");

            var inspector = MeasurementTraceInspector.FromTrace(trace);
            True(inspector.RuleId == null, "Optional trace rule id must remain null.");
            True(inspector.RuleVersion == null, "Optional trace rule version must remain null.");
            Equal(0, inspector.InputFacts.Count, "Inspector must not invent facts.");
            Equal(0, inspector.Adjustments.Count, "Inspector must not invent adjustments.");
            Equal(0, inspector.Warnings.Count, "Inspector must not invent warnings.");
            Equal(0, inspector.Assumptions.Count, "Inspector must not invent assumptions.");
        }

        private static void ProjectionCollectionsAreReadOnly()
        {
            var trace = new MeasurementTrace(
                "SEM-WALL-1",
                "SRC-WALL-1",
                "AreaM2",
                new[] { new MeasurementTraceFact("AreaM2", 10d, "m2", "SRC-WALL-1") },
                10d,
                Array.Empty<MeasurementTraceAdjustment>(),
                10d,
                "m2",
                "none",
                new[] { "review" },
                new[] { "planar" });

            var inspector = MeasurementTraceInspector.FromTrace(trace);
            var factList = inspector.InputFacts as IList<MeasurementTraceInspectorFact>;
            var warningList = inspector.Warnings as IList<string>;

            True(factList != null && factList.IsReadOnly, "Projected facts must be read-only.");
            True(warningList != null && warningList.IsReadOnly, "Projected messages must be read-only.");
            Throws<NotSupportedException>(() => factList!.Add(inspector.InputFacts[0]));
            Throws<NotSupportedException>(() => warningList!.Add("late-warning"));
            Equal(1, trace.InputFacts.Count, "Mutation attempts must not alter the canonical trace facts.");
            Equal(1, trace.Warnings.Count, "Mutation attempts must not alter the canonical trace warnings.");
        }

        private static void NullTraceFailsClosed()
        {
            Throws<ArgumentNullException>(() => MeasurementTraceInspector.FromTrace(null!));
        }

        private static void SequenceEqual(IReadOnlyList<string> expected, IReadOnlyList<string> actual, string message)
        {
            Equal(expected.Count, actual.Count, message + " Count mismatch.");
            for (var i = 0; i < expected.Count; i++)
                Equal(expected[i], actual[i], message + " Item mismatch at index " + i + ".");
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
