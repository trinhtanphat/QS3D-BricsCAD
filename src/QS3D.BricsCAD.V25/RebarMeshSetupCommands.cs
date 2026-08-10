using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RebarMeshSetupCommands
    {
        [CommandMethod("QS3DREBARMESHSETUP", CommandFlags.UsePickSet)]
        public void RebarMeshSetup()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0) return;
                var selectedHandles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var matches = project.Elements
                    .Where(x => (x.Category == ElementCategory.Slab || x.Category == ElementCategory.StructuralWall) && x.SourceHandles.Any(selectedHandles.Contains))
                    .Take(3)
                    .ToList();
                if (matches.Count != 1)
                {
                    document.Editor.WriteMessage("\nQS3D Rebar Mesh Setup: chọn đúng một Slab hoặc StructuralWall semantic source.");
                    return;
                }

                var element = matches[0];
                var window = new RebarMeshSetupWindow(project, element, () =>
                {
                    PaletteCoordinator.RefreshProject();
                    PaletteCoordinator.SetStatus("Đã lưu mesh input cho " + element.Id + ". Rebuild 3D để cập nhật generated bars.");
                });
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DREBARMESHSETUP lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
