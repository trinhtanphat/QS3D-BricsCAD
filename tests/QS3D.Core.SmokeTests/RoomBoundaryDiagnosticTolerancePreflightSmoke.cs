using System;
using System.Collections.Generic;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryDiagnosticTolerancePreflightSmoke
    {
        public static void Run()
        {
            RejectsInvalidToleranceBeforeEnumeration(double.NaN);
            RejectsInvalidToleranceBeforeEnumeration(double.PositiveInfinity);
            RejectsInvalidToleranceBeforeEnumeration(0d);
            RejectsInvalidToleranceBeforeEnumeration(-0.001d);
            ValidEmptyInputRemainsNoInput();
        }

        private static void RejectsInvalidToleranceBeforeEnumeration(double tolerance)
        {
            var service = new RoomBoundaryDiagnosticService();
            try
            {
                service.Analyze(ThrowWhenEnumerated(), tolerance, 0.01d);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (!string.Equals(ex.ParamName, "tolerance", StringComparison.Ordinal))
                    throw new InvalidOperationException("Invalid diagnostic tolerance must fail on tolerance.");
                return;
            }
            catch (EnumerationStartedException ex)
            {
                throw new InvalidOperationException("Invalid diagnostic tolerance enumerated the source before failing.", ex);
            }

            throw new InvalidOperationException("Invalid diagnostic tolerance was accepted.");
        }

        private static void ValidEmptyInputRemainsNoInput()
        {
            var analysis = new RoomBoundaryDiagnosticService().Analyze(Array.Empty<BoundarySegment>(), 0.001d, 0.01d);
            if (analysis.Report.Reason != RoomBoundaryDiagnosticReason.NoInput ||
                analysis.Report.InputSegmentCount != 0 ||
                analysis.Report.CandidateBoundaryCount != 0 ||
                analysis.Report.AcceptedBoundaryCount != 0)
                throw new InvalidOperationException("Valid empty room-boundary diagnostics changed unexpectedly.");
        }

        private static IEnumerable<BoundarySegment> ThrowWhenEnumerated()
        {
            yield return ThrowOnMoveNext();
        }

        private static BoundarySegment ThrowOnMoveNext()
        {
            throw new EnumerationStartedException();
        }

        private sealed class EnumerationStartedException : Exception
        {
        }
    }
}
