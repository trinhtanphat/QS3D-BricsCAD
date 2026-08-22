using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class FamilyManagerCommands
    {
        [CommandMethod("QS3DFAMILIES", CommandFlags.Modal)]
        public void ShowFamilyManager()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                ExistingProjectMutationContext.TryGet(document, out _);
                var window = new FamilyManagerWindow(document);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                PaletteCoordinator.SetStatus("Family Manager: CRUD • properties • inheritance-safe semantic assignment • khóa theo bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DFAMILIES lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}