using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CurtainWallFrameLiveFingerprint
    {
        public static string Compute(Document document, Transaction transaction, ProjectState project, ProjectElement host, Line hostLine) =>
            Compute(document, transaction, project, host, (Entity)hostLine);

        public static string Compute(Document document, Transaction transaction, ProjectState project, ProjectElement host, Polyline hostPolyline) =>
            Compute(document, transaction, project, host, (Entity)hostPolyline);

        public static string Compute(Document document, Transaction transaction, ProjectState project, ProjectElement host, Entity hostSource)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (hostSource == null) throw new ArgumentNullException(nameof(hostSource));

            var text = new StringBuilder();
            text.Append("host=").Append(host.Id).Append('|').Append(hostSource.Handle.ToString()).Append('|');
            AppendHostGeometry(text, hostSource);
            double hostSourceBaseDrawing;
            if (hostSource is Line hostLine) hostSourceBaseDrawing = hostLine.StartPoint.Z;
            else if (hostSource is Polyline hostPolyline) hostSourceBaseDrawing = hostPolyline.Elevation;
            else throw new InvalidOperationException("Curtain live fingerprint supports only LINE or POLYLINE GlassWall sources.");
            var hostPlacement = CadElementVerticalPlacement.Resolve(
                document,
                project,
                host,
                project.FindFamily(host.FamilyId),
                hostSourceBaseDrawing,
                "HeightM",
                3.6d);

            foreach (var opening in project.Elements
                .Where(x => (x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening) &&
                            x.Properties.TryGetValue("HostWallId", out var hostId) &&
                            string.Equals(hostId, host.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var family = project.FindFamily(opening.FamilyId);
                var width = CadGeometryGuard.Number(opening, family, "WidthM", 0d);
                var clearance = CadGeometryGuard.Number(opening, family, "BooleanClearanceM", 0.01d);
                Finite(width, opening.Id + "/WidthM");
                Finite(clearance, opening.Id + "/BooleanClearanceM");

                var sourceIds = CadHandleService.Resolve(document, opening.SourceHandles);
                if (sourceIds.Count != 1)
                    throw new InvalidOperationException("Curtain live fingerprint requires exactly one live CAD source for linked opening " + opening.Id + ".");
                var entity = transaction.GetObject(sourceIds[0], OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException("Curtain live fingerprint cannot read linked opening source " + opening.Id + ".");
                Extents3d extents;
                try { extents = entity.GeometricExtents; }
                catch (Exception ex) { throw new InvalidOperationException("Curtain live fingerprint cannot read extents for " + opening.Id + ".", ex); }
                var openingPlacement = CadHostedOpeningVerticalPlacement.Resolve(
                    document,
                    project,
                    opening,
                    family,
                    extents.MinPoint.Z,
                    hostPlacement,
                    0d,
                    opening.Category == ElementCategory.Door ? 0d : 0.9d);
                var height = openingPlacement.HeightM;
                var sill = openingPlacement.SillHeightM;

                text.Append("|opening=").Append(opening.Id)
                    .Append(':').Append(opening.Category)
                    .Append(':').Append(sourceIds[0].Handle.ToString())
                    .Append(':').Append(width.ToString("R", CultureInfo.InvariantCulture))
                    .Append(':').Append(height.ToString("R", CultureInfo.InvariantCulture))
                    .Append(':').Append(sill.ToString("R", CultureInfo.InvariantCulture))
                    .Append(':').Append(clearance.ToString("R", CultureInfo.InvariantCulture))
                    .Append(':');
                Point(text, extents.MinPoint);
                text.Append('>');
                Point(text, extents.MaxPoint);
            }

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var output = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) output.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }

        private static void AppendHostGeometry(StringBuilder text, Entity hostSource)
        {
            if (hostSource is Line line)
            {
                Point(text, line.StartPoint);
                text.Append('|');
                Point(text, line.EndPoint);
                return;
            }

            if (hostSource is Polyline polyline)
            {
                text.Append("kind=POLYLINE|closed=").Append(polyline.Closed ? "1" : "0")
                    .Append("|elevation=").Append(Finite(polyline.Elevation, "polyline elevation").ToString("R", CultureInfo.InvariantCulture))
                    .Append("|normal=")
                    .Append(Finite(polyline.Normal.X, "polyline normal X").ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Finite(polyline.Normal.Y, "polyline normal Y").ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Finite(polyline.Normal.Z, "polyline normal Z").ToString("R", CultureInfo.InvariantCulture))
                    .Append("|vertices=").Append(polyline.NumberOfVertices.ToString(CultureInfo.InvariantCulture));
                for (var index = 0; index < polyline.NumberOfVertices; index++)
                {
                    var point = polyline.GetPoint2dAt(index);
                    text.Append('|').Append(index.ToString(CultureInfo.InvariantCulture)).Append(':')
                        .Append(Finite(point.X, "polyline vertex X").ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(Finite(point.Y, "polyline vertex Y").ToString("R", CultureInfo.InvariantCulture));
                    if (index < polyline.NumberOfVertices - 1)
                        text.Append(',').Append(Finite(polyline.GetBulgeAt(index), "polyline bulge").ToString("R", CultureInfo.InvariantCulture));
                }
                return;
            }

            throw new InvalidOperationException("Curtain live fingerprint supports only LINE or POLYLINE GlassWall sources.");
        }

        private static void Point(StringBuilder text, Teigha.Geometry.Point3d point)
        {
            text.Append(Finite(point.X, "point X").ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(Finite(point.Y, "point Y").ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(Finite(point.Z, "point Z").ToString("R", CultureInfo.InvariantCulture));
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException(label + " must be finite.");
            return value;
        }
    }
}
