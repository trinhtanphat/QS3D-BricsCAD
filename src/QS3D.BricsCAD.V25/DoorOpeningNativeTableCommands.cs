using System;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DoorOpeningNativeTableCommands
    {
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DDOOROPENINGTABLE", CommandFlags.Modal)]
        public void Build()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                RequireSupportedUcs(document);
                var point = document.Editor.GetPoint("\nChọn điểm đặt QS3D Door / Opening Schedule Table: ");
                if (point.Status != PromptStatus.OK) return;
                var world = point.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem);
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var handle = DoorOpeningNativeTableBuilder.Build(document, project, world);
                FinalizeUi(document, "Door/Opening Table: đã tạo/cập nhật native Table " + handle + ".");
            }
            catch (Exception ex) { Report(document, "QS3DDOOROPENINGTABLE lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DDOOROPENINGTABLEREFRESH", CommandFlags.Modal)]
        public void Refresh()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var handle = DoorOpeningNativeTableBuilder.Build(document, project, DoorOpeningNativeTableBuilder.StoredPosition(project));
                FinalizeUi(document, "Door/Opening Table: đã refresh native Table " + handle + " tại WCS position đã lưu.");
            }
            catch (Exception ex) { Report(document, "QS3DDOOROPENINGTABLEREFRESH lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DDOOROPENINGTABLEREMOVE", CommandFlags.Modal)]
        public void Remove()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                DoorOpeningNativeTableBuilder.Remove(document, project);
                FinalizeUi(document, "Door/Opening Table: đã xóa owned native Table/metadata (nếu có).");
            }
            catch (Exception ex) { Report(document, "QS3DDOOROPENINGTABLEREMOVE lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DDOOROPENINGTABLEHEALTH", CommandFlags.Modal)]
        public void Health()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var issues = DoorOpeningNativeTableBuilder.Inspect(document, project);
                if (issues.Count == 0)
                {
                    Report(document, "Door/Opening Table health: không phát hiện ownership/stale/live CAD drift issue.");
                    return;
                }

                var visible = issues.Take(100).Select(x => x.Code + ": " + x.Message).ToArray();
                var suffix = issues.Count > visible.Length ? "\n- … +" + (issues.Count - visible.Length) + " issue(s)" : string.Empty;
                Report(document, "Door/Opening Table health: " + issues.Count + " issue(s).\n- " + string.Join("\n- ", visible) + suffix);
            }
            catch (Exception ex) { Report(document, "QS3DDOOROPENINGTABLEHEALTH lỗi: " + ex.Message); }
        }

        private static void RequireModelSpace(Document document)
        {
            if (!document.Database.TileMode)
                throw new InvalidOperationException("Door/Opening Table P0 chỉ hỗ trợ ModelSpace.");
        }

        private static void RequireSupportedUcs(Document document)
        {
            var coordinateSystem = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
            var zAxis = coordinateSystem.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("Current UCS có Z axis không hợp lệ.");
            var x = zAxis.X / length;
            var y = zAxis.Y / length;
            var z = zAxis.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("Door/Opening Table P0 chỉ hỗ trợ UCS có mặt phẳng XY song song WCS XY khi chọn điểm đặt mới.");
        }

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                document.Editor.Regen();
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex) { TryWrite(document, "\nQS3D " + message + " UI sync warning: " + ex.Message); }
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWrite(document, "\nQS3D " + message);
        }

        private static void TryWrite(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }
    }
}
