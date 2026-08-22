using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class RoomBoundarySegmentReader
    {
        private const int MaxSplineSegments = 4096;

        public static IReadOnlyList<BoundarySegment> ReadCurrentSelection(Document document, double arcSagittaM = 0.002d, double planarityToleranceM = 0.005d, double splineChordM = 0.02d)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (double.IsNaN(arcSagittaM) || double.IsInfinity(arcSagittaM) || arcSagittaM <= 0d) throw new ArgumentOutOfRangeException(nameof(arcSagittaM));
            if (double.IsNaN(planarityToleranceM) || double.IsInfinity(planarityToleranceM) || planarityToleranceM <= 0d) throw new ArgumentOutOfRangeException(nameof(planarityToleranceM));
            if (double.IsNaN(splineChordM) || double.IsInfinity(splineChordM) || splineChordM <= 0d) throw new ArgumentOutOfRangeException(nameof(splineChordM));
            var editor = document.Editor;
            var selection = editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                var prompted = editor.GetSelection();
                if (prompted.Status != PromptStatus.OK || prompted.Value == null) return Array.Empty<BoundarySegment>();
                selection = prompted;
            }

            var units = CadUnitService.GetPolicy(document);
            var result = new List<BoundarySegment>();
            double? referenceElevationM = null;

            void RequireElevation(double drawingZ, string label)
            {
                var elevationM = units.ToMeters(drawingZ);
                if (double.IsNaN(elevationM) || double.IsInfinity(elevationM)) throw new InvalidOperationException(label + " có cao độ không hữu hạn.");
                if (!referenceElevationM.HasValue) { referenceElevationM = elevationM; return; }
                if (Math.Abs(elevationM - referenceElevationM.Value) > planarityToleranceM)
                    throw new InvalidOperationException("QS3DROOMAUTO yêu cầu toàn bộ boundary đồng phẳng; " + label + " lệch cao độ quá " + planarityToleranceM.ToString("R") + " m.");
            }

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var handle = entity.Handle.ToString();
                    if (entity is Line line)
                    {
                        RequireElevation(line.StartPoint.Z, "LINE " + handle + " start");
                        RequireElevation(line.EndPoint.Z, "LINE " + handle + " end");
                        result.Add(new BoundarySegment(
                            new Point2(units.ToMeters(line.StartPoint.X), units.ToMeters(line.StartPoint.Y)),
                            new Point2(units.ToMeters(line.EndPoint.X), units.ToMeters(line.EndPoint.Y)),
                            handle));
                        continue;
                    }

                    if (entity is Arc arc)
                    {
                        var normal = arc.Normal;
                        if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d)
                            throw new NotSupportedException("QS3DROOMAUTO hiện chỉ nhận ARC plan-view có normal +Z.");
                        RequireElevation(arc.StartPoint.Z, "ARC " + handle + " start");
                        RequireElevation(arc.EndPoint.Z, "ARC " + handle + " end");
                        RequireElevation(arc.Center.Z, "ARC " + handle + " center");
                        var sweep = arc.EndAngle - arc.StartAngle;
                        while (sweep <= 0d) sweep += Math.PI * 2d;
                        if (double.IsNaN(sweep) || double.IsInfinity(sweep) || sweep <= 1e-12d || sweep >= Math.PI * 2d - 1e-12d)
                            throw new InvalidOperationException("ARC " + handle + " có sweep không hợp lệ cho room boundary.");
                        var bulge = Math.Tan(sweep / 4d);
                        if (double.IsNaN(bulge) || double.IsInfinity(bulge)) throw new InvalidOperationException("ARC " + handle + " tạo bulge không hữu hạn.");
                        var start = new Point2(units.ToMeters(arc.StartPoint.X), units.ToMeters(arc.StartPoint.Y));
                        var end = new Point2(units.ToMeters(arc.EndPoint.X), units.ToMeters(arc.EndPoint.Y));
                        var points = BulgeArcTessellator.Tessellate(start, end, bulge, arcSagittaM);
                        for (var pointIndex = 1; pointIndex < points.Count; pointIndex++)
                            result.Add(new BoundarySegment(points[pointIndex - 1], points[pointIndex], handle));
                        continue;
                    }

                    if (entity is Spline spline)
                    {
                        var totalDrawing = spline.GetDistanceAtParameter(spline.EndParam);
                        if (double.IsNaN(totalDrawing) || double.IsInfinity(totalDrawing) || totalDrawing <= 0d)
                            throw new InvalidOperationException("SPLINE " + handle + " có chiều dài không hợp lệ.");
                        var totalM = units.ToMeters(totalDrawing);
                        if (double.IsNaN(totalM) || double.IsInfinity(totalM) || totalM <= 0d)
                            throw new InvalidOperationException("SPLINE " + handle + " có chiều dài metric không hợp lệ.");
                        var required = Math.Ceiling(totalM / splineChordM);
                        if (double.IsNaN(required) || double.IsInfinity(required) || required > MaxSplineSegments)
                            throw new InvalidOperationException("SPLINE " + handle + " cần quá " + MaxSplineSegments + " segment; tăng RoomBoundarySplineChordM hoặc simplify spline.");
                        var splineSegmentCount = Math.Max(1, (int)required);
                        Point2? previous = null;
                        for (var sample = 0; sample <= splineSegmentCount; sample++)
                        {
                            var distance = totalDrawing * (sample / (double)splineSegmentCount);
                            var point = sample == splineSegmentCount ? spline.EndPoint : spline.GetPointAtDist(distance);
                            RequireElevation(point.Z, "SPLINE " + handle + " sample " + sample);
                            var current = new Point2(units.ToMeters(point.X), units.ToMeters(point.Y));
                            if (previous.HasValue && previous.Value.DistanceTo(current) > 1e-12d)
                                result.Add(new BoundarySegment(previous.Value, current, handle));
                            previous = current;
                        }
                        continue;
                    }

                    if (!(entity is Polyline polyline)) continue;
                    var polylineNormal = polyline.Normal;
                    if (Math.Abs(polylineNormal.X) > 1e-9d || Math.Abs(polylineNormal.Y) > 1e-9d || polylineNormal.Z < 1d - 1e-9d)
                        throw new NotSupportedException("QS3DROOMAUTO hiện chỉ nhận POLYLINE plan-view có normal +Z.");
                    RequireElevation(polyline.Elevation, "POLYLINE " + handle);
                    var count = polyline.NumberOfVertices;
                    if (count < 2) continue;
                    var segmentCount = polyline.Closed ? count : count - 1;
                    for (var index = 0; index < segmentCount; index++)
                    {
                        var next = (index + 1) % count;
                        var a = polyline.GetPoint2dAt(index);
                        var b = polyline.GetPoint2dAt(next);
                        var start = new Point2(units.ToMeters(a.X), units.ToMeters(a.Y));
                        var end = new Point2(units.ToMeters(b.X), units.ToMeters(b.Y));
                        var bulge = polyline.GetBulgeAt(index);
                        var points = BulgeArcTessellator.Tessellate(start, end, bulge, arcSagittaM);
                        for (var pointIndex = 1; pointIndex < points.Count; pointIndex++)
                            result.Add(new BoundarySegment(points[pointIndex - 1], points[pointIndex], handle));
                    }
                }
                transaction.Commit();
            }
            return result;
        }
    }
}
