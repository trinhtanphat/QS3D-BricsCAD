using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantityInsightCommands
    {
        [CommandMethod("QS3DQUANTITYINSIGHT", CommandFlags.UsePickSet)]
        public void ShowSelectedQuantityInsight()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var snapshots = EntitySnapshotReader.ReadImpliedSelection(document);
                if (snapshots.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3D Diễn giải khối lượng: hãy chọn cấu kiện trước rồi nhấp chuột phải.");
                    return;
                }

                PaletteCoordinator.SetInspection(snapshots);
                PaletteCoordinator.ShowQuantityInsight();
                document.Editor.WriteMessage("\nQS3D Diễn giải khối lượng: " + snapshots.Count + " đối tượng đang chọn.");
            }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3D Diễn giải khối lượng lỗi: " + ex.Message); }
                catch { }
            }
        }
    }
}