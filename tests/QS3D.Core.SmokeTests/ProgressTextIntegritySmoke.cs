using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;
using QS3D.Core.Progress;

namespace QS3D.Core.SmokeTests
{
    internal static class ProgressTextIntegritySmoke
    {
        internal static void Run()
        {
            IdentityFailsClosedOnMalformedUtf16();
            TextFailsClosedOnMalformedUtf16();
            ExistingCanonicalRulesRemainClosed();
            ValidSupplementaryUnicodeIsPreserved();
        }

        private static void IdentityFailsClosedOnMalformedUtf16()
        {
            AssertArgument(
                "snapshotId",
                () => Snapshot("PROGRESS" + LoneHighSurrogate()),
                "malformed UTF-16");
            AssertArgument(
                "snapshotId",
                () => Snapshot(LoneLowSurrogate() + "PROGRESS"),
                "malformed UTF-16");
        }

        private static void TextFailsClosedOnMalformedUtf16()
        {
            AssertArgument(
                "evidenceReference",
                () => Measurement("Evidence" + LoneHighSurrogate()),
                "malformed UTF-16");
            AssertArgument(
                "evidenceReference",
                () => Measurement(LoneLowSurrogate() + "Evidence"),
                "malformed UTF-16");
        }

        private static void ExistingCanonicalRulesRemainClosed()
        {
            AssertArgument("snapshotId", () => Snapshot(" Leading"), "surrounding whitespace");
            AssertArgument("evidenceReference", () => Measurement("Bad\tControl"), "control characters");
        }

        private static void ValidSupplementaryUnicodeIsPreserved()
        {
            const string snapshotId = "PROGRESS-😀";
            const string evidence = "Biên bản 😀 梁";

            var snapshot = Snapshot(snapshotId);
            var measurement = Measurement(evidence);

            Equal(snapshotId, snapshot.SnapshotId, "Progress snapshot identity must preserve valid supplementary Unicode exactly.");
            Equal(evidence, measurement.EvidenceReference, "Progress evidence text must preserve valid supplementary Unicode exactly.");
        }

        private static ProgressSnapshot Snapshot(string snapshotId) =>
            new ProgressSnapshot(
                snapshotId,
                1,
                new ProjectDate(2026, 8, 27),
                new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc),
                Array.Empty<ProgressMeasurement>());

        private static ProgressMeasurement Measurement(string evidenceReference)
        {
            var trace = new MeasurementTrace(
                "ELEMENT-1",
                "SOURCE-1",
                "AreaM2",
                Array.Empty<MeasurementTraceFact>(),
                1d,
                Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                "m2",
                "none");

            return new ProgressMeasurement(
                "PM-1",
                new ProjectDate(2026, 8, 27),
                trace,
                1m,
                1m,
                evidenceReference: evidenceReference);
        }

        private static string LoneHighSurrogate() => new string(new[] { '\uD800' });
        private static string LoneLowSurrogate() => new string(new[] { '\uDC00' });

        private static void AssertArgument(string parameterName, Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                Equal(parameterName, ex.ParamName, "Progress text rejection must identify the exact parameter.");
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "Progress text rejection did not report the expected invariant. Expected fragment='" +
                        expectedMessage + "', actual='" + ex.Message + "'.");
                return;
            }

            throw new InvalidOperationException("Progress canonical-text boundary accepted malformed or non-canonical input.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class ProgressTextIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProgressTextIntegritySmoke.Run();
        }
    }
}
