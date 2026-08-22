using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BbsCsvResourceBoundSmoke
    {
        private const int MaxRowCount = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactRowBoundRemainsAccepted();
            RowBeyondBoundFailsClosed();
            LazyEnumerationStopsAtFirstOverBoundRow();
        }

        private static void ExactRowBoundRemainsAccepted()
        {
            var row = CanonicalRow();
            var csv = RebarCsvExporter.ToCsv(Enumerable.Repeat(row, MaxRowCount));

            True(csv.StartsWith("ElementId,BarMark,ShapeCode,", StringComparison.Ordinal));
            Equal(MaxRowCount + 1, csv.Count(ch => ch == '\n'));
        }

        private static void RowBeyondBoundFailsClosed()
        {
            var row = CanonicalRow();
            Throws<ArgumentOutOfRangeException>(() =>
                RebarCsvExporter.ToCsv(Enumerable.Repeat(row, MaxRowCount + 1)));
        }

        private static void LazyEnumerationStopsAtFirstOverBoundRow()
        {
            var source = new CountingEnumerable(CanonicalRow());

            Throws<ArgumentOutOfRangeException>(() => RebarCsvExporter.ToCsv(source));
            Equal(MaxRowCount + 1, source.MoveNextCount);
        }

        private static RebarScheduleRow CanonicalRow()
        {
            return new RebarScheduleRow
            {
                ElementId = "E1",
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "1D16",
                DiameterMm = 16d,
                Quantity = 1,
                CuttingLengthM = 2d,
                TotalLengthM = 2d,
                UnitWeightKgM = 1d,
                NetWeightKg = 2d,
                WastePercent = 0d,
                TotalWeightKg = 2d,
                FabricationStatus = "Approved",
                FabricationStandardCode = "STD",
                FabricationDetailingRevision = "REV"
            };
        }

        private sealed class CountingEnumerable : IEnumerable<RebarScheduleRow>
        {
            private readonly RebarScheduleRow _row;

            public CountingEnumerable(RebarScheduleRow row)
            {
                _row = row ?? throw new ArgumentNullException(nameof(row));
            }

            public int MoveNextCount { get; private set; }

            public IEnumerator<RebarScheduleRow> GetEnumerator()
            {
                while (true)
                {
                    MoveNextCount++;
                    yield return _row;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
