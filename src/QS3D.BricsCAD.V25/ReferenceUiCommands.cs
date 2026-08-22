using Bricscad.ApplicationServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Thin clean-room adapters for screenshot-reference buttons that delegate to either an
    /// existing QS3D workflow or a documented/native BricsCAD command. They intentionally avoid
    /// duplicating BricsCAD import/export/edit engines inside QS3D.
    /// </summary>
    public sealed class ReferenceUiCommands
    {
        [CommandMethod("QS3DDRAWBYCAD", CommandFlags.Modal)]
        public void DrawByCad() => Forward("QS3DCONVERT2D", "Theo nét CAD → dùng workflow 2D → BIM hiện có.");

        [CommandMethod("QS3DDRAWPROFILE", CommandFlags.Modal)]
        public void DrawProfile() => Forward("_PLINE", "Biên dạng → Polyline native BricsCAD.");

        [CommandMethod("QS3DFLOORSLOPE", CommandFlags.Modal)]
        public void FloorSlope() => Forward("_3DROTATE", "Dốc sàn → xoay hình học sàn đã chọn quanh trục dốc bằng 3DROTATE.");

        [CommandMethod("QS3DSLABCUT", CommandFlags.Modal)]
        public void SlabCut() => Forward("_SUBTRACT", "Cắt sàn → phép trừ solid native; chọn sàn trước, rồi chọn solid cắt.");

        [CommandMethod("QS3DJOINCORNER", CommandFlags.Modal)]
        public void JoinCorner() => Forward("_FILLET", "Nối góc → FILLET native BricsCAD.");

        [CommandMethod("QS3DJOINTEE", CommandFlags.Modal)]
        public void JoinTee() => Forward("_EXTEND", "Nối chữ T → EXTEND native BricsCAD.");

        [CommandMethod("QS3DIFCIMPORT", CommandFlags.Modal)]
        public void ImportIfc() => Forward("_IMPORT", "Nhập IFC → hộp thoại Import native BricsCAD; chọn IFC/IFCZIP.");

        [CommandMethod("QS3DIFCIMPORTLIGHT", CommandFlags.Modal)]
        public void ImportIfcLight() => Forward("_IMPORT", "Nhập IFC (nhẹ) → trong IFC Import Settings chọn workflow XRef/spatial split phù hợp.");

        [CommandMethod("QS3DIFCREMOVE", CommandFlags.Modal)]
        public void RemoveIfc() => Forward("_XREF", "Xóa IFC → mở quản lý Xref để Detach mô hình IFC đã nhập theo XRef.");

        [CommandMethod("QS3DIFCEXPORT", CommandFlags.Modal)]
        public void ExportIfc() => Forward("_IFCEXPORT", "Xuất IFC → IFCEXPORT native BricsCAD BIM.");

        private static void Forward(string command, string status)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try { document.Editor.WriteMessage("\nQS3D: " + status); } catch { }
            document.SendStringToExecute(command + " ", true, false, false);
        }
    }
}
