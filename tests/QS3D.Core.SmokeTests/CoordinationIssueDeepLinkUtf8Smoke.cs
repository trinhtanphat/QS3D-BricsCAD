using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationIssueDeepLinkUtf8Smoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectInvalidUtf8PercentOctets();
            RejectNonCanonicalPercentAliases();
            ValidSupplementaryUtf8RoundTrips();
            ValidReservedEscapesRoundTripCanonically();
        }

        private static void RejectInvalidUtf8PercentOctets()
        {
            Reject("%C0%AF", "overlong two-byte sequence");
            Reject("%80", "isolated continuation byte");
            Reject("%E2%82", "truncated three-byte sequence");
            Reject("%ED%A0%80", "UTF-8 surrogate encoding");
            Reject("%F4%90%80%80", "code point above U+10FFFF");
            Reject("%F0%80%80%AF", "overlong four-byte sequence");
        }

        private static void RejectNonCanonicalPercentAliases()
        {
            Reject("%41", "percent-encoded unreserved ASCII");
            Reject("PROJECT%2dA", "lower-case percent hex alias");
            Reject("PROJECT%2DA", "percent-encoded unreserved hyphen");
            Reject("PROJECT%2fA", "lower-case reserved percent hex alias");
            Reject("PROJECT-%f0%9f%98%80", "lower-case supplementary UTF-8 alias");

            var canonical = new CoordinationIssueDeepLink("PROJECT-😀", "DRAWING", "ISSUE", 7L).ToCanonicalUri();
            var lowerCaseAlias = canonical.Replace("%F0%9F%98%80", "%f0%9f%98%80");
            RejectUri(lowerCaseAlias, "decoded-equivalent lower-case percent URI");
        }

        private static void ValidSupplementaryUtf8RoundTrips()
        {
            const string project = "PROJECT-😀";
            var link = new CoordinationIssueDeepLink(project, "DRAWING", "ISSUE", 7L);
            var canonical = link.ToCanonicalUri();
            if (canonical.IndexOf("%F0%9F%98%80", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("CoordinationIssueDeepLinkUtf8Smoke: supplementary-plane identity was not encoded as UTF-8 percent octets.");

            var parsed = CoordinationIssueDeepLink.Parse(canonical);
            if (!string.Equals(project, parsed.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("CoordinationIssueDeepLinkUtf8Smoke: valid supplementary UTF-8 did not round-trip.");
            if (!string.Equals(canonical, parsed.ToCanonicalUri(), StringComparison.Ordinal))
                throw new InvalidOperationException("CoordinationIssueDeepLinkUtf8Smoke: valid supplementary UTF-8 lost canonical form.");
        }

        private static void ValidReservedEscapesRoundTripCanonically()
        {
            const string project = "PROJECT/A?B";
            var link = new CoordinationIssueDeepLink(project, "DRAWING", "ISSUE", 9L);
            var canonical = link.ToCanonicalUri();
            if (canonical.IndexOf("PROJECT%2FA%3FB", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("CoordinationIssueDeepLinkUtf8Smoke: reserved identity characters were not canonically escaped.");

            var parsed = CoordinationIssueDeepLink.Parse(canonical);
            if (!string.Equals(project, parsed.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(canonical, parsed.ToCanonicalUri(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CoordinationIssueDeepLinkUtf8Smoke: canonical reserved escapes did not round-trip exactly.");
            }
        }

        private static void Reject(string hostileProjectEncoding, string label)
        {
            RejectUri("qs3d://coordination/issue?v=1&project=" + hostileProjectEncoding + "&drawing=D&issue=I&revision=1", label);
        }

        private static void RejectUri(string uri, string label)
        {
            try
            {
                CoordinationIssueDeepLink.Parse(uri);
            }
            catch (FormatException)
            {
                if (CoordinationIssueDeepLink.TryParse(uri, out var parsed) || parsed != null)
                    throw new InvalidOperationException("CoordinationIssueDeepLinkUtf8Smoke: TryParse accepted " + label + ".");
                return;
            }

            throw new InvalidOperationException("CoordinationIssueDeepLinkUtf8Smoke: parser accepted " + label + ".");
        }
    }
}
