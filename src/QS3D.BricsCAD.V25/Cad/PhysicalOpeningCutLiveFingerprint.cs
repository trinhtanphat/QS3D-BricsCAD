using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class PhysicalOpeningCutLiveFingerprint
    {
        public static string Compute(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement host,
            Entity hostSource,
            IEnumerable<ProjectElement>? openings = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (hostSource == null) throw new ArgumentNullException(nameof(hostSource));

            var hostFamily = project.FindFamily(host.FamilyId);
            var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, hostFamily, "ThicknessM", 0.2d), host.Id + "/ThicknessM");
            var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, hostFamily, "HeightM", 3.6d), host.Id + "/HeightM");
            var bottomOffsetM = CadGeometryGuard.Finite(CadGeometryGuard.Number(host, hostFamily, "BottomOffsetM", 0d), host.Id + "/BottomOffsetM");
            var hostSourceBaseDrawing = HostSourceBase(hostSource);
            var hostPlacement = CadVerticalPlacementResolver.Resolve(
                document, project, host, hostSourceBaseDrawing, heightM, bottomOffsetM);

            var linked = (openings ?? project.Elements.Where(x => IsLinkedOpening(x, host.Id)))
                .Where(x => IsLinkedOpening(x, host.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var text = new StringBuilder();
            text.Append("host=").Append(host.Id)
                .Append('|').Append(host.Category)
                .Append('|').Append(hostSource.Handle.ToString())
                .Append("|thickness=").Append(Number(thicknessM))
                .Append("|height=").Append(Number(heightM))
                .Append("|bottom=").Append(Number(bottomOffsetM))
                .Append('|');
            AppendHostGeometry(text, hostSource);
            if (HasLevelPlacement(hostPlacement.Semantic))
                text.Append("|level=").Append(PlacementToken(hostPlacement.Semantic));

            if (hostSource is Polyline curved && HasBulge(curved))
            {
                text.Append("|curved-settings=")
                    .Append(Number(ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d)))
                    .Append(',').Append(Number(ProjectNumber(project, "PhysicalOpeningMaximumOffsetM", 0.35d, 1e-6d)))
                    .Append(',').Append(Number(ProjectNumber(project, "PhysicalOpeningAmbiguityM", 0.01d, 0d)))
                    .Append(',').Append(Number(ProjectNumber(project, "WallMiterLimit", 4d, 1d)));
            }

            foreach (var opening in linked)
                AppendOpening(
                    document,
                    transaction,
                    project,
                    text,
                    host,
                    opening,
                    hostSourceBaseDrawing,
                    heightM,
                    bottomOffsetM);

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var output = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) output.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }

        private static void AppendOpening(
            Document document,
            Transaction transaction,
            ProjectState project,
            StringBuilder text,
            ProjectElement host,
            ProjectElement opening,
            double hostSourceBaseDrawing,
            double hostHeightM,
            double hostBottomOffsetM)
        {
            var family = project.FindFamily(opening.FamilyId);
            var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, family, "WidthM", 0.9d), opening.Id + "/WidthM");
            var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, family, "HeightM", 2.2d), opening.Id + "/HeightM");
            var sillM = CadGeometryGuard.Finite(
                CadGeometryGuard.Number(opening, family, "SillHeightM", CadGeometryGuard.Number(opening, family, "BottomOffsetM", 0d)),
                opening.Id + "/SillHeightM");
            if (sillM < 0d) throw new InvalidOperationException(opening.Id + "/SillHeightM phải >= 0.");
            var clearanceM = CadGeometryGuard.Finite(CadGeometryGuard.Number(opening, family, "BooleanClearanceM", 0.01d), opening.Id + "/BooleanClearanceM");
            if (clearanceM < 0d) throw new InvalidOperationException(opening.Id + "/BooleanClearanceM phải >= 0.");

            var placement = CadVerticalPlacementResolver.ResolveHostedOpening(
                document,
                project,
                host,
                opening,
                hostSourceBaseDrawing,
                hostHeightM,
                hostBottomOffsetM,
                heightM,
                sillM);

            var ids = CadHandleService.Resolve(document, opening.SourceHandles);
            if (ids.Count != 1)
                throw new InvalidOperationException("Physical opening live fingerprint cần đúng một live CAD source cho " + opening.Id + ".");
            var entity = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
            if (entity == null || entity.IsErased)
                throw new InvalidOperationException("Physical opening live fingerprint không đọc được source " + opening.Id + ".");

            Extents3d extents;
            try { extents = entity.GeometricExtents; }
            catch (Exception ex) { throw new InvalidOperationException("Không đọc được extents của opening " + opening.Id + ".", ex); }

            text.Append("|opening=").Append(opening.Id)
                .Append(':').Append(opening.Category)
                .Append(':').Append(ids[0].Handle.ToString())
                .Append(':').Append(Number(widthM))
                .Append(':').Append(Number(heightM))
                .Append(':').Append(Number(sillM))
                .Append(':').Append(Number(clearanceM))
                .Append(':');
            Point(text, extents.MinPoint);
            text.Append('>');
            Point(text, extents.MaxPoint);
            if (HasLevelPlacement(placement.Host.Semantic) || HasLevelPlacement(placement.Opening.Semantic))
                text.Append(":level:").Append(PlacementToken(placement.Opening.Semantic));
        }

        private static double HostSourceBase(Entity hostSource)
        {
            if (hostSource is Line line)
                return CadGeometryGuard.Finite(line.StartPoint.Z, "host line source base");
            if (hostSource is Polyline polyline)
                return CadGeometryGuard.Finite(polyline.Elevation, "host polyline source base");
            throw new InvalidOperationException("Physical opening cut live fingerprint supports only LINE/POLYLINE hosts.");
        }

        private static bool HasLevelPlacement(ElementVerticalPlacement placement) =>
            placement.UsesBottomLevel || placement.UsesTopLevel;

        private static string PlacementToken(ElementVerticalPlacement placement) =>
            Number(placement.BottomElevationM) + ":" + Number(placement.TopElevationM) + ":" + Number(placement.HeightM);

        private static void AppendHostGeometry(StringBuilder text, Entity hostSource)
        {
            if (hostSource is Line line)
            {
                text.Append("kind=LINE|");
                Point(text, line.StartPoint);
                text.Append('>');
                Point(text, line.EndPoint);
                return;
            }

            if (hostSource is Polyline polyline)
            {
                text.Append("kind=POLYLINE|closed=").Append(polyline.Closed ? "1" : "0")
                    .Append("|elevation=").Append(Number(CadGeometryGuard.Finite(polyline.Elevation, "polyline elevation")))
                    .Append("|normal=")
                    .Append(Number(CadGeometryGuard.Finite(polyline.Normal.X, "polyline normal X"))).Append(',')
                    .Append(Number(CadGeometryGuard.Finite(polyline.Normal.Y, "polyline normal Y"))).Append(',')
                    .Append(Number(CadGeometryGuard.Finite(polyline.Normal.Z, "polyline normal Z")))
                    .Append("|vertices=").Append(polyline.NumberOfVertices.ToString(CultureInfo.InvariantCulture));
                for (var index = 0; index < polyline.NumberOfVertices; index++)
                {
                    var point = polyline.GetPoint2dAt(index);
                    text.Append('|').Append(index.ToString(CultureInfo.InvariantCulture)).Append(':')
                        .Append(Number(CadGeometryGuard.Finite(point.X, "polyline vertex X"))).Append(',')
                        .Append(Number(CadGeometryGuard.Finite(point.Y, "polyline vertex Y")));
                    if (index < polyline.NumberOfVertices - 1)
                        text.Append(',').Append(Number(CadGeometryGuard.Finite(polyline.GetBulgeAt(index), "polyline bulge")));
                }
                return;
            }

            throw new InvalidOperationException("Physical opening cut live fingerprint chỉ hỗ trợ host LINE/POLYLINE.");
        }

        private static bool IsLinkedOpening(ProjectElement element, string hostId) =>
            (element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening) &&
            element.Properties.TryGetValue("HostWallId", out var linkedHostId) &&
            string.Equals(linkedHostId?.Trim(), hostId, StringComparison.OrdinalIgnoreCase);

        internal static bool HasBulge(Polyline polyline)
        {
            for (var index = 0; index < polyline.NumberOfVertices - 1; index++)
                if (Math.Abs(CadGeometryGuard.Finite(polyline.GetBulgeAt(index), "polyline bulge")) > 1e-12d) return true;
            return false;
        }

        private static double ProjectNumber(ProjectState project, string key, double fallback, double minimum)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static void Point(StringBuilder text, Teigha.Geometry.Point3d point)
        {
            text.Append(Number(CadGeometryGuard.Finite(point.X, "point X"))).Append(',')
                .Append(Number(CadGeometryGuard.Finite(point.Y, "point Y"))).Append(',')
                .Append(Number(CadGeometryGuard.Finite(point.Z, "point Z")));
        }

        private static string Number(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Physical opening live fingerprint contains non-finite value.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
