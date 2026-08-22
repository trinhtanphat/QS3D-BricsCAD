using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationIssueDeepLinkSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            UnicodeRoundTripIsDeterministic();
            SnapshotValidationFailsClosed();
            MalformedLinksFailClosed();
            ConstructorIdentityControlsFailClosed();
            QueryOrderCanCanonicalize();
        }

        private static void UnicodeRoundTripIsDeterministic()
        {
            var link = new CoordinationIssueDeepLink("Dự án A", "Bản-vẽ-α", "issue-đụng-001", 17L);
            var first = link.ToCanonicalUri();
            var second = link.ToCanonicalUri();
            Equal(first, second, "Canonical deep-link serialization changed for identical identity.");
            if (!first.StartsWith("qs3d://coordination/issue?v=1&project=", StringComparison.Ordinal))
                throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: canonical prefix/order changed.");
            if (first.IndexOf("Dự án", StringComparison.Ordinal) >= 0 || first.IndexOf("đụng", StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: Unicode identity was not URI escaped.");

            var parsed = CoordinationIssueDeepLink.Parse(first);
            Equal("Dự án A", parsed.ProjectId, "ProjectId changed across deep-link round-trip.");
            Equal("Bản-vẽ-α", parsed.DrawingFingerprint, "DrawingFingerprint changed across deep-link round-trip.");
            Equal("issue-đụng-001", parsed.IssueId, "IssueId changed across deep-link round-trip.");
            Equal(17L, parsed.Revision, "Revision changed across deep-link round-trip.");
            Equal(first, parsed.ToCanonicalUri(), "Canonical deep-link did not round-trip exactly.");
        }

        private static void SnapshotValidationFailsClosed()
        {
            var project = CreateProject();
            var issue = CreateIssue();
            CoordinationIssuePersistence.Save(project, new[] { issue }, 9L);
            var snapshot = CoordinationIssuePersistence.Load(project)
                ?? throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: expected persisted snapshot.");

            var validLink = new CoordinationIssueDeepLink(project.ProjectId, project.DrawingFingerprint, issue.IssueId, 9L);
            var valid = validLink.Validate(snapshot);
            if (!valid.IsActionable || valid.Status != CoordinationIssueDeepLinkValidationStatus.Valid || valid.Issue == null)
                throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: exact persisted identity did not validate.");
            Equal(issue.IssueId, valid.Issue.IssueId, "Validated deep-link resolved the wrong issue.");

            Blocked(
                new CoordinationIssueDeepLink("other-project", project.DrawingFingerprint, issue.IssueId, 9L).Validate(snapshot),
                CoordinationIssueDeepLinkValidationStatus.ProjectMismatch);
            Blocked(
                new CoordinationIssueDeepLink(project.ProjectId, "OTHER-DRAWING", issue.IssueId, 9L).Validate(snapshot),
                CoordinationIssueDeepLinkValidationStatus.DrawingMismatch);
            Blocked(
                new CoordinationIssueDeepLink(project.ProjectId, project.DrawingFingerprint, issue.IssueId, 10L).Validate(snapshot),
                CoordinationIssueDeepLinkValidationStatus.RevisionMismatch);
            Blocked(
                new CoordinationIssueDeepLink(project.ProjectId, project.DrawingFingerprint, "missing-issue", 9L).Validate(snapshot),
                CoordinationIssueDeepLinkValidationStatus.IssueNotFound);

            var uri = validLink.ToCanonicalUri();
            if (uri.IndexOf("AB", StringComparison.OrdinalIgnoreCase) >= 0 || uri.IndexOf("CD", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: native CAD handles leaked into portable deep-link authority.");
        }

        private static void MalformedLinksFailClosed()
        {
            Reject("QS3D://coordination/issue?v=1&project=P&drawing=D&issue=I&revision=1");
            Reject("qs3d://coordination/other?v=1&project=P&drawing=D&issue=I&revision=1");
            Reject("qs3d://coordination/issue?v=1&project=P&drawing=D&revision=1");
            Reject("qs3d://coordination/issue?v=1&project=P&drawing=D&issue=I&issue=J&revision=1");
            Reject("qs3d://coordination/issue?v=1&project=P&drawing=D&issue=I&revision=1&handle=AB");
            Reject("qs3d://coordination/issue?v=1&project=P%ZZ&drawing=D&issue=I&revision=1");
            Reject("qs3d://coordination/issue?v=1&project=P%0A&drawing=D&issue=I&revision=1");
            Reject("qs3d://coordination/issue?v=1&project=P&drawing=D&issue=I&revision=0");
            Reject("qs3d://coordination/issue?v=2&project=P&drawing=D&issue=I&revision=1");
            Reject("qs3d://coordination/issue?v=1&project=P&drawing=D&issue=I&revision=1#fragment");
        }

        private static void ConstructorIdentityControlsFailClosed()
        {
            var controlPadded = new[]
            {
                "\tPROJECT",
                "PROJECT\t",
                "PRO\tJECT",
                "\rPROJECT",
                "PROJECT\r",
                "\nPROJECT",
                "PROJECT\n"
            };

            foreach (var malformed in controlPadded)
            {
                RejectConstructor(() => new CoordinationIssueDeepLink(malformed, "DRAWING", "ISSUE", 1L));
                RejectConstructor(() => new CoordinationIssueDeepLink("PROJECT", malformed, "ISSUE", 1L));
                RejectConstructor(() => new CoordinationIssueDeepLink("PROJECT", "DRAWING", malformed, 1L));
            }

            var spaced = new CoordinationIssueDeepLink("  PROJECT  ", "  DRAWING  ", "  ISSUE  ", 1L);
            Equal("PROJECT", spaced.ProjectId, "Ordinary project-ID surrounding spaces stopped canonicalizing.");
            Equal("DRAWING", spaced.DrawingFingerprint, "Ordinary drawing surrounding spaces stopped canonicalizing.");
            Equal("ISSUE", spaced.IssueId, "Ordinary issue-ID surrounding spaces stopped canonicalizing.");
            Equal(
                "qs3d://coordination/issue?v=1&project=PROJECT&drawing=DRAWING&issue=ISSUE&revision=1",
                spaced.ToCanonicalUri(),
                "Control-free surrounding-space canonical URI changed.");
        }

        private static void QueryOrderCanCanonicalize()
        {
            var reordered = "qs3d://coordination/issue?issue=I%20A&revision=4&drawing=D%2F1&v=1&project=P%201";
            var parsed = CoordinationIssueDeepLink.Parse(reordered);
            var canonical = parsed.ToCanonicalUri();
            Equal(
                "qs3d://coordination/issue?v=1&project=P%201&drawing=D%2F1&issue=I%20A&revision=4",
                canonical,
                "Reordered deep-link did not normalize to canonical field order.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("project-deeplink-1", "Deep Link Smoke")
            {
                DrawingFingerprint = "DRAWING-DEEPLINK-A"
            };
            project.Elements.Add(new ProjectElement("semantic-left", ElementCategory.Beam) { DrawingFingerprint = project.DrawingFingerprint });
            project.Elements.Add(new ProjectElement("semantic-right", ElementCategory.StructuralWall) { DrawingFingerprint = project.DrawingFingerprint });
            return project;
        }

        private static CoordinationIssue CreateIssue()
        {
            var drawingId = new DrawingId(Guid.Parse("3b4a66dd-9a53-4aa1-86fc-32b42c2f7338"));
            var created = new DateTime(2026, 8, 22, 2, 0, 0, DateTimeKind.Utc);
            return new CoordinationIssue(
                "issue-deeplink-001",
                CoordinationIssueKind.ExactHardClash,
                CoordinationIssueSeverity.High,
                "Deep-link hard clash",
                "semantic-left",
                "semantic-right",
                new CadReference(drawingId, new CadHandle("00ab")),
                new CadReference(drawingId, new CadHandle("000cd")),
                "Structure",
                "Beam/Wall",
                "Supply",
                "Level-01",
                0d,
                created);
        }

        private static void Reject(string uri)
        {
            try
            {
                CoordinationIssueDeepLink.Parse(uri);
            }
            catch (FormatException)
            {
                if (CoordinationIssueDeepLink.TryParse(uri, out var rejected) || rejected != null)
                    throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: TryParse accepted a rejected deep-link.");
                return;
            }
            throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: malformed deep-link was accepted: " + uri);
        }

        private static void RejectConstructor(Func<CoordinationIssueDeepLink> create)
        {
            try
            {
                create();
            }
            catch (ArgumentException)
            {
                return;
            }
            throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: raw constructor identity control was accepted.");
        }

        private static void Blocked(
            CoordinationIssueDeepLinkValidationResult result,
            CoordinationIssueDeepLinkValidationStatus expected)
        {
            if (result.IsActionable || result.Issue != null || result.Status != expected)
                throw new InvalidOperationException(
                    "CoordinationIssueDeepLinkSmoke: expected blocked status " + expected + ", got " + result.Status + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: " + message + " Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Equal(long expected, long actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException("CoordinationIssueDeepLinkSmoke: " + message + " Expected " + expected + ", got " + actual + ".");
        }
    }
}
