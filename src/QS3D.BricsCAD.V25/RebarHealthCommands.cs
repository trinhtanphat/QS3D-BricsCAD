using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RebarHealthCommands
    {
        [CommandMethod("QS3DREBARHEALTH", CommandFlags.Modal)]
        public void ReviewGeneratedRebar()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var handles = project.Elements
                    .SelectMany(ParseHandles)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var live = CadHandleService.GetLiveSolidHandles(document, handles);
                var issues = new GeneratedRebarHealthService().Inspect(project, live);
                var summary = new HealthSummary(issues);
                var message = "Rebar Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                Application.ShowModelessWindow(IntPtr.Zero, new ModelHealthWindow(issues, issue =>
                {
                    var element = project.FindElement(issue.ElementId);
                    if (element == null) return;
                    var count = CadHandleService.Select(document, ParseHandles(element));
                    PaletteCoordinator.SetStatus("Rebar Health Định vị " + element.Id + " • " + count + " generated bar solid(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                }), true);
            }
            catch (Exception ex)
            {
                var message = "QS3DREBARHEALTH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static IEnumerable<string> ParseHandles(QS3D.Core.Domain.ProjectElement element)
        {
            if (!element.Properties.TryGetValue("GeneratedRebarHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0);
        }
    }
}
