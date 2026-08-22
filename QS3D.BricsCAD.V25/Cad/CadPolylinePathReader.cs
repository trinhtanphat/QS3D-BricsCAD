using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CadPolylinePathReader
    {
        public static IReadOnlyList<Point2> ReadOpenWcsXy(Document document, Polyline polyline, double maximumSagittaM, string label)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));
            maximumSagittaM = CadGeometryGuard.Positive(maximumSagittaM, label + "/maximum sagitta");
            if (polyline.IsErased) throw new InvalidOperationException(label + " source POLYLINE đã bị xóa.");
            if (polyline.Closed) throw new InvalidOperationException(label + " source POLYLINE phải open.");
            if (polyline.NumberOfVertices < 2) throw new InvalidOperationException(label + " source POLYLINE cần ít nhất 2 vertex.");
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(normal.Z - 1d) > 1e-9d)
                throw new InvalidOperationException(label + " source POLYLINE hiện phải nằm trong WCS XY với +Z normal; OCS nghiêng bị fail closed để tránh dựng frame sai.");

            var result = new List<Point2>();
            for (var segment = 0; segment < polyline.NumberOfVertices - 1; segment++)
            {
                var startDrawing = polyline.GetPoint2dAt(segment);
                var endDrawing = polyline.GetPoint2dAt(segment + 1);
                var start = new Point2(
                    CadGeometryGuard.ToMeters(document, startDrawing.X, label + "/X"),
                    CadGeometryGuard.ToMeters(document, startDrawing.Y, label + "/Y"));
                var end = new Point2(
                    CadGeometryGuard.ToMeters(document, endDrawing.X, label + "/X"),
                    CadGeometryGuard.ToMeters(document, endDrawing.Y, label + "/Y"));
                var bulge = CadGeometryGuard.Finite(polyline.GetBulgeAt(segment), label + "/bulge");
                IReadOnlyList<Point2> segmentPoints = Math.Abs(bulge) <= 1e-12d
                    ? new[] { start, end }
                    : BulgeArcTessellator.Tessellate(start, end, bulge, maximumSagittaM);
                if (result.Count == 0) result.Add(segmentPoints[0]);
                for (var index = 1; index < segmentPoints.Count; index++) result.Add(segmentPoints[index]);
            }
            if (result.Count < 2) throw new InvalidOperationException(label + " tessellation không tạo được path hợp lệ.");
            return result.AsReadOnly();
        }
    }
}
