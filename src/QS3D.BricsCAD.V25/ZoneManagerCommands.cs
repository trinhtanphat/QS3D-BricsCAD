using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ZoneManagerCommands
    {
        [CommandMethod("QS3DZONES", CommandFlags.Modal)]
        public void ShowZoneManager()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                ExistingProjectMutationContext.TryGet(document, out _);
                var window = new ZoneManagerWindow(document);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                PaletteCoordinator.SetStatus("Zone Manager: CRUD • active Zone • semantic assignment • khóa theo bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DZONES lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}