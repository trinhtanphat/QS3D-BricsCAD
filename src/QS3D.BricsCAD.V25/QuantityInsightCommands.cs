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
                // Ribbon activation must work both with PICKFIRST and when the user clicks
                // Diễn giải before selecting anything. ReadCurrentSelection preserves an
                // existing implied selection and otherwise opens BricsCAD's normal selection
                // prompt, so the topbar button is a complete workflow instead of requiring a
                // separate right-click/context-menu path first.
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3D Diễn giải khối lượng: chưa có cấu kiện nào được chọn.");
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