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
        public static IReadOnlyList<BoundarySegment> ReadCurrentSelection(Document document, double arcSagittaM = 0.002d)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (double.IsNaN(arcSagittaM) || double.IsInfinity(arcSagittaM) || arcSagittaM <= 0d) throw new ArgumentOutOfRangeException(nameof(arcSagittaM));
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
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var handle = entity.Handle.ToString();
                    if (entity is Line line)
                    {
                        result.Add(new BoundarySegment(
                            new Point2(units.ToMeters(line.StartPoint.X), units.ToMeters(line.StartPoint.Y)),
                            new Point2(units.ToMeters(line.EndPoint.X), units.ToMeters(line.EndPoint.Y)),
                            handle));
                        continue;
                    }

                    if (!(entity is Polyline polyline)) continue;
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
