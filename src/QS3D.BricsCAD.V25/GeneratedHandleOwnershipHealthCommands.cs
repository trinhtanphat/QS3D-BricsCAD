using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GeneratedHandleOwnershipHealthCommands
    {
        [CommandMethod("QS3DHANDLEHEALTH", CommandFlags.Modal)]
        public void ReviewHandleOwnership()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var issues = new GeneratedHandleOwnershipHealthService().Inspect(project);
                var summary = new HealthSummary(issues);
                var message = "Handle Ownership Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                Application.ShowModelessWindow(IntPtr.Zero, new ModelHealthWindow(document, issues, issue =>
                {
                    var element = project.FindElement(issue.ElementId);
                    if (element == null) return;
                    var count = Cad.CadHandleService.Select(document, Services.SemanticReferenceHandles.Get(element));
                    PaletteCoordinator.SetStatus("Handle Health Định vị " + element.Id + " • " + count + " semantic source/generated reference(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                }), true);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DHANDLEHEALTH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}