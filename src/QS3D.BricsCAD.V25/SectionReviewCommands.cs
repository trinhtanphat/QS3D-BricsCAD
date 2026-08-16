using System;
using System.Globalization;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SectionReviewCommands
    {
        private const string BimDetailCommand = "_BIMSECTION _Detail ";
        private const string SectionPlaneCommand = "_SECTIONPLANE ";
        private const string ClipDisplayCommand = "_CLIPDISPLAY ";

        [CommandMethod("QS3DSECTIONBOX", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void SectionBox()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DSECTIONBOX", () =>
            {
                var highlighted = ModelReviewService.HighlightSelection(document, false);
                var message = highlighted > 0
                    ? "Section Box: giữ highlight " + highlighted + " đối tượng tham chiếu; chọn 2 góc đáy và chiều cao cho BIM Detail section."
                    : "Section Box: chọn 2 góc đáy và chiều cao cho BIM Detail section.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message + " Command này dùng native BIMSECTION/Detail và cần BricsCAD BIM hỗ trợ lệnh BIMSECTION.");
                document.SendStringToExecute(BimDetailCommand, true, false, true);
            });
        }

        [CommandMethod("QS3DCUTBYOBJECT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CutByObject()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DCUTBYOBJECT", () =>
            {
                if (!TryGetSelectedBoundsInCurrentUcs(document, out var minPoint, out var maxPoint, out var selectedCount))
                {
                    const string emptyMessage = "Cắt theo đối tượng: chưa có đối tượng hợp lệ để tạo vùng cắt.";
                    PaletteCoordinator.SetStatus(emptyMessage);
                    document.Editor.WriteMessage("\nQS3D " + emptyMessage);
                    return;
                }

                var command = BuildDetailCommand(minPoint, maxPoint);
                if (string.IsNullOrWhiteSpace(command))
                {
                    const string boundsMessage = "Cắt theo đối tượng: bounds không hữu hạn/quá nhỏ hoặc padding không thể biểu diễn ổn định; không gửi BIM Detail command.";
                    PaletteCoordinator.SetStatus(boundsMessage);
                    document.Editor.WriteMessage("\nQS3D " + boundsMessage);
                    return;
                }

                var message = "Cắt theo đối tượng: tạo BIM Detail volume tự động bao quanh " + selectedCount + " đối tượng theo UCS hiện tại.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                document.SendStringToExecute(command, true, false, true);
            });
        }

        [CommandMethod("QS3DSECTIONPLANE", CommandFlags.Modal)]
        public void SectionPlane()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DSECTIONPLANE", () =>
            {
                PaletteCoordinator.SetStatus("Section Plane: đang chuyển sang native SECTIONPLANE để đặt mặt cắt tương tác.");
                document.Editor.WriteMessage("\nQS3D Section Plane: dùng native SECTIONPLANE. Chọn phương thức/điểm theo command bar của BricsCAD.");
                document.SendStringToExecute(SectionPlaneCommand, true, false, true);
            });
        }

        [CommandMethod("QS3DCLIPDISPLAY", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ClipDisplay()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DCLIPDISPLAY", () =>
            {
                PaletteCoordinator.SetStatus("Clip Display: chọn BIM/Section entity cần bật/tắt vùng cắt.");
                document.Editor.WriteMessage("\nQS3D Clip Display: dùng native CLIPDISPLAY trên section entity được chọn/prompt.");
                document.SendStringToExecute(ClipDisplayCommand, true, false, true);
            });
        }

        private static bool TryGetSelectedBoundsInCurrentUcs(Document document, out Point3d minPoint, out Point3d maxPoint, out int selectedCount)
        {
            minPoint = new Point3d(0, 0, 0);
            maxPoint = new Point3d(0, 0, 0);
            selectedCount = 0;

            var editor = document.Editor;
            var selection = editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
                selection = editor.GetSelection();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
                return false;

            var ids = selection.Value.GetObjectIds();
            if (ids == null || ids.Length == 0)
                return false;

            editor.SetImpliedSelection(ids);
            ModelReviewService.HighlightSelection(document, false);

            var worldToUcs = editor.CurrentUserCoordinateSystem.Inverse();
            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var minZ = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            var maxZ = double.NegativeInfinity;

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;

                    Extents3d extents;
                    try { extents = entity.GeometricExtents; }
                    catch { continue; }

                    var worldMin = extents.MinPoint;
                    var worldMax = extents.MaxPoint;
                    var xs = new[] { worldMin.X, worldMax.X };
                    var ys = new[] { worldMin.Y, worldMax.Y };
                    var zs = new[] { worldMin.Z, worldMax.Z };

                    for (var xi = 0; xi < 2; xi++)
                    for (var yi = 0; yi < 2; yi++)
                    for (var zi = 0; zi < 2; zi++)
                    {
                        var point = new Point3d(xs[xi], ys[yi], zs[zi]).TransformBy(worldToUcs);
                        minX = Math.Min(minX, point.X);
                        minY = Math.Min(minY, point.Y);
                        minZ = Math.Min(minZ, point.Z);
                        maxX = Math.Max(maxX, point.X);
                        maxY = Math.Max(maxY, point.Y);
                        maxZ = Math.Max(maxZ, point.Z);
                    }

                    selectedCount++;
                }
                transaction.Commit();
            }

            if (selectedCount <= 0 ||
                !Finite(minX) || !Finite(minY) || !Finite(minZ) ||
                !Finite(maxX) || !Finite(maxY) || !Finite(maxZ))
                return false;

            minPoint = new Point3d(minX, minY, minZ);
            maxPoint = new Point3d(maxX, maxY, maxZ);
            return true;
        }

        private static string? BuildDetailCommand(Point3d minPoint, Point3d maxPoint)
        {
            if (!SectionDetailVolumePlanner.TryCreate(
                    minPoint.X,
                    minPoint.Y,
                    minPoint.Z,
                    maxPoint.X,
                    maxPoint.Y,
                    maxPoint.Z,
                    1e-9d,
                    out var plan))
                return null;

            var first = PointToken(plan.FirstX, plan.FirstY, plan.BaseZ);
            var opposite = PointToken(plan.OppositeX, plan.OppositeY, plan.BaseZ);
            return BimDetailCommand + first + " " + opposite + " " + NumberToken(plan.Height) + " ";
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static string PointToken(double x, double y, double z) =>
            NumberToken(x) + "," + NumberToken(y) + "," + NumberToken(z);

        private static string NumberToken(double value) => value.ToString("0.###############", CultureInfo.InvariantCulture);

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;

        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (System.Exception ex)
            {
                var message = operation + " lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
