using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Measurement
{
    public enum MeasurementSnapshotChangeKind
    {
        Added,
        Removed,
        Unchanged,
        Changed
    }

    public sealed class MeasurementSnapshotDeltaLine
    {
        internal MeasurementSnapshotDeltaLine(
            MeasurementSnapshotChangeKind changeKind,
            MeasurementTrace? previousTrace,
            MeasurementTrace? currentTrace,
            string unit,
            double? previousValue,
            double? currentValue,
            double deltaValue)
        {
            var identityTrace = currentTrace ?? previousTrace ?? throw new ArgumentException("Measurement snapshot delta line requires at least one trace.");
            ChangeKind = changeKind;
            SemanticIdentity = identityTrace.SemanticIdentity;
            SourceIdentity = identityTrace.SourceIdentity;
            QuantityKey = identityTrace.QuantityKey;
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            PreviousTrace = previousTrace;
            CurrentTrace = currentTrace;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            DeltaValue = NormalizeZero(deltaValue);
        }

        public MeasurementSnapshotChangeKind ChangeKind { get; }
        public string SemanticIdentity { get; }
        public string SourceIdentity { get; }
        public string QuantityKey { get; }
        public string Unit { get; }
        public MeasurementTrace? PreviousTrace { get; }
        public MeasurementTrace? CurrentTrace { get; }
        public double? PreviousValue { get; }
        public double? CurrentValue { get; }
        public double DeltaValue { get; }

        private static double NormalizeZero(double value) => value == 0d ? 0d : value;
    }

    /// <summary>
    /// Deterministic line-by-line comparison of two frozen measurement snapshots.
    /// Quantities are never recalculated or converted here; each line consumes the
    /// canonical MeasurementTrace values already present in the snapshots.
    /// </summary>
    public sealed class MeasurementSnapshotDelta
    {
        public MeasurementSnapshotDelta(MeasurementSnapshot before, MeasurementSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));

            var lines = new List<MeasurementSnapshotDeltaLine>();
            var beforeIndex = 0;
            var afterIndex = 0;

            while (beforeIndex < before.Traces.Count || afterIndex < after.Traces.Count)
            {
                if (beforeIndex >= before.Traces.Count)
                {
                    lines.Add(Added(after.Traces[afterIndex++]));
                    continue;
                }
                if (afterIndex >= after.Traces.Count)
                {
                    lines.Add(Removed(before.Traces[beforeIndex++]));
                    continue;
                }

                var previous = before.Traces[beforeIndex];
                var current = after.Traces[afterIndex];
                var compare = CompareIdentity(previous, current);
                if (compare < 0)
                {
                    lines.Add(Removed(previous));
                    beforeIndex++;
                    continue;
                }
                if (compare > 0)
                {
                    lines.Add(Added(current));
                    afterIndex++;
                    continue;
                }

                lines.Add(CompareSharedIdentity(previous, current));
                beforeIndex++;
                afterIndex++;
            }

            Lines = new ReadOnlyCollection<MeasurementSnapshotDeltaLine>(lines.ToArray());
        }

        public IReadOnlyList<MeasurementSnapshotDeltaLine> Lines { get; }

        private static MeasurementSnapshotDeltaLine Added(MeasurementTrace current)
        {
            return new MeasurementSnapshotDeltaLine(
                MeasurementSnapshotChangeKind.Added,
                null,
                current,
                current.Unit,
                null,
                current.NetValue,
                current.NetValue);
        }

        private static MeasurementSnapshotDeltaLine Removed(MeasurementTrace previous)
        {
            return new MeasurementSnapshotDeltaLine(
                MeasurementSnapshotChangeKind.Removed,
                previous,
                null,
                previous.Unit,
                previous.NetValue,
                null,
                -previous.NetValue);
        }

        private static MeasurementSnapshotDeltaLine CompareSharedIdentity(MeasurementTrace previous, MeasurementTrace current)
        {
            if (!string.Equals(previous.Unit, current.Unit, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Measurement snapshot delta cannot compare unlike units for " +
                    previous.SemanticIdentity + "/" + previous.SourceIdentity + "/" + previous.QuantityKey +
                    ": " + previous.Unit + " -> " + current.Unit + ".");
            }

            var deltaValue = current.NetValue - previous.NetValue;
            if (double.IsNaN(deltaValue) || double.IsInfinity(deltaValue))
                throw new InvalidOperationException("Measurement snapshot delta produced a non-finite value for a canonical measurement identity.");
            if ((deltaValue == current.NetValue && previous.NetValue != 0d) ||
                (deltaValue == -previous.NetValue && current.NetValue != 0d))
                throw new InvalidOperationException("Measurement snapshot delta lost a finite non-zero endpoint during subtraction.");

            return new MeasurementSnapshotDeltaLine(
                previous.Equals(current) ? MeasurementSnapshotChangeKind.Unchanged : MeasurementSnapshotChangeKind.Changed,
                previous,
                current,
                current.Unit,
                previous.NetValue,
                current.NetValue,
                deltaValue);
        }

        private static int CompareIdentity(MeasurementTrace left, MeasurementTrace right)
        {
            var compare = StringComparer.Ordinal.Compare(left.SemanticIdentity, right.SemanticIdentity);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.SourceIdentity, right.SourceIdentity);
            if (compare != 0) return compare;
            return StringComparer.Ordinal.Compare(left.QuantityKey, right.QuantityKey);
        }
    }
}
