using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

namespace QS3D.Core.Coordination
{
    public enum CoordinationRelinkStatus
    {
        ReadyForHostValidation = 0,
        ProjectMismatch = 1,
        DrawingMismatch = 2,
        IssueNotFound = 3,
        MissingLeftSemantic = 4,
        MissingRightSemantic = 5,
        MissingBothSemantics = 6,
        MissingLeftCadReference = 7,
        MissingRightCadReference = 8,
        MissingBothCadReferences = 9,
        StaleLeftCadReference = 10,
        StaleRightCadReference = 11,
        StaleBothCadReferences = 12,
        Relinked = 13
    }

    public sealed class CoordinationRelinkResult
    {
        internal CoordinationRelinkResult(CoordinationRelinkStatus status, CoordinationIssue? issue)
        {
            Status = status;
            Issue = issue;
        }

        public CoordinationRelinkStatus Status { get; }
        public CoordinationIssue? Issue { get; }
        public bool IsActionable => Status == CoordinationRelinkStatus.Relinked;
    }

    public sealed class CoordinationIssuePersistenceSnapshot
    {
        private readonly IReadOnlyList<CoordinationIssue> _issues;

        internal CoordinationIssuePersistenceSnapshot(long revision, string projectId, string drawingFingerprint, IReadOnlyList<CoordinationIssue> issues)
        {
            Revision = revision;
            ProjectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
            DrawingFingerprint = drawingFingerprint ?? throw new ArgumentNullException(nameof(drawingFingerprint));
            _issues = issues ?? throw new ArgumentNullException(nameof(issues));
        }

        public long Revision { get; }
        public string ProjectId { get; }
        public string DrawingFingerprint { get; }
        public IReadOnlyList<CoordinationIssue> Issues => _issues;

        public CoordinationIssue? Find(string issueId)
        {
            var normalized = (issueId ?? string.Empty).Trim();
            if (normalized.Length == 0) return null;
            CoordinationIssue? match = null;
            for (var i = 0; i < _issues.Count; i++)
            {
                if (!string.Equals(_issues[i].IssueId, normalized, StringComparison.OrdinalIgnoreCase)) continue;
                if (match != null) throw new InvalidOperationException("Coordination persistence snapshot contains duplicate issue id: " + normalized + ".");
                match = _issues[i];
            }
            return match;
        }

        public CoordinationRelinkResult EvaluateRelink(
            ProjectState project,
            string issueId,
            Func<CadReference, bool>? isLiveCadReference = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!string.Equals(project.ProjectId, ProjectId, StringComparison.Ordinal))
                return new CoordinationRelinkResult(CoordinationRelinkStatus.ProjectMismatch, null);
            if (!string.Equals(project.DrawingFingerprint, DrawingFingerprint, StringComparison.Ordinal))
                return new CoordinationRelinkResult(CoordinationRelinkStatus.DrawingMismatch, null);

            var issue = Find(issueId);
            if (issue == null) return new CoordinationRelinkResult(CoordinationRelinkStatus.IssueNotFound, null);

            var leftSemantic = project.FindElement(issue.LeftSemanticId) != null;
            var rightSemantic = project.FindElement(issue.RightSemanticId) != null;
            if (!leftSemantic && !rightSemantic) return new CoordinationRelinkResult(CoordinationRelinkStatus.MissingBothSemantics, issue);
            if (!leftSemantic) return new CoordinationRelinkResult(CoordinationRelinkStatus.MissingLeftSemantic, issue);
            if (!rightSemantic) return new CoordinationRelinkResult(CoordinationRelinkStatus.MissingRightSemantic, issue);

            var leftReference = issue.LeftCadReference;
            var rightReference = issue.RightCadReference;
            if (!leftReference.HasValue && !rightReference.HasValue)
                return new CoordinationRelinkResult(CoordinationRelinkStatus.MissingBothCadReferences, issue);
            if (!leftReference.HasValue)
                return new CoordinationRelinkResult(CoordinationRelinkStatus.MissingLeftCadReference, issue);
            if (!rightReference.HasValue)
                return new CoordinationRelinkResult(CoordinationRelinkStatus.MissingRightCadReference, issue);
            if (isLiveCadReference == null)
                return new CoordinationRelinkResult(CoordinationRelinkStatus.ReadyForHostValidation, issue);

