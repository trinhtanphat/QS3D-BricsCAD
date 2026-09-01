using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// First-class guarded editing for authoritative QS3D source CAD.
    ///
    /// MOVE and ROTATE are deliberately implemented as invertible native entity transforms and
    /// immediately flow through the existing SourceReconcileService. This is not a second semantic
    /// or generated-geometry model. STRETCH/grip/jig remain separate follow-up work because their
    /// topology/vertex semantics cannot be represented truthfully as a generic scale transform.
    /// </summary>
    public sealed class SourceEditCommands
    {
        private const double PointTolerance = 1e-9d;
        private const double AngleTolerance = 1e-12d;

        [CommandMethod("QS3DEDITSOURCE", CommandFlags.UsePickSet)]
        public void EditSource()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var selection = CaptureAuthoritativeSelection(document);
                if (selection == null) return;

                var operation = PromptOperation(document);
                if (operation == SourceEditOperation.None) return;

                var transform = PromptTransform(document, operation);
                if (transform == null) return;

                RequireFreshSelection(document, selection);
                ApplyTransform(document, selection, transform.Value.Forward);

                SourceReconcileResult reconcile;
                try
                {
                    document.Editor.SetImpliedSelection(selection.ObjectIds);
                    reconcile = SourceReconcileService.ReconcileSelection(document);
                }
                catch (Exception reconcileError)
                {
                    try
                    {
                        ApplyTransform(document, selection, transform.Value.Inverse);
                        try { document.Editor.SetImpliedSelection(selection.ObjectIds); } catch { }
                    }
                    catch (Exception rollbackError)
                    {
                        throw new InvalidOperationException(
                            "QS3DEDITSOURCE reconcile failed and the authoritative CAD transform could not be reversed safely. Run UNDO and repair/reconcile the source before continuing.",
                            new AggregateException(reconcileError, rollbackError));
                    }

                    throw new InvalidOperationException(
                        "QS3DEDITSOURCE reconcile failed; the authoritative CAD transform was reversed. No edited source geometry was intentionally retained.",
                        reconcileError);
                }

                FinalizeSuccess(document, operation, reconcile);
            }
            catch (Exception)
            {
                ReportFailure(document, "QS3DEDITSOURCE lỗi: không thể hoàn tất edit/reconcile source CAD đã chọn.");
            }
        }

        private static SourceEditSelection? CaptureAuthoritativeSelection(Document document)
        {
            EnsureActive(document, "selection");
            var editor = document.Editor;
            var selected = editor.SelectImplied();
            if (selected.Status != PromptStatus.OK || selected.Value == null)
            {
                var options = new PromptSelectionOptions
                {
                    MessageForAdding = "\nQS3D Edit Source - chọn source CAD đang được QS3D theo dõi: "
                };
                selected = editor.GetSelection(options);
                if (selected.Status != PromptStatus.OK || selected.Value == null) return null;
            }

            var objectIds = selected.Value.GetObjectIds()
                .Where(x => !x.IsNull && x.IsValid && !x.IsErased)
                .ToArray();
            if (objectIds.Length == 0) return null;
            if (objectIds.Length != selected.Value.Count)
                throw new InvalidOperationException("Selection contains invalid/erased CAD objects; select authoritative live source entities only.");

            editor.SetImpliedSelection(objectIds);
            var snapshots = EntitySnapshotReader.ReadImpliedSelection(document);
            if (snapshots.Count != objectIds.Length)
                throw new InvalidOperationException("Could not snapshot every selected CAD object. Select authoritative entity objects only.");

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("QS3DEDITSOURCE yêu cầu QS3D project hiện hữu; lệnh không tạo project mới.");

            var handles = snapshots.Select(x => NormalizeHandle(x.Handle)).ToArray();
            ValidateAuthoritativeOwnership(project, handles);
            return new SourceEditSelection(project.ProjectId, project.ChangeVersion, objectIds, handles);
        }

        private static void ValidateAuthoritativeOwnership(ProjectState project, IReadOnlyList<string> handles)
        {
            var generatedOwners = GeneratedHandleOwnershipIndex.Build(project);
            foreach (var handle in handles)
            {
                if (generatedOwners.TryFindOwner(handle, out var generatedOwner, out var generatedSlot))
                    throw new InvalidOperationException(
                        "Selected handle " + handle + " is QS3D-generated output owned by " + generatedOwner!.Id + "/" + generatedSlot +
                        ". Select the authoritative source CAD instead.");
            }

            var resolvedElements = SemanticHandleOwnershipResolver.Resolve(project, handles);
            var sourceOwners = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in resolvedElements)
            {
                if (element.SourceHandles.Count != 1)
                    throw new InvalidOperationException(
                        "QS3DEDITSOURCE P0 requires exactly one authoritative source handle per semantic element: " + element.Id + ".");

                var sourceHandle = NormalizeHandle(element.SourceHandles[0]);
                if (sourceOwners.ContainsKey(sourceHandle))
                    throw new InvalidOperationException(
                        "CAD source handle " + sourceHandle + " is claimed by multiple semantic elements. Repair source ownership before editing.");
                sourceOwners.Add(sourceHandle, element);
            }

            var seenElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in handles)
            {
                if (!sourceOwners.TryGetValue(handle, out var element))
                    throw new InvalidOperationException(
                        "Selected CAD source is not tracked by QS3D: " + handle + ". Capture it first instead of editing an unknown source.");
                if (!seenElements.Add(element.Id))
                    throw new InvalidOperationException(
                        "Multiple selected CAD objects resolve to semantic element " + element.Id + ". Edit one authoritative source per element.");
            }
        }

        private static SourceEditOperation PromptOperation(Document document)
        {
            var options = new PromptKeywordOptions("\nQS3D Edit Source [Move/Rotate] <Move>: ")
            {
                AllowNone = true
            };
            options.Keywords.Add("Move");
            options.Keywords.Add("Rotate");
            var result = document.Editor.GetKeywords(options);
            if (result.Status == PromptStatus.None) return SourceEditOperation.Move;
            if (result.Status != PromptStatus.OK) return SourceEditOperation.None;
            return string.Equals(result.StringResult, "Rotate", StringComparison.OrdinalIgnoreCase)
                ? SourceEditOperation.Rotate
                : SourceEditOperation.Move;
        }

        private static SourceEditTransform? PromptTransform(Document document, SourceEditOperation operation)
        {
            EnsureActive(document, "prompt");
            var editor = document.Editor;
            var promptUcs = editor.CurrentUserCoordinateSystem;
            var baseResult = editor.GetPoint(new PromptPointOptions(
                operation == SourceEditOperation.Move
                    ? "\nQS3D Edit Source MOVE - chọn điểm gốc: "
                    : "\nQS3D Edit Source ROTATE - chọn tâm xoay: "));
            if (baseResult.Status != PromptStatus.OK) return null;

            if (operation == SourceEditOperation.Move)
            {
                var targetOptions = new PromptPointOptions("\nQS3D Edit Source MOVE - chọn điểm thứ hai: ")
                {
                    UseBasePoint = true,
                    BasePoint = baseResult.Value
                };
                var targetResult = editor.GetPoint(targetOptions);
                if (targetResult.Status != PromptStatus.OK) return null;

                RequirePromptContext(document, promptUcs);
                var baseWcs = baseResult.Value.TransformBy(promptUcs);
                var targetWcs = targetResult.Value.TransformBy(promptUcs);
                var displacement = targetWcs - baseWcs;
                if (displacement.Length <= PointTolerance) return null;
                return SourceEditTransform.Create(Matrix3d.Displacement(displacement));
            }

            var angleOptions = new PromptAngleOptions("\nQS3D Edit Source ROTATE - nhập/chọn góc xoay: ")
            {
                UseBasePoint = true,
                BasePoint = baseResult.Value,
                AllowNone = false
            };
            var angleResult = editor.GetAngle(angleOptions);
            if (angleResult.Status != PromptStatus.OK) return null;
            if (double.IsNaN(angleResult.Value) || double.IsInfinity(angleResult.Value) || Math.Abs(angleResult.Value) <= AngleTolerance)
                return null;

            RequirePromptContext(document, promptUcs);
            var rotationBaseWcs = baseResult.Value.TransformBy(promptUcs);
            var originWcs = Point3d.Origin.TransformBy(promptUcs);
            var zPointWcs = new Point3d(0d, 0d, 1d).TransformBy(promptUcs);
            var rotationAxis = (zPointWcs - originWcs).GetNormal();
            return SourceEditTransform.Create(Matrix3d.Rotation(angleResult.Value, rotationAxis, rotationBaseWcs));
        }

        private static void RequireFreshSelection(Document document, SourceEditSelection expected)
        {
            EnsureActive(document, "commit");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("QS3D project không còn khả dụng trước khi edit commit.");
            if (!string.Equals(project.ProjectId, expected.ProjectId, StringComparison.OrdinalIgnoreCase) ||
                project.ChangeVersion != expected.ProjectChangeVersion)
                throw new InvalidOperationException("QS3D project đã thay đổi trong lúc chọn thao tác edit. Hãy chạy lại QS3DEDITSOURCE.");
            ValidateAuthoritativeOwnership(project, expected.Handles);
        }

        private static void RequirePromptContext(Document document, Matrix3d expectedUcs)
        {
            EnsureActive(document, "prompt completion");
            if (!document.Editor.CurrentUserCoordinateSystem.Equals(expectedUcs))
                throw new InvalidOperationException("UCS đã thay đổi trong lúc nhập thao tác edit. Hãy chạy lại QS3DEDITSOURCE.");
        }

        private static void ApplyTransform(Document document, SourceEditSelection selection, Matrix3d transform)
        {
            EnsureActive(document, "CAD transform");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                for (var index = 0; index < selection.ObjectIds.Length; index++)
                {
                    var id = selection.ObjectIds[index];
                    if (id.IsNull || !id.IsValid || id.IsErased)
                        throw new InvalidOperationException("Authoritative CAD source became invalid before edit commit.");
                    var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                    if (entity == null)
                        throw new InvalidOperationException("Selected authoritative source is no longer a writable CAD entity.");
                    var liveHandle = NormalizeHandle(entity.Handle.ToString());
                    if (!string.Equals(liveHandle, selection.Handles[index], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Authoritative source identity changed before edit commit.");
                    entity.TransformBy(transform);
                }
                transaction.Commit();
            }
        }

        private static void EnsureActive(Document document, string phase)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("QS3DEDITSOURCE " + phase + ": DWG active đã thay đổi; không mutation file cũ.");
        }

        private static string NormalizeHandle(string handle) =>
            QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);

        private static void FinalizeSuccess(Document document, SourceEditOperation operation, SourceReconcileResult result)
        {
            var status = "Edit Source " + operation.ToString().ToUpperInvariant() + ": đã edit + reconcile " + result.Elements +
                " semantic source • regenerate " + result.Regenerated +
                ". Generated dependents stale đã được invalidate/remove theo ownership; rebuild native output khi cần.";
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { document.Editor.Regen(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { uiSyncFailed = true; }
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Edit Source UI sync warning: edit + reconcile đã hoàn tất; một phần UI không thể đồng bộ.");
        }

        private static void ReportFailure(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWriteMessage(document, "\n" + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }

        private enum SourceEditOperation
        {
            None,
            Move,
            Rotate
        }

        private sealed class SourceEditSelection
        {
            public SourceEditSelection(string projectId, long projectChangeVersion, ObjectId[] objectIds, string[] handles)
            {
                ProjectId = projectId ?? string.Empty;
                ProjectChangeVersion = projectChangeVersion;
                ObjectIds = objectIds ?? Array.Empty<ObjectId>();
                Handles = handles ?? Array.Empty<string>();
                if (ObjectIds.Length != Handles.Length)
                    throw new ArgumentException("Source edit selection object/handle cardinality must match.");
            }

            public string ProjectId { get; }
            public long ProjectChangeVersion { get; }
            public ObjectId[] ObjectIds { get; }
            public string[] Handles { get; }
        }

        private readonly struct SourceEditTransform
        {
            private SourceEditTransform(Matrix3d forward, Matrix3d inverse)
            {
                Forward = forward;
                Inverse = inverse;
            }

            public Matrix3d Forward { get; }
            public Matrix3d Inverse { get; }

            public static SourceEditTransform Create(Matrix3d forward) =>
                new SourceEditTransform(forward, forward.Inverse());
        }
    }
}
