using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class WallQuantityCommands
    {
        [CommandMethod("QS3DWALLQTY", CommandFlags.Modal)]
        public void ShowWallQuantity()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new WallQuantityWindow(document), true);
                PaletteCoordinator.SetStatus("Khối lượng Tường: danh sách • thuộc tính • chi tiết • XLSX • read-only.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DWALLQTY lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