            var leftLive = isLiveCadReference(leftReference.Value);
            var rightLive = isLiveCadReference(rightReference.Value);
            if (!leftLive && !rightLive) return new CoordinationRelinkResult(CoordinationRelinkStatus.StaleBothCadReferences, issue);
            if (!leftLive) return new CoordinationRelinkResult(CoordinationRelinkStatus.StaleLeftCadReference, issue);
            if (!rightLive) return new CoordinationRelinkResult(CoordinationRelinkStatus.StaleRightCadReference, issue);
            return new CoordinationRelinkResult(CoordinationRelinkStatus.Relinked, issue);
        }
    }

    public static class CoordinationIssuePersistence
    {
        public static void Save(ProjectState project, IEnumerable<CoordinationIssue> issues, long revision)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            if (revision <= 0L) throw new ArgumentOutOfRangeException(nameof(revision), "Coordination persistence revision must be positive.");
            if (string.IsNullOrWhiteSpace(project.DrawingFingerprint))
                throw new InvalidOperationException("A canonical drawing fingerprint is required before coordination issues can be persisted.");

            var payload = CoordinationIssuePersistenceCodec.Value(project.ProjectId, project.DrawingFingerprint, revision, issues);
            project.Metadata[CoordinationIssuePersistenceCodec.IssuesKey] = payload;
        }

        public static CoordinationIssuePersistenceSnapshot? Load(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var snapshot = CoordinationIssuePersistenceCodec.Read(project.Metadata);
            if (snapshot == null) return null;
            if (!string.Equals(snapshot.ProjectId, project.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination issue persistence belongs to a different project id.");
            if (!string.Equals(snapshot.DrawingFingerprint, project.DrawingFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination issue persistence belongs to a different drawing fingerprint.");
            return snapshot;
        }
    }

    internal static class CoordinationIssuePersistenceCodec
    {
        internal const string ReservedRoot = "QS3D.Coordination.";
        internal const string Prefix = "QS3D.Coordination.v1.";
        internal const string IssuesKey = Prefix + "Issues";
        private const string PayloadVersion = "1";
        private const int MaxPayloadChars = 4 * 1024 * 1024;
        private const int MaxIssues = 100000;
        private const int MaxCommentsPerIssue = 10000;

        internal static bool IsReservedKey(string key) =>
            key != null && key.StartsWith(ReservedRoot, StringComparison.OrdinalIgnoreCase);

        internal static CoordinationIssuePersistenceSnapshot? Read(IEnumerable<KeyValuePair<string, string>> metadata)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            var found = false;
            string? payload = null;
            foreach (var pair in metadata)
            {
                if (!IsReservedKey(pair.Key)) continue;
                if (!string.Equals(pair.Key, IssuesKey, StringComparison.Ordinal))
                    throw new FormatException("Coordination project metadata contains an unsupported or non-canonical reserved key: " + pair.Key + ".");
                if (found) throw new FormatException("Coordination project metadata contains duplicate issue persistence state.");
                found = true;
                payload = pair.Value ?? string.Empty;
            }
            return found ? Decode(payload ?? string.Empty) : null;
        }

        internal static string Value(string projectId, string drawingFingerprint, long revision, IEnumerable<CoordinationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("Project id is required.", nameof(projectId));
            if (string.IsNullOrWhiteSpace(drawingFingerprint)) throw new ArgumentException("Drawing fingerprint is required.", nameof(drawingFingerprint));
            if (revision <= 0L) throw new ArgumentOutOfRangeException(nameof(revision));
            if (issues == null) throw new ArgumentNullException(nameof(issues));

            var ordered = new List<CoordinationIssue>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var issue in issues)
            {
                if (issue == null) throw new ArgumentException("Coordination issue collection contains null.", nameof(issues));
                if (!ids.Add(issue.IssueId)) throw new InvalidOperationException("Duplicate coordination issue id: " + issue.IssueId + ".");
                ValidateIssue(issue);
                ordered.Add(issue);
                if (ordered.Count > MaxIssues) throw new InvalidOperationException("Coordination persistence exceeds the maximum supported issue count.");
            }
            ordered.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.IssueId, right.IssueId));

            var fields = new List<string>();
            fields.Add(PayloadVersion);
            fields.Add(Long(revision));
            fields.Add(projectId);
            fields.Add(drawingFingerprint);
            fields.Add(Int(ordered.Count));
            for (var i = 0; i < ordered.Count; i++) AddIssue(fields, ordered[i]);

            var builder = new StringBuilder();
            for (var i = 0; i < fields.Count; i++) AppendField(builder, fields[i]);
            var payload = builder.ToString();
            if (payload.Length > MaxPayloadChars)
                throw new InvalidOperationException("Coordination issue persistence exceeds the maximum supported metadata payload of 4 MiB characters.");
            PersistedTextXml.Verify(payload, nameof(issues), "Coordination issue persistence metadata");
            Decode(payload);
            return payload;
        }

        private static CoordinationIssuePersistenceSnapshot Decode(string payload)
        {
            try
            {
                if (payload.Length > MaxPayloadChars)
                    throw new FormatException("Coordination issue persistence exceeds the maximum supported metadata payload of 4 MiB characters.");
                PersistedTextXml.Verify(payload, nameof(payload), "Coordination issue persistence metadata");
                var offset = 0;
                var version = ReadField(payload, ref offset);
                if (!string.Equals(version, PayloadVersion, StringComparison.Ordinal))
                    throw new FormatException("Coordination issue persistence payload version is unsupported: " + version + ".");
                var revision = ReadPositiveLong(payload, ref offset, "revision");
                var projectId = Required(ReadField(payload, ref offset), "project id");
                var drawingFingerprint = Required(ReadField(payload, ref offset), "drawing fingerprint");
                var count = ReadCount(payload, ref offset, MaxIssues, "issue");
                var issues = new List<CoordinationIssue>(count);
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < count; i++)
                {
                    var issue = ReadIssue(payload, ref offset);
                    if (!ids.Add(issue.IssueId)) throw new FormatException("Coordination persistence contains duplicate issue id: " + issue.IssueId + ".");
                    issues.Add(issue);
                }
                if (offset != payload.Length) throw new FormatException("Coordination issue persistence contains trailing data.");
                issues.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.IssueId, right.IssueId));
                return new CoordinationIssuePersistenceSnapshot(revision, projectId, drawingFingerprint, new ReadOnlyCollection<CoordinationIssue>(issues));
            }
            catch (FormatException) { throw; }
            catch (ArgumentException ex) { throw new FormatException("Coordination issue persistence metadata is invalid.", ex); }
            catch (InvalidOperationException ex) { throw new FormatException("Coordination issue persistence metadata is inconsistent.", ex); }
            catch (OverflowException ex) { throw new FormatException("Coordination issue persistence metadata overflowed a supported numeric range.", ex); }
        }

        private static void AddIssue(List<string> fields, CoordinationIssue issue)
        {
            fields.Add(issue.IssueId);
            fields.Add(Int((int)issue.Kind));
            fields.Add(Int((int)issue.Severity));
            fields.Add(Int((int)issue.Status));
            fields.Add(issue.Title);
            fields.Add(issue.LeftSemanticId);
            fields.Add(issue.RightSemanticId);
            AddCadReference(fields, issue.LeftCadReference);
            AddCadReference(fields, issue.RightCadReference);
            fields.Add(issue.DisciplineContext);
            fields.Add(issue.CategoryContext);
            fields.Add(issue.SystemContext);
            fields.Add(issue.RegionContext);
            fields.Add(Double(issue.SeparationM));
            fields.Add(issue.Assignee ?? string.Empty);
            fields.Add(Date(issue.CreatedAtUtc));
            fields.Add(Date(issue.UpdatedAtUtc));

            var comments = issue.Comments.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
            fields.Add(Int(comments.Count));
            for (var i = 0; i < comments.Count; i++)
            {
                fields.Add(comments[i].Id);
                fields.Add(comments[i].Author);
                fields.Add(comments[i].Text);
                fields.Add(Date(comments[i].CreatedAtUtc));
            }
        }

        private static CoordinationIssue ReadIssue(string payload, ref int offset)
        {
            var issueId = Required(ReadField(payload, ref offset), "issue id");
            var kind = ReadEnum<CoordinationIssueKind>(payload, ref offset, "issue kind");
            var severity = ReadEnum<CoordinationIssueSeverity>(payload, ref offset, "issue severity");
            var status = ReadEnum<CoordinationIssueStatus>(payload, ref offset, "issue status");
            var title = Required(ReadField(payload, ref offset), "issue title");
            var leftSemanticId = Required(ReadField(payload, ref offset), "left semantic id");
            var rightSemanticId = Required(ReadField(payload, ref offset), "right semantic id");
            var leftCadReference = ReadCadReference(payload, ref offset, "left");
            var rightCadReference = ReadCadReference(payload, ref offset, "right");
            var discipline = Required(ReadField(payload, ref offset), "discipline context");
            var category = Required(ReadField(payload, ref offset), "category context");
            var system = Required(ReadField(payload, ref offset), "system context");
            var region = Required(ReadField(payload, ref offset), "region context");
            var separation = ReadNonNegativeDouble(payload, ref offset, "separation");
            var assignee = EmptyToNull(ReadField(payload, ref offset));
            var created = ReadDate(payload, ref offset, "created timestamp");
            var updated = ReadDate(payload, ref offset, "updated timestamp");
            if (updated < created) throw new FormatException("Coordination issue updated timestamp precedes creation.");

            var commentCount = ReadCount(payload, ref offset, MaxCommentsPerIssue, "comment");
            var comments = new List<CoordinationIssueComment>(commentCount);
            var commentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < commentCount; i++)
            {
                var id = Required(ReadField(payload, ref offset), "comment id");
                var author = Required(ReadField(payload, ref offset), "comment author");
                var text = Required(ReadField(payload, ref offset), "comment text");
                var time = ReadDate(payload, ref offset, "comment timestamp");
                if (time < created || time > updated) throw new FormatException("Coordination issue comment timestamp is outside the issue lifetime.");
                if (!commentIds.Add(id)) throw new FormatException("Coordination issue contains duplicate comment id: " + id + ".");
                comments.Add(new CoordinationIssueComment(id, author, text, time));
            }
            comments.Sort((left, right) =>
            {
                var time = left.CreatedAtUtc.CompareTo(right.CreatedAtUtc);
                return time != 0 ? time : StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id);
            });

            var issue = new CoordinationIssue(
                issueId, kind, severity, title, leftSemanticId, rightSemanticId,
                leftCadReference, rightCadReference, discipline, category, system, region,
                separation, created, assignee);
            for (var i = 0; i < comments.Count; i++) issue.AddComment(comments[i]);
            if (status != CoordinationIssueStatus.Open)
                issue.TransitionTo(status, updated);
            else if (issue.UpdatedAtUtc < updated)
                issue.Rename(issue.Title, updated);
            if (issue.UpdatedAtUtc != updated)
                throw new FormatException("Coordination issue reconstruction did not preserve updated timestamp.");
            return issue;
        }

        private static void ValidateIssue(CoordinationIssue issue)
        {
            if (issue.UpdatedAtUtc < issue.CreatedAtUtc)
                throw new InvalidOperationException("Coordination issue updated timestamp precedes creation: " + issue.IssueId + ".");
            if (issue.Comments.Count > MaxCommentsPerIssue)
                throw new InvalidOperationException("Coordination issue exceeds the maximum supported comment count: " + issue.IssueId + ".");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < issue.Comments.Count; i++)
            {
                var comment = issue.Comments[i];
                if (!ids.Add(comment.Id)) throw new InvalidOperationException("Coordination issue contains duplicate comment id: " + comment.Id + ".");
                if (comment.CreatedAtUtc < issue.CreatedAtUtc || comment.CreatedAtUtc > issue.UpdatedAtUtc)
                    throw new InvalidOperationException("Coordination issue comment timestamp is outside the issue lifetime: " + issue.IssueId + ".");
            }
            if (double.IsNaN(issue.SeparationM) || double.IsInfinity(issue.SeparationM) || issue.SeparationM < 0d)
                throw new InvalidOperationException("Coordination issue separation must be finite and non-negative: " + issue.IssueId + ".");
        }

        private static void AddCadReference(List<string> fields, CadReference? reference)
        {
            if (!reference.HasValue)
            {
                fields.Add("0");
                return;
            }
            fields.Add("1");
            fields.Add(reference.Value.DrawingId.Value.ToString("D", CultureInfo.InvariantCulture));
            fields.Add(reference.Value.Handle.Value);
        }

        private static CadReference? ReadCadReference(string payload, ref int offset, string side)
        {
            var marker = ReadField(payload, ref offset);
            if (string.Equals(marker, "0", StringComparison.Ordinal)) return null;
            if (!string.Equals(marker, "1", StringComparison.Ordinal))
                throw new FormatException("Coordination " + side + " CAD reference presence marker must be 0 or 1.");
            var drawingToken = ReadField(payload, ref offset);
            if (!Guid.TryParseExact(drawingToken, "D", out var drawingGuid) || drawingGuid == Guid.Empty ||
                !string.Equals(drawingToken, drawingGuid.ToString("D", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("Coordination " + side + " CAD reference drawing id is invalid or non-canonical.");
            var handle = new CadHandle(Required(ReadField(payload, ref offset), side + " CAD handle"));
            return new CadReference(new DrawingId(drawingGuid), handle);
        }

        private static T ReadEnum<T>(string payload, ref int offset, string label) where T : struct
        {
            var value = ReadInt(payload, ref offset, label);
            if (!Enum.IsDefined(typeof(T), value)) throw new FormatException("Coordination " + label + " is undefined: " + value + ".");
            return (T)Enum.ToObject(typeof(T), value);
        }

        private static void AppendField(StringBuilder builder, string value)
        {
            value = value ?? string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        private static string ReadField(string payload, ref int offset)
        {
            var colon = payload.IndexOf(':', offset);
            if (colon <= offset) throw new FormatException("Coordination persistence field length is missing.");
            var lengthToken = payload.Substring(offset, colon - offset);
            if (!int.TryParse(lengthToken, NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length < 0 ||
                !string.Equals(lengthToken, length.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("Coordination persistence field length is invalid or non-canonical.");
            offset = colon + 1;
            if (length > payload.Length - offset) throw new FormatException("Coordination persistence field exceeds available data.");
            var value = payload.Substring(offset, length);
            offset += length;
            return value;
        }

        private static int ReadCount(string payload, ref int offset, int maximum, string label)
        {
            var value = ReadInt(payload, ref offset, label + " count");
            if (value < 0 || value > maximum)
                throw new FormatException("Coordination " + label + " count is outside the supported range 0.." + maximum + ".");
            return value;
        }

        private static int ReadInt(string payload, ref int offset, string label)
        {
            var token = ReadField(payload, ref offset);
            if (!int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ||
                !string.Equals(token, value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("Coordination " + label + " is invalid or non-canonical.");
            return value;
        }

        private static long ReadPositiveLong(string payload, ref int offset, string label)
        {
            var token = ReadField(payload, ref offset);
            if (!long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) || value <= 0L ||
                !string.Equals(token, value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("Coordination " + label + " must be a positive canonical integer.");
            return value;
        }

        private static double ReadNonNegativeDouble(string payload, ref int offset, string label)
        {
            var token = ReadField(payload, ref offset);
            const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
            if (!double.TryParse(token, styles, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < 0d ||
                !string.Equals(token, Double(value), StringComparison.Ordinal))
                throw new FormatException("Coordination " + label + " must be finite, non-negative and canonical.");
            return value == 0d ? 0d : value;
        }

        private static DateTime ReadDate(string payload, ref int offset, string label)
        {
            var token = ReadField(payload, ref offset);
            if (!DateTime.TryParseExact(token, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) || value.Kind != DateTimeKind.Utc ||
                !string.Equals(token, Date(value), StringComparison.Ordinal))
                throw new FormatException("Coordination " + label + " must be a canonical UTC round-trip timestamp.");
            return value;
        }

        private static string Required(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new FormatException("Coordination " + label + " is required.");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new FormatException("Coordination " + label + " must be canonical without surrounding whitespace.");
            return value;
        }

        private static string Date(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc) throw new InvalidOperationException("Coordination persistence timestamps must be UTC.");
            return value.ToString("O", CultureInfo.InvariantCulture);
        }

        private static string Double(double value) => (value == 0d ? 0d : value).ToString("R", CultureInfo.InvariantCulture);
        private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Long(long value) => value.ToString(CultureInfo.InvariantCulture);
        private static string? EmptyToNull(string value) => value.Length == 0 ? null : value;
    }
}
