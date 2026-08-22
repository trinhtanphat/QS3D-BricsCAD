using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotDeltaBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            NullSnapshotsFailClosed();
            IdentityOrderingAndLineMetadataAreDeterministic();
            SharedIdentityUnitMismatchFailsClosed();
            LostFiniteEndpointFailsClosed();
            ReasonClassifierPinsCanonicalEvidenceAndOrder();
        }

        private static void NullSnapshotsFailClosed()
        {
            var empty = new MeasurementSnapshot(Array.Empty<MeasurementTrace>());
            Expect<ArgumentNullException>(() => new MeasurementSnapshotDelta(null!, empty), "null before snapshot");
            Expect<ArgumentNullException>(() => new MeasurementSnapshotDelta(empty, null!), "null after snapshot");
        }

        private static void IdentityOrderingAndLineMetadataAreDeterministic()
        {
            var unchangedBefore = Trace("b", "src", "qty", 2d);
            var removed = Trace("d", "src", "qty", 4d);
            var changedBefore = Trace("c", "src", "qty", 3d);

            var added = Trace("a", "src", "qty", 1d);
            var unchangedAfter = Trace("b", "src", "qty", 2d);
            var changedAfter = Trace("c", "src", "qty", 5d);

            var before = new MeasurementSnapshot(new[] { removed, changedBefore, unchangedBefore });
            var after = new MeasurementSnapshot(new[] { changedAfter, unchangedAfter, added });
            var delta = new MeasurementSnapshotDelta(before, after);

            Equal(4, delta.Lines.Count, "delta line count");
            Line(delta.Lines[0], MeasurementSnapshotChangeKind.Added, "a", null, 1d, 1d);
            Line(delta.Lines[1], MeasurementSnapshotChangeKind.Unchanged, "b", 2d, 2d, 0d);
            Line(delta.Lines[2], MeasurementSnapshotChangeKind.Changed, "c", 3d, 5d, 2d);
            Line(delta.Lines[3], MeasurementSnapshotChangeKind.Removed, "d", 4d, null, -4d);

            if (BitConverter.DoubleToInt64Bits(delta.Lines[1].DeltaValue) != 0L)
                throw new InvalidOperationException("Unchanged delta must normalize signed zero to canonical +0.");

            var repeated = new MeasurementSnapshotDelta(before, after);
            for (var i = 0; i < delta.Lines.Count; i++)
            {
                Equal(delta.Lines[i].SemanticIdentity, repeated.Lines[i].SemanticIdentity, "repeat semantic identity");
                Equal(delta.Lines[i].ChangeKind, repeated.Lines[i].ChangeKind, "repeat change kind");
                Equal(delta.Lines[i].DeltaValue, repeated.Lines[i].DeltaValue, "repeat delta value");
            }
        }

        private static void SharedIdentityUnitMismatchFailsClosed()
        {
            var before = new MeasurementSnapshot(new[] { Trace("same", "src", "qty", 1d, unit: "m") });
            var after = new MeasurementSnapshot(new[] { Trace("same", "src", "qty", 1d, unit: "m2") });
            Expect<InvalidOperationException>(() => new MeasurementSnapshotDelta(before, after), "unlike shared-identity units");
        }

        private static void LostFiniteEndpointFailsClosed()
        {
            var before = new MeasurementSnapshot(new[] { Trace("same", "src", "qty", 1d) });
            var after = new MeasurementSnapshot(new[] { Trace("same", "src", "qty", 1e308d) });
            Expect<InvalidOperationException>(() => new MeasurementSnapshotDelta(before, after), "finite endpoint lost in subtraction");

            var representableBefore = new MeasurementSnapshot(new[] { Trace("same", "src", "qty", 1e307d) });
            var representableAfter = new MeasurementSnapshot(new[] { Trace("same", "src", "qty", 2e307d) });
            var delta = new MeasurementSnapshotDelta(representableBefore, representableAfter);
            Equal(1e307d, delta.Lines[0].DeltaValue, "large representable subtraction");
        }

        private static void ReasonClassifierPinsCanonicalEvidenceAndOrder()
        {
            var unchanged = new MeasurementSnapshotDelta(
                new MeasurementSnapshot(new[] { Trace("same", "src", "qty", 2d) }),
                new MeasurementSnapshot(new[] { Trace("same", "src", "qty", 2d) }));
            Reasons(unchanged.Lines[0], MeasurementSnapshotDeltaReasonKind.Unchanged);

            var unresolved = SharedDelta(
                Trace("same", "src", "qty", 2d),
                Trace("same", "src", "qty", 3d));
            Reasons(unresolved, MeasurementSnapshotDeltaReasonKind.Unresolved);

            var previous = Trace(
                "same", "src", "qty", 4d,
                facts: new[] { new MeasurementTraceFact("length", 4d, "m", "geom") },
                adjustments: new[] { new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, 1d, "m", "opening", "door", "R-A", "1") },
                roundingPolicy: "display-a",
                warnings: new[] { "old-warning" },
                ruleId: "TOP-A",
                ruleVersion: "1");
            var current = Trace(
                "same", "src", "qty", 5d,
                facts: new[] { new MeasurementTraceFact("length", 5d, "m", "geom") },
                adjustments: new[] { new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, 2d, "m", "opening", "door", "R-B", "2") },
                roundingPolicy: "display-b",
                warnings: new[] { "new-warning" },
                ruleId: "TOP-B",
                ruleVersion: "2");

            Reasons(
                SharedDelta(previous, current),
                MeasurementSnapshotDeltaReasonKind.RuleProvenanceChanged,
                MeasurementSnapshotDeltaReasonKind.InputFactsChanged,
                MeasurementSnapshotDeltaReasonKind.AdjustmentsChanged,
                MeasurementSnapshotDeltaReasonKind.RoundingPolicyChanged,
                MeasurementSnapshotDeltaReasonKind.AnnotationsChanged);

            var addedDelta = new MeasurementSnapshotDelta(
                new MeasurementSnapshot(Array.Empty<MeasurementTrace>()),
                new MeasurementSnapshot(new[] { Trace("new", "src", "qty", 1d) }));
            Reasons(addedDelta.Lines[0], MeasurementSnapshotDeltaReasonKind.Added);

            var removedDelta = new MeasurementSnapshotDelta(
                new MeasurementSnapshot(new[] { Trace("old", "src", "qty", 1d) }),
                new MeasurementSnapshot(Array.Empty<MeasurementTrace>()));
            Reasons(removedDelta.Lines[0], MeasurementSnapshotDeltaReasonKind.Removed);
        }

        private static MeasurementSnapshotDeltaLine SharedDelta(MeasurementTrace previous, MeasurementTrace current)
        {
            var delta = new MeasurementSnapshotDelta(
                new MeasurementSnapshot(new[] { previous }),
                new MeasurementSnapshot(new[] { current }));
            Equal(1, delta.Lines.Count, "shared delta line count");
            Equal(MeasurementSnapshotChangeKind.Changed, delta.Lines[0].ChangeKind, "shared delta change kind");
            return delta.Lines[0];
        }

        private static MeasurementTrace Trace(
            string semantic,
            string source,
            string quantity,
            double net,
            string unit = "m",
            IEnumerable<MeasurementTraceFact>? facts = null,
            IEnumerable<MeasurementTraceAdjustment>? adjustments = null,
            string roundingPolicy = "display",
            IEnumerable<string>? warnings = null,
            IEnumerable<string>? assumptions = null,
            string? ruleId = null,
            string? ruleVersion = null)
        {
            return new MeasurementTrace(
                semantic,
                source,
                quantity,
                facts ?? Array.Empty<MeasurementTraceFact>(),
                net,
                adjustments ?? Array.Empty<MeasurementTraceAdjustment>(),
                net,
                unit,
                roundingPolicy,
                warnings,
                assumptions,
                ruleId,
                ruleVersion);
        }

        private static void Line(
            MeasurementSnapshotDeltaLine line,
            MeasurementSnapshotChangeKind kind,
            string semantic,
            double? previous,
            double? current,
            double delta)
        {
            Equal(kind, line.ChangeKind, semantic + " change kind");
            Equal(semantic, line.SemanticIdentity, semantic + " identity");
            Equal("src", line.SourceIdentity, semantic + " source");
            Equal("qty", line.QuantityKey, semantic + " quantity key");
            Equal("m", line.Unit, semantic + " unit");
            NullableEqual(previous, line.PreviousValue, semantic + " previous value");
            NullableEqual(current, line.CurrentValue, semantic + " current value");
            Equal(delta, line.DeltaValue, semantic + " delta value");
        }

        private static void Reasons(MeasurementSnapshotDeltaLine line, params MeasurementSnapshotDeltaReasonKind[] expected)
        {
            var actual = MeasurementSnapshotDeltaReasonClassifier.Classify(line);
            Equal(expected.Length, actual.Count, "reason count");
            for (var i = 0; i < expected.Length; i++)
                Equal(expected[i], actual[i], "reason at index " + i);
        }

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }

        private static void NullableEqual(double? expected, double? actual, string label)
        {
            if (expected.HasValue != actual.HasValue || (expected.HasValue && !expected.Value.Equals(actual!.Value)))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
