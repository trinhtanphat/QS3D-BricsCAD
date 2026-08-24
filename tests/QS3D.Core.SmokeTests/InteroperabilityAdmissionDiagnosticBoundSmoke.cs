using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Interoperability;

namespace QS3D.Core.SmokeTests
{
    internal static class InteroperabilityAdmissionDiagnosticBoundSmoke
    {
        internal static void Run()
        {
            ExactBoundaryIsAccepted();
            TenThousandAndFirstDiagnosticIsRejectedImmediately();
        }

        private static void ExactBoundaryIsAccepted()
        {
            var source = new StreamingDiagnostics(InteroperabilityAdmission.MaxAdditionalDiagnostics);
            var result = InteroperabilityAdmission.Evaluate(CreateEmptyFactSet(), source);

            Equal(
                InteroperabilityAdmission.MaxAdditionalDiagnostics,
                source.YieldedCount,
                "Admission must enumerate all diagnostics at the exact 10,000-item boundary.");
            Equal(
                InteroperabilityAdmission.MaxAdditionalDiagnostics,
                result.Diagnostics.Count,
                "Admission must preserve all diagnostics at the exact 10,000-item boundary.");
        }

        private static void TenThousandAndFirstDiagnosticIsRejectedImmediately()
        {
            var source = new StreamingDiagnostics(InteroperabilityAdmission.MaxAdditionalDiagnostics + 2);
            var error = Capture<InvalidOperationException>(
                () => InteroperabilityAdmission.Evaluate(CreateEmptyFactSet(), source));

            Equal(
                InteroperabilityAdmission.MaxAdditionalDiagnostics + 1,
                source.YieldedCount,
                "Admission must stop immediately after observing diagnostic 10,001.");
            Contains(
                InteroperabilityAdmission.MaxAdditionalDiagnostics.ToString(),
                error.Message,
                "Oversize admission failure must report the configured diagnostic bound.");
        }

        private static InteroperabilityFactSet CreateEmptyFactSet()
        {
            var provenance = new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.BricsCad,
                InteroperabilityTransport.NativeHost,
                "SMOKE-DOCUMENT",
                null,
                null,
                "SMOKE-BATCH");

            return InteroperabilityFactSet.Create(
                provenance,
                Array.Empty<InteroperabilityElementRecord>());
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException(
                "Expected " + typeof(TException).Name + " was not thrown.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    message + " Expected message fragment='" + expected + "', Actual='" + actual + "'.");
        }

        private sealed class StreamingDiagnostics : IEnumerable<InteroperabilityLossDiagnostic>
        {
            private readonly int _count;

            internal StreamingDiagnostics(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<InteroperabilityLossDiagnostic> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    YieldedCount++;
                    yield return new InteroperabilityLossDiagnostic(
                        "SMOKE_DIAGNOSTIC",
                        InteroperabilityDiagnosticSeverity.Info,
                        "diagnostic " + index);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
