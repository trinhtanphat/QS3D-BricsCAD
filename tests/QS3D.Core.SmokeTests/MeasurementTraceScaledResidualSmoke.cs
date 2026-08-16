using System;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceScaledResidualSmoke
    {
        internal static void Run()
        {
            var adjustments = new[]
            {
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, double.MaxValue, "m2", "large-deduction-a", "SRC-D1"),
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, double.MaxValue, "m2", "large-deduction-b", "SRC-D2"),
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Addition, double.MaxValue, "m2", "large-addition-a", "SRC-A1"),
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Addition, double.MaxValue, "m2", "large-addition-b", "SRC-A2"),
                new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Addition, double.Epsilon, "m2", "tiny-residual", "SRC-A3")
            };

            var trace = new MeasurementTrace(
                "SEM-WALL-TINY",
                "SRC-WALL",
                "NetAreaM2",
                Array.Empty<MeasurementTraceFact>(),
                0d,
                adjustments,
                double.Epsilon,
                "m2",
                "none");

            if (!trace.NetValue.Equals(double.Epsilon))
                throw new InvalidOperationException("Scaled measurement reconciliation must preserve a representable tiny residual.");
        }
    }
}
