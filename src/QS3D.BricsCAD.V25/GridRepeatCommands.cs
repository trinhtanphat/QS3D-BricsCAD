using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridRepeatCommands
    {
        [CommandMethod("QS3DGRIDRECTREPEAT")]
        public void RepeatRectangularGrid()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            if (!Cad.GridAuthoringRepeatState.HasRectangularTemplate(document))
            {
                ReportOperationFailure(document,
                    "QS3DGRIDRECTREPEAT: DWG hiện tại chưa có rectangular Grid template đã commit; chạy QS3DGRIDRECT trước.");
                return;
            }

            var editor = document.Editor;
            var key = editor.GetString(new PromptStringOptions("\nNew rectangular Grid system key (lowercase, no spaces): "));
            if (key.Status != PromptStatus.OK) return;
            var origin = editor.GetPoint(new PromptPointOptions("\nNew rectangular Grid origin: "));
            if (origin.Status != PromptStatus.OK) return;
            var directionOptions = new PromptPointOptions("\nPoint on positive U direction: ")
            {
                BasePoint = origin.Value,
                UseBasePoint = true
            };
            var direction = editor.GetPoint(directionOptions);
            if (direction.Status != PromptStatus.OK) return;

            if (!Cad.GridAuthoringRepeatState.TryCreateRectangularRequest(
                    document, key.StringResult, origin.Value, direction.Value, out var request))
            {
                ReportOperationFailure(document,
                    "QS3DGRIDRECTREPEAT: rectangular Grid repeat state đã thay đổi trước materialization; không có mutation nào được thực hiện.");
                return;
            }

            Cad.RectangularGridNativeResult result;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                result = Cad.RectangularGridNativeSourceBuilder.Build(document, project, request);
            }
            catch (Exception)
            {
                ReportOperationFailure(document,
                    "QS3DGRIDRECTREPEAT lỗi: không thể materialize repeated rectangular Grid; native/semantic state đã được fail-closed.");
                return;
            }

            ReportCommitted(document, "Repeated rectangular Grid " + result.SystemKey + ": đã materialize " +
                result.Curves + " canonical LINE source(s); replaced " + result.Replaced + ".");
        }

        [CommandMethod("QS3DGRIDRADIALREPEAT")]
        public void RepeatRadialGrid()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            if (!Cad.GridAuthoringRepeatState.HasRadialTemplate(document))
            {
                ReportOperationFailure(document,
                    "QS3DGRIDRADIALREPEAT: DWG hiện tại chưa có radial Grid template đã commit; chạy QS3DGRIDRADIAL trước.");
                return;
            }

            var editor = document.Editor;
            var key = editor.GetString(new PromptStringOptions("\nNew radial Grid system key (lowercase, no spaces): "));
            if (key.Status != PromptStatus.OK) return;
            var center = editor.GetPoint(new PromptPointOptions("\nNew radial Grid center: "));
            if (center.Status != PromptStatus.OK) return;
            var directionOptions = new PromptPointOptions("\nPoint on first ray direction: ")
            {
                BasePoint = center.Value,
                UseBasePoint = true
            };
            var direction = editor.GetPoint(directionOptions);
            if (direction.Status != PromptStatus.OK) return;

            if (!Cad.GridAuthoringRepeatState.TryCreateRadialRequest(
                    document, key.StringResult, center.Value, direction.Value, out var request))
            {
                ReportOperationFailure(document,
                    "QS3DGRIDRADIALREPEAT: radial Grid repeat state đã thay đổi trước materialization; không có mutation nào được thực hiện.");
                return;
            }

            Cad.RadialGridNativeResult result;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                result = Cad.RadialGridNativeSourceBuilder.Build(document, project, request);
            }
            catch (Exception)
            {
                ReportOperationFailure(document,
                    "QS3DGRIDRADIALREPEAT lỗi: không thể materialize repeated radial Grid; native/semantic state đã được fail-closed.");
                return;
            }

            ReportCommitted(document, "Repeated radial Grid " + result.SystemKey + ": đã materialize " +
                result.Curves + " canonical LINE/ARC source(s); replaced " + result.Replaced + ".");
        }

        private static void ReportCommitted(Document document, string status)
        {
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }
            TryWriteMessage(document, "\nQS3D " + status);
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Grid repeat: native + semantic Grid đã commit; một phần UI không thể đồng bộ.");
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
