using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CurvedOpeningBooleanService
    {
        private sealed class PendingHostUpdate
        {
            public ProjectElement Host { get; set; } = null!;
            public string SolidHandle { get; set; } = string.Empty;
            public string Fingerprint { get; set; } = string.Empty;
            public int OpeningCount { get; set; }
        }

        public static int CutLinkedOpenings(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var linked = project.Elements
                .Where(x => (x.Category == ElementCategory.WallOpening || x.Category == ElementCategory.Door) && x.Properties.TryGetValue("HostWallId", out var hostId) && !string.IsNullOrWhiteSpace(hostId))
                .GroupBy(x => x.Properties["HostWallId"], StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (linked.Count == 0) return 0;

            var pending = new List<PendingHostUpdate>();
            var cuts = 0;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var group in linked)
                {
                    var host = project.FindElement(group.Key) ?? throw new InvalidOperationException("Opening host not found: " + group.Key);
                    if (!SupportedHost(host.Category)) continue;
                    if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var solidHandle) || string.IsNullOrWhiteSpace(solidHandle)) continue;
                    var hostSourceId = ResolveSingle(document, host.SourceHandles, "curved host source " + host.Id);
                    if (hostSourceId.IsNull) continue;
                    var hostSource = transaction.GetObject(hostSourceId, OpenMode.ForRead, false) as Polyline;
                    if (hostSource == null || hostSource.IsErased) continue;
                    ValidateHostPolyline(hostSource, host.Id);
                    if (!HasBulge(hostSource)) continue;

                    var solidId = ResolveSingle(document, new[] { solidHandle }, "curved generated host solid " + host.Id);
                    if (solidId.IsNull) continue;
                    var hostSolid = transaction.GetObject(solidId, OpenMode.ForWrite, false) as Solid3d;
                    if (hostSolid == null || hostSolid.IsErased) continue;

                    var family = project.FindFamily(host.FamilyId);
                    var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "ThicknessM", 0.2d), host.Id + "/ThicknessM");
                    var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "HeightM", 3.6d), host.Id + "/HeightM");
                    var bottomOffsetM = CadGeometryGuard.Number(host, family, "BottomOffsetM", 0d);
                    var sagittaM = ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d);
                    var maximumOffsetM = ProjectNumber(project, "PhysicalOpeningMaximumOffsetM", 0.35d, 1e-6d);
                    var ambiguityM = ProjectNumber(project, "PhysicalOpeningAmbiguityM", 0.01d, 0d);
                    var miterLimit = ProjectNumber(project, "WallMiterLimit", 4d, 1d);
                    var centerline = ReadCenterline(document, hostSource, sagittaM, host.Id);
                    var fingerprintParts = new List<string>();
                    var hostCutCount = 0;

                    foreach (var opening in group.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                    {
                        var openingFamily = project.FindFamily(opening.FamilyId);
                        var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "WidthM", 0.9d), opening.Id + "/WidthM");
                        var openingHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "HeightM", 2.2d), opening.Id + "/HeightM");
                        var sillM = CadGeometryGuard.Number(opening, openingFamily, "SillHeightM", CadGeometryGuard.Number(opening, openingFamily, "BottomOffsetM", 0d));
                        if (sillM < 0d) throw new InvalidOperationException(opening.Id + "/SillHeightM phải >= 0.");
                        var clearanceM = CadGeometryGuard.Number(opening, openingFamily, "BooleanClearanceM", 0.01d);
                        if (clearanceM < 0d) throw new InvalidOperationException(opening.Id + "/BooleanClearanceM phải >= 0.");
                        var openingSourceId = ResolveSingle(document, opening.SourceHandles, "curved opening source " + opening.Id);
                        if (openingSourceId.IsNull) throw new InvalidOperationException("Opening " + opening.Id + " chưa có live CAD source để xác định vị trí khoét cong.");
                        var openingEntity = transaction.GetObject(openingSourceId, OpenMode.ForRead, false) as Entity;
                        if (openingEntity == null || openingEntity.IsErased) throw new InvalidOperationException("Opening source không còn live: " + opening.Id);
                        var extents = openingEntity.GeometricExtents;
                        var centerX = CadGeometryGuard.Midpoint(extents.MinPoint.X, extents.MaxPoint.X, opening.Id + "/center X");
                        var centerY = CadGeometryGuard.Midpoint(extents.MinPoint.Y, extents.MaxPoint.Y, opening.Id + "/center Y");
                        var openingPoint = new Point2(
                            CadGeometryGuard.ToMeters(document, centerX, opening.Id + "/center X"),
                            CadGeometryGuard.ToMeters(document, centerY, opening.Id + "/center Y"));

                        var footprint = CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
                        {
                            Centerline = centerline,
                            OpeningPoint = openingPoint,
                            OpeningWidthM = widthM,
                            HostThicknessM = thicknessM,
                            ClearanceM = clearanceM,
                            MaximumCenterlineOffsetM = maximumOffsetM,
                            AmbiguityMarginM = ambiguityM,
                            MiterLimit = miterLimit,
                            ToleranceM = 1e-9d
                        });
                        var vertical = OpeningCutPlanner.Plan(new OpeningCutInput
                        {
                            HostLengthM = footprint.HostCenterlineLengthM,
                            HostThicknessM = thicknessM,
                            HostHeightM = heightM,
                            OpeningWidthM = widthM,
                            OpeningHeightM = openingHeightM,
                            SillHeightM = sillM,
                            CenterAlongHostM = footprint.CenterStationM,
                            ClearanceM = clearanceM
                        });

                        using (var cutter = BuildCutter(document, hostSource.Elevation, bottomOffsetM, footprint.CutterPolygon, vertical.CutterHeightM, vertical.BaseElevationM, opening.Id))
                            hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
                        cuts++;
                        hostCutCount++;
                        fingerprintParts.Add(opening.Id + ":" + openingSourceId.Handle + ":" +
                            openingPoint.X.ToString("R", CultureInfo.InvariantCulture) + "," + openingPoint.Y.ToString("R", CultureInfo.InvariantCulture) + ":" +
                            widthM.ToString("R", CultureInfo.InvariantCulture) + ":" + openingHeightM.ToString("R", CultureInfo.InvariantCulture) + ":" +
                            sillM.ToString("R", CultureInfo.InvariantCulture) + ":" + clearanceM.ToString("R", CultureInfo.InvariantCulture) + ":" +
                            footprint.CenterStationM.ToString("R", CultureInfo.InvariantCulture));
                    }

                    var fingerprint = CurvedFingerprint(hostSourceId.Handle.ToString(), centerline, thicknessM, heightM, bottomOffsetM, sagittaM, fingerprintParts);
                    var currentSolidHandle = solidId.Handle.ToString();
                    if (host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle", out var previousSolid) &&
                        string.Equals(previousSolid, currentSolidHandle, StringComparison.OrdinalIgnoreCase) &&
                        host.Properties.TryGetValue("PhysicalOpeningCutFingerprint", out var previousFingerprint))
                    {
                        if (string.Equals(previousFingerprint, fingerprint, StringComparison.Ordinal)) continue;
                        throw new InvalidOperationException("Host " + host.Id + " đã được khoét trên generated solid hiện tại nhưng geometry/fingerprint đã thay đổi. Build 3D lại host trước khi khoét curved openings.");
                    }
                    pending.Add(new PendingHostUpdate { Host = host, SolidHandle = currentSolidHandle, Fingerprint = fingerprint, OpeningCount = hostCutCount });
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                update.Host.Properties["PhysicalOpeningCutSolidHandle"] = update.SolidHandle;
                update.Host.Properties["PhysicalOpeningCutFingerprint"] = update.Fingerprint;
                update.Host.Properties["PhysicalOpeningCutCount"] = update.OpeningCount.ToString(CultureInfo.InvariantCulture);
                update.Host.Properties["PhysicalOpeningCutMode"] = "CurvedCenterlineFootprint";
                AuditTrail.ForProject(project).Record("geometry.opening.boolean.curved", update.Host.Id, update.OpeningCount.ToString(CultureInfo.InvariantCulture) + " opening(s) • solid " + update.SolidHandle);
            }
            if (pending.Count > 0) { project.Touch(); document.Editor.Regen(); }
            return cuts;
        }

        private static Solid3d BuildCutter(Document document, double hostElevationDrawing, double hostBottomOffsetM, IReadOnlyList<Point2> polygonM, double heightM, double baseElevationM, string label)
        {
            if (polygonM == null || polygonM.Count < 3) throw new InvalidOperationException("Curved opening cutter footprint is invalid: " + label);
            var baseZ = CadGeometryGuard.Add(hostElevationDrawing,
                CadGeometryGuard.ToDrawingUnits(document, hostBottomOffsetM + baseElevationM, label + "/cutter base"), label + "/cutter world base");
            var height = CadGeometryGuard.ToDrawingUnits(document, heightM, label + "/cutter height");
            using (var boundary = new Polyline())
            {
                boundary.SetDatabaseDefaults(document.Database);
                boundary.Elevation = baseZ;
                for (var i = 0; i < polygonM.Count; i++)
                    boundary.AddVertexAt(i, new Point2d(
                        CadGeometryGuard.ToDrawingUnits(document, polygonM[i].X, label + "/footprint X"),
                        CadGeometryGuard.ToDrawingUnits(document, polygonM[i].Y, label + "/footprint Y")), 0d, 0d, 0d);
                boundary.Closed = true;
                var curves = new DBObjectCollection { boundary };
                var regions = Region.CreateFromCurves(curves);
                if (regions == null || regions.Count != 1) throw new InvalidOperationException("Curved opening cutter footprint must create exactly one Region: " + label);
                var region = regions[0] as Region ?? throw new InvalidOperationException("Curved opening cutter Region is invalid: " + label);
                try
                {
                    var solid = new Solid3d();
                    try
                    {
                        solid.SetDatabaseDefaults(document.Database);
                        solid.CreateExtrudedSolid(region, new Vector3d(0d, 0d, height), new SweepOptions());
                        var completed = solid; solid = null!; return completed;
                    }
                    finally { solid?.Dispose(); }
                }
                finally
                {
                    foreach (DBObject item in regions) item.Dispose();
                }
            }
        }

        private static IReadOnlyList<Point2> ReadCenterline(Document document, Polyline polyline, double sagittaM, string label)
        {
            var units = CadUnitService.GetPolicy(document);
            var points = new List<Point2>();
            for (var i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                var a = polyline.GetPoint2dAt(i); var b = polyline.GetPoint2dAt(i + 1);
                var start = new Point2(units.ToMeters(a.X), units.ToMeters(a.Y));
                var end = new Point2(units.ToMeters(b.X), units.ToMeters(b.Y));
                var bulge = polyline.GetBulgeAt(i);
                var segmentPoints = Math.Abs(bulge) <= 1e-12d ? (IReadOnlyList<Point2>)new[] { start, end } : BulgeArcTessellator.Tessellate(start, end, bulge, sagittaM);
                foreach (var point in segmentPoints)
                    if (points.Count == 0 || points[points.Count - 1].DistanceTo(point) > 1e-10d) points.Add(point);
            }
            if (points.Count < 2) throw new InvalidOperationException("Curved host centerline is degenerate: " + label);
            return points.AsReadOnly();
        }

        private static void ValidateHostPolyline(Polyline polyline, string label)
        {
            if (polyline.Closed) throw new InvalidOperationException("Curved physical opening cut yêu cầu open POLYLINE centerline: " + label);
            if (polyline.NumberOfVertices < 2) throw new InvalidOperationException("Curved host POLYLINE quá ngắn: " + label);
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d)
                throw new InvalidOperationException("Curved host POLYLINE phải plan-view +Z: " + label);
        }

        private static bool HasBulge(Polyline polyline)
        {
            for (var i = 0; i < polyline.NumberOfVertices - 1; i++) if (Math.Abs(polyline.GetBulgeAt(i)) > 1e-12d) return true;
            return false;
        }

        private static ObjectId ResolveSingle(Document document, IEnumerable<string> handles, string label)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count == 0) return ObjectId.Null;
            if (ids.Count > 1) throw new InvalidOperationException(label + " resolves to multiple live CAD objects.");
            return ids[0];
        }

        private static string CurvedFingerprint(string sourceHandle, IReadOnlyList<Point2> centerline, double thicknessM, double heightM, double bottomOffsetM, double sagittaM, IReadOnlyList<string> openings)
        {
            var geometry = string.Join(";", centerline.Select(x => x.X.ToString("R", CultureInfo.InvariantCulture) + "," + x.Y.ToString("R", CultureInfo.InvariantCulture)));
            return "CURVED:" + sourceHandle + ":" + geometry + ":" + thicknessM.ToString("R", CultureInfo.InvariantCulture) + ":" +
                heightM.ToString("R", CultureInfo.InvariantCulture) + ":" + bottomOffsetM.ToString("R", CultureInfo.InvariantCulture) + ":" +
                sagittaM.ToString("R", CultureInfo.InvariantCulture) + "|" + string.Join("|", openings);
        }

        private static double ProjectNumber(ProjectState project, string key, double fallback, double minimum)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static bool SupportedHost(ElementCategory category) => category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier || category == ElementCategory.StructuralWall;
    }
}
