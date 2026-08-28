using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

namespace QS3D.Core.Coordination
{
    public enum CoordinationIssueDeepLinkValidationStatus
    {
        Valid = 0,
        ProjectMismatch = 1,
        DrawingMismatch = 2,
        RevisionMismatch = 3,
        IssueNotFound = 4
    }

    public sealed class CoordinationIssueDeepLinkValidationResult
    {
        internal CoordinationIssueDeepLinkValidationResult(
            CoordinationIssueDeepLinkValidationStatus status,
            CoordinationIssue? issue)
        {
            Status = status;
            Issue = issue;
        }

        public CoordinationIssueDeepLinkValidationStatus Status { get; }
        public CoordinationIssue? Issue { get; }
        public bool IsActionable => Status == CoordinationIssueDeepLinkValidationStatus.Valid && Issue != null;
    }

    /// <summary>
    /// Portable, host-neutral reference to one persisted coordination issue revision.
    /// Native handles/ObjectIds are intentionally excluded: live CAD references are resolved only
    /// after this stable identity is validated against the current persistence snapshot.
    /// </summary>
    public sealed class CoordinationIssueDeepLink
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxIdentityCharacters = 4096;
        public const int MaxEncodedComponentCharacters = MaxIdentityCharacters * 9;
        public const int MaxUriCharacters = 128 * 1024;
        private const string Prefix = "qs3d://coordination/issue?";
        private static readonly string[] RequiredKeys = { "v", "project", "drawing", "issue", "revision" };

        public CoordinationIssueDeepLink(
            string projectId,
            string drawingFingerprint,
            string issueId,
            long revision)
        {
            ProjectId = RequiredToken(projectId, nameof(projectId));
            DrawingFingerprint = RequiredToken(drawingFingerprint, nameof(drawingFingerprint));
            IssueId = RequiredToken(issueId, nameof(issueId));
            if (revision <= 0L)
                throw new ArgumentOutOfRangeException(nameof(revision), "Coordination persistence revision must be positive.");
            Revision = revision;
        }

        public int SchemaVersion => CurrentSchemaVersion;
        public string ProjectId { get; }
        public string DrawingFingerprint { get; }
        public string IssueId { get; }
        public long Revision { get; }

        public string ToCanonicalUri()
        {
            var builder = new StringBuilder(Prefix);
            Append(builder, "v", CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "project", ProjectId, true);
            Append(builder, "drawing", DrawingFingerprint, true);
            Append(builder, "issue", IssueId, true);
            Append(builder, "revision", Revision.ToString(CultureInfo.InvariantCulture), true);
            return builder.ToString();
        }

        public override string ToString() => ToCanonicalUri();

        public CoordinationIssueDeepLinkValidationResult Validate(CoordinationIssuePersistenceSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!string.Equals(snapshot.ProjectId, ProjectId, StringComparison.Ordinal))
                return Result(CoordinationIssueDeepLinkValidationStatus.ProjectMismatch, null);
            if (!string.Equals(snapshot.DrawingFingerprint, DrawingFingerprint, StringComparison.Ordinal))
                return Result(CoordinationIssueDeepLinkValidationStatus.DrawingMismatch, null);
            if (snapshot.Revision != Revision)
                return Result(CoordinationIssueDeepLinkValidationStatus.RevisionMismatch, null);

