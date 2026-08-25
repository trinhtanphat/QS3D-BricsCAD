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
            int generatedElementsInvalidated,
            int nativeGeometryRebuilt,
            int semanticElementsRegenerated)
        {
            CoreResult = coreResult ?? throw new ArgumentNullException(nameof(coreResult));
            GeneratedElementsInvalidated = generatedElementsInvalidated;
            NativeGeometryRebuilt = nativeGeometryRebuilt;
            SemanticElementsRegenerated = semanticElementsRegenerated;
        }

        public ProjectInterchangeFieldMergeResult CoreResult { get; }
        public int GeneratedElementsInvalidated { get; }
        public int NativeGeometryRebuilt { get; }
        public int SemanticElementsRegenerated { get; }
    }

    /// <summary>
    /// BricsCAD-native transaction boundary for a previously reviewed Core field-merge plan.
    /// Native erasure is prepared while reviewed ownership metadata is intact; Core then re-plans
    /// and verifies exact authorization. Supported generated outputs are rebuilt before the single
    /// outer CAD commit. One semantic/native Undo transition covers invalidate + apply + rebuild,
    /// and an outer ProjectState snapshot restores semantic state if anything fails pre-commit.
    /// </summary>
    internal static class InterchangeFieldMergeImportService
    {
        private const InterchangeGeneratedOutputKind AutomaticRebuildKinds =
            InterchangeGeneratedOutputKind.NativeGeometry |
            InterchangeGeneratedOutputKind.Quantity;

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

            GeneratedNativeCleanupCoverageGuard.EnsureSupported(invalidationTargets);

            ProjectStateSnapshot? rollback = null;
            SourceReconcileUndoCoordinator.PendingTransition? undoTransition = null;
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

                    var lockedInvalidationTargets = ResolveAffectedTargets(
                        lockedProject,
                        reviewedPlan.CorePlan.AffectedTargetElementIds);

                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        EnsureActive(document, "Interchange field merge / native mutation");
                        rollback = ProjectStateSnapshot.Capture(lockedProject);
                        var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(lockedProject);

                        GeneratedNativeCleanupCoverageGuard.EnsureSupported(lockedInvalidationTargets);
                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange field merge / pre-native cleanup");

                        // Build the complete automatic-rebuild manifest while the retiring owner metadata and
                        // reviewed source handles still exist. This is observational and must fail closed before
                        // any native entity is erased.
                        var rebuildPlan = InterchangeFieldMergeGeneratedRebuildPlan.Create(
                            reviewedPlan.CorePlan.AffectedTargetElementIds,
                            AutomaticRebuildKinds);
                        var rebuildManifest = InterchangeFieldMergeGeneratedRebuildExecutor.Prepare(
                            document,
                            lockedProject,
                            lockedInvalidationTargets,
                            rebuildPlan);

                        // Own one Undo marker for the entire FieldMerge command. Child production builders see
                        // the external scope below and deliberately suppress their own semantic/native markers.
                        undoTransition = SourceReconcileUndoCoordinator.BeginTransition(
                            document,
                            transaction,
                            lockedProject,
                            rollback,
                            rollbackStamp);
                        undoTransition.StageNativeMarker();

                        using (SourceReconcileUndoCoordinator.BeginExternalTransitionScope(document))
                        {
                            var invalidation = GeneratedDependentGeometryInvalidator.Prepare(
                                document,
                                transaction,
                                lockedProject,
                                lockedInvalidationTargets);

                            ProjectContextCoordinator.RequireBackingStoreUnchanged(
                                document,
                                lockedProject,
                                "Interchange field merge / pre-core apply");

                            var coreResult = ProjectInterchangeFieldMergeImporter.Import(
                                lockedProject,
                                json,
                                policy,
                                reviewedPlan.Authorization);

                            // Old owner metadata must be gone before production builders claim replacement
                            // generated solids. Rebuild-before-CommitMetadata would let this sweep erase the
                            // newly claimed ownership and create semantic/native divergence.
                            invalidation.CommitMetadata();

                            var rebuildResult = InterchangeFieldMergeGeneratedRebuildExecutor.Execute(
                                document,
                                lockedProject,
                                rebuildManifest);

                            ProjectContextCoordinator.RequireBackingStoreUnchanged(
                                document,
                                lockedProject,
                                "Interchange field merge / pre-CAD commit");

                            undoTransition.StageAfter(
                                lockedProject,
                                ProjectStateSnapshot.Capture(lockedProject));
                            transaction.Commit();
                            undoTransition.ConfirmCommitted();
                            cadCommitted = true;
                            return new InterchangeFieldMergeNativeResult(
                                coreResult,
                                invalidation.ElementCount,
                                rebuildResult.NativeGeometryRebuilt,
                                rebuildResult.SemanticElementsRegenerated);
                        }
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
            finally
            {
                undoTransition?.Dispose();
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
