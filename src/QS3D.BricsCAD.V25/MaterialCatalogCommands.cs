using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class MaterialCatalogCommands
    {
        [CommandMethod("QS3DMATERIALS", CommandFlags.Modal)]
        public void ShowMaterialCatalog()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ExistingProjectMutationContext.TryGet(document, out var project))
                    throw new InvalidOperationException("Material Catalog cần QS3D project hiện hữu. Hãy chạy QS3DINIT hoặc mở/nạp project trước.");

                var window = new MaterialCatalogWindow(document, project);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                PaletteCoordinator.SetStatus("Material Catalog: built-in + custom + apply theo semantic selection • khóa theo bản vẽ đang mở.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DMATERIALS lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}