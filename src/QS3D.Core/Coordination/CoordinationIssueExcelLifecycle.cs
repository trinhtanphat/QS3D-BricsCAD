using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

namespace QS3D.Core.Coordination
{
    public sealed class CoordinationIssueExcelRow
    {
        internal CoordinationIssueExcelRow(long workbookRevision, string issueRevision, CoordinationIssue issue)
        {
            WorkbookRevision = workbookRevision;
            IssueRevision = issueRevision ?? throw new ArgumentNullException(nameof(issueRevision));
            IssueId = issue.IssueId;
            Kind = issue.Kind;
            Status = issue.Status;
            Severity = issue.Severity;
            Title = issue.Title;
            Assignee = issue.Assignee ?? string.Empty;
            LeftSemanticId = issue.LeftSemanticId;
            RightSemanticId = issue.RightSemanticId;
            LeftCadReference = issue.LeftCadReference;
            RightCadReference = issue.RightCadReference;
            DisciplineContext = issue.DisciplineContext;
            CategoryContext = issue.CategoryContext;
            SystemContext = issue.SystemContext;
            RegionContext = issue.RegionContext;
            SeparationM = issue.SeparationM;
            UpdatedAtUtc = issue.UpdatedAtUtc;
        }

        public long WorkbookRevision { get; }
        public string IssueRevision { get; }
        public string IssueId { get; }
        public CoordinationIssueKind Kind { get; }
        public CoordinationIssueStatus Status { get; }
        public CoordinationIssueSeverity Severity { get; }
        public string Title { get; }
        public string Assignee { get; }
        public string LeftSemanticId { get; }
        public string RightSemanticId { get; }
        public CadReference? LeftCadReference { get; }
        public CadReference? RightCadReference { get; }
        public string DisciplineContext { get; }
        public string CategoryContext { get; }
        public string SystemContext { get; }
        public string RegionContext { get; }
        public double SeparationM { get; }
        public DateTime UpdatedAtUtc { get; }
    }

    public sealed class CoordinationIssueExcelEdit
    {
        public CoordinationIssueExcelEdit(string issueId, string issueRevision, string status, string severity, string assignee, string commentAuthor = "", string comment = "")
        {
            IssueId = Required(issueId, nameof(issueId));
            IssueRevision = Required(issueRevision, nameof(issueRevision));
            Status = Required(status, nameof(status));
            Severity = Required(severity, nameof(severity));
            Assignee = Optional(assignee, nameof(assignee));
            CommentAuthor = Optional(commentAuthor, nameof(commentAuthor));
            Comment = Optional(comment, nameof(comment));
            if (Comment.Length != 0 && CommentAuthor.Length == 0)
                throw new ArgumentException("Comment author is required when a coordination issue comment is supplied.", nameof(commentAuthor));
            if (Comment.Length == 0 && CommentAuthor.Length != 0)
                throw new ArgumentException("Comment text is required when a coordination issue comment author is supplied.", nameof(comment));
        }

        public string IssueId { get; }
        public string IssueRevision { get; }
        public string Status { get; }
        public string Severity { get; }
        public string Assignee { get; }
        public string CommentAuthor { get; }
        public string Comment { get; }

        private static string Required(string value, string parameter)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Value is required.", parameter);
            if (normalized.Length > 32767) throw new ArgumentException("Value exceeds the Excel text limit.", parameter);
            return normalized;
        }

