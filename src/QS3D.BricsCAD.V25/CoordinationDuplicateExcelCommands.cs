using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Read-only duplicate review bridge. Detection is delegated to the canonical Core
    /// DuplicateDetectionService; this adapter only binds selected live Solid3d geometry to
    /// stable QS3D semantic ids and current CAD handles for Excel locate evidence.
    /// </summary>
    public sealed class CoordinationDuplicateExcelCommands
    {
        private const int MaxSelectedSolids = 500;

        [CommandMethod("QS3DDUPLICATEEXPORT", CommandFlags.UsePickSet)]
        public void ExportDuplicates()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Duplicate → Excel cần một QS3D project hiện hữu; export không tạo project mới.");
                if (string.IsNullOrWhiteSpace(project.DrawingFingerprint))
                    throw new InvalidOperationException("QS3D project chưa có Drawing Fingerprint để bảo vệ Excel round-trip.");

                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count < 2)
                    throw new InvalidOperationException("QS3DDUPLICATEEXPORT cần ít nhất hai Solid3d đã bind semantic trong selection.");
                if (snapshots.Count > MaxSelectedSolids)
                    throw new InvalidOperationException("QS3DDUPLICATEEXPORT giới hạn " + MaxSelectedSolids + " entity mỗi lần; hãy thu hẹp selection.");

                var semantic = BuildSemanticIndex(project);
                var snapshotByHandle = MepExactClashCommands.BuildSnapshotIndex(snapshots);
                var objectIds = CadHandleService.Resolve(document, snapshotByHandle.Keys);
                var runtime = new Dictionary<string, RuntimeProjection>(StringComparer.OrdinalIgnoreCase);
                var candidates = BuildCandidates(document, objectIds, snapshotByHandle, semantic, runtime, out var skipped);
                if (candidates.Count < 2)
                    throw new InvalidOperationException("Selection không có đủ hai Solid3d live với semantic ownership hợp lệ để kiểm duplicate.");

                // Exact native-bounds equivalence is unit-independent. Near-duplicate tolerance is intentionally
                // not guessed from drawing units here; it remains available in the canonical Core detector and
                // can be enabled by a unit-aware adapter without changing workbook identity/provenance.
                var detector = new DuplicateDetectionService();
                var result = detector.Detect(
                    candidates,
                    new DuplicateDetectionOptions
                    {
                        CoordinateToleranceM = 0d,
                        RequireSameDisciplineForGeometry = true,
                        RequireSameCategoryForGeometry = true,
                        EnableSemanticIdentity = false
                    });
                if (result.Pairs.Count == 0)
                    throw new InvalidOperationException("Selection không có duplicate pair theo canonical detector.");

                var rows = new List<CoordinationDuplicateExportRow>(result.Pairs.Count);
                foreach (var pair in result.Pairs)
                {
                    if (!runtime.TryGetValue(pair.LeftElementId, out var left) ||
                        !runtime.TryGetValue(pair.RightElementId, out var right))
                        throw new InvalidOperationException("Duplicate detector trả về semantic identity không còn trong snapshot hiện tại.");
                    rows.Add(CoordinationDuplicateExportRow.Create(
                        project.DrawingFingerprint,
                        left.ElementId,
                        left.Handle,
                        right.ElementId,
                        right.Handle,
                        pair.MatchKinds,
                        left.Category,
                        right.Category,
                        CommonFloor(left, right)));
                }

                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "QS3D • Duplicate → Excel",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-QS3D-DUPLICATES.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                CoordinationUnifiedWorkbookExporter.Export(
                    dialog.FileName,
                    Array.Empty<CoordinationClashExportRow>(),
                    rows);
                var status = "Duplicate → Excel: " + rows.Count + " pair • semantic solids=" + candidates.Count +
                             " • skipped=" + skipped + " • " + dialog.FileName;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage(
                    "\nQS3D " + status + "\nDùng QS3DEXCELLOCATEDUPLICATE để định vị một dòng DUPLICATES ngược về mô hình.");
            }
            catch (Exception error)
            {
                Report(document, "QS3DDUPLICATEEXPORT", error);
            }
        }

        [CommandMethod("QS3DEXCELLOCATEDUPLICATE", CommandFlags.Modal)]
        public void LocateDuplicateWorkbookRow()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Excel Duplicate → CAD cần một QS3D project hiện hữu; locate không tạo project mới.");
                if (string.IsNullOrWhiteSpace(project.DrawingFingerprint))
                    throw new InvalidOperationException("QS3D project chưa có Drawing Fingerprint để xác minh workbook.");

                var dialog = new OpenFileDialog
                {
                    Title = "QS3D • Excel Duplicate → CAD",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var rowPrompt = new PromptIntegerOptions("\nNhập số dòng DUPLICATES cần định vị: ")
                {
                    AllowNone = false,
                    LowerLimit = 2,
                    UseDefaultValue = true,
                    DefaultValue = 2
                };
                var row = document.Editor.GetInteger(rowPrompt);
                if (row.Status != PromptStatus.OK) return;

                var trace = CoordinationUnifiedWorkbookTraceReader.ReadDuplicate(dialog.FileName, row.Value);
                if (!string.Equals(project.DrawingFingerprint, trace.DrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Workbook thuộc drawing/project khác (Drawing Fingerprint mismatch). Locate bị chặn để tránh chọn nhầm model.");

                // Resolve both members before changing PICKFIRST. Any stale/missing member therefore fails closed
                // and preserves the user's current selection.
                var objectIds = CadHandleService.Resolve(document, new[] { trace.LeftHandle, trace.RightHandle });
                if (objectIds.Count != 2)
                    throw new InvalidOperationException(
                        "Duplicate pair có Handle stale/missing. Locate yêu cầu cả hai entity còn live; selection hiện tại không bị thay đổi.");

                document.Editor.SetImpliedSelection(objectIds.ToArray());
                var status = "Excel Duplicate → CAD dòng " + trace.RowNumber + ": " + trace.DuplicateId + " • 2 CAD object";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
                document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
            }
            catch (Exception error)
            {
                Report(document, "QS3DEXCELLOCATEDUPLICATE", error);
            }
        }

        private static List<DuplicateCandidate> BuildCandidates(
            Document document,
            IReadOnlyList<ObjectId> objectIds,
            IReadOnlyDictionary<string, EntitySnapshot> snapshotByHandle,
            IReadOnlyDictionary<string, SemanticProjection> semantic,
            IDictionary<string, RuntimeProjection> runtime,
            out int skipped)
        {
            var result = new List<DuplicateCandidate>();
            skipped = 0;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var objectId in objectIds)
                {
                    try
                    {
                        var solid = transaction.GetObject(objectId, OpenMode.ForRead, false) as Solid3d;
                        if (solid == null || solid.IsErased)
                        {
                            skipped++;
                            continue;
                        }
                        var handle = CadHandleService.NormalizeHexHandle(solid.Handle.ToString());
                        if (string.IsNullOrWhiteSpace(handle) ||
                            !snapshotByHandle.TryGetValue(handle, out var snapshot) ||
                            !semantic.TryGetValue(handle, out var projection))
                        {
                            skipped++;
                            continue;
                        }
                        if (runtime.ContainsKey(projection.ElementId))
                            throw new InvalidOperationException(
                                "Selection chứa nhiều Solid3d cùng semantic ElementId " + projection.ElementId +
                                "; duplicate export fail-closed để tránh pair identity mơ hồ.");

                        var extents = solid.GeometricExtents;
                        if (!HasFiniteExtents(extents))
                        {
                            skipped++;
                            continue;
                        }
                        var bounds = new AxisAlignedBox(
                            extents.MinPoint.X, extents.MinPoint.Y, extents.MinPoint.Z,
                            extents.MaxPoint.X, extents.MaxPoint.Y, extents.MaxPoint.Z);
                        var system = string.IsNullOrWhiteSpace(snapshot.Layer) ? "Default" : snapshot.Layer.Trim();
                        var region = string.IsNullOrWhiteSpace(projection.Floor) ? "Unassigned" : projection.Floor;
                        var element = new CoordinationElement(
                            projection.ElementId,
                            "QS3D",
                            projection.Category,
                            system,
                            region,
                            bounds);
                        result.Add(new DuplicateCandidate(element));
                        runtime.Add(projection.ElementId, new RuntimeProjection(
                            projection.ElementId,
                            handle,
                            projection.Category,
                            projection.Floor));
                    }
                    catch (Exception error) when (IsRecoverableEntityFailure(error))
                    {
                        skipped++;
                    }
                }
                transaction.Commit();
            }
            return result;
        }

        private static Dictionary<string, SemanticProjection> BuildSemanticIndex(ProjectState project)
        {
            var result = new Dictionary<string, SemanticProjection>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                var floorId = (element.FloorId ?? string.Empty).Trim();
                var floor = floorId.Length == 0 ? null : project.FindFloor(floorId);
                var projection = new SemanticProjection(
                    element.Id,
                    element.Category.ToString(),
                    floor == null ? floorId : floor.Name);
                foreach (var rawHandle in SemanticReferenceHandles.GetSelectionAliases(element))
                {
                    var handle = (CadHandleService.NormalizeHexHandle(rawHandle) ?? string.Empty).Trim();
                    if (handle.Length == 0) continue;
                    if (result.TryGetValue(handle, out var existing) &&
                        !string.Equals(existing.ElementId, element.Id, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "Semantic Handle " + handle + " map tới nhiều element; duplicate export fail-closed.");
                    result[handle] = projection;
                }
            }
            return result;
        }

        private static bool HasFiniteExtents(Extents3d extents) =>
            IsFinite(extents.MinPoint.X) && IsFinite(extents.MinPoint.Y) && IsFinite(extents.MinPoint.Z) &&
            IsFinite(extents.MaxPoint.X) && IsFinite(extents.MaxPoint.Y) && IsFinite(extents.MaxPoint.Z) &&
            extents.MaxPoint.X >= extents.MinPoint.X &&
            extents.MaxPoint.Y >= extents.MinPoint.Y &&
            extents.MaxPoint.Z >= extents.MinPoint.Z;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool IsRecoverableEntityFailure(Exception exception) =>
            !(exception is OutOfMemoryException) &&
            !(exception is StackOverflowException) &&
            !(exception is AccessViolationException);

        private static string CommonFloor(RuntimeProjection left, RuntimeProjection right) =>
            string.Equals(left.Floor, right.Floor, StringComparison.OrdinalIgnoreCase) ? left.Floor : string.Empty;

        private static void Report(Document document, string operation, Exception error)
        {
            var message = error is AggregateException aggregate ? aggregate.GetBaseException().Message : error.Message;
            try { PaletteCoordinator.SetStatus(operation + ": " + message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + operation + ": " + message); } catch { }
        }

        private sealed class SemanticProjection
        {
            internal SemanticProjection(string elementId, string category, string floor)
            {
                ElementId = (elementId ?? string.Empty).Trim();
                Category = string.IsNullOrWhiteSpace(category) ? "Unknown" : category.Trim();
                Floor = (floor ?? string.Empty).Trim();
            }
            internal string ElementId { get; }
            internal string Category { get; }
            internal string Floor { get; }
        }

        private sealed class RuntimeProjection
        {
            internal RuntimeProjection(string elementId, string handle, string category, string floor)
            {
                ElementId = elementId;
                Handle = handle;
                Category = category;
                Floor = floor;
            }
            internal string ElementId { get; }
            internal string Handle { get; }
            internal string Category { get; }
            internal string Floor { get; }
        }
    }
}
