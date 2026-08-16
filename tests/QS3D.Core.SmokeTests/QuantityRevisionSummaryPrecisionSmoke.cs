using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionSummaryPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CollectivelySignificantSmallTotalsArePreserved();
            OrdinarySummaryAggregationRemainsStable();
            OverflowStillFailsClosed();
        }

        private static void CollectivelySignificantSmallTotalsArePreserved()
        {
            const double expected = 10000000000000002d;
            var summaries = Summarize(
                Row("large", "Concrete", 1e16d, 1e16d),
                Row("small-a", "Concrete", 1d, 1d),
                Row("small-b", "Concrete", 1d, 1d));

            Assert(summaries.Count == 1, "Precision regression must remain in one quantity revision summary group.");
            var summary = summaries[0];
            Assert(summary.Before.Equals(expected), "Quantity revision summary Before lost collectively significant small contributions.");
            Assert(summary.After.Equals(expected), "Quantity revision summary After lost collectively significant small contributions.");
            Assert(summary.Delta.Equals(0d), "Equal compensated quantity revision totals must retain a zero delta.");
        }

        private static void OrdinarySummaryAggregationRemainsStable()
        {
            var summaries = Summarize(
                Row("ordinary-a", "Concrete", 10d, 20d),
                Row("ordinary-b", "concrete", 2d, 3d),
                Row("ordinary-c", "Concrete", 1d, 4d));

            Assert(summaries.Count == 1, "Quantity revision summary grouping must remain case-insensitive.");
            Assert(summaries[0].Before.Equals(13d), "Ordinary exact Before aggregation changed unexpectedly.");
            Assert(summaries[0].After.Equals(27d), "Ordinary exact After aggregation changed unexpectedly.");
            Assert(summaries[0].Delta.Equals(14d), "Ordinary exact summary delta changed unexpectedly.");
        }

        private static void OverflowStillFailsClosed()
        {
            Capture<OverflowException>(() => Summarize(
                Row("overflow-a", "Concrete", double.MaxValue, double.MaxValue),
                Row("overflow-b", "Concrete", double.MaxValue, double.MaxValue)));
        }

        private static IReadOnlyList<QuantityRevisionSummary> Summarize(params QuantityRevisionRow[] rows) =>
            new QuantityRevisionReport().Summarize(rows);

        private static QuantityRevisionRow Row(
            string elementId,
            string quantityName,
            double before,
            double after) =>
            new QuantityRevisionRow
            {
                ElementId = elementId,
                Category = "StructuralColumn",
                QuantityName = quantityName,
                Change = "Changed",
                Before = before,
                After = after
            };

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
