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
    public sealed class RebarHealthAllCommands
    {
        [CommandMethod("QS3DREBARHEALTHALL", CommandFlags.Modal)]
        public void ReviewAllGeneratedRebar()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var columnHandles = Collect(project, "GeneratedRebarHandles");
                var shapeHandles = Collect(project, "GeneratedShapeRebarHandles");
                var tieHandles = Collect(project, "GeneratedTieRebarHandles");
                var liveColumn = CadHandleService.GetLiveSolidHandles(document, columnHandles);
                var liveShape = CadHandleService.GetLiveSolidHandles(document, shapeHandles);
                var liveTie = CadHandleService.GetLiveSolidHandles(document, tieHandles);

                var issues = new List<ModelHealthIssue>();
                issues.AddRange(new GeneratedRebarHealthService().InspectAll(project, liveColumn, liveShape));
                issues.AddRange(new GeneratedTieRebarHealthService().Inspect(project, liveTie));
                var summary = new HealthSummary(issues);
                var message = "Rebar Health All: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                Application.ShowModelessWindow(IntPtr.Zero, new ModelHealthWindow(issues, issue =>
                {
                    var element = project.FindElement(issue.ElementId);
                    if (element == null) return;
                    var handles = HandlesForIssue(element, issue.Code);
                    var count = CadHandleService.Select(document, handles);
                    PaletteCoordinator.SetStatus("Rebar Health All Định vị " + element.Id + " • " + count + " solid(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                }), true);
            }
            catch (Exception ex)
            {
                var message = "QS3DREBARHEALTHALL lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static string[] Collect(ProjectState project, string key) => project.Elements
            .SelectMany(x => Parse(x, key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        private static IEnumerable<string> HandlesForIssue(ProjectElement element, string code)
        {
            if (code.IndexOf("TIE_REBAR", StringComparison.OrdinalIgnoreCase) >= 0) return Parse(element, "GeneratedTieRebarHandles");
            if (code.IndexOf("SHAPE_REBAR", StringComparison.OrdinalIgnoreCase) >= 0) return Parse(element, "GeneratedShapeRebarHandles");
            return Parse(element, "GeneratedRebarHandles");
        }

        private static IEnumerable<string> Parse(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0);
        }
    }
}
