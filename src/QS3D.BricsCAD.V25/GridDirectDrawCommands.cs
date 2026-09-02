using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Repeated straight-Grid source authoring. Each accepted native LINE is immediately
    /// captured through the existing Grid semantic authority; this class does not own a
    /// second Grid store, numbering engine, system planner, or generated Grid geometry.
    /// </summary>
    public sealed class GridDirectDrawCommands
    {
        private const double CoordinateTolerance = 1e-9d;

        [CommandMethod("QS3DGRIDDRAW", CommandFlags.Modal)]
        public void DrawStraightGridRepeated()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                RequireModelSpace(document);
                var editor = document.Editor;
                var created = 0;

                while (true)
                {
                    var context = CaptureContext(document);
                    var promptUcs = editor.CurrentUserCoordinateSystem;

                    var startOptions = new PromptPointOptions(
                        "\nQS3D Grid - chọn điểm đầu (Enter/Esc để kết thúc): ")
                    {
                        AllowNone = true
                    };
                    var start = editor.GetPoint(startOptions);
                    if (start.Status != PromptStatus.OK) break;

                    var endOptions = new PromptPointOptions(
                        "\nQS3D Grid - chọn điểm cuối (Esc để hủy đoạn hiện tại): ")
                    {
                        UseBasePoint = true,
                        BasePoint = start.Value
                    };
                    var end = editor.GetPoint(endOptions);
                    if (end.Status != PromptStatus.OK) break;
                    if (start.Value.DistanceTo(end.Value) <= CoordinateTolerance)
                    {
                        TryWriteMessage(document, "\nQS3D Grid: bỏ qua đoạn có hai điểm trùng nhau.");
                        continue;
                    }

                    RequireFreshContext(document, context, promptUcs);
                    var source = AppendSourceLine(document, promptUcs, start.Value, end.Value);
                    try
                    {
                        var snapshots = EntitySnapshotReader.ReadHandles(document, new[] { source.Handle });
                        if (snapshots.Count != 1 ||
                            !string.Equals(snapshots[0].EntityType, "Line", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Không đọc lại được đúng LINE nguồn vừa tạo.");

                        if (!SemanticCaptureService.CaptureSnapshot(document, snapshots[0], ElementCategory.Grid))
                            throw new InvalidOperationException("Semantic Grid capture từ LINE nguồn không thành công.");
                    }
                    catch (Exception captureError)
                    {
                        CompensateSourceOrThrow(document, source, captureError);
                        throw;
                    }

                    created++;
                    FinalizeAcceptedSource(document, source.ObjectId, context.FamilyName, created);
                }

                var status = created == 0
                    ? "Grid Direct Draw: không tạo Grid mới."
                    : "Grid Direct Draw: đã tạo/capture " + created + " trục thẳng. Enter/Esc đã kết thúc lệnh.";
                TrySetStatus(status);
                TryWriteMessage(document, "\nQS3D " + status);
            }
            catch (Exception)
            {
                const string message = "QS3DGRIDDRAW lỗi: thao tác không hoàn tất; native/semantic state đã được fail-closed. Kiểm tra Grid Family, DWG/UCS và thử lại.";
                TrySetStatus(message);
                TryWriteMessage(document, "\n" + message);
            }
        }

        private static GridDrawContext CaptureContext(Document document)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("Bản vẽ chưa có QS3D project. Mở Workspace và chọn Grid Family trước khi vẽ.");

            var family = ProjectFamilyActivationService.GetActive(project);
            if (family == null || family.Category != ElementCategory.Grid)
                throw new InvalidOperationException("QS3DGRIDDRAW yêu cầu Grid Family/Type đang active.");
            if (FamilyNameHasSubtype(family.Name, "Lưới Cong"))
                throw new InvalidOperationException("QS3DGRIDDRAW chỉ tạo Grid thẳng. Chọn Family Lưới Thẳng hoặc dùng workflow Grid cong hiện có.");

            return new GridDrawContext(
                project.ProjectId ?? string.Empty,
                project.ChangeVersion,
                family.Id ?? string.Empty,
                family.Name ?? string.Empty,
                project.ActiveFloorId ?? string.Empty,
                project.ActiveZoneId ?? string.Empty);
        }

        private static void RequireFreshContext(Document document, GridDrawContext expected, Matrix3d promptUcs)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("DWG active đã thay đổi trong lúc chọn Grid. Hãy chạy lại lệnh.");

            RequireModelSpace(document);
            if (!document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs))
                throw new InvalidOperationException("UCS đã thay đổi trong lúc chọn Grid. Hãy chạy lại lệnh.");

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("QS3D project không còn khả dụng trước khi commit Grid source.");
            if (!string.Equals(project.ProjectId ?? string.Empty, expected.ProjectId, StringComparison.OrdinalIgnoreCase) ||
                project.ChangeVersion != expected.ChangeVersion)
                throw new InvalidOperationException("QS3D project/Floor/Zone/thuộc tính đã thay đổi trong lúc vẽ Grid.");

            var family = ProjectFamilyActivationService.GetActive(project);
            if (family == null || family.Category != ElementCategory.Grid ||
                !string.Equals(family.Id ?? string.Empty, expected.FamilyId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Grid Family active đã thay đổi trong lúc vẽ.");
            if (FamilyNameHasSubtype(family.Name, "Lưới Cong"))
                throw new InvalidOperationException("Grid Family active đã chuyển sang Lưới Cong trong lúc vẽ.");

            if (!string.Equals(project.ActiveFloorId ?? string.Empty, expected.FloorId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(project.ActiveZoneId ?? string.Empty, expected.ZoneId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Floor/Zone active đã thay đổi trong lúc vẽ Grid.");
        }

        private static CreatedGridSource AppendSourceLine(
            Document document,
            Matrix3d promptUcs,
            Point3d start,
            Point3d end)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var line = new Line(start, end);
                line.SetDatabaseDefaults(document.Database);
                line.TransformBy(promptUcs);
                var objectId = modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line, true);
                var handle = line.Handle.ToString();
                var ownerId = line.OwnerId;
                transaction.Commit();
                return new CreatedGridSource(objectId, handle, ownerId);
            }
        }

        private static void CompensateSourceOrThrow(Document document, CreatedGridSource source, Exception captureError)
        {
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    if (source.ObjectId.IsNull || !source.ObjectId.IsValid || source.ObjectId.IsErased)
                        throw new InvalidOperationException("Created Grid source is no longer a live exact compensation target.");

                    var entity = transaction.GetObject(source.ObjectId, OpenMode.ForWrite, false) as Line;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException("Created Grid source changed type/state before compensation.");
                    if (!string.Equals(entity.Handle.ToString(), source.Handle, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Created Grid source handle changed before compensation.");
                    if (entity.OwnerId != source.OwnerId)
                        throw new InvalidOperationException("Created Grid source owner space changed before compensation.");

                    entity.Erase();
                    transaction.Commit();
                }
            }
            catch (Exception cleanupError)
            {
                throw new InvalidOperationException(
                    "Grid semantic capture failed and exact native-source compensation could not be proven.",
                    new AggregateException(captureError, cleanupError));
            }
        }

        private static void FinalizeAcceptedSource(Document document, ObjectId sourceId, string familyName, int count)
        {
            var uiSyncFailed = false;
            try
            {
                if (!sourceId.IsNull && sourceId.IsValid) document.Editor.SetImpliedSelection(new[] { sourceId });
                document.Editor.Regen();
            }
            catch (Exception)
            {
                uiSyncFailed = true;
            }

            var status = "Đã tạo Grid thẳng #" + count + " • Family “" + familyName + "”. Chọn điểm đầu tiếp theo hoặc Enter/Esc để kết thúc.";
            TrySetStatus(status);
            TryWriteMessage(document, "\nQS3D " + status);
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Grid: native + semantic source đã commit; một phần UI review không thể đồng bộ.");
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                if (!document.Database.CurrentSpaceId.Equals(modelSpaceId))
                    throw new InvalidOperationException("QS3DGRIDDRAW hiện chỉ hỗ trợ Model Space.");
                transaction.Commit();
            }
        }

        private static bool FamilyNameHasSubtype(string familyName, string subtype)
        {
            var name = (familyName ?? string.Empty).Trim();
            var prefix = (subtype ?? string.Empty).Trim();
            if (string.Equals(name, prefix, StringComparison.OrdinalIgnoreCase)) return true;
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || name.Length <= prefix.Length) return false;
            var separator = name[prefix.Length];
            return separator == '-' || separator == '_' || char.IsWhiteSpace(separator);
        }

        private static void TrySetStatus(string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }

        private sealed class GridDrawContext
        {
            public GridDrawContext(
                string projectId,
                long changeVersion,
                string familyId,
                string familyName,
                string floorId,
                string zoneId)
            {
                ProjectId = projectId;
                ChangeVersion = changeVersion;
                FamilyId = familyId;
                FamilyName = familyName;
                FloorId = floorId;
                ZoneId = zoneId;
            }

            public string ProjectId { get; }
            public long ChangeVersion { get; }
            public string FamilyId { get; }
            public string FamilyName { get; }
            public string FloorId { get; }
            public string ZoneId { get; }
        }

        private sealed class CreatedGridSource
        {
            public CreatedGridSource(ObjectId objectId, string handle, ObjectId ownerId)
            {
                ObjectId = objectId;
                Handle = handle ?? string.Empty;
                OwnerId = ownerId;
            }

            public ObjectId ObjectId { get; }
            public string Handle { get; }
            public ObjectId OwnerId { get; }
        }
    }
}