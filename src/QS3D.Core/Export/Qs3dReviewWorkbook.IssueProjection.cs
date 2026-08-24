using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using QS3D.Platform.Parity;

namespace QS3D.Core.Export
{
    public sealed class Qs3dReviewIssueProjectionResult
    {
        internal Qs3dReviewIssueProjectionResult(
            IReadOnlyList<CoordinationClashExportRow> clashes,
            IReadOnlyList<CoordinationDuplicateExportRow> duplicates,
            IReadOnlyDictionary<string, CoordinationIssueExcelRow> lifecycleByFindingId)
        {
            Clashes = clashes ?? throw new ArgumentNullException(nameof(clashes));
            Duplicates = duplicates ?? throw new ArgumentNullException(nameof(duplicates));
            LifecycleByFindingId = lifecycleByFindingId ?? throw new ArgumentNullException(nameof(lifecycleByFindingId));
        }

        public IReadOnlyList<CoordinationClashExportRow> Clashes { get; }
        public IReadOnlyList<CoordinationDuplicateExportRow> Duplicates { get; }
        public IReadOnlyDictionary<string, CoordinationIssueExcelRow> LifecycleByFindingId { get; }
    }

    public static class Qs3dReviewIssueProjection
    {
        public static Qs3dReviewIssueProjectionResult Build(
            ProjectState project,
            CoordinationIssuePersistenceSnapshot snapshot)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!string.Equals(project.ProjectId, snapshot.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("QS3D Review coordination state belongs to a different project id.");
            if (!string.Equals(project.DrawingFingerprint, snapshot.DrawingFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("QS3D Review coordination state belongs to a different drawing fingerprint.");

            var lifecycleRows = CoordinationIssueExcelLifecycle.Project(snapshot);
            var lifecycle = new Dictionary<string, CoordinationIssueExcelRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in lifecycleRows) lifecycle.Add(row.IssueId, row);

            var clashes = new List<CoordinationClashExportRow>();
            var duplicates = new List<CoordinationDuplicateExportRow>();
            foreach (var issue in snapshot.Issues)
            {
                var left = project.FindElement(issue.LeftSemanticId)
                    ?? throw new InvalidOperationException("QS3D Review issue references a missing left semantic element: " + issue.LeftSemanticId + ".");
                var right = project.FindElement(issue.RightSemanticId)
                    ?? throw new InvalidOperationException("QS3D Review issue references a missing right semantic element: " + issue.RightSemanticId + ".");
                var leftReference = issue.LeftCadReference
                    ?? throw new InvalidOperationException("QS3D Review issue is missing its left CAD reference: " + issue.IssueId + ".");
                var rightReference = issue.RightCadReference
                    ?? throw new InvalidOperationException("QS3D Review issue is missing its right CAD reference: " + issue.IssueId + ".");
                if (leftReference.DrawingId.Value == Guid.Empty || rightReference.DrawingId.Value == Guid.Empty ||
                    leftReference.DrawingId != rightReference.DrawingId)
                    throw new InvalidOperationException("QS3D Review issue CAD references do not identify one canonical drawing: " + issue.IssueId + ".");

                var leftHandle = CoordinationWorkbookIdentity.CanonicalHandle(leftReference.Handle.Value);
                var rightHandle = CoordinationWorkbookIdentity.CanonicalHandle(rightReference.Handle.Value);
                if (string.Equals(leftHandle, rightHandle, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D Review issue resolves both sides to the same CAD Handle: " + issue.IssueId + ".");
                RequireSemanticHandle(project, left.Id, leftHandle, "left", issue.IssueId);
                RequireSemanticHandle(project, right.Id, rightHandle, "right", issue.IssueId);

                var floor = CommonFloor(project, left, right, issue.RegionContext);
                var comment = issue.Comments.Count == 0 ? string.Empty : issue.Comments[issue.Comments.Count - 1].Text;
                if (issue.Kind == CoordinationIssueKind.Review)
                {
                    duplicates.Add(new CoordinationDuplicateExportRow(
                        issue.IssueId,
                        DuplicateMatchKind.SemanticIdentity,
                        floor,
                        left.Id,
                        leftHandle,
                        left.Category.ToString(),
                        right.Id,
                        rightHandle,
                        right.Category.ToString(),
                        "QS3D_PERSISTED_REVIEW_V1",
                        project.DrawingFingerprint,
                        comment));
                    continue;
                }

                clashes.Add(new CoordinationClashExportRow(
                    issue.IssueId,
                    issue.Kind.ToString(),
                    issue.Severity.ToString(),
                    issue.Status.ToString(),
                    floor,
                    left.Id,
                    leftHandle,
                    left.Category.ToString(),
                    right.Id,
                    rightHandle,
                    right.Category.ToString(),
                    "QS3D_PERSISTED_" + issue.Kind.ToString().ToUpperInvariant() + "_V1",
                    project.DrawingFingerprint,
                    comment));
            }

            clashes.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ClashId, right.ClashId));
            duplicates.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.DuplicateId, right.DuplicateId));
            return new Qs3dReviewIssueProjectionResult(
                new ReadOnlyCollection<CoordinationClashExportRow>(clashes),
                new ReadOnlyCollection<CoordinationDuplicateExportRow>(duplicates),
                new ReadOnlyDictionary<string, CoordinationIssueExcelRow>(lifecycle));
        }

        private static void RequireSemanticHandle(
            ProjectState project,
            string elementId,
            string expectedHandle,
            string side,
            string issueId)
        {
            var handles = SourceHandleResolver.Resolve(project, new[] { elementId })
                .Select(CoordinationWorkbookIdentity.CanonicalHandle)
                .ToArray();
            if (!handles.Contains(expectedHandle, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "QS3D Review issue " + side + " CAD reference does not belong to its semantic element: " + issueId + ".");
        }

        private static string CommonFloor(ProjectState project, ProjectElement left, ProjectElement right, string fallback)
        {
            if (string.Equals(left.FloorId, right.FloorId, StringComparison.OrdinalIgnoreCase))
            {
                var floor = project.FindFloor(left.FloorId);
                if (floor != null) return floor.Name;
                if (!string.IsNullOrWhiteSpace(left.FloorId)) return left.FloorId;
            }
            return (fallback ?? string.Empty).Trim();
        }
    }
}
