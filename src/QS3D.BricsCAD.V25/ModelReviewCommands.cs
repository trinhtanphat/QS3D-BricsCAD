using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ModelReviewCommands
    {
        [CommandMethod("QS3DHIGHLIGHT", CommandFlags.UsePickSet)] public void Highlight() { var document = Active(); if (document == null) return; Guard(document, "QS3DHIGHLIGHT", () => { var count = ModelReviewService.HighlightSelection(document, true); PaletteCoordinator.SetStatus("Highlight tạm thời: " + count + " object."); document.Editor.WriteMessage("\nQS3D highlight: " + count + " object(s)."); }); }
        [CommandMethod("QS3DUNHIGHLIGHT", CommandFlags.Modal)] public void Unhighlight() { var document = Active(); if (document == null) return; Guard(document, "QS3DUNHIGHLIGHT", () => { var count = ModelReviewService.ClearHighlight(document); PaletteCoordinator.SetStatus("Đã bỏ highlight " + count + " object."); }); }
        [CommandMethod("QS3DFOCUS", CommandFlags.UsePickSet)] public void Focus() { var document = Active(); if (document == null) return; Guard(document, "QS3DFOCUS", () => { var count = ModelReviewService.HighlightSelection(document, true); if (count == 0) return; if (!ViewportCommands.TryZoomSelection(document)) { document.Editor.WriteMessage("\nQS3D: không thể zoom vùng chọn hiện tại."); PaletteCoordinator.SetStatus("Focus: không thể zoom vùng chọn hiện tại."); return; } PaletteCoordinator.SetStatus("Focus + highlight " + count + " object."); }); }
        [CommandMethod("QS3DISOLATE", CommandFlags.UsePickSet)] public void Isolate() { var document = Active(); if (document == null) return; Guard(document, "QS3DISOLATE", () => { var count = ModelReviewService.HighlightSelection(document, true); if (count == 0) return; ModelReviewService.ClearHighlight(document); document.SendStringToExecute("_ISOLATEOBJECTS ", true, false, false); PaletteCoordinator.SetStatus("Isolate " + count + " object. Dùng QS3DUNISOLATE để khôi phục."); }); }
        [CommandMethod("QS3DUNISOLATE", CommandFlags.Modal)] public void Unisolate() { var document = Active(); if (document == null) return; document.SendStringToExecute("_UNISOLATEOBJECTS ", true, false, false); PaletteCoordinator.SetStatus("Đã yêu cầu khôi phục object bị isolate."); }
        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (System.Exception ex) { var message = operation + " lỗi: " + ex.Message; PaletteCoordinator.SetStatus(message); document.Editor.WriteMessage("\n" + message); } }
    }
}
