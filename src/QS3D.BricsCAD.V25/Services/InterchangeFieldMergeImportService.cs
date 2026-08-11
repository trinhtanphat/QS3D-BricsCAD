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
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    EnsureActive(document, "Interchange field merge / native mutation");

                    // Prepare native erasure before Core mutation while the target's reviewed generated
                    // handle metadata still exists. Prepare is rollback-capable and does not clear semantic
                    // ownership metadata. Core Import re-plans next and rejects stale target/source/policy/
                    // handle authorization before any semantic mutation can be accepted.
                    var invalidation = GeneratedDependentGeometryInvalidator.Prepare(
                        document,
                        transaction,
                        project,
                        invalidationTargets);

                    var coreResult = ProjectInterchangeFieldMergeImporter.Import(
                        project,
                        json,
                        policy,
                        reviewedPlan.Authorization);

                    // Core clears generated/native ownership metadata for the full affected closure after
                    // authorization succeeds. CommitMetadata is intentionally retained as the native
                    // invalidator's final parity sweep; after the Core clear it is idempotent.
                    invalidation.CommitMetadata();

                    transaction.Commit();
                    cadCommitted = true;
                    return new InterchangeFieldMergeNativeResult(coreResult, invalidation.ElementCount);
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
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
