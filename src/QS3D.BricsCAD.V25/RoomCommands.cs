using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RoomCommands
    {
        [CommandMethod("QS3DROOMAUTO", CommandFlags.UsePickSet)]
        public void AutoRoomBoundaries()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var result = AutomaticRoomService.Generate(document);
                PaletteCoordinator.RefreshProject();
                var status = "Auto Room: " + result.Boundaries + " boundary • " + result.Created + " tạo • " + result.Updated + " cập nhật";
                if (result.RemovedStale > 0) status += " • " + result.RemovedStale + " xóa cũ";
                if (result.RetainedStale > 0) status += " • " + result.RetainedStale + " stale được giữ";
                if (result.UnsupportedEntities > 0) status += " • " + result.UnsupportedEntities + " source không hỗ trợ";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
                if (result.Boundaries == 0) document.Editor.WriteMessage("\nQS3D Auto Room: chọn mạng LINE/POLYLINE phẳng tạo thành vùng kín; arc/bulge hiện chưa được suy diễn thành boundary.");
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DROOMAUTO error: " + ex.Message);
                PaletteCoordinator.SetStatus("QS3DROOMAUTO lỗi: " + ex.Message);
            }
        }
    }
}
