using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Fits the current selected live entities into the active BricsCAD view without changing DWG data.
    /// Native entities are opened read-only; only transient editor view state is changed.
    /// </summary>
    public sealed class MepZoomCommands
    {
        private const double MarginFactor = 1.15d;

        [CommandMethod("QS3DMEPZOOMSELECTION", CommandFlags.UsePickSet)]
        public void ZoomSelection()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DMEPZOOMSELECTION: chọn entity cần zoom.");
                    return;
                }

                var handles = new List<string>(snapshots.Count);
                for (var i = 0; i < snapshots.Count; i++) handles.Add(snapshots[i].Handle);
                var ids = CadHandleService.Resolve(document, handles);
                if (ids.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DMEPZOOMSELECTION: selection không còn entity live.");
                    return;
                }

                if (!TryReadBounds(document, ids, out var minPoint, out var maxPoint, out var skipped))
                {
                    document.Editor.WriteMessage("\nQS3DMEPZOOMSELECTION: không đọc được geometric extents hợp lệ.");
                    return;
                }

                FitCurrentView(document, minPoint, maxPoint);
                document.Editor.WriteMessage(
                    "\nQS3DMEPZOOMSELECTION: fitted=" + (ids.Count - skipped) + " • skipped=" + skipped + ".");
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DMEPZOOMSELECTION lỗi: " + ex.Message);
            }
        }

        private static bool TryReadBounds(
            Document document,
            IReadOnlyList<ObjectId> ids,
            out Point3d minPoint,
            out Point3d maxPoint,
            out int skipped)
        {
            var hasBounds = false;
            var minX = 0d;
            var minY = 0d;
            var minZ = 0d;
            var maxX = 0d;
            var maxY = 0d;
            var maxZ = 0d;
            skipped = 0;

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                for (var i = 0; i < ids.Count; i++)
                {
                    try
                    {
                        var entity = transaction.GetObject(ids[i], OpenMode.ForRead, false) as Entity;
                        if (entity == null || entity.IsErased)
                        {
                            skipped++;
                            continue;
                        }
                        var extents = entity.GeometricExtents;
                        if (!FiniteExtents(extents))
                        {
                            skipped++;
                            continue;
                        }

                        if (!hasBounds)
                        {
                            minX = extents.MinPoint.X;
                            minY = extents.MinPoint.Y;
                            minZ = extents.MinPoint.Z;
                            maxX = extents.MaxPoint.X;
                            maxY = extents.MaxPoint.Y;
                            maxZ = extents.MaxPoint.Z;
                            hasBounds = true;
                        }
                        else
                        {
                            minX = Math.Min(minX, extents.MinPoint.X);
                            minY = Math.Min(minY, extents.MinPoint.Y);
                            minZ = Math.Min(minZ, extents.MinPoint.Z);
                            maxX = Math.Max(maxX, extents.MaxPoint.X);
                            maxY = Math.Max(maxY, extents.MaxPoint.Y);
                            maxZ = Math.Max(maxZ, extents.MaxPoint.Z);
                        }
                    }
                    catch (System.Exception ex) when (IsRecoverableEntityFailure(ex))
                    {
                        skipped++;
                    }
                }
                transaction.Commit();
            }

            minPoint = new Point3d(minX, minY, minZ);
            maxPoint = new Point3d(maxX, maxY, maxZ);
            return hasBounds;
        }

        private static void FitCurrentView(Document document, Point3d minPoint, Point3d maxPoint)
        {
            using (var view = document.Editor.GetCurrentView())
            {
                var worldToDisplay = Matrix3d.PlaneToWorld(view.ViewDirection);
                worldToDisplay = Matrix3d.Displacement(view.Target - Point3d.Origin) * worldToDisplay;
                worldToDisplay = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * worldToDisplay;
                worldToDisplay = worldToDisplay.Inverse();

                var dcsMinX = double.PositiveInfinity;
                var dcsMinY = double.PositiveInfinity;
                var dcsMaxX = double.NegativeInfinity;
                var dcsMaxY = double.NegativeInfinity;
                for (var mask = 0; mask < 8; mask++)
                {
                    var point = new Point3d(
                        (mask & 1) == 0 ? minPoint.X : maxPoint.X,
                        (mask & 2) == 0 ? minPoint.Y : maxPoint.Y,
                        (mask & 4) == 0 ? minPoint.Z : maxPoint.Z).TransformBy(worldToDisplay);
                    dcsMinX = Math.Min(dcsMinX, point.X);
                    dcsMinY = Math.Min(dcsMinY, point.Y);
                    dcsMaxX = Math.Max(dcsMaxX, point.X);
                    dcsMaxY = Math.Max(dcsMaxY, point.Y);
                }

                if (!Finite(dcsMinX) || !Finite(dcsMinY) || !Finite(dcsMaxX) || !Finite(dcsMaxY))
                    throw new InvalidOperationException("View transform produced non-finite extents.");

                var currentWidth = view.Width;
                var currentHeight = view.Height;
                var aspect = currentHeight > 0d && currentWidth > 0d ? currentWidth / currentHeight : 1.6d;
                if (!Finite(aspect) || aspect <= 0d) aspect = 1.6d;

                var width = dcsMaxX - dcsMinX;
                var height = dcsMaxY - dcsMinY;
                var minimumWidth = currentWidth > 0d ? currentWidth * 0.02d : 1d;
                var minimumHeight = currentHeight > 0d ? currentHeight * 0.02d : 1d;
                width = Math.Max(width, minimumWidth);
                height = Math.Max(height, minimumHeight);
                if (width / height > aspect) height = width / aspect;
                else width = height * aspect;
                width *= MarginFactor;
                height *= MarginFactor;

                if (!Finite(width) || !Finite(height) || width <= 0d || height <= 0d)
                    throw new InvalidOperationException("Computed view size is invalid.");

                view.CenterPoint = new Point2d((dcsMinX + dcsMaxX) * 0.5d, (dcsMinY + dcsMaxY) * 0.5d);
                view.Width = width;
                view.Height = height;
                document.Editor.SetCurrentView(view);
            }
        }

        private static bool FiniteExtents(Extents3d extents) =>
            Finite(extents.MinPoint.X) && Finite(extents.MinPoint.Y) && Finite(extents.MinPoint.Z) &&
            Finite(extents.MaxPoint.X) && Finite(extents.MaxPoint.Y) && Finite(extents.MaxPoint.Z) &&
            extents.MaxPoint.X >= extents.MinPoint.X &&
            extents.MaxPoint.Y >= extents.MinPoint.Y &&
            extents.MaxPoint.Z >= extents.MinPoint.Z;

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool IsRecoverableEntityFailure(System.Exception exception) =>
            !(exception is OutOfMemoryException) &&
            !(exception is StackOverflowException) &&
            !(exception is AccessViolationException);
    }
}
