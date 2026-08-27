using System;
using System.Linq;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationIssueExcelLifecycleSmoke
    {
        internal static void Run()
        {
            AcceptedEditReturnsClonedNextRevisionPlan();
            StaleWorkbookAndIssueRevisionFailClosed();
            UnknownInvalidAndPartialBatchesFailClosed();
            NonCanonicalIdentityFailsClosed();
            Console.WriteLine("PASS coordination Excel issue lifecycle conflict guard");
        }

        private static void AcceptedEditReturnsClonedNextRevisionPlan()
        {
            var snapshot = CreateSnapshot(9L, CreateIssue("issue-a", "left-a", "right-a"));
            var row = CoordinationIssueExcelLifecycle.Project(snapshot).Single();
            var changedAt = row.UpdatedAtUtc.AddMinutes(5);
            var plan = CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                snapshot.ProjectId,
                snapshot.DrawingFingerprint,
                row.WorkbookRevision,
                new[]
                {
                    new CoordinationIssueExcelEdit(
                        row.IssueId,
                        row.IssueRevision,
                        CoordinationIssueStatus.InReview.ToString(),
                        CoordinationIssueSeverity.Critical.ToString(),
                        "Coordination Lead",
                        "QS Lead",
                        "Accepted from reviewed workbook")
                },
                changedAt);

            if (plan.SourceRevision != 9L || plan.NextRevision != 10L)
                throw new InvalidOperationException("Coordination Excel import plan did not advance the persistence revision exactly once.");
            if (plan.ChangedIssueCount != 1 || plan.Issues.Count != 1)
                throw new InvalidOperationException("Coordination Excel import plan changed an unexpected issue count.");

            var changed = plan.Issues.Single();
            if (changed.Status != CoordinationIssueStatus.InReview || changed.Severity != CoordinationIssueSeverity.Critical)
                throw new InvalidOperationException("Coordination Excel editable status/severity were not applied to the cloned issue.");
            if (!string.Equals(changed.Assignee, "Coordination Lead", StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination Excel assignee edit was not applied.");
            if (changed.Comments.Count != 1 || !string.Equals(changed.Comments[0].Author, "QS Lead", StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination Excel comment edit was not appended deterministically.");
            if (!changed.Comments[0].Id.StartsWith("XLSX:", StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination Excel comment id is not namespaced deterministically.");

            var source = snapshot.Issues.Single();
            if (source.Status != CoordinationIssueStatus.Open || source.Severity != CoordinationIssueSeverity.High ||
                source.Assignee != null || source.Comments.Count != 0)
                throw new InvalidOperationException("PlanImport mutated canonical source issues before persistence commit.");
            if (!string.Equals(changed.LeftSemanticId, source.LeftSemanticId, StringComparison.Ordinal) ||
                !string.Equals(changed.RightSemanticId, source.RightSemanticId, StringComparison.Ordinal) ||
                !SameCadReference(changed.LeftCadReference, source.LeftCadReference) ||
                !SameCadReference(changed.RightCadReference, source.RightCadReference))
                throw new InvalidOperationException("Coordination Excel import changed immutable semantic/CAD trace.");
        }

        private static void StaleWorkbookAndIssueRevisionFailClosed()
        {
            var snapshot = CreateSnapshot(4L, CreateIssue("issue-b", "left-b", "right-b"));
            var row = CoordinationIssueExcelLifecycle.Project(snapshot).Single();
            var edit = new CoordinationIssueExcelEdit(
                row.IssueId,
                row.IssueRevision,
                row.Status.ToString(),
                row.Severity.ToString(),
                row.Assignee);

            Expect<InvalidOperationException>(() => CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                snapshot.ProjectId,
                snapshot.DrawingFingerprint,
                3L,
                new[] { edit },
                row.UpdatedAtUtc));

            var staleRow = new CoordinationIssueExcelEdit(
                row.IssueId,
                row.IssueRevision + "-stale",
                row.Status.ToString(),
                row.Severity.ToString(),
                row.Assignee);
            Expect<InvalidOperationException>(() => CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                snapshot.ProjectId,
                snapshot.DrawingFingerprint,
                snapshot.Revision,
                new[] { staleRow },
                row.UpdatedAtUtc));

            Expect<InvalidOperationException>(() => CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                snapshot.ProjectId,
                "OTHER-DRAWING",
                snapshot.Revision,
                new[] { edit },
                row.UpdatedAtUtc));
        }

        private static void UnknownInvalidAndPartialBatchesFailClosed()
        {
            var issueA = CreateIssue("issue-c", "left-c", "right-c");
            var issueB = CreateIssue("issue-d", "left-d", "right-d");
            var snapshot = CreateSnapshot(12L, issueA, issueB);
            var rows = CoordinationIssueExcelLifecycle.Project(snapshot).ToDictionary(x => x.IssueId, StringComparer.OrdinalIgnoreCase);
            var rowA = rows["issue-c"];
            var rowB = rows["issue-d"];
            var changedAt = rowA.UpdatedAtUtc.AddMinutes(10);

            var validFirst = new CoordinationIssueExcelEdit(
                rowA.IssueId,
                rowA.IssueRevision,
                CoordinationIssueStatus.Resolved.ToString(),
                CoordinationIssueSeverity.Critical.ToString(),
                "Lead");
            var unknownSecond = new CoordinationIssueExcelEdit(
                "missing-issue",
                rowB.IssueRevision,
                rowB.Status.ToString(),
                rowB.Severity.ToString(),
                rowB.Assignee);
            Expect<InvalidOperationException>(() => CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                snapshot.ProjectId,
                snapshot.DrawingFingerprint,
                snapshot.Revision,
                new[] { validFirst, unknownSecond },
                changedAt));
            if (issueA.Status != CoordinationIssueStatus.Open || issueA.Severity != CoordinationIssueSeverity.High || issueA.Assignee != null)
                throw new InvalidOperationException("Rejected coordination Excel batch partially mutated an earlier valid row.");

            var invalidEnum = new CoordinationIssueExcelEdit(
                rowA.IssueId,
                rowA.IssueRevision,
                "open",
                rowA.Severity.ToString(),
                rowA.Assignee);
            Expect<InvalidOperationException>(() => CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                snapshot.ProjectId,
                snapshot.DrawingFingerprint,
                snapshot.Revision,
                new[] { invalidEnum },
                changedAt));

            var duplicateRow = new CoordinationIssueExcelEdit(
                rowA.IssueId,
                rowA.IssueRevision,
                rowA.Status.ToString(),
                rowA.Severity.ToString(),
                rowA.Assignee);
            Expect<InvalidOperationException>(() => CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                snapshot.ProjectId,
                snapshot.DrawingFingerprint,
                snapshot.Revision,
                new[] { duplicateRow, duplicateRow },
                changedAt));
        }

        private static void NonCanonicalIdentityFailsClosed()
        {
            var snapshot = CreateSnapshot(15L, CreateIssue("issue-e", "left-e", "right-e"));
            var row = CoordinationIssueExcelLifecycle.Project(snapshot).Single();

            Expect<ArgumentException>(() => new CoordinationIssueExcelEdit(
                " " + row.IssueId,
                row.IssueRevision,
                row.Status.ToString(),
                row.Severity.ToString(),
                row.Assignee));
            Expect<ArgumentException>(() => new CoordinationIssueExcelEdit(
                row.IssueId + " ",
                row.IssueRevision,
                row.Status.ToString(),
                row.Severity.ToString(),
                row.Assignee));
            Expect<ArgumentException>(() => new CoordinationIssueExcelEdit(
                row.IssueId,
                " " + row.IssueRevision,
                row.Status.ToString(),
                row.Severity.ToString(),
                row.Assignee));
            Expect<ArgumentException>(() => new CoordinationIssueExcelEdit(
                row.IssueId,
                row.IssueRevision + " ",
                row.Status.ToString(),
                row.Severity.ToString(),
                row.Assignee));

            var edit = new CoordinationIssueExcelEdit(
                row.IssueId,
                row.IssueRevision,
                " " + row.Status + " ",
                " " + row.Severity + " ",
                "  Lead  ");
            if (!string.Equals(edit.Status, row.Status.ToString(), StringComparison.Ordinal) ||
                !string.Equals(edit.Severity, row.Severity.ToString(), StringComparison.Ordinal) ||
                !string.Equals(edit.Assignee, "Lead", StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination Excel editable presentation fields stopped normalizing as intended.");

            Expect<InvalidOperationException>(() => CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                " " + snapshot.ProjectId,
                snapshot.DrawingFingerprint,
                snapshot.Revision,
                new[] { edit },
                row.UpdatedAtUtc));
            Expect<InvalidOperationException>(() => CoordinationIssueExcelLifecycle.PlanImport(
                snapshot,
                snapshot.ProjectId,
                snapshot.DrawingFingerprint + " ",
                snapshot.Revision,
                new[] { edit },
                row.UpdatedAtUtc));
        }

        private static CoordinationIssuePersistenceSnapshot CreateSnapshot(long revision, params CoordinationIssue[] issues)
        {
            var project = new ProjectState("project-excel-lifecycle", "Coordination Excel Lifecycle Smoke")
            {
                DrawingFingerprint = "DRAWING-EXCEL-LIFECYCLE"
            };
            CoordinationIssuePersistence.Save(project, issues, revision);
            return CoordinationIssuePersistence.Load(project)
                ?? throw new InvalidOperationException("Coordination issue snapshot was not restored for Excel lifecycle smoke.");
        }

        private static CoordinationIssue CreateIssue(string issueId, string leftSemanticId, string rightSemanticId)
        {
            var drawingId = new DrawingId(Guid.Parse("a320e15f-221c-4c7c-b8d3-1c1df35ca70e"));
            var created = new DateTime(2026, 8, 22, 3, 0, 0, DateTimeKind.Utc);
            return new CoordinationIssue(
                issueId,
                CoordinationIssueKind.HardClash,
                CoordinationIssueSeverity.High,
                "Hard clash " + issueId,
                leftSemanticId,
                rightSemanticId,
                new CadReference(drawingId, new CadHandle("A" + issueId.Length.ToString("X"))),
                new CadReference(drawingId, new CadHandle("B" + issueId.Length.ToString("X"))),
                "Structure/MEP",
                "Beam/Duct",
                "Supply",
                "Level-01",
                0d,
                created);
        }

        private static bool SameCadReference(CadReference? left, CadReference? right)
        {
            if (!left.HasValue || !right.HasValue) return left.HasValue == right.HasValue;
            var leftValue = left.GetValueOrDefault();
            var rightValue = right.GetValueOrDefault();
            return leftValue.DrawingId.Value == rightValue.DrawingId.Value &&
                   string.Equals(leftValue.Handle.Value, rightValue.Handle.Value, StringComparison.Ordinal);
        }

        private static void Expect<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
