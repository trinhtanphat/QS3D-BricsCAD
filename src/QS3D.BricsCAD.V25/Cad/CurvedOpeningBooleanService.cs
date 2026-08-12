using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
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
            public IReadOnlyList<string> OpeningIds { get; set; } = Array.Empty<string>();
        }

        private sealed class PreparedCut
        {
            public string OpeningId { get; set; } = string.Empty;
            public CadHostedOpeningPlacement HostedPlacement { get; set; } = null!;
            public CurvedOpeningFootprintPlan Footprint { get; set; } = null!;
            public OpeningCutPlan Vertical { get; set; } = null!;
            public string FingerprintPart { get; set; } = string.Empty;
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
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            try
            {
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
                        if (host.IsGeneratedSolidStale())
                            throw new InvalidOperationException("Host " + host.Id + " has stale generated geometry. Rebuild 3D before cutting curved openings.");
                        GeneratedGeometryService.RequireMatchingOwnership(hostSolid, project, host, "cut curved openings in Solid3d " + solidHandle.Trim());

                        var family = project.FindFamily(host.FamilyId);
                        var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "ThicknessM", 0.2d), host.Id + "/ThicknessM");
                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "HeightM", 3.6d), host.Id + "/HeightM");
                        var bottomOffsetM = CadGeometryGuard.Number(host, family, "BottomOffsetM", 0d);
                        var hostPlacement = CadVerticalPlacementResolver.Resolve(
                            document, project, host, hostSource.Elevation, heightM, bottomOffsetM);
                        var sagittaM = ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d);
                        var maximumOffsetM = ProjectNumber(project, "PhysicalOpeningMaximumOffsetM", 0.35d, 1e-6d);
                        var ambiguityM = ProjectNumber(project, "PhysicalOpeningAmbiguityM", 0.01d, 0d);
                        var miterLimit = ProjectNumber(project, "WallMiterLimit", 4d, 1d);
                        var centerline = ReadCenterline(document, hostSource, sagittaM, host.Id);
                        var preparedCuts = new List<PreparedCut>();

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
                            var hostedPlacement = CadVerticalPlacementResolver.ResolveHostedOpening(
                                document,
                                project,
                                host,
                                opening,
                                hostSource.Elevation,
                                heightM,
                                bottomOffsetM,
                                openingHeightM,
                                sillM);

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
                                HostHeightM = hostedPlacement.Host.HeightM,
                                OpeningWidthM = widthM,
                                OpeningHeightM = hostedPlacement.Opening.HeightM,
                                SillHeightM = hostedPlacement.RelativeSillM,
                                CenterAlongHostM = footprint.CenterStationM,
                                ClearanceM = clearanceM
                            });
                            preparedCuts.Add(new PreparedCut
                            {
                                OpeningId = opening.Id,
                                HostedPlacement = hostedPlacement,
                                Footprint = footprint,
                                Vertical = vertical,
                                FingerprintPart = opening.Id + ":" + openingSourceId.Handle + ":" +
                                    openingPoint.X.ToString("R", CultureInfo.InvariantCulture) + "," + openingPoint.Y.ToString("R", CultureInfo.InvariantCulture) + ":" +
                                    widthM.ToString("R", CultureInfo.InvariantCulture) + ":" + openingHeightM.ToString("R", CultureInfo.InvariantCulture) + ":" +
                                    sillM.ToString("R", CultureInfo.InvariantCulture) + ":" + clearanceM.ToString("R", CultureInfo.InvariantCulture) + ":" +
                                    footprint.CenterStationM.ToString("R", CultureInfo.InvariantCulture) +
                                    OpeningPlacementToken(hostedPlacement)
                            });
                        }

                        var fingerprint = CurvedFingerprint(
                            hostSourceId.Handle.ToString(),
                            centerline,
                            thicknessM,
                            heightM,
                            bottomOffsetM,
                            sagittaM,
                            hostPlacement,
                            preparedCuts.Select(x => x.FingerprintPart).ToList());
                        var openingIds = PhysicalOpeningCutTargetState.Normalize(preparedCuts.Select(x => x.OpeningId));
                        var currentSolidHandle = solidId.Handle.ToString();
                        var hasCutSolid = host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle", out var previousSolid) && !string.IsNullOrWhiteSpace(previousSolid);
                        var hasCutFingerprint = host.Properties.TryGetValue("PhysicalOpeningCutFingerprint", out var previousFingerprint) && !string.IsNullOrWhiteSpace(previousFingerprint);
                        if (hasCutSolid != hasCutFingerprint)
                            throw new InvalidOperationException("Host " + host.Id + " có physical opening metadata không đầy đủ. Hãy Build 3D lại host trước khi khoét curved openings.");

                        if (hasCutSolid && string.Equals(previousSolid!.Trim(), currentSolidHandle, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.Equals(previousFingerprint, fingerprint, StringComparison.Ordinal))
                                throw new InvalidOperationException("Host " + host.Id + " đã được khoét trên generated solid hiện tại nhưng geometry/fingerprint đã thay đổi. Build 3D lại host trước khi khoét curved openings.");

                            if (PhysicalOpeningCutTargetState.TryRead(host, out var storedIds))
                            {
                                if (!storedIds.SequenceEqual(openingIds, StringComparer.OrdinalIgnoreCase))
                                    throw new InvalidOperationException("Host " + host.Id + " có physical opening target-state không khớp fingerprint hiện tại. Build 3D lại host trước khi khoét curved openings.");
                                continue;
                            }

                            // A matching legacy curved fingerprint proves that the current linked
                            // opening set is the set baked into this exact solid, so we can backfill
                            // target-state without subtracting cutters a second time.
                            pending.Add(new PendingHostUpdate
                            {
                                Host = host,
                                SolidHandle = currentSolidHandle,
                                Fingerprint = fingerprint,
                                OpeningCount = preparedCuts.Count,
                                OpeningIds = openingIds
                            });
                            continue;
                        }
                        foreach (var prepared in preparedCuts)
                        {
                            using (var cutter = BuildCutter(
                                document,
                                prepared.HostedPlacement,
                                hostSource.Elevation,
                                bottomOffsetM,
                                prepared.Footprint.CutterPolygon,
                                prepared.Vertical.CutterHeightM,
                                prepared.Vertical.BaseElevationM,
                                prepared.OpeningId))
                            {
                                hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
                            }
                            cuts++;
                        }
                        pending.Add(new PendingHostUpdate
                        {
                            Host = host,
                            SolidHandle = currentSolidHandle,
                            Fingerprint = fingerprint,
                            OpeningCount = preparedCuts.Count,
                            OpeningIds = openingIds
                        });
                    }

                    foreach (var update in pending) CommitSemanticUpdate(project, update);
                    transaction.Commit();
                    cadCommitted = true;
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Curved physical opening boolean failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            if (pending.Count > 0) TryRegen(document);
            return cuts;
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingHostUpdate update)
        {
            update.Host.Properties["PhysicalOpeningCutSolidHandle"] = update.SolidHandle;
            update.Host.Properties["PhysicalOpeningCutFingerprint"] = update.Fingerprint;
            update.Host.Properties["PhysicalOpeningCutCount"] = update.OpeningCount.ToString(CultureInfo.InvariantCulture);
            update.Host.Properties["PhysicalOpeningCutMode"] = "CurvedCenterlineFootprint";
            PhysicalOpeningCutTargetState.Write(update.Host, update.OpeningIds);
            AuditTrail.ForProject(project).Record("geometry.opening.boolean.curved", update.Host.Id, update.OpeningCount.ToString(CultureInfo.InvariantCulture) + " opening(s) • solid " + update.SolidHandle);
        }

        private static void TryRegen(Document document)
        {
            try { document.Editor.Regen(); }
            catch { }
        }

        private static Solid3d BuildCutter(
            Document document,
            CadHostedOpeningPlacement placement,
            double hostElevationDrawing,
            double hostBottomOffsetM,
            IReadOnlyList<Point2> polygonM,
            double heightM,
            double baseElevationM,
            string label)
        {
            if (polygonM == null || polygonM.Count < 3) throw new InvalidOperationException("Curved opening cutter footprint is invalid: " + label);
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            double baseZ;
            if (!HasLevelPlacement(placement.Host.Semantic) && !HasLevelPlacement(placement.Opening.Semantic))
            {
                var baseOffsetM = CadGeometryGuard.Add(hostBottomOffsetM, baseElevationM, label + "/cutter base offset");
                baseZ = CadGeometryGuard.Add(hostElevationDrawing,
                    CadGeometryGuard.ToDrawingUnits(document, baseOffsetM, label + "/cutter base"), label + "/cutter world base");
            }
            else
            {
                baseZ = CadGeometryGuard.Add(
                    placement.Host.BottomDrawingUnits,
                    CadGeometryGuard.ToDrawingUnits(document, baseElevationM, label + "/resolved cutter base"),
                    label + "/resolved cutter world base");
            }
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
                        var completed = solid;
                        solid = null!;
                        return completed;
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
                var a = polyline.GetPoint2dAt(i);
                var b = polyline.GetPoint2dAt(i + 1);
                var start = new Point2(units.ToMeters(a.X), units.ToMeters(a.Y));
                var end = new Point2(units.ToMeters(b.X), units.ToMeters(b.Y));
                var bulge = polyline.GetBulgeAt(i);
                var segmentPoints = Math.Abs(bulge) <= 1e-12d
                    ? (IReadOnlyList<Point2>)new[] { start, end }
                    : BulgeArcTessellator.Tessellate(start, end, bulge, sagittaM);
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
            for (var i = 0; i < polyline.NumberOfVertices - 1; i++)
                if (Math.Abs(polyline.GetBulgeAt(i)) > 1e-12d) return true;
            return false;
        }

        private static ObjectId ResolveSingle(Document document, IEnumerable<string> handles, string label)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count == 0) return ObjectId.Null;
            if (ids.Count > 1) throw new InvalidOperationException(label + " resolves to multiple live CAD objects.");
            return ids[0];
        }

        private static string CurvedFingerprint(
            string sourceHandle,
            IReadOnlyList<Point2> centerline,
            double thicknessM,
            double heightM,
            double bottomOffsetM,
            double sagittaM,
            CadVerticalPlacement placement,
            IReadOnlyList<string> openings)
        {
            var geometry = string.Join(";", centerline.Select(x => x.X.ToString("R", CultureInfo.InvariantCulture) + "," + x.Y.ToString("R", CultureInfo.InvariantCulture)));
            var legacy = "CURVED:" + sourceHandle + ":" + geometry + ":" + thicknessM.ToString("R", CultureInfo.InvariantCulture) + ":" +
                heightM.ToString("R", CultureInfo.InvariantCulture) + ":" + bottomOffsetM.ToString("R", CultureInfo.InvariantCulture) + ":" +
                sagittaM.ToString("R", CultureInfo.InvariantCulture) + "|" + string.Join("|", openings);
            return HasLevelPlacement(placement.Semantic)
                ? legacy + "|LEVEL:" + PlacementToken(placement.Semantic)
                : legacy;
        }

        private static string OpeningPlacementToken(CadHostedOpeningPlacement placement) =>
            HasLevelPlacement(placement.Opening.Semantic)
                ? ":LEVEL:" + PlacementToken(placement.Opening.Semantic)
                : string.Empty;

        private static bool HasLevelPlacement(ElementVerticalPlacement placement) =>
            placement.UsesBottomLevel || placement.UsesTopLevel;

        private static string PlacementToken(ElementVerticalPlacement placement) =>
            placement.BottomElevationM.ToString("R", CultureInfo.InvariantCulture) + ":" +
            placement.TopElevationM.ToString("R", CultureInfo.InvariantCulture) + ":" +
            placement.HeightM.ToString("R", CultureInfo.InvariantCulture);

        private static double ProjectNumber(ProjectState project, string key, double fallback, double minimum)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static bool SupportedHost(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;
    }
}
