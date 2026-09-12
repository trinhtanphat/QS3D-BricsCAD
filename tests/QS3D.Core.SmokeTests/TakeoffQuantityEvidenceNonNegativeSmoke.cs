using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class TakeoffQuantityEvidenceNonNegativeSmoke
    {
        internal static void Run()
        {
            RejectsNegativeEvidence();
            AcceptsZeroAndPositiveEvidence();
            PreservesNegativeRevisionDelta();
        }

        private static void RejectsNegativeEvidence()
        {
            Capture<ArgumentOutOfRangeException>(() => Evidence("NEG", -0.001d));
        }

        private static void AcceptsZeroAndPositiveEvidence()
        {
            Equal(0d, Evidence("ZERO", 0d).Quantity, "Zero-valued takeoff evidence must remain valid.");
            Equal(12.5d, Evidence("POS", 12.5d).Quantity, "Positive takeoff evidence must remain valid.");
        }

        private static void PreservesNegativeRevisionDelta()
        {
            var previous = Evidence("M-1", 12d);
            var current = Evidence("M-1", 7d);
            var delta = new RevisionMarkupDelta2D("M-1", RevisionMarkupChangeKind.Changed, previous, current);
            Equal(-5d, delta.QuantityDelta, "Revision decreases must remain representable as signed deltas.");
        }

        private static TakeoffQuantityEvidence2D Evidence(string markupId, double quantity)
        {
            return new TakeoffQuantityEvidence2D(
                markupId,
                "A101",
                "R1",
                "A101.pdf",
                "handle-" + markupId,
                "03-CONCRETE",
                "L01",
                "TAKEOFF",
                quantity,
                "m3");
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

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class TakeoffQuantityEvidenceNonNegativeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TakeoffQuantityEvidenceNonNegativeSmoke.Run();
        }
    }
}
