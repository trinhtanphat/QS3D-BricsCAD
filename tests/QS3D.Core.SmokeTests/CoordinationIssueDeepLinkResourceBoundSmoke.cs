using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationIssueDeepLinkResourceBoundSmoke
    {
        private const string Prefix = "qs3d://coordination/issue?";

        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactIdentityBoundaryRoundTrips();
            ConstructorRejectsOversizedIdentity();
            ParserRejectsOversizedDecodedIdentity();
            ParserRejectsOversizedEncodedComponent();
            ParserRejectsOversizedRawUri();
        }

        private static void ExactIdentityBoundaryRoundTrips()
        {
            var identity = new string('A', CoordinationIssueDeepLink.MaxIdentityCharacters);
            var link = new CoordinationIssueDeepLink(identity, "DRAWING", "ISSUE", 1L);
            var canonical = link.ToCanonicalUri();
            var parsed = CoordinationIssueDeepLink.Parse(canonical);
            Equal(identity, parsed.ProjectId, "Exact maximum identity did not round-trip.");
            Equal(canonical, parsed.ToCanonicalUri(), "Exact maximum identity changed canonical URI.");
        }

        private static void ConstructorRejectsOversizedIdentity()
        {
            var oversized = new string('A', CoordinationIssueDeepLink.MaxIdentityCharacters + 1);
            RejectArgument(
                () => new CoordinationIssueDeepLink(oversized, "DRAWING", "ISSUE", 1L),
                "maximum length");
        }

        private static void ParserRejectsOversizedDecodedIdentity()
        {
            var oversized = new string('A', CoordinationIssueDeepLink.MaxIdentityCharacters + 1);
            var uri = Prefix + "v=1&project=" + oversized + "&drawing=D&issue=I&revision=1";
            RejectFormat(uri, "decoded size limit");
        }

        private static void ParserRejectsOversizedEncodedComponent()
        {
            var oversized = new string('A', CoordinationIssueDeepLink.MaxEncodedComponentCharacters + 1);
            var uri = Prefix + "v=1&project=" + oversized + "&drawing=D&issue=I&revision=1";
            RejectFormat(uri, "encoded size limit");
        }

        private static void ParserRejectsOversizedRawUri()
        {
            var suffixLength = CoordinationIssueDeepLink.MaxUriCharacters + 1 - Prefix.Length;
            var uri = Prefix + new string('A', suffixLength);
            RejectFormat(uri, "maximum URI length");
        }

        private static void RejectArgument(Func<CoordinationIssueDeepLink> action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("CoordinationIssueDeepLinkResourceBoundSmoke: wrong constructor rejection: " + ex.Message);
                return;
            }
            throw new InvalidOperationException("CoordinationIssueDeepLinkResourceBoundSmoke: oversized constructor identity was accepted.");
        }

        private static void RejectFormat(string uri, string expectedMessage)
        {
            try
            {
                CoordinationIssueDeepLink.Parse(uri);
            }
            catch (FormatException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("CoordinationIssueDeepLinkResourceBoundSmoke: wrong parser rejection: " + ex.Message);
                if (CoordinationIssueDeepLink.TryParse(uri, out var parsed) || parsed != null)
                    throw new InvalidOperationException("CoordinationIssueDeepLinkResourceBoundSmoke: TryParse accepted rejected oversized input.");
                return;
            }
            throw new InvalidOperationException("CoordinationIssueDeepLinkResourceBoundSmoke: oversized parser input was accepted.");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("CoordinationIssueDeepLinkResourceBoundSmoke: " + message);
        }
    }
}
