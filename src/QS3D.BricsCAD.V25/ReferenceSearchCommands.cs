using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ReferenceSearchCommands
    {
        [CommandMethod("QS3DREFSEARCH", CommandFlags.Modal)]
        public void ShowReferenceSearch()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new ReferenceSearchWindow(document), true);
                PaletteCoordinator.SetStatus("Tham khảo thi công: ảnh • web • video • mua sắm • video ngắn • tin tức.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DREFSEARCH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
