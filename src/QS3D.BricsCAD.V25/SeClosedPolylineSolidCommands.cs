using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SeClosedPolylineSolidCommands
    {
        private static readonly HashSet<ElementCategory> SupportedCategories = new HashSet<ElementCategory>
        {
            ElementCategory.Slab,
            ElementCategory.Foundation,
            ElementCategory.Stair,
            ElementCategory.Earthwork,
            ElementCategory.Column
        };

        [CommandMethod("SE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CreateSolidFromClosedPolylines()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var observedProject))
                {
                    Report(document, "SE requires an existing QS3D project.");
                    return;
                }

                var observedFamily = ProjectFamilyActivationService.GetActive(observedProject);
                if (observedFamily == null)
                {
                    Report(document, "SE requires an active Family/Type.");
                    return;
                }
                if (!SupportedCategories.Contains(observedFamily.Category))
                {
                    Report(document, "SE supports closed-polyline solids only for Slab, Foundation, Stair, Earthwork, and Column.");
                    return;
                }

                var expectedProjectId = observedProject.ProjectId;
                var expectedChangeVersion = observedProject.ChangeVersion;
                var expectedFamilyId = observedFamily.Id;
                var expectedCategory = observedFamily.Category;

                var selected = EntitySnapshotReader.ReadCurrentSelection(document);
                if (selected.Count == 0)
                {
                    Report(document, "SE: no source was selected.");
                    return;
                }

                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException("The active drawing changed during selection.");

                var project = ExistingProjectMutationContext.Require(document, "SE");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("The QS3D project changed during selection. Run SE again.");

                var activeFamily = ProjectFamilyActivationService.GetActive(project);
                RequireSameActiveFamily(activeFamily, expectedFamilyId, expectedCategory);

                var sources = selected
                    .Where(x => x != null)
                    .GroupBy(x => x.Handle, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
                var originalHandles = sources.Select(x => x.Handle).ToArray();
                var failures = new List<string>();
                var successCount = 0;

                try
                {
                    foreach (var snapshot in sources)
                    {
                        var rollback = ProjectStateSnapshot.Capture(project);
                        try
                        {
                            activeFamily = ProjectFamilyActivationService.GetActive(project);
                            RequireSameActiveFamily(activeFamily, expectedFamilyId, expectedCategory);

                            var sourceId = RequireClosedPolylineSource(document, snapshot.Handle);
                            if (!SemanticCaptureActiveFamilyAdapter.CaptureSnapshot(document, project, snapshot, activeFamily!))
                                throw new InvalidOperationException("Semantic capture did not update an element.");

                            document.Editor.SetImpliedSelection(new[] { sourceId });
                            if (StructuralSolidBuilder.BuildSelected(document, project, expectedCategory) != 1)
                                throw new InvalidOperationException("Native solid builder did not create exactly one solid.");

                            successCount++;
                        }
                        catch (Exception itemError)
                        {
                            try
                            {
                                rollback.Restore(project);
                            }
                            catch (Exception restoreError)
                            {
                                throw new InvalidOperationException(
                                    "SE item failed and project rollback also failed.",
                                    new AggregateException(itemError, restoreError));
                            }
                            failures.Add(snapshot.Handle + ": " + itemError.Message);
                        }
                    }
                }
                finally
                {
                    RestoreSelection(document, originalHandles);
                }

                Report(document, "SE: " + successCount + "/" + sources.Count + " source(s) created native solids; source polylines were retained.");
                for (var i = 0; i < Math.Min(failures.Count, 10); i++)
                    Report(document, "SE skipped " + failures[i]);
                if (failures.Count > 10)
                    Report(document, "SE: " + (failures.Count - 10) + " additional item error(s).");
            }
            catch (Exception ex)
            {
                Report(document, "SE error: " + ex.Message);
            }
        }

        private static ObjectId RequireClosedPolylineSource(Document document, string handle)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1)
                throw new InvalidOperationException("Source handle does not resolve uniquely in the active drawing.");

            var id = ids[0];
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (!(entity is Polyline polyline) || entity.IsErased)
                    throw new InvalidOperationException("SE accepts live POLYLINE sources only.");
                if (!polyline.Closed)
                    throw new InvalidOperationException("SE accepts closed POLYLINE sources only.");

                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                if (polyline.OwnerId != blockTable[BlockTableRecord.ModelSpace])
                    throw new InvalidOperationException("SE accepts Model Space sources only.");
                transaction.Commit();
            }
            return id;
        }

        private static void RequireSameActiveFamily(ProjectFamily? family, string expectedFamilyId, ElementCategory expectedCategory)
        {
            if (family == null ||
                !string.Equals(family.Id, expectedFamilyId, StringComparison.OrdinalIgnoreCase) ||
                family.Category != expectedCategory)
                throw new InvalidOperationException("Active Family changed during SE. Run the command again.");
        }

        private static void RestoreSelection(Document document, IEnumerable<string> handles)
        {
            try
            {
                var ids = CadHandleService.Resolve(document, handles);
                document.Editor.SetImpliedSelection(ids.Count == 0 ? Array.Empty<ObjectId>() : ids.ToArray());
            }
            catch
            {
                try { CadHandleService.ClearSelection(document); } catch { }
            }
        }

        private static void Report(Document document, string message)
        {
            document.Editor.WriteMessage("\n" + message);
        }
    }
}
