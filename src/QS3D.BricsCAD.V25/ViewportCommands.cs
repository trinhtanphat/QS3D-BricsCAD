using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ViewportCommands
    {
        [CommandMethod("QS3DVIEW3D", CommandFlags.Modal)] public void View3D() { var doc = Active(); if (doc == null) return; EnsureTiledModelSpace(doc); doc.SendStringToExecute("_.VPOINT 1,-1,1 _.ZOOM _E ", true, false, false); PaletteCoordinator.SetStatus("Viewport 3D isometric • pan/zoom/orbit trực tiếp trên BricsCAD."); }
        [CommandMethod("QS3DVIEWTOP", CommandFlags.Modal)] public void ViewTop() { var doc = Active(); if (doc == null) return; EnsureTiledModelSpace(doc); doc.SendStringToExecute("_.PLAN _W _.ZOOM _E ", true, false, false); PaletteCoordinator.SetStatus("Viewport Top/Plan."); }
        [CommandMethod("QS3DORBIT", CommandFlags.Modal)] public void Orbit() { var doc = Active(); if (doc == null) return; EnsureTiledModelSpace(doc); doc.SendStringToExecute("_.3DORBIT ", true, false, false); PaletteCoordinator.SetStatus("3D Orbit: kéo trực tiếp trong viewport BricsCAD."); }
        [CommandMethod("QS3DFOCUSMODEL", CommandFlags.Modal)] public void FocusModel() { var doc = Active(); if (doc == null) return; EnsureTiledModelSpace(doc); doc.Editor.UpdateScreen(); PaletteCoordinator.SetStatus("Đã focus Model Space."); }
        [CommandMethod("QS3DZOOMSELECTED", CommandFlags.Modal)] public void ZoomSelected() { var doc = Active(); if (doc == null) return; if (!TryZoomSelection(doc)) { doc.Editor.WriteMessage("\nQS3D: chưa có đối tượng được chọn để zoom."); PaletteCoordinator.SetStatus("Zoom chọn: chưa có đối tượng."); } }
        [CommandMethod("QS3DZOOMALL", CommandFlags.Modal)] public void ZoomAll() { var doc = Active(); if (doc == null) return; EnsureTiledModelSpace(doc); doc.SendStringToExecute("_.ZOOM _E ", true, false, false); PaletteCoordinator.SetStatus("Zoom Extents."); }

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

            var handles = snapshots.Select(x => x.Handle).ToArray();
            try
            {
                var project = ExistingProjectMutationContext.Require(doc, "Untrack semantic elements");
                var result = SemanticUntrackService.Untrack(project, handles, predicate);
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus("Đã bỏ theo dõi " + result.Count + " " + label + "; hình học CAD được giữ nguyên.");
                doc.Editor.WriteMessage("\nQS3D: untracked " + result.Count + " " + label + "; CAD geometry was not erased.");
            }
            catch (Exception ex)
            {
                var message = "Không thể bỏ theo dõi " + label + ": " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                doc.Editor.WriteMessage("\nQS3D: " + message);
            }
        }

        private static void EnsureTiledModelSpace(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (document.Database.TileMode) return;
            document.Database.TileMode = true;
            document.Editor.UpdateScreen();
        }

        internal static bool TryZoomSelection(Document document)
        {
            var result = document.Editor.SelectImplied();
            if (result.Status != PromptStatus.OK || result.Value == null) return false;
            var objectIds = result.Value.GetObjectIds();
            if (objectIds.Length == 0) return false;

            using (var view = document.Editor.GetCurrentView())
            {
                // Entity.GeometricExtents is expressed in WCS, while view CenterPoint/Width/Height
                // are display-coordinate-system (DCS) framing values. Transform every entity bound
                // into the current view's DCS before computing the zoom rectangle so rotated and
                // isometric views keep their camera direction and frame the selected geometry.
                var worldToDisplay = WorldToDisplay(view);
                var hasExtents = false;
                var min = new Point3d();
                var max = new Point3d();

                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (var id in objectIds)
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (entity == null || entity.IsErased) continue;
                        try
                        {
                            var extents = entity.GeometricExtents;
                            extents.TransformBy(worldToDisplay);
                            var extentMin = extents.MinPoint;
                            var extentMax = extents.MaxPoint;
                            if (!Finite(extentMin) || !Finite(extentMax)) continue;

                            if (!hasExtents)
                            {
                                min = extentMin;
                                max = extentMax;
                                hasExtents = true;
                            }
                            else
                            {
                                min = new Point3d(
                                    Math.Min(min.X, extentMin.X),
                                    Math.Min(min.Y, extentMin.Y),
                                    Math.Min(min.Z, extentMin.Z));
                                max = new Point3d(
                                    Math.Max(max.X, extentMax.X),
                                    Math.Max(max.Y, extentMax.Y),
                                    Math.Max(max.Z, extentMax.Z));
                            }
                        }
                        catch { }
                    }
                    transaction.Commit();
                }

                if (!hasExtents) return false;

                var minimumSpan = MinimumViewSpan(view);
                var width = Math.Max(max.X - min.X, minimumSpan);
                var height = Math.Max(max.Y - min.Y, minimumSpan);
                var ratio = FinitePositive(view.Height) && FinitePositive(view.Width)
                    ? view.Width / view.Height
                    : 1.0d;
                if (!FinitePositive(ratio)) ratio = 1.0d;

                if (width / height > ratio) height = width / ratio;
                else width = height * ratio;

                var centerX = (min.X + max.X) * 0.5d;
                var centerY = (min.Y + max.Y) * 0.5d;
                if (!Finite(centerX) || !Finite(centerY) || !FinitePositive(width) || !FinitePositive(height)) return false;

                view.CenterPoint = new Point2d(centerX, centerY);
                view.Width = width * 1.25d;
                view.Height = height * 1.25d;
                document.Editor.SetCurrentView(view);
            }

            document.Editor.UpdateScreen();
            PaletteCoordinator.SetStatus("Zoom tới " + objectIds.Length + " đối tượng theo hướng nhìn hiện tại.");
            return true;
        }

        private static Matrix3d WorldToDisplay(ViewTableRecord view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            var matrix = Matrix3d.PlaneToWorld(view.ViewDirection);
            matrix = Matrix3d.Displacement(view.Target - Point3d.Origin) * matrix;
            matrix = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * matrix;
            return matrix.Inverse();
        }

        private static double MinimumViewSpan(ViewTableRecord view)
        {
            var scale = Math.Min(Math.Abs(view.Width), Math.Abs(view.Height));
            if (!FinitePositive(scale)) scale = 1.0d;
            var minimum = scale * 1e-6d;
            return FinitePositive(minimum) ? minimum : 1e-6d;
        }

        private static bool Finite(Point3d point) => Finite(point.X) && Finite(point.Y) && Finite(point.Z);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool FinitePositive(double value) => Finite(value) && value > 0d;

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
    }
}
