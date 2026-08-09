using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Units;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class Commands
    {
        [CommandMethod("QS3D", CommandFlags.Modal)] public void ShowWorkspace() => PaletteCoordinator.Show();
        [CommandMethod("QS3DHIDE", CommandFlags.Modal)] public void HideWorkspace() => PaletteCoordinator.Hide();
        [CommandMethod("QS3DINSPECT", CommandFlags.UsePickSet)]
        public void InspectSelection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return;
            try { var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(doc); PaletteCoordinator.SetInspection(snapshots); PaletteCoordinator.Show(); doc.Editor.WriteMessage($"\nQS3D: inspected {snapshots.Count} object(s)."); }
            catch (Exception ex) { doc.Editor.WriteMessage($"\nQS3DINSPECT error: {ex.Message}"); }
        }
        [CommandMethod("QS3DBQ", CommandFlags.UsePickSet)]
        public void ShowQuantitySummary()
        {
            var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return;
            try { var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(doc); var rows = SnapshotQuantityAdapter.Build(snapshots, DrawingUnit.Millimeter); var window = new QuantitySummaryWindow(rows); Application.ShowModelessWindow(IntPtr.Zero, window, true); }
            catch (Exception ex) { doc.Editor.WriteMessage($"\nQS3DBQ error: {ex.Message}"); }
        }
        [CommandMethod("QS3DABOUT", CommandFlags.Modal)] public void About() { var doc = Application.DocumentManager.MdiActiveDocument; doc?.Editor.WriteMessage("\nQS3D for BricsCAD V25 — clean-room quantity takeoff foundation."); }
    }
}
