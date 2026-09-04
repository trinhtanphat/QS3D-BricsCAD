using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// First-version BLT-familiar basic drafting tools from the owner reference.
    ///
    /// These commands deliberately create native BricsCAD drafting geometry only. They do not
    /// pretend that an arbitrary rectangle/circle is a category-specific BIM element. The active
    /// QS3D Family/Type is nevertheless a real command precondition and is persisted on each
    /// operation-owned entity through versioned XData so changing the Family changes the context
    /// consumed by the next command rather than only changing a label in the palette.
    ///
    /// Category-specific semantic/native authoring remains owned by QS3DDRAWACTIVE / QS3DDRAW*.
    /// </summary>
    public sealed class BasicDrawingCommands
    {
        private const string RegAppName = "QS3DBASICDRAW";
        private const string MarkerVersion = "1";
        private const double CoordinateTolerance = 1e-9d;

        [CommandMethod("QS3DDRAWLINE", CommandFlags.Modal)]
        public void DrawLine()
        {
            Run("QS3DDRAWLINE", document =>
            {
                RequireModelSpace(document);
                var context = CaptureContext(document, "QS3DDRAWLINE");
                var editor = document.Editor;
                var promptUcs = editor.CurrentUserCoordinateSystem;

                var startResult = editor.GetPoint(new PromptPointOptions("\nQS3D Đường - chọn điểm đầu: "));
                if (startResult.Status != PromptStatus.OK) return;

                var endOptions = new PromptPointOptions("\nQS3D Đường - chọn điểm cuối: ")
                {
                    UseBasePoint = true,
                    BasePoint = startResult.Value
                };
                var endResult = editor.GetPoint(endOptions);
                if (endResult.Status != PromptStatus.OK) return;
                if (startResult.Value.DistanceTo(endResult.Value) <= CoordinateTolerance)
                    throw new InvalidOperationException("Hai điểm của đường thẳng không được trùng nhau.");

                RequireFreshContext(document, context, promptUcs, "QS3DDRAWLINE");
                var start = ToPromptUcsPoint(startResult.Value, promptUcs);
                var end = ToPromptUcsPoint(endResult.Value, promptUcs);
                var id = AppendEntity(
                    document,
                    promptUcs,
                    context,
                    BasicPrimitiveKind.Line,
                    () => new Line(start, end));
                FinalizeSuccess(document, id, context, "Đường");
            });
        }

        [CommandMethod("QS3DDRAWRECT", CommandFlags.Modal)]
        public void DrawRectangle()
        {
            Run("QS3DDRAWRECT", document =>
            {
                RequireModelSpace(document);
                var context = CaptureContext(document, "QS3DDRAWRECT");
                var editor = document.Editor;
                var promptUcs = editor.CurrentUserCoordinateSystem;

                var firstResult = editor.GetPoint(new PromptPointOptions("\nQS3D Chữ nhật - chọn góc thứ nhất: "));
                if (firstResult.Status != PromptStatus.OK) return;

                var oppositeOptions = new PromptPointOptions("\nQS3D Chữ nhật - chọn góc đối diện: ")
                {
                    UseBasePoint = true,
                    BasePoint = firstResult.Value
                };
                var oppositeResult = editor.GetPoint(oppositeOptions);
                if (oppositeResult.Status != PromptStatus.OK) return;

                RequireFreshContext(document, context, promptUcs, "QS3DDRAWRECT");
                var first = ToPromptUcsPoint(firstResult.Value, promptUcs);
                var opposite = ToPromptUcsPoint(oppositeResult.Value, promptUcs);
                if (Math.Abs(first.X - opposite.X) <= CoordinateTolerance ||
                    Math.Abs(first.Y - opposite.Y) <= CoordinateTolerance)
                    throw new InvalidOperationException("Hình chữ nhật phải có chiều rộng và chiều cao khác 0 trong UCS hiện tại.");

                var id = AppendEntity(
                    document,
                    promptUcs,
                    context,
                    BasicPrimitiveKind.Rectangle,
                    () => CreateRectangle(first, opposite));
                FinalizeSuccess(document, id, context, "Chữ nhật");
            });
        }

        [CommandMethod("QS3DDRAWCIRCLE", CommandFlags.Modal)]
        public void DrawCircle()
        {
            Run("QS3DDRAWCIRCLE", document =>
            {
                RequireModelSpace(document);
                var context = CaptureContext(document, "QS3DDRAWCIRCLE");
                var editor = document.Editor;
                var promptUcs = editor.CurrentUserCoordinateSystem;

                var centerResult = editor.GetPoint(new PromptPointOptions("\nQS3D Hình tròn - chọn tâm: "));
                if (centerResult.Status != PromptStatus.OK) return;

                var radiusOptions = new PromptDistanceOptions("\nQS3D Hình tròn - chọn điểm trên đường tròn hoặc nhập bán kính: ")
                {
                    UseBasePoint = true,
                    BasePoint = centerResult.Value,
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = false
                };
                var radiusResult = editor.GetDistance(radiusOptions);
                if (radiusResult.Status != PromptStatus.OK) return;
                if (double.IsNaN(radiusResult.Value) || double.IsInfinity(radiusResult.Value) || !(radiusResult.Value > CoordinateTolerance))
                    throw new InvalidOperationException("Bán kính hình tròn phải là số hữu hạn > 0.");

                RequireFreshContext(document, context, promptUcs, "QS3DDRAWCIRCLE");
                var center = ToPromptUcsPoint(centerResult.Value, promptUcs);
                var radius = radiusResult.Value;
                var id = AppendEntity(
                    document,
                    promptUcs,
                    context,
                    BasicPrimitiveKind.Circle,
                    () => new Circle(center, Vector3d.ZAxis, radius));
                FinalizeSuccess(document, id, context, "Hình tròn");
            });
        }

        private static void Run(string operation, Action<Document> action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                action(document);
            }
            catch (Exception ex)
            {
                Report(document, operation + " lỗi: " + ex.Message);
            }
        }

        private static BasicDrawingContext CaptureContext(Document document, string operation)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException(operation + ": bản vẽ chưa có QS3D project. Mở Workspace, Add/chọn Family trước khi vẽ.");

            var family = ProjectFamilyActivationService.GetActive(project);
            if (family == null)
                throw new InvalidOperationException(operation + ": chưa có Family / Type active. Chọn Family trong Workspace trước khi vẽ.");

            var projectId = RequireCanonicalIdentity(
                project.ProjectId,
                operation + ": QS3D project không có identity canonical hợp lệ.");
            var familyId = RequireCanonicalIdentity(
                family.Id,
                operation + ": Family active không có identity canonical hợp lệ.");

            return new BasicDrawingContext(
                projectId,
                project.ChangeVersion,
                familyId,
                family.Name,
                family.Category,
                project.ActiveFloorId,
                project.ActiveZoneId);
        }

        private static string RequireCanonicalIdentity(string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
                ContainsControlCharacter(value))
                throw new InvalidOperationException(message);
            return value;
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (var character in value)
                if (char.IsControl(character)) return true;
            return false;
        }

        private static void RequireFreshContext(
            Document document,
            BasicDrawingContext expected,
            Matrix3d promptUcs,
            string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + ": DWG active đã thay đổi trong lúc chọn hình học. Hãy chạy lại lệnh.");

            RequireModelSpace(document);
            if (!document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs))
                throw new InvalidOperationException(operation + ": UCS đã thay đổi trong lúc chọn hình học. Hãy chạy lại lệnh.");

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException(operation + ": QS3D project không còn khả dụng trước khi commit CAD.");
            if (!string.Equals(project.ProjectId, expected.ProjectId, StringComparison.OrdinalIgnoreCase) ||
                project.ChangeVersion != expected.ChangeVersion)
                throw new InvalidOperationException(operation + ": QS3D project/Floor/Zone/thuộc tính đã thay đổi trong lúc vẽ. Hãy chạy lại để dùng ngữ cảnh mới.");

            var family = ProjectFamilyActivationService.GetActive(project);
            if (family == null ||
                !string.Equals(family.Id, expected.FamilyId, StringComparison.OrdinalIgnoreCase) ||
                family.Category != expected.Category)
                throw new InvalidOperationException(operation + ": Family active đã thay đổi trong lúc vẽ. Hãy chạy lại để dùng Family mới.");

            if (!string.Equals(project.ActiveFloorId ?? string.Empty, expected.FloorId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(project.ActiveZoneId ?? string.Empty, expected.ZoneId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(operation + ": Zone/Tầng làm việc đã thay đổi trong lúc vẽ. Hãy chạy lại lệnh.");
        }

        private static Point3d ToPromptUcsPoint(Point3d worldPoint, Matrix3d promptUcs)
        {
            return worldPoint.TransformBy(promptUcs.Inverse());
        }

        private static Polyline CreateRectangle(Point3d first, Point3d opposite)
        {
            var minX = Math.Min(first.X, opposite.X);
            var maxX = Math.Max(first.X, opposite.X);
            var minY = Math.Min(first.Y, opposite.Y);
            var maxY = Math.Max(first.Y, opposite.Y);

            var polyline = new Polyline
            {
                Elevation = first.Z,
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(minX, minY), 0d, 0d, 0d);
            polyline.AddVertexAt(1, new Point2d(maxX, minY), 0d, 0d, 0d);
            polyline.AddVertexAt(2, new Point2d(maxX, maxY), 0d, 0d, 0d);
            polyline.AddVertexAt(3, new Point2d(minX, maxY), 0d, 0d, 0d);
            return polyline;
        }

        private static ObjectId AppendEntity(
            Document document,
            Matrix3d promptUcs,
            BasicDrawingContext context,
            BasicPrimitiveKind kind,
            Func<Entity> entityFactory)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                EnsureRegApp(document.Database, transaction);

                var entity = entityFactory();
                entity.SetDatabaseDefaults(document.Database);
                entity.TransformBy(promptUcs);
                var id = modelSpace.AppendEntity(entity);
                transaction.AddNewlyCreatedDBObject(entity, true);
                MarkContext(entity, context, kind);
                transaction.Commit();
                return id;
            }
        }

        private static void EnsureRegApp(Database database, Transaction transaction)
        {
            var table = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
            if (table.Has(RegAppName)) return;
            table.UpgradeOpen();
            var record = new RegAppTableRecord { Name = RegAppName };
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }

        private static void MarkContext(Entity entity, BasicDrawingContext context, BasicPrimitiveKind kind)
        {
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, MarkerVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, RequiredIdentityToken("p1:", context.ProjectId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, RequiredIdentityToken("f1:", context.FamilyId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, context.Category.ToString()),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, IdentityToken("l1:", context.FloorId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, IdentityToken("z1:", context.ZoneId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, kind.ToString())))
                entity.XData = marker;
        }

        private static string RequiredIdentityToken(string prefix, string value)
        {
            var canonical = RequireCanonicalIdentity(value, "QS3D basic draw marker identity không canonical.");
            return HashIdentity(prefix, canonical);
        }

        private static string IdentityToken(string prefix, string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) return prefix + "none";
            return HashIdentity(prefix, normalized);
        }

        private static string HashIdentity(string prefix, string value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(prefix.Length + hash.Length * 2);
                builder.Append(prefix);
                foreach (var item in hash)
                    builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                if (!document.Database.CurrentSpaceId.Equals(modelSpaceId))
                    throw new InvalidOperationException("Vẽ cơ bản QS3D hiện chỉ hỗ trợ Model Space. Chuyển sang tab Model trước khi vẽ.");
                transaction.Commit();
            }
        }

        private static void FinalizeSuccess(Document document, ObjectId id, BasicDrawingContext context, string primitiveLabel)
        {
            var status = "Đã vẽ " + primitiveLabel + " • Family “" + context.FamilyName + "” • " + context.Category + ".";
            try
            {
                if (!id.IsNull && id.IsValid) document.Editor.SetImpliedSelection(new[] { id });
                document.Editor.Regen();
            }
            catch (Exception uiError)
            {
                try { document.Editor.WriteMessage("\nQS3D basic draw UI sync warning: " + uiError.Message); } catch { }
            }
            Report(document, status);
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\nQS3D: " + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }

        private enum BasicPrimitiveKind
        {
            Line,
            Rectangle,
            Circle
        }

        private sealed class BasicDrawingContext
        {
            public BasicDrawingContext(
                string projectId,
                long changeVersion,
                string familyId,
                string familyName,
                ElementCategory category,
                string floorId,
                string zoneId)
            {
                ProjectId = projectId ?? string.Empty;
                ChangeVersion = changeVersion;
                FamilyId = familyId ?? string.Empty;
                FamilyName = familyName ?? string.Empty;
                Category = category;
                FloorId = floorId ?? string.Empty;
                ZoneId = zoneId ?? string.Empty;
            }

            public string ProjectId { get; }
            public long ChangeVersion { get; }
            public string FamilyId { get; }
            public string FamilyName { get; }
            public ElementCategory Category { get; }
            public string FloorId { get; }
            public string ZoneId { get; }
        }
    }
}
