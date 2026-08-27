using System;
using System.IO;
using System.Linq;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationIssuePersistenceSmoke
    {
        internal static void Run()
        {
            ColdReopenPreservesCanonicalIssueState();
            RepeatedPersistenceIsDeterministic();
            MismatchAndStaleReferencesFailClosed();
            UnsupportedPayloadVersionFailsClosed();
            Console.WriteLine("PASS coordination issue persistence cold-reopen");
        }

        private static void ColdReopenPreservesCanonicalIssueState()
        {
            var project = CreateProject();
            var issue = CreateIssue();
            CoordinationIssuePersistence.Save(project, new[] { issue }, 7L);

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-coordination-persistence-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "coordination.qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var reopened = store.Load(path);
                var snapshot = CoordinationIssuePersistence.Load(reopened)
                    ?? throw new InvalidOperationException("Cold-reopen coordination snapshot was not restored.");
                if (snapshot.Revision != 7L) throw new InvalidOperationException("Coordination revision changed across QSDB cold reopen.");
                if (!string.Equals(snapshot.ProjectId, reopened.ProjectId, StringComparison.Ordinal)) throw new InvalidOperationException("Coordination project identity changed across QSDB cold reopen.");
                if (!string.Equals(snapshot.DrawingFingerprint, reopened.DrawingFingerprint, StringComparison.Ordinal)) throw new InvalidOperationException("Coordination drawing identity changed across QSDB cold reopen.");
                if (snapshot.Issues.Count != 1) throw new InvalidOperationException("Coordination issue count changed across QSDB cold reopen.");

                var restored = snapshot.Issues[0];
                if (!string.Equals(restored.IssueId, issue.IssueId, StringComparison.Ordinal)) throw new InvalidOperationException("Coordination IssueId changed across QSDB cold reopen.");
                if (restored.Status != CoordinationIssueStatus.Resolved) throw new InvalidOperationException("Coordination status changed across QSDB cold reopen.");
                if (restored.Severity != CoordinationIssueSeverity.Critical) throw new InvalidOperationException("Coordination severity changed across QSDB cold reopen.");
                if (!string.Equals(restored.Title, "Resolved hard clash", StringComparison.Ordinal)) throw new InvalidOperationException("Coordination title changed across QSDB cold reopen.");
                if (!string.Equals(restored.Assignee, "MEP Lead", StringComparison.Ordinal)) throw new InvalidOperationException("Coordination assignee changed across QSDB cold reopen.");
                if (restored.Comments.Count != 2) throw new InvalidOperationException("Coordination comments changed across QSDB cold reopen.");
                if (!string.Equals(restored.Comments[0].Id, "comment-a", StringComparison.Ordinal) ||
                    !string.Equals(restored.Comments[1].Id, "comment-b", StringComparison.Ordinal))
                    throw new InvalidOperationException("Coordination comment order changed across QSDB cold reopen.");
                if (restored.UpdatedAtUtc != issue.UpdatedAtUtc) throw new InvalidOperationException("Coordination updated timestamp changed across QSDB cold reopen.");
                if (!restored.LeftCadReference.HasValue || !restored.RightCadReference.HasValue) throw new InvalidOperationException("Coordination CAD references were lost across QSDB cold reopen.");
                if (restored.LeftCadReference.Value.Handle.Value != "AB" || restored.RightCadReference.Value.Handle.Value != "CD")
                    throw new InvalidOperationException("Coordination CAD handles were not reconstructed canonically.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void RepeatedPersistenceIsDeterministic()
        {
            var project = CreateProject();
            var issue = CreateIssue();
            CoordinationIssuePersistence.Save(project, new[] { issue }, 11L);
            var first = CoordinationPayload(project);
            var changeVersion = project.ChangeVersion;
            CoordinationIssuePersistence.Save(project, new[] { issue }, 11L);
            var second = CoordinationPayload(project);
            if (!string.Equals(first, second, StringComparison.Ordinal))
                throw new InvalidOperationException("Repeated coordination persistence changed the canonical payload.");
            if (project.ChangeVersion != changeVersion)
                throw new InvalidOperationException("Repeated identical coordination persistence dirtied the project.");
        }

        private static void MismatchAndStaleReferencesFailClosed()
        {
            var project = CreateProject();
            var issue = CreateIssue();
            CoordinationIssuePersistence.Save(project, new[] { issue }, 3L);
            var snapshot = CoordinationIssuePersistence.Load(project)
                ?? throw new InvalidOperationException("Coordination snapshot was not restored.");

            var canonicalLookup = snapshot.Find(issue.IssueId);
            if (canonicalLookup == null || !string.Equals(canonicalLookup.IssueId, issue.IssueId, StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical coordination IssueId lookup failed.");

            var paddedIssueId = " " + issue.IssueId + " ";
            if (snapshot.Find(paddedIssueId) != null)
                throw new InvalidOperationException("Padded coordination IssueId must not alias a canonical persisted issue.");

            var paddedRelink = snapshot.EvaluateRelink(project, paddedIssueId, _ => true);
            if (paddedRelink.Status != CoordinationRelinkStatus.IssueNotFound || paddedRelink.IsActionable || paddedRelink.Issue != null)
                throw new InvalidOperationException("Padded coordination IssueId must fail closed as IssueNotFound during relink.");

            var ready = snapshot.EvaluateRelink(project, issue.IssueId);
            if (ready.Status != CoordinationRelinkStatus.ReadyForHostValidation || ready.IsActionable)
                throw new InvalidOperationException("Coordination relink became actionable before host CAD validation.");

            var stale = snapshot.EvaluateRelink(project, issue.IssueId, reference => reference.Handle.Value == "AB");
            if (stale.Status != CoordinationRelinkStatus.StaleRightCadReference || stale.IsActionable)
                throw new InvalidOperationException("A stale CAD handle did not fail closed.");

            var relinked = snapshot.EvaluateRelink(project, issue.IssueId, _ => true);
            if (relinked.Status != CoordinationRelinkStatus.Relinked || !relinked.IsActionable)
                throw new InvalidOperationException("Validated live CAD references did not become actionable.");

            project.Elements.Remove(project.FindElement("semantic-right") ?? throw new InvalidOperationException("Expected right semantic element."));
            var missing = snapshot.EvaluateRelink(project, issue.IssueId, _ => true);
            if (missing.Status != CoordinationRelinkStatus.MissingRightSemantic || missing.IsActionable)
                throw new InvalidOperationException("Missing semantic identity did not block relink.");

            var mismatch = CreateProject();
            mismatch.DrawingFingerprint = "DRAWING-B";
            var mismatchResult = snapshot.EvaluateRelink(mismatch, issue.IssueId, _ => true);
            if (mismatchResult.Status != CoordinationRelinkStatus.DrawingMismatch || mismatchResult.IsActionable)
                throw new InvalidOperationException("Drawing mismatch did not fail closed.");

            var mismatchPayloadProject = ProjectStateSnapshot.CreateDetachedCopy(project);
            mismatchPayloadProject.DrawingFingerprint = "DRAWING-C";
            Expect<InvalidOperationException>(() => CoordinationIssuePersistence.Load(mismatchPayloadProject));
        }

        private static void UnsupportedPayloadVersionFailsClosed()
        {
            var project = CreateProject();
            CoordinationIssuePersistence.Save(project, new[] { CreateIssue() }, 2L);
            var key = project.Metadata.Keys.Single(x => x.StartsWith("QS3D.Coordination.", StringComparison.Ordinal));
            var payload = project.Metadata[key];
            if (!payload.StartsWith("1:1", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected coordination payload version prefix.");
            var unsupported = "1:2" + payload.Substring(3);
            Expect<FormatException>(() => project.Metadata[key] = unsupported);
            if (!string.Equals(project.Metadata[key], payload, StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected coordination metadata mutation changed the stored canonical payload.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("project-coordination-1", "Coordination Persistence Smoke")
            {
                DrawingFingerprint = "DRAWING-A"
            };
            var left = new ProjectElement("semantic-left", ElementCategory.Beam) { DrawingFingerprint = "DRAWING-A" };
            var right = new ProjectElement("semantic-right", ElementCategory.StructuralWall) { DrawingFingerprint = "DRAWING-A" };
            project.Elements.Add(left);
            project.Elements.Add(right);
            return project;
        }

        private static CoordinationIssue CreateIssue()
        {
            var drawingId = new DrawingId(Guid.Parse("1b0d3125-7c84-4ca1-bf18-4cc3af6629d2"));
            var created = new DateTime(2026, 8, 22, 1, 0, 0, DateTimeKind.Utc);
            var issue = new CoordinationIssue(
                "issue-hard-001",
                CoordinationIssueKind.ExactHardClash,
                CoordinationIssueSeverity.High,
                "Hard clash",
                "semantic-left",
                "semantic-right",
                new CadReference(drawingId, new CadHandle("00ab")),
                new CadReference(drawingId, new CadHandle("000cd")),
                "Structure/MEP",
                "Beam/StructuralWall",
                "Supply",
                "Level-03",
                0d,
                created);
            issue.AddComment(new CoordinationIssueComment("comment-a", "Checker", "Detected exact hard clash", created.AddMinutes(1)));
            issue.AddComment(new CoordinationIssueComment("comment-b", "Coordinator", "Reviewed with discipline lead", created.AddMinutes(2)));
            issue.Assign("MEP Lead", created.AddMinutes(3));
            issue.SetSeverity(CoordinationIssueSeverity.Critical, created.AddMinutes(4));
            issue.Rename("Resolved hard clash", created.AddMinutes(5));
            issue.TransitionTo(CoordinationIssueStatus.Resolved, created.AddMinutes(6));
            return issue;
        }

        private static string CoordinationPayload(ProjectState project)
        {
            return project.Metadata.Single(x => x.Key.StartsWith("QS3D.Coordination.", StringComparison.Ordinal)).Value;
        }

        private static void Expect<T>(Action action) where T : Exception
        {
            try
            {
                action();
                throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
            }
            catch (T)
            {
            }
        }
    }
}
