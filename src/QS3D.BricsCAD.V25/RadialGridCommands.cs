using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RadialGridCommands
    {
        [CommandMethod("QS3DGRIDRADIAL")]
        public void CreateRadialGridSystem()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            if (!TryPromptRequest(document, out var request)) return;

            Cad.RadialGridNativeResult result;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                result = Cad.RadialGridNativeSourceBuilder.Build(document, project, request);
                Cad.GridAuthoringRepeatState.RememberRadial(document, request);
            }
            catch (Exception)
            {
                ReportOperationFailure(document, "QS3DGRIDRADIAL lỗi: không thể materialize radial Grid; native/semantic state đã được fail-closed.");
                return;
            }

            var status = "Radial Grid " + result.SystemKey + ": đã materialize " + result.Curves +
                         " canonical LINE/ARC source(s); replaced " + result.Replaced +
                         ". Chạy QS3DGRIDINTERSECTIONS để refresh pair-owned markers nếu cần.";
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }
            TryWriteMessage(document, "\nQS3D " + status);
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Grid: native + semantic radial Grid đã commit; một phần UI không thể đồng bộ.");
        }

        private static bool TryPromptRequest(Document document, out Cad.RadialGridNativeRequest request)
        {
            request = new Cad.RadialGridNativeRequest();
            var editor = document.Editor;
            var key = editor.GetString(new PromptStringOptions("\nRadial Grid system key (lowercase, no spaces): "));
            if (key.Status != PromptStatus.OK) return false;
            var center = editor.GetPoint(new PromptPointOptions("\nRadial Grid center: "));
            if (center.Status != PromptStatus.OK) return false;
            var directionOptions = new PromptPointOptions("\nPoint on first ray direction: ") { BasePoint = center.Value, UseBasePoint = true };
            var direction = editor.GetPoint(directionOptions);
            if (direction.Status != PromptStatus.OK) return false;
            var rayCount = editor.GetInteger(new PromptIntegerOptions("\nRay count [1..200]: "));
            if (rayCount.Status != PromptStatus.OK) return false;
            var rayStep = editor.GetDouble(new PromptDoubleOptions("\nRay angular step (degrees, > 0): "));
            if (rayStep.Status != PromptStatus.OK) return false;
            var innerRadius = editor.GetDouble(new PromptDoubleOptions("\nRay inner radius (m, >= 0): "));
            if (innerRadius.Status != PromptStatus.OK) return false;
            var firstRing = editor.GetDouble(new PromptDoubleOptions("\nFirst ring radius (m, > 0): "));
            if (firstRing.Status != PromptStatus.OK) return false;
            var ringCount = editor.GetInteger(new PromptIntegerOptions("\nRing count [1..200]: "));
            if (ringCount.Status != PromptStatus.OK) return false;
            var ringSpacing = editor.GetDouble(new PromptDoubleOptions("\nRing spacing (m, > 0 when count > 1): "));
            if (ringSpacing.Status != PromptStatus.OK) return false;

            request = new Cad.RadialGridNativeRequest
            {
                SystemKey = key.StringResult,
                CenterDrawing = center.Value,
                FirstRayDirectionPointDrawing = direction.Value,
                RayCount = rayCount.Value,
                RayStepDegrees = rayStep.Value,
                InnerRadiusM = innerRadius.Value,
                FirstRingRadiusM = firstRing.Value,
                RingCount = ringCount.Value,
                RingSpacingM = ringSpacing.Value
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
