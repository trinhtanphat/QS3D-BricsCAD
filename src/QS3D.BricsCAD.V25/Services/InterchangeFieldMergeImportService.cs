using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class InterchangeFieldMergeNativePlan
    {
        internal InterchangeFieldMergeNativePlan(
            ProjectInterchangeFieldMergeExecutionPlan corePlan,
            ProjectInterchangeFieldMergeAuthorization? authorization)
        {
            CorePlan = corePlan ?? throw new ArgumentNullException(nameof(corePlan));
            Authorization = authorization;
        }

        public ProjectInterchangeFieldMergeExecutionPlan CorePlan { get; }
        public ProjectInterchangeFieldMergeAuthorization? Authorization { get; }
        public bool CanExecute => CorePlan.CanExecute && Authorization != null;
    }

    internal sealed class InterchangeFieldMergeNativeResult
    {
        internal InterchangeFieldMergeNativeResult(
            ProjectInterchangeFieldMergeResult coreResult,
            int generatedElementsInvalidated)
        {
            CoreResult = coreResult ?? throw new ArgumentNullException(nameof(coreResult));
            GeneratedElementsInvalidated = generatedElementsInvalidated;
        }

        public ProjectInterchangeFieldMergeResult CoreResult { get; }
        public int GeneratedElementsInvalidated { get; }
    }

    /// <summary>
    /// BricsCAD-native transaction boundary for a previously reviewed Core field-merge plan.
    /// Native erasure is prepared while the reviewed target ownership metadata is still intact;
    /// the Core importer then re-plans and verifies its exact authorization before semantic mutation.
    /// CAD commit happens only after the Core apply succeeds. An outer ProjectState snapshot restores
    /// semantic state when the native transaction aborts or commit fails.
    /// </summary>
    internal static class InterchangeFieldMergeImportService
    {
        public static InterchangeFieldMergeNativePlan Plan(
            ProjectState target,
            string json,
            ProjectInterchangeFieldMergePolicy policy)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var corePlan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);
            var authorization = corePlan.CanExecute ? corePlan.CreateAuthorization() : null;
            return new InterchangeFieldMergeNativePlan(corePlan, authorization);
        }

        public static InterchangeFieldMergeNativeResult Import(
            Document document,
            string json,
            ProjectInterchangeFieldMergePolicy policy,
            InterchangeFieldMergeNativePlan reviewedPlan)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (reviewedPlan == null) throw new ArgumentNullException(nameof(reviewedPlan));
            if (!reviewedPlan.CanExecute || reviewedPlan.Authorization == null)
                throw new InvalidOperationException("Field merge requires an executable reviewed plan and its exact authorization.");

            EnsureActive(document, "Interchange field merge");
            var project = ExistingProjectMutationContext.Require(document, "Interchange field merge");
            var invalidationTargets = ResolveAffectedTargets(project, reviewedPlan.CorePlan.AffectedTargetElementIds);

            // Core recognizes Generated*Handle(s) owner slots generically, but the native invalidator
            // can erase only the slots for which BricsCAD liveness/ownership/erase handlers exist.
            // Refuse an unsupported or split physical-opening owner alias before a CAD transaction
            // can erase anything or Core can clear the corresponding ownership metadata.
            GeneratedNativeCleanupCoverageGuard.EnsureSupported(invalidationTargets);

            ProjectStateSnapshot? rollback = null;
            var cadCommitted = false;

            try
            {
                using (document.LockDocument())
                {
                    EnsureActive(document, "Interchange field merge / locked mutation");
                    var lockedProject = ExistingProjectMutationContext.Require(document, "Interchange field merge / locked mutation");
                    if (!ReferenceEquals(lockedProject, project))
                        throw new InvalidOperationException(
                            "Interchange field merge target project changed before the native mutation lock was acquired. Re-plan and review the merge.");

                    // Never carry pre-lock element references into destructive native work. Re-resolve
                    // the reviewed affected ids from the exact canonical project under the document lock.
                    var lockedInvalidationTargets = ResolveAffectedTargets(
                        lockedProject,
                        reviewedPlan.CorePlan.AffectedTargetElementIds);

                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        EnsureActive(document, "Interchange field merge / native mutation");
                        rollback = ProjectStateSnapshot.Capture(lockedProject);

                        // Repeat the coverage check under the document lock so a modeless/event callback
                        // cannot swap generated owner-slot metadata between the early precheck and native
                        // invalidation. This check must remain immediately before destructive preparation.
                        GeneratedNativeCleanupCoverageGuard.EnsureSupported(lockedInvalidationTargets);
                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange field merge / pre-native cleanup");

                        // Prepare native erasure before Core mutation while the target's reviewed generated
                        // handle metadata still exists. Prepare is rollback-capable and does not clear semantic
                        // ownership metadata. Core Import re-plans next and rejects stale target/source/policy/
                        // handle authorization before any semantic mutation can be accepted.
                        var invalidation = GeneratedDependentGeometryInvalidator.Prepare(
                            document,
                            transaction,
                            lockedProject,
                            lockedInvalidationTargets);

                        // A document lock cannot prevent an external process from replacing the sidecar.
                        // Recheck after native preparation while CAD erasure is still uncommitted so a changed
                        // backing store aborts the transaction before any semantic source data is applied.
                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange field merge / pre-core apply");

                        var coreResult = ProjectInterchangeFieldMergeImporter.Import(
                            lockedProject,
                            json,
                            policy,
                            reviewedPlan.Authorization);

                        // Core clears generated/native ownership metadata for the full affected closure after
                        // authorization succeeds. CommitMetadata is intentionally retained as the native
                        // invalidator's final parity sweep; after the Core clear it is idempotent.
                        invalidation.CommitMetadata();

                        // Core mutation and metadata cleanup are still rollback-capable until CAD commit.
                        // Refuse to commit against a sidecar revision that changed during either phase.
                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange field merge / pre-CAD commit");

                        transaction.Commit();
                        cadCommitted = true;
                        return new InterchangeFieldMergeNativeResult(coreResult, invalidation.ElementCount);
                    }
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted && rollback != null)
                {
                    try
                    {
                        rollback.Restore(project);
                    }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Interchange field merge failed before CAD commit and semantic rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        private static IReadOnlyList<ProjectElement> ResolveAffectedTargets(
            ProjectState project,
            IEnumerable<string> affectedIds)
        {
            var result = new List<ProjectElement>();
            foreach (var id in (affectedIds ?? Enumerable.Empty<string>())
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var element = project.FindElement(id);
                if (element == null)
                    throw new InvalidOperationException(
                        "Reviewed field-merge affected element disappeared before native mutation: " + id + ". Re-plan and review the merge.");
                result.Add(element);
            }
            return result.AsReadOnly();
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the DWG that started the operation to remain active.");
        }
    }
}
