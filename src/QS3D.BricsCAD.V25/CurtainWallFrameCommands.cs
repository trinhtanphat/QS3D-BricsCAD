using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CurtainWallFrameCommands
    {
        [CommandMethod("QS3DCURTAINFRAMES3D", CommandFlags.UsePickSet)]
        public void BuildCurtainFrames3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selected = EntitySnapshotReader.ReadCurrentSelection(document);
                if (selected.Count == 0)
                {
                    FinalizeUi(document, "Curtain Frames 3D: chọn GlassWall semantic LINE hoặc open/bulged POLYLINE WCS-XY.", string.Empty);
                    return;
                }

                var project = ExistingProjectMutationContext.Require(document, "Curtain Frames 3D");
                var line = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);
                var path = CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project);
                var elements = checked(line.Elements + path.Elements);
                var frames = checked(line.Frames + path.Frames);
                var stampWarning = string.Empty;
                var stamped = elements > 0 ? CurtainWallFrameLiveStateService.TryStampSelected(document, project, out stampWarning) : 0;
                var message = frames == 0
                    ? "Curtain Frames 3D: chọn GlassWall semantic LINE hoặc open/bulged POLYLINE WCS-XY."
                    : "Curtain Frames 3D: đã tạo/cập nhật " + frames + " frame solid trên " + elements + " vách kính • live fingerprint " + stamped + (string.IsNullOrWhiteSpace(stampWarning) ? "." : " • fingerprint pending.");
                FinalizeUi(document, message, stampWarning);
            }
            catch (Exception)
            {
                Report(document, "QS3DCURTAINFRAMES3D không thể hoàn tất. Kiểm tra selection/project/frame geometry và thử lại.");
            }
        }

        private static void FinalizeUi(Document document, string message, string stampWarning)
        {
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { document.Editor.Regen(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(message); } catch { uiSyncFailed = true; }
            TryWriteMessage(document, "\nQS3D " + message);
            if (!string.IsNullOrWhiteSpace(stampWarning))
                TryWriteMessage(document, "\nQS3D warning: " + stampWarning);
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Curtain Frames 3D: native update đã hoàn tất; một phần UI không thể đồng bộ.");
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            TryWriteMessage(document, "\nQS3D " + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); }
            catch { }
        }
    }
}
