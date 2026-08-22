using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SafeGeneratedHandleOwnershipHealthCommands
    {
        [CommandMethod("QS3DOWNERSHIPHEALTH", CommandFlags.Modal)]
        public void ReviewOwnership()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);
                var summary = new HealthSummary(issues);
                var message = "Ownership Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                Application.ShowModelessWindow(IntPtr.Zero, new ModelHealthWindow(document, issues, issue =>
                {
                    var element = project.FindElement(issue.ElementId);
                    if (element == null) return;
                    var count = CadHandleService.Select(document, SemanticReferenceHandles.Get(element));
                    PaletteCoordinator.SetStatus("Ownership Health Định vị " + element.Id + " • " + count + " semantic/generated reference(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                }), true);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DOWNERSHIPHEALTH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}