            var issue = snapshot.Find(IssueId);
            return issue == null
                ? Result(CoordinationIssueDeepLinkValidationStatus.IssueNotFound, null)
                : Result(CoordinationIssueDeepLinkValidationStatus.Valid, issue);
        }

        public static CoordinationIssueDeepLink Parse(string uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (uri.Length > MaxUriCharacters)
                throw new FormatException("Coordination deep-link exceeds the maximum URI length.");
            if (!uri.StartsWith(Prefix, StringComparison.Ordinal))
                throw new FormatException("Coordination deep-link must use canonical qs3d://coordination/issue path.");
            if (uri.Length == Prefix.Length)
                throw new FormatException("Coordination deep-link query is required.");
            if (uri.IndexOf('#') >= 0)
                throw new FormatException("Coordination deep-link fragments are not supported.");

            var rawQuery = uri.Substring(Prefix.Length);
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            var segments = rawQuery.Split('&');
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (segment.Length == 0)
                    throw new FormatException("Coordination deep-link contains an empty query segment.");
                var equals = segment.IndexOf('=');
                if (equals <= 0 || equals != segment.LastIndexOf('='))
                    throw new FormatException("Coordination deep-link query fields must contain exactly one '=' separator.");

                var key = segment.Substring(0, equals);
                if (!RequiredKeys.Contains(key, StringComparer.Ordinal))
                    throw new FormatException("Coordination deep-link contains an unknown query key: " + key + ".");
                if (fields.ContainsKey(key))
                    throw new FormatException("Coordination deep-link contains duplicate query key: " + key + ".");

                var encoded = segment.Substring(equals + 1);
                if (encoded.Length > MaxEncodedComponentCharacters)
                    throw new FormatException("Coordination deep-link query value exceeds the encoded size limit: " + key + ".");
                ValidatePercentEncoding(encoded, key);
                string decoded;
                try
                {
                    decoded = Uri.UnescapeDataString(encoded);
                }
                catch (UriFormatException ex)
                {
                    throw new FormatException("Coordination deep-link query value is not valid percent-encoding: " + key + ".", ex);
                }
                if (decoded.Length > MaxIdentityCharacters)
                    throw new FormatException("Coordination deep-link query value exceeds the decoded size limit: " + key + ".");
                if (decoded.Any(char.IsControl))
                    throw new FormatException("Coordination deep-link query value contains control characters: " + key + ".");
                fields.Add(key, decoded);
            }

            for (var i = 0; i < RequiredKeys.Length; i++)
            {
                if (!fields.ContainsKey(RequiredKeys[i]))
                    throw new FormatException("Coordination deep-link is missing query key: " + RequiredKeys[i] + ".");
            }

            if (!string.Equals(fields["v"], CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("Coordination deep-link schema version is unsupported: " + fields["v"] + ".");

            if (!long.TryParse(fields["revision"], NumberStyles.None, CultureInfo.InvariantCulture, out var revision) || revision <= 0L)
                throw new FormatException("Coordination deep-link revision must be a positive integer.");

            try
            {
                return new CoordinationIssueDeepLink(fields["project"], fields["drawing"], fields["issue"], revision);
            }
            catch (ArgumentException ex)
            {
                throw new FormatException("Coordination deep-link identity is invalid.", ex);
            }
        }

        public static bool TryParse(string uri, out CoordinationIssueDeepLink? deepLink)
        {
            try
            {
                deepLink = Parse(uri);
                return true;
            }
            catch (ArgumentException)
            {
                deepLink = null;
                return false;
            }
            catch (FormatException)
            {
                deepLink = null;
                return false;
            }
        }

        private static CoordinationIssueDeepLinkValidationResult Result(
            CoordinationIssueDeepLinkValidationStatus status,
            CoordinationIssue? issue)
        {
            return new CoordinationIssueDeepLinkValidationResult(status, issue);
        }

        private static void Append(StringBuilder builder, string key, string value, bool separator)
        {
            if (separator) builder.Append('&');
            builder.Append(key).Append('=').Append(Uri.EscapeDataString(value));
        }

        private static string RequiredToken(string value, string parameterName)
        {
            var raw = value ?? string.Empty;
            if (raw.Length > MaxIdentityCharacters)
                throw new ArgumentException("Coordination deep-link identity exceeds the maximum length.", parameterName);
            if (raw.Any(char.IsControl))
                throw new ArgumentException("Control characters are not allowed.", parameterName);
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
                throw new ArgumentException("Value is required.", parameterName);
            if (!string.Equals(raw, trimmed, StringComparison.Ordinal))
                throw new ArgumentException("Coordination deep-link identity must not contain leading or trailing whitespace.", parameterName);
            return raw;
        }

        private static void ValidatePercentEncoding(string encoded, string key)
        {
            for (var i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] != '%') continue;
                if (i + 2 >= encoded.Length || !IsHex(encoded[i + 1]) || !IsHex(encoded[i + 2]))
                    throw new FormatException("Coordination deep-link contains malformed percent-encoding in query key: " + key + ".");
                i += 2;
            }
        }

        private static bool IsHex(char value)
        {
            return (value >= '0' && value <= '9') ||
                   (value >= 'a' && value <= 'f') ||
                   (value >= 'A' && value <= 'F');
        }
    }
}
