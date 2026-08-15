using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Recognition;
using QS3D.Core.Services;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class RecognitionApplyItem
    {
        public RecognitionApplyItem(EntitySnapshot snapshot, ElementCategory category, string ruleId, double confidence, string evidenceText)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Category = category;
            RuleId = ruleId ?? string.Empty;
            Confidence = confidence;
            EvidenceText = evidenceText ?? string.Empty;
        }

        public EntitySnapshot Snapshot { get; }
        public ElementCategory Category { get; }
        public string RuleId { get; }
        public double Confidence { get; }
        public string EvidenceText { get; }
        public string Handle => Snapshot.Handle;
    }

    internal sealed class RecognitionApplySkip
    {
        public RecognitionApplySkip(string handle, string reason)
        {
            Handle = handle ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string Handle { get; }
        public string Reason { get; }
    }

    internal sealed class RecognitionApplyBatchPlan
    {
        public RecognitionApplyBatchPlan(long projectChangeVersion, IReadOnlyList<RecognitionApplyItem> items, IReadOnlyList<RecognitionApplySkip> skips)
        {
            ProjectChangeVersion = projectChangeVersion;
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Skips = skips ?? throw new ArgumentNullException(nameof(skips));
        }

        public long ProjectChangeVersion { get; }
        public IReadOnlyList<RecognitionApplyItem> Items { get; }
        public IReadOnlyList<RecognitionApplySkip> Skips { get; }
        public int SkippedCount => Skips.Count;
    }

    internal static class RecognitionApplyBatchService
    {
        private const int MaxBatchItems = 250000;
        private const double AutoAcceptConfidence = 0.92d;
        private const double AutoAcceptMinimumMargin = 0.15d;

        public static RecognitionApplyBatchPlan PrepareStrict(
            Document document,
            string expectedProjectId,
            IEnumerable<RecognitionResult> results,
            bool requireAutoAcceptance = false)
        {
            var rows = Materialize(results);
            var operation = requireAutoAcceptance ? "Recognition Confident Apply" : "Recognition Apply";
            var project = RequireCurrentProject(document, expectedProjectId, operation);
            var version = project.ChangeVersion;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<RecognitionApplyItem>(rows.Count);

            foreach (var result in rows)
            {
                if (result == null) throw new InvalidOperationException(operation + ": batch contains a null review row.");
                if (result.TopCandidate == null) continue;
                if (!seen.Add(result.Handle))
                    throw new InvalidOperationException(operation + ": duplicate CAD handle in review batch: " + result.Handle + ".");
                items.Add(PrepareOne(document, project, result, requireAutoAcceptance));
            }

            EnsureProjectUnchanged(document, project, expectedProjectId, version, operation + " preflight");
            return new RecognitionApplyBatchPlan(version, items.AsReadOnly(), Array.Empty<RecognitionApplySkip>());
        }

        public static RecognitionApplyBatchPlan PrepareBestEffort(
            Document document,
            string expectedProjectId,
            IEnumerable<RecognitionResult> results)
        {
            var rows = Materialize(results);
            var project = RequireCurrentProject(document, expectedProjectId, "Recognition Auto Apply");
            var version = project.ChangeVersion;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<RecognitionApplyItem>(rows.Count);
            var skips = new List<RecognitionApplySkip>();

            foreach (var result in rows)
            {
                if (result == null)
                {
                    skips.Add(new RecognitionApplySkip(string.Empty, "Recognition batch contains a null review row."));
                    continue;
                }

                try
                {
                    if (result.TopCandidate == null)
                        throw new InvalidOperationException("No recognition candidate is available.");
                    if (!seen.Add(result.Handle))
                        throw new InvalidOperationException("Duplicate CAD handle in recognition batch.");
                    items.Add(PrepareOne(document, project, result, requireAutoAcceptance: true));
                }
                catch (Exception ex)
                {
                    skips.Add(new RecognitionApplySkip(result.Handle, ex.Message));
                }
            }

            EnsureProjectUnchanged(document, project, expectedProjectId, version, "Recognition Auto Apply preflight");
            return new RecognitionApplyBatchPlan(version, items.AsReadOnly(), skips.AsReadOnly());
        }

        public static int Commit(Document document, string expectedProjectId, RecognitionApplyBatchPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var project = RequireCurrentProject(document, expectedProjectId, "Recognition Apply commit");
            if (project.ChangeVersion != plan.ProjectChangeVersion)
                throw new InvalidOperationException("Recognition Apply: QS3D project changed after review preflight. No recognition rows were applied; run Recognition again.");
            if (plan.Items.Count == 0 && plan.Skips.Count == 0) return 0;

            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                foreach (var item in plan.Items)
                {
                    if (!SemanticCaptureService.CaptureSnapshot(document, item.Snapshot, item.Category))
                        throw new InvalidOperationException("Recognition Apply: semantic capture returned no committed owner for CAD handle " + item.Handle + ".");

                    var captured = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, item.Handle);
                    if (captured == null || captured.Category != item.Category)
                        throw new InvalidOperationException("Recognition Apply: capture did not produce one matching semantic owner for CAD handle " + item.Handle + ".");
                }

                var audit = AuditTrail.ForProject(project);
                foreach (var item in plan.Items)
                {
                    var captured = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, item.Handle)
                        ?? throw new InvalidOperationException("Recognition Apply: committed owner disappeared before audit for CAD handle " + item.Handle + ".");
                    audit.Record(
                        "recognition.apply",
                        captured.Id,
                        item.RuleId + " • confidence " + item.Confidence.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + " • " + item.EvidenceText);
                }
                foreach (var skip in plan.Skips)
                    audit.Record("recognition.skip", skip.Handle, skip.Reason);

                return plan.Items.Count;
            }
            catch (Exception operationError)
            {
                try
                {
                    rollback.Restore(project);
                }
                catch (Exception restoreError)
                {
                    throw new InvalidOperationException(
                        "Recognition batch failed and project rollback also failed.",
                        new AggregateException(operationError, restoreError));
                }
                throw;
            }
        }

        private static RecognitionApplyItem PrepareOne(
            Document document,
            ProjectState project,
            RecognitionResult result,
            bool requireAutoAcceptance)
        {
            var expectedCandidate = result.TopCandidate
                ?? throw new InvalidOperationException("Recognition Apply: review row has no candidate for CAD handle " + result.Handle + ".");

            var liveSnapshots = EntitySnapshotReader.ReadHandles(document, new[] { result.Handle });
            if (liveSnapshots.Count != 1)
                throw new InvalidOperationException("Recognition Apply: CAD handle " + result.Handle + " no longer exists. Run Recognition again.");

            var refreshed = new ProjectRecognitionService().Suggest(project, liveSnapshots[0]);
            var candidate = refreshed.TopCandidate
                ?? throw new InvalidOperationException("Recognition Apply: CAD handle " + result.Handle + " no longer has a valid candidate. Run Recognition again.");
            if (candidate.Category != expectedCandidate.Category)
                throw new InvalidOperationException(
                    "Recognition Apply: result for " + result.Handle + " changed from " + expectedCandidate.Category + " to " + candidate.Category + ". Run Recognition again before applying.");
            if (!refreshed.IsCaptureReady)
                throw new InvalidOperationException("Recognition Apply: CAD handle " + result.Handle + " is no longer capture-ready: " + refreshed.CaptureReadinessReason);
            if (requireAutoAcceptance && (candidate.Confidence < AutoAcceptConfidence || refreshed.Margin < AutoAcceptMinimumMargin))
                throw new InvalidOperationException(
                    "Recognition Auto Apply: live confidence/margin for CAD handle " + result.Handle + " fell below the auto-accept gate. Review it manually.");

            var collision = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, result.Handle);
            if (collision != null && collision.Category == candidate.Category) collision = null;
            if (collision != null)
                throw new InvalidOperationException("CAD handle " + result.Handle + " already belongs to " + collision.Category + ".");

            return new RecognitionApplyItem(
                refreshed.Snapshot,
                candidate.Category,
                candidate.RuleId,
                candidate.Confidence,
                candidate.EvidenceText);
        }

        private static ProjectState RequireCurrentProject(Document document, string expectedProjectId, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + ": activate the source drawing before applying recognition results.");
            if (!ExistingProjectMutationContext.TryGet(document, out var project))
                throw new InvalidOperationException(operation + ": current QS3D project is no longer available. Run Recognition again.");
            if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(operation + ": QS3D project was replaced after the review opened. Run Recognition again.");
            return project;
        }

        private static void EnsureProjectUnchanged(
            Document document,
            ProjectState project,
            string expectedProjectId,
            long expectedVersion,
            string operation)
        {
            var current = RequireCurrentProject(document, expectedProjectId, operation);
            if (!ReferenceEquals(current, project) || current.ChangeVersion != expectedVersion)
                throw new InvalidOperationException(operation + ": QS3D project changed while recognition rows were being revalidated. Run Recognition again.");
        }

        private static List<RecognitionResult> Materialize(IEnumerable<RecognitionResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (results is ICollection<RecognitionResult> collection && collection.Count > MaxBatchItems)
                throw new InvalidOperationException("Recognition apply supports at most " + MaxBatchItems + " rows.");
            if (results is IReadOnlyCollection<RecognitionResult> readOnlyCollection && readOnlyCollection.Count > MaxBatchItems)
                throw new InvalidOperationException("Recognition apply supports at most " + MaxBatchItems + " rows.");
            var rows = results.Take(MaxBatchItems + 1).ToList();
            if (rows.Count > MaxBatchItems)
                throw new InvalidOperationException("Recognition apply supports at most " + MaxBatchItems + " rows.");
            return rows;
        }
    }
}
