using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotDeltaPrecisionSmoke
    {
        internal static void Run()
        {
            IncreasingPrecisionCollapseFailsClosed();
            DecreasingPrecisionCollapseFailsClosed();
            ExactFiniteDeltaRemainsStable();
            ZeroEndpointDeltasRemainStable();
        }

        private static void IncreasingPrecisionCollapseFailsClosed()
        {
            var error = Capture<InvalidOperationException>(() => Compare(1d, 1e16d));
            Contains(
                "lost a finite non-zero endpoint",
                error.Message,
                "Measurement snapshot delta must reject a previous finite non-zero endpoint swallowed by subtraction.");
        }

        private static void DecreasingPrecisionCollapseFailsClosed()
        {
            var error = Capture<InvalidOperationException>(() => Compare(1e16d, 1d));
            Contains(
                "lost a finite non-zero endpoint",
                error.Message,
                "Measurement snapshot delta must reject a current finite non-zero endpoint swallowed by subtraction.");
        }

        private static void ExactFiniteDeltaRemainsStable()
        {
            var delta = Compare(5d, 12d);
            Equal(1, delta.Lines.Count, "Exact finite snapshot delta line count changed.");
            var line = delta.Lines[0];
            Equal(MeasurementSnapshotChangeKind.Changed, line.ChangeKind, "Exact finite snapshot change kind changed.");
            Equal(5d, line.PreviousValue, "Exact finite snapshot previous value changed.");
            Equal(12d, line.CurrentValue, "Exact finite snapshot current value changed.");
            Equal(7d, line.DeltaValue, "Exact finite snapshot delta changed.");
        }

        private static void ZeroEndpointDeltasRemainStable()
        {
            var addedValue = Compare(0d, 12d).Lines[0];
            Equal(12d, addedValue.DeltaValue, "Zero previous endpoint must retain the exact current value as delta.");

            var removedValue = Compare(12d, 0d).Lines[0];
            Equal(-12d, removedValue.DeltaValue, "Zero current endpoint must retain the exact negated previous value as delta.");
        }

        private static MeasurementSnapshotDelta Compare(double previousValue, double currentValue)
        {
            var before = new MeasurementSnapshot(new[] { Trace(previousValue) });
            var after = new MeasurementSnapshot(new[] { Trace(currentValue) });
            return new MeasurementSnapshotDelta(before, after);
        }

        private static MeasurementTrace Trace(double value)
        {
            return new MeasurementTrace(
                "SEM-PRECISION",
                "SRC-PRECISION",
                "QTY-PRECISION",
                Array.Empty<MeasurementTraceFact>(),
                value,
                Array.Empty<MeasurementTraceAdjustment>(),
                value,
                "m",
                "none");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class MeasurementSnapshotDeltaPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementSnapshotDeltaPrecisionSmoke.Run();
        }
    }
}
