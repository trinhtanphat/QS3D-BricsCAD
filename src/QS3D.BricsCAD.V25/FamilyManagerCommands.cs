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
                Application.ShowModelessWindow(IntPtr.Zero, new FamilyManagerWindow(document), true);
                PaletteCoordinator.SetStatus("Family Manager: CRUD • properties • inheritance-safe semantic assignment • khóa theo bản vẽ.");
            }
            catch (Exception ex)
            {
                var message = "QS3DFAMILIES lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
