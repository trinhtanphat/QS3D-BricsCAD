using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarProcurementCsvBoundSmoke
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
            var summary = CanonicalSummary();
            var csv = RebarProcurementCsvExporter.ToCsv(Enumerable.Repeat(summary, MaxRowCount));

            True(csv.StartsWith("AlgorithmId,GroupId,Grade,", StringComparison.Ordinal));
            Equal(MaxRowCount + 1, csv.Count(ch => ch == '\n'));
        }

        private static void RowBeyondBoundFailsClosed()
        {
            var summary = CanonicalSummary();
            Throws<ArgumentOutOfRangeException>(() =>
                RebarProcurementCsvExporter.ToCsv(Enumerable.Repeat(summary, MaxRowCount + 1)));
        }

        private static void LazyEnumerationStopsAtFirstOverBoundRow()
        {
            var source = new CountingEnumerable(CanonicalSummary());

            Throws<ArgumentOutOfRangeException>(() => RebarProcurementCsvExporter.ToCsv(source));
            Equal(MaxRowCount + 1, source.MoveNextCount);
        }

        private static RebarProcurementSummary CanonicalSummary()
        {
            var demand = new RebarStockDemand(
                "G1",
                "CB400",
                16d,
                12d,
                new[] { new RebarCutRequirement("C1", 3d, 1) },
                new RebarCutAllowancePolicy());
            var result = RebarCuttingOptimizer.Plan(demand);
            return RebarProcurementReportBuilder.Build(new[] { result })[0];
        }

        private sealed class CountingEnumerable : IEnumerable<RebarProcurementSummary>
        {
            private readonly RebarProcurementSummary _row;

            public CountingEnumerable(RebarProcurementSummary row)
            {
                _row = row ?? throw new ArgumentNullException(nameof(row));
            }

            public int MoveNextCount { get; private set; }

            public IEnumerator<RebarProcurementSummary> GetEnumerator()
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
