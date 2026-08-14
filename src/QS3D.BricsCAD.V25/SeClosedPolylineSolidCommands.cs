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
                if (sources.Count == 0)
                {
                    Report(document, "SE: no usable source was selected.");
                    return;
                }

                var originalHandles = sources.Select(x => x.Handle).ToArray();
                var sourceIds = new List<ObjectId>(sources.Count);

                // Validate the complete input batch before any semantic mutation. This keeps a bad
                // profile from leaving a successfully-created subset behind.
                foreach (var snapshot in sources)
                    sourceIds.Add(RequireClosedPolylineSource(document, snapshot.Handle));

                var batchRollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    foreach (var snapshot in sources)
                    {
                        activeFamily = ProjectFamilyActivationService.GetActive(project);
                        RequireSameActiveFamily(activeFamily, expectedFamilyId, expectedCategory);
                        if (!SemanticCaptureActiveFamilyAdapter.CaptureSnapshot(document, project, snapshot, activeFamily!))
                            throw new InvalidOperationException("Semantic capture did not update source " + snapshot.Handle + ".");
                    }

                    // StructuralSolidBuilder owns one CAD transaction for the implied selection.
                    // Supplying the whole batch at once makes a builder exception abort every
                    // native solid instead of committing profile-by-profile.
                    document.Editor.SetImpliedSelection(sourceIds.ToArray());
                    var built = StructuralSolidBuilder.BuildSelected(document, project, expectedCategory);
                    if (built != sources.Count)
                        throw new InvalidOperationException(
                            "Native solid builder created " + built + " of " + sources.Count + " requested solids.");

                    Report(document, "SE: " + built + "/" + sources.Count + " source(s) created native solids atomically; source polylines were retained.");
                }
                catch (Exception batchError)
                {
                    try
                    {
                        batchRollback.Restore(project);
                    }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "SE batch failed and project rollback also failed.",
                            new AggregateException(batchError, restoreError));
                    }

                    throw new InvalidOperationException(
                        "SE batch failed; QS3D semantic changes were rolled back. " + batchError.Message,
                        batchError);
                }
                finally
                {
                    RestoreSelection(document, originalHandles);
                }
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

                var normal = polyline.Normal;
                if (Math.Abs(normal.X) > 1e-9 ||
                    Math.Abs(normal.Y) > 1e-9 ||
                    Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9)
                    throw new InvalidOperationException("SE requires a planar 2D POLYLINE parallel to the drawing XY plane.");

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
