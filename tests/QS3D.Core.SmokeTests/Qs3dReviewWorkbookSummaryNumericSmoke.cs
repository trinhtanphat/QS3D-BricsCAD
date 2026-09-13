using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs3dReviewWorkbookSummaryNumericSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            HighDynamicRangeSummaryPreservesResidual();
            ReverseOrderProducesSameSummary();
        }

        private static void HighDynamicRangeSummaryPreservesResidual()
        {
            var rows = Rows(1e16d, 1d, 1d);
            var total = InvokeSummarySum(rows);
            Equal(10000000000000002d, total, "high-dynamic-range summary");
        }

        private static void ReverseOrderProducesSameSummary()
        {
            var forward = InvokeSummarySum(Rows(1e16d, 1d, 1d));
            var reverse = InvokeSummarySum(Rows(1d, 1d, 1e16d));
            Equal(forward, reverse, "summary accumulation order");
        }

        private static IReadOnlyList<QuantityReportRow> Rows(params double[] values)
        {
            var rows = new List<QuantityReportRow>(values.Length);
            foreach (var value in values)
            {
                rows.Add(new QuantityReportRow
                {
                    NetConcreteM3 = value,
                    HasNetConcreteM3Evidence = true
                });
            }
            return rows;
        }

        private static double InvokeSummarySum(IReadOnlyList<QuantityReportRow> rows)
        {
            var method = typeof(Qs3dReviewWorkbookExporter).GetMethod(
                "Sum", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("QS3D Review summary Sum helper was not found.");
            var result = method.Invoke(null, new object[]
            {
                rows,
                new Func<QuantityReportRow, double>(row => row.NetConcreteM3),
                new Func<QuantityReportRow, bool>(row => row.HasNetConcreteM3Evidence)
            });
            return result is double value
                ? value
                : throw new InvalidOperationException("QS3D Review summary Sum returned no numeric value.");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "Qs3dReviewWorkbookSummaryNumericSmoke: " + label +
                    " expected " + expected.ToString("R") +
                    " but was " + actual.ToString("R") + ".");
        }
    }
}
