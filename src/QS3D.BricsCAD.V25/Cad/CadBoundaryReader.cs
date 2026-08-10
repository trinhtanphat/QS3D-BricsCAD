using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class CadBoundarySelection
    {
        public CadBoundarySelection(IReadOnlyList<BoundarySegment2> segments, IReadOnlyList<string> sourceHandles, int unsupportedEntities)
        {
            Segments = segments;
            SourceHandles = sourceHandles;
            UnsupportedEntities = unsupportedEntities;
        }

        public IReadOnlyList<BoundarySegment2> Segments { get; }
        public IReadOnlyList<string> SourceHandles { get; }
        public int UnsupportedEntities { get; }
    }

    internal static class CadBoundaryReader
    {
        public static CadBoundarySelection ReadCurrentSelection(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var editor = document.Editor;
            var selection = editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                var prompt = editor.GetSelection();
                if (prompt.Status != PromptStatus.OK || prompt.Value == null)
                    return new CadBoundarySelection(Array.Empty<BoundarySegment2>(), Array.Empty<string>(), 0);
                selection = prompt;
            }

            var segments = new List<BoundarySegment2>();
            var handles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unsupported = 0;
            var policy = CadUnitService.GetPolicy(document);
            var drawingTolerance = Math.Abs(policy.FromMeters(0.001d));
            if (double.IsNaN(drawingTolerance) || double.IsInfinity(drawingTolerance) || drawingTolerance <= 0d) drawingTolerance = 1e-6;

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null) continue;
                    var handle = entity.Handle.ToString();
                    if (entity is Line line)
                    {
                        if (Math.Abs(line.EndPoint.Z - line.StartPoint.Z) > drawingTolerance) { unsupported++; continue; }
                        if (TryAdd(segments, line.StartPoint.X, line.StartPoint.Y, line.EndPoint.X, line.EndPoint.Y, handle, policy)) handles.Add(handle);
                        continue;
                    }
                    if (entity is Polyline polyline)
                    {
                        if (!TryAddPolyline(polyline, handle, policy, segments)) { unsupported++; continue; }
                        handles.Add(handle);
                        continue;
                    }
                    unsupported++;
                }
                transaction.Commit();
            }

            return new CadBoundarySelection(segments, handles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal).ToArray(), unsupported);
        }

        private static bool TryAddPolyline(Polyline polyline, string handle, QS3D.Core.Units.ProjectUnitPolicy policy, ICollection<BoundarySegment2> output)
        {
            var count = polyline.NumberOfVertices;
            if (count < 2) return false;
            var segmentCount = polyline.Closed ? count : count - 1;
            for (var i = 0; i < segmentCount; i++)
            {
                if (Math.Abs(polyline.GetBulgeAt(i)) > 1e-12) return false;
            }

            var pending = new List<BoundarySegment2>();
            for (var i = 0; i < segmentCount; i++)
            {
                var next = (i + 1) % count;
                var a = polyline.GetPoint2dAt(i);
                var b = polyline.GetPoint2dAt(next);
                if (!TryCreate(a.X, a.Y, b.X, b.Y, handle, policy, out var segment)) continue;
                pending.Add(segment);
            }
            if (pending.Count == 0) return false;
            foreach (var segment in pending) output.Add(segment);
            return true;
        }

        private static bool TryAdd(ICollection<BoundarySegment2> output, double x1, double y1, double x2, double y2, string handle, QS3D.Core.Units.ProjectUnitPolicy policy)
        {
            if (!TryCreate(x1, y1, x2, y2, handle, policy, out var segment)) return false;
            output.Add(segment);
            return true;
        }

        private static bool TryCreate(double x1, double y1, double x2, double y2, string handle, QS3D.Core.Units.ProjectUnitPolicy policy, out BoundarySegment2 segment)
        {
            segment = null!;
            var ax = policy.ToMeters(x1); var ay = policy.ToMeters(y1);
            var bx = policy.ToMeters(x2); var by = policy.ToMeters(y2);
            if (!Finite(ax) || !Finite(ay) || !Finite(bx) || !Finite(by)) return false;
            var a = new Point2(ax, ay); var b = new Point2(bx, by);
            if (a.DistanceTo(b) <= 1e-9) return false;
            segment = new BoundarySegment2(a, b, handle);
            return true;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