        private static string Optional(string value, string parameter)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length > 32767) throw new ArgumentException("Value exceeds the Excel text limit.", parameter);
            return normalized;
        }
    }

    public sealed class CoordinationIssueExcelImportPlan
    {
        internal CoordinationIssueExcelImportPlan(long sourceRevision, IReadOnlyList<CoordinationIssue> issues, int changedIssueCount)
        {
            SourceRevision = sourceRevision;
            NextRevision = changedIssueCount == 0 ? sourceRevision : checked(sourceRevision + 1L);
            Issues = issues ?? throw new ArgumentNullException(nameof(issues));
            ChangedIssueCount = changedIssueCount;
        }

        public long SourceRevision { get; }
        public long NextRevision { get; }
        public IReadOnlyList<CoordinationIssue> Issues { get; }
        public int ChangedIssueCount { get; }
    }

    /// <summary>
    /// Core-only coordination issue Excel edit contract. Projection and import are deliberately
    /// separated so a host can parse an XLSX first, validate the complete edit batch, and only
    /// then persist the returned cloned issue set. Source issues are never mutated by PlanImport.
    /// </summary>
    public static class CoordinationIssueExcelLifecycle
    {
        public static IReadOnlyList<CoordinationIssueExcelRow> Project(CoordinationIssuePersistenceSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var rows = new List<CoordinationIssueExcelRow>(snapshot.Issues.Count);
            for (var i = 0; i < snapshot.Issues.Count; i++)
            {
                var issue = snapshot.Issues[i] ?? throw new InvalidOperationException("Coordination snapshot contains a null issue.");
                rows.Add(new CoordinationIssueExcelRow(snapshot.Revision, Revision(snapshot, issue), issue));
            }
            rows.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.IssueId, right.IssueId));
            return new ReadOnlyCollection<CoordinationIssueExcelRow>(rows);
        }

        public static CoordinationIssueExcelImportPlan PlanImport(
            CoordinationIssuePersistenceSnapshot current,
            string workbookProjectId,
            string workbookDrawingFingerprint,
            long workbookRevision,
            IEnumerable<CoordinationIssueExcelEdit> edits,
            DateTime changedAtUtc)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (edits == null) throw new ArgumentNullException(nameof(edits));
            if (changedAtUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Coordination Excel import timestamp must be UTC.", nameof(changedAtUtc));
            if (!string.Equals(current.ProjectId, (workbookProjectId ?? string.Empty).Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination workbook belongs to a different project id.");
            if (!string.Equals(current.DrawingFingerprint, (workbookDrawingFingerprint ?? string.Empty).Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination workbook belongs to a different drawing fingerprint.");
            if (workbookRevision != current.Revision)
                throw new InvalidOperationException("Coordination workbook revision is stale. Re-export before importing edits.");

            var currentById = new Dictionary<string, CoordinationIssue>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < current.Issues.Count; i++)
            {
                var issue = current.Issues[i] ?? throw new InvalidOperationException("Coordination snapshot contains a null issue.");
                if (currentById.ContainsKey(issue.IssueId))
                    throw new InvalidOperationException("Coordination snapshot contains duplicate issue id: " + issue.IssueId + ".");
                currentById.Add(issue.IssueId, issue);
            }

            var validated = new List<ValidatedEdit>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var edit in edits)
            {
                if (edit == null) throw new InvalidOperationException("Coordination workbook edit batch contains a null row.");
                if (!seen.Add(edit.IssueId))
                    throw new InvalidOperationException("Coordination workbook contains duplicate issue id: " + edit.IssueId + ".");
                CoordinationIssue issue;
                if (!currentById.TryGetValue(edit.IssueId, out issue))
                    throw new InvalidOperationException("Coordination workbook references an unknown IssueId: " + edit.IssueId + ".");
                if (!string.Equals(edit.IssueRevision, Revision(current, issue), StringComparison.Ordinal))
                    throw new InvalidOperationException("Coordination issue revision is stale for IssueId " + edit.IssueId + ". Re-export before importing edits.");
                if (changedAtUtc < issue.UpdatedAtUtc)
                    throw new InvalidOperationException("Coordination Excel import timestamp predates the current issue state for IssueId " + edit.IssueId + ".");

                var status = ParseCanonical<CoordinationIssueStatus>(edit.Status, "status", edit.IssueId);
                var severity = ParseCanonical<CoordinationIssueSeverity>(edit.Severity, "severity", edit.IssueId);
                if (!CoordinationIssue.CanTransition(issue.Status, status))
                    throw new InvalidOperationException("Invalid coordination issue transition for IssueId " + edit.IssueId + ": " + issue.Status + " -> " + status + ".");

                validated.Add(new ValidatedEdit(issue, status, severity, edit.Assignee, edit.CommentAuthor, edit.Comment));
            }

            var clones = new Dictionary<string, CoordinationIssue>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in currentById) clones.Add(pair.Key, Clone(pair.Value));

            var changedCount = 0;
            for (var i = 0; i < validated.Count; i++)
            {
                var edit = validated[i];
                var issue = clones[edit.Source.IssueId];
                var changed = false;
                if (issue.Status != edit.Status)
                {
                    issue.TransitionTo(edit.Status, changedAtUtc);
                    changed = true;
                }
                if (issue.Severity != edit.Severity)
                {
                    issue.SetSeverity(edit.Severity, changedAtUtc);
                    changed = true;
                }
                var currentAssignee = issue.Assignee ?? string.Empty;
                if (!string.Equals(currentAssignee, edit.Assignee, StringComparison.Ordinal))
                {
                    issue.Assign(edit.Assignee.Length == 0 ? null : edit.Assignee, changedAtUtc);
                    changed = true;
                }
                if (edit.Comment.Length != 0)
                {
                    var commentId = "XLSX:" + Sha256Hex(
                        issue.IssueId + "\u001f" + Revision(current, edit.Source) + "\u001f" +
                        edit.CommentAuthor + "\u001f" + edit.Comment + "\u001f" +
                        changedAtUtc.ToString("O", CultureInfo.InvariantCulture));
                    issue.AddComment(new CoordinationIssueComment(commentId, edit.CommentAuthor, edit.Comment, changedAtUtc));
                    changed = true;
                }
                if (changed) changedCount++;
            }

            var ordered = new List<CoordinationIssue>(clones.Values);
            ordered.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.IssueId, right.IssueId));
            return new CoordinationIssueExcelImportPlan(current.Revision, new ReadOnlyCollection<CoordinationIssue>(ordered), changedCount);
        }

        public static string Revision(CoordinationIssuePersistenceSnapshot snapshot, CoordinationIssue issue)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (issue == null) throw new ArgumentNullException(nameof(issue));
            return "ISSUE-REV-1:" + Sha256Hex(
                snapshot.ProjectId + "\u001f" + snapshot.DrawingFingerprint + "\u001f" +
                snapshot.Revision.ToString(CultureInfo.InvariantCulture) + "\u001f" + issue.IssueId + "\u001f" +
                ((int)issue.Kind).ToString(CultureInfo.InvariantCulture) + "\u001f" +
                ((int)issue.Status).ToString(CultureInfo.InvariantCulture) + "\u001f" +
                ((int)issue.Severity).ToString(CultureInfo.InvariantCulture) + "\u001f" +
                (issue.Assignee ?? string.Empty) + "\u001f" +
                issue.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture) + "\u001f" +
                issue.Comments.Count.ToString(CultureInfo.InvariantCulture));
        }

        private static T ParseCanonical<T>(string value, string field, string issueId) where T : struct
        {
            T parsed;
            if (!Enum.TryParse<T>(value, false, out parsed) || !Enum.IsDefined(typeof(T), parsed) ||
                !string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination workbook contains invalid " + field + " for IssueId " + issueId + ": " + value + ".");
            return parsed;
        }

        private static CoordinationIssue Clone(CoordinationIssue source)
        {
            var clone = new CoordinationIssue(
                source.IssueId,
                source.Kind,
                source.Severity,
                source.Title,
                source.LeftSemanticId,
                source.RightSemanticId,
                source.LeftCadReference,
                source.RightCadReference,
                source.DisciplineContext,
                source.CategoryContext,
                source.SystemContext,
                source.RegionContext,
                source.SeparationM,
                source.CreatedAtUtc,
                source.Assignee);
            for (var i = 0; i < source.Comments.Count; i++) clone.AddComment(source.Comments[i]);
            if (source.Status != CoordinationIssueStatus.Open)
                clone.TransitionTo(source.Status, source.UpdatedAtUtc);
            else if (clone.UpdatedAtUtc < source.UpdatedAtUtc)
                clone.Rename(clone.Title, source.UpdatedAtUtc);
            if (clone.UpdatedAtUtc != source.UpdatedAtUtc)
                throw new InvalidOperationException("Coordination issue clone did not preserve UpdatedAtUtc for IssueId " + source.IssueId + ".");
            return clone;
        }

        private static string Sha256Hex(string value)
        {
            using (var hash = SHA256.Create())
            {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(bytes.Length * 2);
                for (var i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private sealed class ValidatedEdit
        {
            internal ValidatedEdit(CoordinationIssue source, CoordinationIssueStatus status, CoordinationIssueSeverity severity, string assignee, string commentAuthor, string comment)
            {
                Source = source;
                Status = status;
                Severity = severity;
                Assignee = assignee;
                CommentAuthor = commentAuthor;
                Comment = comment;
            }

            internal CoordinationIssue Source { get; }
            internal CoordinationIssueStatus Status { get; }
            internal CoordinationIssueSeverity Severity { get; }
            internal string Assignee { get; }
            internal string CommentAuthor { get; }
            internal string Comment { get; }
        }
    }
}
