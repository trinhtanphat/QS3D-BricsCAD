using System;
using System.Collections.Generic;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotDeltaSmoke
    {
        internal static void Run()
        {
            ClassifiesAndOrdersDeterministically();
            RuleOnlyChangeRemainsVisible();
            IdentityIsExactOrdinal();
            UnitMismatchFailsClosed();
            InvalidInputFailsClosed();
        }

        private static void ClassifiesAndOrdersDeterministically()
        {
            var removed = CreateTrace("SEM-A", "SRC-A", "NetAreaM2", 5d, "m2", "area", "1");
            var unchanged = CreateTrace("SEM-B", "SRC-B", "Count", 2d, "ea", null, null);
            var changedBefore = CreateTrace("SEM-C", "SRC-C", "NetAreaM2", 10d, "m2", "area", "1");
            var changedAfter = CreateTrace("SEM-C", "SRC-C", "NetAreaM2", 12d, "m2", "area", "1");
            var added = CreateTrace("SEM-D", "SRC-D", "Count", 3d, "ea", null, null);

            var before = new MeasurementSnapshot(new[] { changedBefore, unchanged, removed });
            var after = new MeasurementSnapshot(new[] { added, unchanged, changedAfter });
            var delta = new MeasurementSnapshotDelta(before, after);

            Equal(4, delta.Lines.Count, "Delta must include every measurement identity from either snapshot.");
            AssertLine(delta.Lines[0], "SEM-A", MeasurementSnapshotChangeKind.Removed, 5d, null, -5d);
            AssertLine(delta.Lines[1], "SEM-B", MeasurementSnapshotChangeKind.Unchanged, 2d, 2d, 0d);
            AssertLine(delta.Lines[2], "SEM-C", MeasurementSnapshotChangeKind.Changed, 10d, 12d, 2d);
            AssertLine(delta.Lines[3], "SEM-D", MeasurementSnapshotChangeKind.Added, null, 3d, 3d);

            True(object.ReferenceEquals(removed, delta.Lines[0].PreviousTrace), "Removed delta must retain the canonical previous trace.");
            True(delta.Lines[0].CurrentTrace == null, "Removed delta must expose current trace absence.");
            True(object.ReferenceEquals(unchanged, delta.Lines[1].PreviousTrace), "Unchanged delta must retain previous trace provenance.");
            True(object.ReferenceEquals(unchanged, delta.Lines[1].CurrentTrace), "Unchanged delta must retain current trace provenance.");
            True(object.ReferenceEquals(changedAfter, delta.Lines[2].CurrentTrace), "Changed delta must retain current trace provenance.");
            True(object.ReferenceEquals(added, delta.Lines[3].CurrentTrace), "Added delta must retain the canonical current trace.");
        }

        private static void RuleOnlyChangeRemainsVisible()
        {
            var beforeTrace = CreateTrace("SEM-WALL", "SRC-WALL", "NetAreaM2", 10d, "m2", "wall-area", "1");
            var afterTrace = CreateTrace("SEM-WALL", "SRC-WALL", "NetAreaM2", 10d, "m2", "wall-area", "2");
            var delta = new MeasurementSnapshotDelta(
                new MeasurementSnapshot(new[] { beforeTrace }),
                new MeasurementSnapshot(new[] { afterTrace }));

            Equal(1, delta.Lines.Count, "Shared measurement identity must produce one delta line.");
            Equal(MeasurementSnapshotChangeKind.Changed, delta.Lines[0].ChangeKind, "Rule-version-only changes must remain visible even when net quantity is unchanged.");
            Equal(0d, delta.Lines[0].DeltaValue, "Rule-version-only change must preserve zero numeric quantity delta.");
            Equal("1", delta.Lines[0].PreviousTrace!.RuleVersion, "Previous rule provenance must be retained.");
            Equal("2", delta.Lines[0].CurrentTrace!.RuleVersion, "Current rule provenance must be retained.");
        }

        private static void IdentityIsExactOrdinal()
        {
            var upper = CreateTrace("SEM-A", "SRC-A", "Count", 1d, "ea", null, null);
            var lower = CreateTrace("sem-a", "SRC-A", "Count", 1d, "ea", null, null);
            var delta = new MeasurementSnapshotDelta(
                new MeasurementSnapshot(new[] { upper }),
                new MeasurementSnapshot(new[] { lower }));

            Equal(2, delta.Lines.Count, "Case-distinct canonical identities must not collapse into one changed row.");
            Equal(MeasurementSnapshotChangeKind.Removed, delta.Lines[0].ChangeKind, "Ordinal upper-case identity must remain distinct.");
            Equal("SEM-A", delta.Lines[0].SemanticIdentity, "Expected ordinal ordering for case-distinct identities.");
            Equal(MeasurementSnapshotChangeKind.Added, delta.Lines[1].ChangeKind, "Ordinal lower-case identity must remain distinct.");
            Equal("sem-a", delta.Lines[1].SemanticIdentity, "Expected ordinal ordering for case-distinct identities.");
        }

        private static void UnitMismatchFailsClosed()
        {
            var beforeTrace = CreateTrace("SEM-A", "SRC-A", "Quantity", 1d, "m2", null, null);
            var afterTrace = CreateTrace("SEM-A", "SRC-A", "Quantity", 1d, "m3", null, null);
            Throws<InvalidOperationException>(() => new MeasurementSnapshotDelta(
                new MeasurementSnapshot(new[] { beforeTrace }),
                new MeasurementSnapshot(new[] { afterTrace })));
        }

        private static void InvalidInputFailsClosed()
        {
            var empty = new MeasurementSnapshot(Array.Empty<MeasurementTrace>());
            Throws<ArgumentNullException>(() => new MeasurementSnapshotDelta(null!, empty));
            Throws<ArgumentNullException>(() => new MeasurementSnapshotDelta(empty, null!));
        }

        private static void AssertLine(
            MeasurementSnapshotDeltaLine line,
            string semanticIdentity,
            MeasurementSnapshotChangeKind changeKind,
            double? previousValue,
            double? currentValue,
            double deltaValue)
        {
            Equal(semanticIdentity, line.SemanticIdentity, "Delta lines must be in deterministic ordinal identity order.");
            Equal(changeKind, line.ChangeKind, "Unexpected measurement delta classification.");
            Equal(previousValue, line.PreviousValue, "Unexpected previous measurement value presence/value.");
            Equal(currentValue, line.CurrentValue, "Unexpected current measurement value presence/value.");
            Equal(deltaValue, line.DeltaValue, "Unexpected signed measurement delta.");
        }

        private static MeasurementTrace CreateTrace(
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey,
            double netValue,
            string unit,
            string? ruleId,
            string? ruleVersion)
        {
            return new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                Array.Empty<MeasurementTraceFact>(),
                netValue,
                Array.Empty<MeasurementTraceAdjustment>(),
                netValue,
                unit,
                "none",
                ruleId: ruleId,
                ruleVersion: ruleVersion);
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
