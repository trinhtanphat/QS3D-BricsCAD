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
    public sealed class CurtainWallFrameHealthCommands
    {
        [CommandMethod("QS3DCURTAINFRAMEHEALTH", CommandFlags.Modal)]
        public void CurtainFrameHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var handles = project.Elements.SelectMany(ParseHandles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var live = CadHandleService.GetLiveSolidHandles(document, handles);
                var issues = new List<ModelHealthIssue>(new GeneratedCurtainFrameHealthService().Inspect(project, live));
                issues.AddRange(CurtainWallFrameLiveStateService.Inspect(document, project));
                var summary = new HealthSummary(issues);
                var message = "Curtain Frame Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                Application.ShowModelessWindow(IntPtr.Zero, new ModelHealthWindow(document, issues, issue =>
                {
                    var element = project.FindElement(issue.ElementId);
                    if (element == null) return;
                    var count = CadHandleService.Select(document, ParseHandles(element));
                    PaletteCoordinator.SetStatus("Curtain Frame Health Định vị " + element.Id + " • " + count + " frame solid(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                }), true);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DCURTAINFRAMEHEALTH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static IEnumerable<string> ParseHandles(QS3D.Core.Domain.ProjectElement element)
        {
            if (!element.Properties.TryGetValue("GeneratedCurtainFrameHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0);
        }
    }
}