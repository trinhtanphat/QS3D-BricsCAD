using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ShapeRebarHealthCommands
    {
        [CommandMethod("QS3DREBARSHAPEHEALTH", CommandFlags.Modal)]
        public void ReviewShapeRebar()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    var blocked = "Shape Rebar Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.";
                    PaletteCoordinator.SetStatus(blocked);
                    document.Editor.WriteMessage("\nQS3D " + blocked);
                    return;
                }

                var handles = project.Elements.SelectMany(ParseShapeHandles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var live = CadHandleService.GetLiveSolidHandles(document, handles);
                var issues = new GeneratedRebarHealthService().InspectShape(project, live);
                var summary = new HealthSummary(issues);
                var message = "Shape Rebar Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                ModelHealthWindowPresenter.Show(document, issues, issue =>
                {
                    if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)) return;
                    var element = currentProject.FindElement(issue.ElementId);
                    if (element == null) return;
                    var count = CadHandleService.Select(document, ParseShapeHandles(element));
                    PaletteCoordinator.SetStatus("Shape Rebar Health Định vị " + element.Id + " • " + count + " solid(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
            }
            catch (System.Exception)
            {
                var message = "QS3DREBARSHAPEHEALTH lỗi: không thể hoàn tất health check.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static IEnumerable<string> ParseShapeHandles(ProjectElement element)
        {
            if (!element.Properties.TryGetValue("GeneratedShapeRebarHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0);
        }
    }
}
