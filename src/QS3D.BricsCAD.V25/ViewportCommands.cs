using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ViewportCommands
    {
        [CommandMethod("QS3DVIEW3D", CommandFlags.Modal)] public void View3D() { var doc = Active(); if (doc == null) return; doc.Editor.SwitchToModelSpace(); doc.SendStringToExecute("_.VPOINT 1,-1,1 _.ZOOM _E ", true, false, false); PaletteCoordinator.SetStatus("Viewport 3D isometric • pan/zoom/orbit trực tiếp trên BricsCAD."); }
        [CommandMethod("QS3DVIEWTOP", CommandFlags.Modal)] public void ViewTop() { var doc = Active(); if (doc == null) return; doc.Editor.SwitchToModelSpace(); doc.SendStringToExecute("_.PLAN _W _.ZOOM _E ", true, false, false); PaletteCoordinator.SetStatus("Viewport Top/Plan."); }
        [CommandMethod("QS3DORBIT", CommandFlags.Modal)] public void Orbit() { var doc = Active(); if (doc == null) return; doc.Editor.SwitchToModelSpace(); doc.SendStringToExecute("_.3DORBIT ", true, false, false); PaletteCoordinator.SetStatus("3D Orbit: kéo trực tiếp trong viewport BricsCAD."); }
        [CommandMethod("QS3DFOCUSMODEL", CommandFlags.Modal)] public void FocusModel() { var doc = Active(); if (doc == null) return; doc.Editor.SwitchToModelSpace(); doc.Editor.UpdateScreen(); PaletteCoordinator.SetStatus("Đã focus Model Space."); }
        [CommandMethod("QS3DZOOMSELECTED", CommandFlags.Modal)] public void ZoomSelected() { var doc = Active(); if (doc == null) return; if (!TryZoomSelection(doc)) { doc.Editor.WriteMessage("\nQS3D: chưa có đối tượng được chọn để zoom."); PaletteCoordinator.SetStatus("Zoom chọn: chưa có đối tượng."); } }
        [CommandMethod("QS3DZOOMALL", CommandFlags.Modal)] public void ZoomAll() { var doc = Active(); if (doc == null) return; doc.Editor.SwitchToModelSpace(); doc.SendStringToExecute("_.ZOOM _E ", true, false, false); PaletteCoordinator.SetStatus("Zoom Extents."); }

        [CommandMethod("QS3DUNTRACK", CommandFlags.Modal)] public void UntrackSelected() => UntrackSelectedElements(null, "cấu kiện");

        [CommandMethod("QS3DUNTRACKFINISH", CommandFlags.Modal)]
        public void UntrackSelectedFinishes()
        {
            var finishCategories = new HashSet<ElementCategory>
            {
                ElementCategory.FloorFinish,
                ElementCategory.Waterproofing,
                ElementCategory.Skirting,
                ElementCategory.WallFinish,
                ElementCategory.CeilingFinish
            };
            UntrackSelectedElements(x => finishCategories.Contains(x.Category), "cấu kiện hoàn thiện");
        }

        private static void UntrackSelectedElements(Func<ProjectElement, bool>? predicate, string label)
        {
            var doc = Active();
            if (doc == null) return;
            var snapshots = EntitySnapshotReader.ReadImpliedSelection(doc);
            if (snapshots.Count == 0)
            {
                doc.Editor.WriteMessage("\nQS3D: chọn " + label + " cần bỏ khỏi project trước.");
                PaletteCoordinator.SetStatus("Chưa chọn " + label + " để bỏ theo dõi.");
                return;
            }

            var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
            var project = ProjectContextCoordinator.GetOrCreate(doc);
            var matched = project.Elements.Where(x => x.SourceHandles.Any(handles.Contains) && (predicate == null || predicate(x))).ToList();
            foreach (var element in matched) project.Elements.Remove(element);
            if (matched.Count > 0) project.Touch();
            PaletteCoordinator.RefreshProject();
            PaletteCoordinator.SetStatus("Đã bỏ theo dõi " + matched.Count + " " + label + "; hình học CAD được giữ nguyên.");
            doc.Editor.WriteMessage("\nQS3D: untracked " + matched.Count + " " + label + "; CAD geometry was not erased.");
        }

        private static bool TryZoomSelection(Document document)
        {
            var result = document.Editor.SelectImplied();
            if (result.Status != PromptStatus.OK || result.Value == null) return false;
            var selectedCount = result.Value.GetObjectIds().Length;
            var hasExtents = false;
            var min = new Point3d();
            var max = new Point3d();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in result.Value.GetObjectIds())
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null) continue;
                    try
                    {
                        var extents = entity.GeometricExtents;
                        if (!hasExtents) { min = extents.MinPoint; max = extents.MaxPoint; hasExtents = true; }
                        else
                        {
                            min = new Point3d(Math.Min(min.X, extents.MinPoint.X), Math.Min(min.Y, extents.MinPoint.Y), Math.Min(min.Z, extents.MinPoint.Z));
                            max = new Point3d(Math.Max(max.X, extents.MaxPoint.X), Math.Max(max.Y, extents.MaxPoint.Y), Math.Max(max.Z, extents.MaxPoint.Z));
                        }
                    }
                    catch { }
                }
                transaction.Commit();
            }
            if (!hasExtents) return false;
            using (var view = document.Editor.GetCurrentView())
            {
                var width = Math.Max(max.X - min.X, 1e-3);
                var height = Math.Max(max.Y - min.Y, 1e-3);
                var ratio = view.Height > 1e-9 ? view.Width / view.Height : 1.0;
                if (width / height > ratio) height = width / Math.Max(ratio, 1e-6);
                else width = height * Math.Max(ratio, 1e-6);
                view.CenterPoint = new Point2d((min.X + max.X) * 0.5, (min.Y + max.Y) * 0.5);
                view.Width = width * 1.25;
                view.Height = height * 1.25;
                document.Editor.SetCurrentView(view);
            }
            document.Editor.UpdateScreen();
            PaletteCoordinator.SetStatus("Zoom tới " + selectedCount + " đối tượng.");
            return true;
        }

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
    }
}
