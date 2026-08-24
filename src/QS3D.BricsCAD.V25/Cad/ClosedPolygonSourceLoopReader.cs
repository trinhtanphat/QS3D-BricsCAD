using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class ClosedPolygonSourceLoopReadResult
    {
        public ClosedPolygonSourceLoopReadResult(
            string sourceHandle,
            IReadOnlyList<Point2> loop,
            double drawingElevation,
            string fingerprint)
        {
            if (string.IsNullOrWhiteSpace(sourceHandle)) throw new ArgumentException("Source handle is required.", nameof(sourceHandle));
            SourceHandle = sourceHandle;
            Loop = loop ?? throw new ArgumentNullException(nameof(loop));
            DrawingElevation = drawingElevation;
            Fingerprint = string.IsNullOrWhiteSpace(fingerprint)
                ? throw new ArgumentException("Geometry fingerprint is required.", nameof(fingerprint))
                : fingerprint;
        }

        public string SourceHandle { get; }
        public IReadOnlyList<Point2> Loop { get; }
        public double DrawingElevation { get; }
        public string Fingerprint { get; }
    }

    internal static class ClosedPolygonSourceLoopReader
    {
        internal const int MaxSourceVertices = 4096;
        internal const int MaxTessellatedVertices = 4096;
        private const double HorizontalNormalTolerance = 1e-9d;
        private const double HorizontalElevationTolerance = 1e-8d;

        public static ClosedPolygonSourceLoopReadResult Read(
            Document document,
            Polyline polyline,
            double maximumSagittaM,
            string label)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));
            if (string.IsNullOrWhiteSpace(label)) label = "closed polygon";

            maximumSagittaM = CadGeometryGuard.Positive(maximumSagittaM, label + "/maximum sagitta");
            if (polyline.IsErased) throw new InvalidOperationException(label + " source POLYLINE đã bị xóa.");
            if (!polyline.Closed) throw new InvalidOperationException(label + " source POLYLINE phải closed.");
            if (polyline.NumberOfVertices < 3) throw new InvalidOperationException(label + " source POLYLINE cần ít nhất 3 vertex.");
            if (polyline.NumberOfVertices > MaxSourceVertices)
                throw new InvalidOperationException(label + " source POLYLINE vượt quá giới hạn " + MaxSourceVertices + " vertex.");

            var normal = polyline.Normal;
            var normalX = CadGeometryGuard.Finite(normal.X, label + "/normal X");
            var normalY = CadGeometryGuard.Finite(normal.Y, label + "/normal Y");
            var normalZ = CadGeometryGuard.Finite(normal.Z, label + "/normal Z");
            if (Math.Abs(normalX) > HorizontalNormalTolerance
                || Math.Abs(normalY) > HorizontalNormalTolerance
                || Math.Abs(Math.Abs(normalZ) - 1d) > HorizontalNormalTolerance)
            {
                throw new InvalidOperationException(label + " source POLYLINE phải nằm trên mặt phẳng ngang; OCS nghiêng bị fail closed.");
            }

            var elevationOcs = CadGeometryGuard.Finite(polyline.Elevation, label + "/elevation");
            var source = new List<BulgedPolygonVertex2>(polyline.NumberOfVertices);
            for (var index = 0; index < polyline.NumberOfVertices; index++)
            {
                var point = polyline.GetPoint2dAt(index);
                var x = CadGeometryGuard.Finite(point.X, label + "/vertex X");
                var y = CadGeometryGuard.Finite(point.Y, label + "/vertex Y");
                var bulge = CadGeometryGuard.Finite(polyline.GetBulgeAt(index), label + "/bulge");
                source.Add(new BulgedPolygonVertex2(new Point2(x, y), bulge));
            }

            var maximumSagittaDrawing = CadGeometryGuard.ToDrawingUnits(document, maximumSagittaM, label + "/maximum sagitta");
            var tessellatedOcs = BulgedPolygonFootprintTessellator.TessellateClosed(source.AsReadOnly(), maximumSagittaDrawing);
            if (tessellatedOcs.Count > MaxTessellatedVertices)
                throw new InvalidOperationException(label + " tessellation vượt quá giới hạn " + MaxTessellatedVertices + " vertex.");

            var planeToWorld = Matrix3d.PlaneToWorld(normal);
            var world = new List<Point2>(tessellatedOcs.Count);
            double? drawingElevation = null;
            for (var index = 0; index < tessellatedOcs.Count; index++)
            {
                var ocs = tessellatedOcs[index];
                var wcs = new Point3d(ocs.X, ocs.Y, elevationOcs).TransformBy(planeToWorld);
                var wcsX = CadGeometryGuard.Finite(wcs.X, label + "/WCS X");
                var wcsY = CadGeometryGuard.Finite(wcs.Y, label + "/WCS Y");
                var wcsZ = CadGeometryGuard.Finite(wcs.Z, label + "/WCS Z");

                if (!drawingElevation.HasValue)
                {
                    drawingElevation = wcsZ;
                }
                else
                {
                    var scale = Math.Max(1d, Math.Max(Math.Abs(drawingElevation.Value), Math.Abs(wcsZ)));
                    if (Math.Abs(wcsZ - drawingElevation.Value) > HorizontalElevationTolerance * scale)
                        throw new InvalidOperationException(label + " source POLYLINE không tạo thành một footprint WCS ngang ổn định.");
                }

                world.Add(new Point2(
                    CadGeometryGuard.ToMeters(document, wcsX, label + "/WCS X"),
                    CadGeometryGuard.ToMeters(document, wcsY, label + "/WCS Y")));
            }

            if (!drawingElevation.HasValue) throw new InvalidOperationException(label + " tessellation không tạo được vertex.");
            var loop = PolygonScanlineClipper.NormalizeAndValidate(world.AsReadOnly());
            if (loop.Count > MaxTessellatedVertices)
                throw new InvalidOperationException(label + " normalized loop vượt quá giới hạn " + MaxTessellatedVertices + " vertex.");

            var elevationM = CadGeometryGuard.ToMeters(document, drawingElevation.Value, label + "/WCS elevation");
            return new ClosedPolygonSourceLoopReadResult(
                polyline.Handle.ToString(),
                loop,
                drawingElevation.Value,
                ComputeFingerprint(loop, elevationM));
        }

        private static string ComputeFingerprint(IReadOnlyList<Point2> loop, double elevationM)
        {
            if (loop == null) throw new ArgumentNullException(nameof(loop));
            if (loop.Count < 3) throw new ArgumentException("Loop requires at least three vertices.", nameof(loop));

            var start = 0;
            for (var index = 1; index < loop.Count; index++)
            {
                if (ComparePoint(loop[index], loop[start]) < 0) start = index;
            }

            var forward = SerializeRing(loop, start, 1, elevationM);
            var reverse = SerializeRing(loop, start, -1, elevationM);
            var canonical = string.CompareOrdinal(forward, reverse) <= 0 ? forward : reverse;
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static int ComparePoint(Point2 left, Point2 right)
        {
            var x = left.X.CompareTo(right.X);
            return x != 0 ? x : left.Y.CompareTo(right.Y);
        }

        private static string SerializeRing(IReadOnlyList<Point2> loop, int start, int direction, double elevationM)
        {
            var builder = new StringBuilder(loop.Count * 48 + 32);
            builder.Append("z=").Append(elevationM.ToString("R", CultureInfo.InvariantCulture));
            for (var offset = 0; offset < loop.Count; offset++)
            {
                var index = (start + direction * offset) % loop.Count;
                if (index < 0) index += loop.Count;
                var point = loop[index];
                builder.Append('|')
                    .Append(point.X.ToString("R", CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(point.Y.ToString("R", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }
    }
}
