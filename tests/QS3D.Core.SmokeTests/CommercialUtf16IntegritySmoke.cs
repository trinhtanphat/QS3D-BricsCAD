using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialUtf16IntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MalformedTokensFailClosed();
            MalformedCanonicalTextFailsClosed();
            ValidSupplementaryUnicodeRemainsAccepted();
        }

        private static void MalformedTokensFailClosed()
        {
            ExpectArgument(
                () => new CommercialRevisionRef("rate\uD800", "SOURCE-1", "REV-1"),
                "well-formed UTF-16");
            ExpectArgument(
                () => new CommercialRevisionRef("rate\uDC00", "SOURCE-1", "REV-1"),
                "well-formed UTF-16");
            ExpectArgument(
                () => new CommercialRevisionRef("rate\uD800x", "SOURCE-1", "REV-1"),
                "well-formed UTF-16");
        }

        private static void MalformedCanonicalTextFailsClosed()
        {
            ExpectArgument(
                () => Audit("reason\uD800"),
                "well-formed UTF-16");
            ExpectArgument(
                () => Audit("reason\uDC00"),
                "well-formed UTF-16");
        }

        private static void ValidSupplementaryUnicodeRemainsAccepted()
        {
            const string rocket = "\uD83D\uDE80";
            var revision = new CommercialRevisionRef("rate-" + rocket, "SOURCE-1", "REV-1");
            Equal("rate-" + rocket, revision.SourceKind, "Valid surrogate pair token must be preserved exactly.");

            var audit = Audit("approved " + rocket);
            Equal("approved " + rocket, audit.Reason, "Valid surrogate pair canonical text must be preserved exactly.");
        }

        private static CommercialAuditRecord Audit(string reason)
        {
            return new CommercialAuditRecord(
                "EVENT-UTF16",
                "estimate-line",
                "LINE-1",
                "rate-reviewed",
                "tester",
                new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc),
                reason,
                "CORR-UTF16",
                "before",
                "after",
                Array.Empty<CommercialRevisionRef>());
        }

        private static void ExpectArgument(Action action, string expectedMessageFragment)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("Unexpected commercial UTF-16 diagnostic: " + ex.Message);
            }

            throw new InvalidOperationException("Expected malformed commercial UTF-16 input to fail closed.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }
    }
}
