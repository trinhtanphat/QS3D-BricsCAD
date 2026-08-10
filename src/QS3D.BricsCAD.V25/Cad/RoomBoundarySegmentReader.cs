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
        public static IReadOnlyList<BoundarySegment> ReadCurrentSelection(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
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
                        if (Math.Abs(polyline.GetBulgeAt(index)) > 1e-12d)
                            throw new NotSupportedException("QS3DROOMAUTO hiện chỉ nhận segment LINE/Polyline thẳng; polyline có cung (bulge) cần được chia segment trước.");
                        var a = polyline.GetPoint2dAt(index);
                        var b = polyline.GetPoint2dAt(next);
                        result.Add(new BoundarySegment(
                            new Point2(units.ToMeters(a.X), units.ToMeters(a.Y)),
                            new Point2(units.ToMeters(b.X), units.ToMeters(b.Y)),
                            handle));
                    }
                }
                transaction.Commit();
            }
            return result;
        }
    }
}
