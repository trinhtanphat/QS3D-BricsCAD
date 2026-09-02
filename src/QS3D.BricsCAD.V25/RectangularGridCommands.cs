using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RectangularGridCommands
    {
        [CommandMethod("QS3DGRIDRECT")]
        public void CreateRectangularGridSystem()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            if (!TryPromptRectangularRequest(document, out var request)) return;

            Cad.RectangularGridNativeResult result;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                result = Cad.RectangularGridNativeSourceBuilder.Build(document, project, request);
                Cad.GridAuthoringRepeatState.RememberRectangular(document, request);
            }
            catch (Exception)
            {
                ReportOperationFailure(document, "QS3DGRIDRECT lỗi: không thể materialize rectangular Grid; native/semantic state đã được fail-closed.");
                return;
            }

            var status = "Rectangular Grid " + result.SystemKey + ": đã materialize " + result.Curves +
                         " canonical LINE source(s); replaced " + result.Replaced +
                         ". Chạy QS3DGRIDINTERSECTIONS để refresh pair-owned markers nếu cần.";
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }
            TryWriteMessage(document, "\nQS3D " + status);
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Grid: native + semantic rectangular Grid đã commit; một phần UI không thể đồng bộ.");
        }

        private static bool TryPromptRectangularRequest(Document document, out Cad.RectangularGridNativeRequest request)
        {
            request = new Cad.RectangularGridNativeRequest();
            var editor = document.Editor;

            var key = editor.GetString(new PromptStringOptions("\nRectangular Grid system key (lowercase, no spaces): "));
            if (key.Status != PromptStatus.OK) return false;
            var origin = editor.GetPoint(new PromptPointOptions("\nRectangular Grid origin: "));
            if (origin.Status != PromptStatus.OK) return false;
            var directionOptions = new PromptPointOptions("\nPoint on positive U direction: ")
            {
                BasePoint = origin.Value,
                UseBasePoint = true
            };
            var direction = editor.GetPoint(directionOptions);
            if (direction.Status != PromptStatus.OK) return false;
            var uCount = editor.GetInteger(new PromptIntegerOptions("\nU axis count [2..200]: "));
            if (uCount.Status != PromptStatus.OK) return false;
            var uSpacing = editor.GetDouble(new PromptDoubleOptions("\nU spacing (m): "));
            if (uSpacing.Status != PromptStatus.OK) return false;
            var vCount = editor.GetInteger(new PromptIntegerOptions("\nV axis count [2..200]: "));
            if (vCount.Status != PromptStatus.OK) return false;
            var vSpacing = editor.GetDouble(new PromptDoubleOptions("\nV spacing (m): "));
            if (vSpacing.Status != PromptStatus.OK) return false;

            request = new Cad.RectangularGridNativeRequest
            {
                SystemKey = key.StringResult,
                OriginDrawing = origin.Value,
                UDirectionPointDrawing = direction.Value,
                UCount = uCount.Value,
                VCount = vCount.Value,
                USpacingM = uSpacing.Value,
                VSpacingM = vSpacing.Value
            };
            return true;
        }

        private static void ReportOperationFailure(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWriteMessage(document, "\n" + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }
    }
}
