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
    internal static class OpeningBooleanService
    {
        private sealed class PendingHostUpdate
        {
            public ProjectElement Host { get; set; } = null!;
            public string SolidHandle { get; set; } = string.Empty;
            public string Fingerprint { get; set; } = string.Empty;
            public IReadOnlyList<string> OpeningIds { get; set; } = Array.Empty<string>();
            public int OpeningCount { get; set; }
            public int NewOpeningCount { get; set; }
        }

        private sealed class PreparedCut
        {
            public ProjectElement Opening { get; set; } = null!;
            public OpeningCutPlan Plan { get; set; } = null!;
            public Point3d Target { get; set; }
            public double Angle { get; set; }
            public string FingerprintPart { get; set; } = string.Empty;
        }

        private sealed class PreparedHost
        {
            public string FingerprintPrefix { get; set; } = string.Empty;
            public IList<PreparedCut> Cuts { get; } = new List<PreparedCut>();
        }

        private sealed class OpeningGeometry
        {
            public ProjectElement Opening { get; set; } = null!;
            public CadHostedOpeningPlacement HostedPlacement { get; set; } = null!;
            public ObjectId SourceId { get; set; }
            public double WidthM { get; set; }
            public double HeightM { get; set; }
            public double SillM { get; set; }
            public double ClearanceM { get; set; }
            public double CenterXDrawing { get; set; }
            public double CenterYDrawing { get; set; }
            public double CenterXM { get; set; }
            public double CenterYM { get; set; }
        }

        public static int CutLinkedOpenings(Document document, ProjectState project) =>
            CutLinkedOpenings(document, project, null);

        public static int CutLinkedOpenings(Document document, ProjectState project, IReadOnlyCollection<string>? openingIds)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var requested = NormalizeRequestedOpenings(project, openingIds);
            if (requested != null && requested.Count == 0) return 0;

            var linked = project.Elements
                .Where(x => IsOpening(x) &&
                            x.Properties.TryGetValue("HostWallId", out var hostId) &&
                            !string.IsNullOrWhiteSpace(hostId) &&
                            (requested == null || requested.Contains(x.Id)))
                .GroupBy(x => x.Properties["HostWallId"], StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (linked.Count == 0) return 0;

            var pending = new List<PendingHostUpdate>();
            var totalCuts = 0;
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
                        if (!IsSupportedHost(host.Category)) continue;
                        if (host.IsGeneratedSolidStale())
                            throw new InvalidOperationException("Host " + host.Id + " has stale generated geometry. Rebuild 3D before cutting openings.");
                        if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle)) continue;

                        var solidId = ResolveSingle(document, transaction, new[] { generatedHandle }, typeof(Solid3d), "generated host solid " + host.Id);
                        if (solidId.IsNull) continue;
                        var hostSourceId = ResolveSingle(document, transaction, host.SourceHandles, null, "host source " + host.Id);
                        if (hostSourceId.IsNull) continue;
                        var hostSolid = transaction.GetObject(solidId, OpenMode.ForWrite, false) as Solid3d;
                        var hostSource = transaction.GetObject(hostSourceId, OpenMode.ForRead, false) as Entity;
                        if (hostSolid == null || hostSource == null || hostSolid.IsErased || hostSource.IsErased) continue;

                        var currentSolidHandle = solidId.Handle.ToString();
                        GeneratedGeometryService.RequireMatchingOwnership(hostSolid, project, host, "boolean-cut generated host solid " + currentSolidHandle);
                        var family = project.FindFamily(host.FamilyId);
                        var hostThicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "ThicknessM", 0.2d), host.Id + "/ThicknessM");
                        var hostHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "HeightM", 3.6d), host.Id + "/HeightM");
                        var hostBottomOffsetM = CadGeometryGuard.Number(host, family, "BottomOffsetM", 0d);
                        var requestedElements = group.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
                        var requestedOpenings = requestedElements
                            .Select(x => ReadOpening(document, transaction, project, x))
                            .ToList();
                        var requestedPrepared = PrepareHost(document, project, host, hostSource, hostThicknessM, hostHeightM, hostBottomOffsetM, requestedOpenings);
                        var requestedFingerprint = Fingerprint(requestedPrepared);

                        var hasCutSolid = host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle", out var cutSolidHandle) && !string.IsNullOrWhiteSpace(cutSolidHandle);
                        var hasCutFingerprint = host.Properties.TryGetValue("PhysicalOpeningCutFingerprint", out var cutFingerprint) && !string.IsNullOrWhiteSpace(cutFingerprint);
                        if (hasCutSolid != hasCutFingerprint)
                            throw new InvalidOperationException("Host " + host.Id + " có physical opening metadata không đầy đủ. Hãy Build 3D lại host trước khi khoét tiếp.");

                        var cutsToApply = requestedPrepared.Cuts.ToList();
                        var finalElements = requestedElements;
                        var finalPrepared = requestedPrepared;
                        var finalFingerprint = requestedFingerprint;
                        var sameCutSolid = hasCutSolid && string.Equals(cutSolidHandle!.Trim(), currentSolidHandle, StringComparison.OrdinalIgnoreCase);

                        if (sameCutSolid)
                        {
                            if (PhysicalOpeningCutTargetState.TryRead(host, out var previousIds))
                            {
                                var previousElements = PhysicalOpeningCutTargetState.Resolve(project, host, previousIds)
                                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                                    .ToList();
                                var previousOpenings = previousElements
                                    .Select(x => ReadOpening(document, transaction, project, x))
                                    .ToList();
                                var previousPrepared = PrepareHost(document, project, host, hostSource, hostThicknessM, hostHeightM, hostBottomOffsetM, previousOpenings);
                                var previousFingerprint = Fingerprint(previousPrepared);
                                if (!string.Equals(cutFingerprint, previousFingerprint, StringComparison.Ordinal))
                                    throw new InvalidOperationException("Host " + host.Id + " physical opening state đã stale so với geometry/thông số hiện tại. Hãy Build 3D lại host trước khi khoét tiếp.");

                                var previousSet = new HashSet<string>(previousIds, StringComparer.OrdinalIgnoreCase);
                                var finalById = previousElements
                                    .Concat(requestedElements)
                                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                                    .Select(x => x.First())
                                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                                    .ToList();
                                var finalOpenings = finalById
                                    .Select(x => ReadOpening(document, transaction, project, x))
                                    .ToList();
                                finalElements = finalById;
                                finalPrepared = PrepareHost(document, project, host, hostSource, hostThicknessM, hostHeightM, hostBottomOffsetM, finalOpenings);
                                finalFingerprint = Fingerprint(finalPrepared);
                                cutsToApply = finalPrepared.Cuts
                                    .Where(x => !previousSet.Contains(x.Opening.Id))
                                    .ToList();
                                if (cutsToApply.Count == 0)
                                {
                                    if (!string.Equals(cutFingerprint, finalFingerprint, StringComparison.Ordinal))
                                        throw new InvalidOperationException("Host " + host.Id + " physical opening state không nhất quán. Hãy Build 3D lại host trước khi khoét tiếp.");
                                    continue;
                                }
                            }
                            else
                            {
                                if (!string.Equals(cutFingerprint, requestedFingerprint, StringComparison.Ordinal))
                                    throw new InvalidOperationException("Host " + host.Id + " có legacy physical opening state không xác định được tập opening đã khoét. Hãy Build 3D lại host trước khi khoét thêm.");
                                cutsToApply.Clear();
                            }
                        }

                        foreach (var item in cutsToApply)
                        {
                            var cutterWidth = CadGeometryGuard.ToDrawingUnits(document, item.Plan.CutterWidthM, item.Opening.Id + "/cutter width");
                            var cutterDepth = CadGeometryGuard.ToDrawingUnits(document, item.Plan.CutterDepthM, item.Opening.Id + "/cutter depth");
                            var cutterHeight = CadGeometryGuard.ToDrawingUnits(document, item.Plan.CutterHeightM, item.Opening.Id + "/cutter height");
                            using (var cutter = new Solid3d())
                            {
                                cutter.SetDatabaseDefaults(document.Database);
                                cutter.CreateBox(cutterWidth, cutterDepth, cutterHeight);
                                cutter.TransformBy(Matrix3d.Displacement(new Vector3d(-cutterWidth / 2d, -cutterDepth / 2d, -cutterHeight / 2d)));
                                cutter.TransformBy(Matrix3d.Rotation(item.Angle, Vector3d.ZAxis, Point3d.Origin));
                                cutter.TransformBy(Matrix3d.Displacement(new Vector3d(item.Target.X, item.Target.Y, item.Target.Z)));
                                hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
                            }
                            totalCuts++;
                        }

                        pending.Add(new PendingHostUpdate
                        {
                            Host = host,
                            SolidHandle = currentSolidHandle,
                            Fingerprint = finalFingerprint,
                            OpeningIds = finalElements.Select(x => x.Id).ToList().AsReadOnly(),
                            OpeningCount = finalPrepared.Cuts.Count,
                            NewOpeningCount = cutsToApply.Count
                        });
                    }

                    foreach (var update in pending) CommitSemanticUpdate(project, update);
                    if (pending.Count > 0) project.Touch();
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
                            "Physical opening boolean failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            if (pending.Count > 0) TryRegen(document);
            return totalCuts;
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingHostUpdate update)
        {
            update.Host.Properties["PhysicalOpeningCutSolidHandle"] = update.SolidHandle;
            update.Host.Properties["PhysicalOpeningCutFingerprint"] = update.Fingerprint;
            update.Host.Properties["PhysicalOpeningCutCount"] = update.OpeningCount.ToString(CultureInfo.InvariantCulture);
            PhysicalOpeningCutTargetState.Write(update.Host, update.OpeningIds);
            AuditTrail.ForProject(project).Record(
                "geometry.opening.boolean",
                update.Host.Id,
                update.NewOpeningCount.ToString(CultureInfo.InvariantCulture) + " new / " +
                update.OpeningCount.ToString(CultureInfo.InvariantCulture) + " total opening(s) • solid " + update.SolidHandle);
        }

        private static void TryRegen(Document document)
        {
            try { document.Editor.Regen(); }
            catch { }
        }

        private static HashSet<string>? NormalizeRequestedOpenings(ProjectState project, IReadOnlyCollection<string>? openingIds)
        {
            if (openingIds == null) return null;
            var requested = new HashSet<string>(
                openingIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            foreach (var id in requested)
            {
                var opening = project.FindElement(id) ?? throw new InvalidOperationException("Target opening not found: " + id);
                if (!IsOpening(opening)) throw new InvalidOperationException("Target element is not Door/WallOpening: " + id);
                if (!opening.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId))
                    throw new InvalidOperationException("Target opening is not linked to a host: " + id);
            }
            return requested;
        }

        private static bool IsOpening(ProjectElement element) =>
            element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door;

        private static PreparedHost PrepareHost(
            Document document,
            ProjectState project,
            ProjectElement host,
            Entity hostSource,
            double hostThicknessM,
            double hostHeightM,
            double hostBottomOffsetM,
            IReadOnlyList<OpeningGeometry> openings)
        {
            if (hostSource is Line hostLine)
                return PrepareLineHost(document, project, host, hostLine, hostThicknessM, hostHeightM, hostBottomOffsetM, openings);
            if (hostSource is Polyline hostPolyline && IsPolylineHost(host.Category))
                return PreparePolylineHost(document, project, host, hostPolyline, hostThicknessM, hostHeightM, hostBottomOffsetM, openings);
            throw new InvalidOperationException("Host source type chưa hỗ trợ physical opening cut: " + host.Id + " / " + hostSource.GetType().Name);
        }

        private static string Fingerprint(PreparedHost prepared) =>
            prepared.FingerprintPrefix + "|" + string.Join("|", prepared.Cuts.Select(x => x.FingerprintPart));

        private static PreparedHost PrepareLineHost(
            Document document,
            ProjectState project,
            ProjectElement host,
            Line hostLine,
            double hostThicknessM,
            double hostHeightM,
            double hostBottomOffsetM,
            IReadOnlyList<OpeningGeometry> openings)
        {
            var hostPlacement = CadVerticalPlacementResolver.Resolve(
                document, project, host, hostLine.StartPoint.Z, hostHeightM, hostBottomOffsetM);
            var dx = CadGeometryGuard.Finite(hostLine.EndPoint.X - hostLine.StartPoint.X, host.Id + "/dx");
            var dy = CadGeometryGuard.Finite(hostLine.EndPoint.Y - hostLine.StartPoint.Y, host.Id + "/dy");
            var hostLengthDrawing = CadGeometryGuard.Hypot(dx, dy, host.Id + "/source length");
            if (hostLengthDrawing <= 1e-6d) throw new InvalidOperationException("Host source LINE quá ngắn: " + host.Id);
            var hostLengthM = CadGeometryGuard.ToMeters(document, hostLengthDrawing, host.Id + "/source length");
            var ux = dx / hostLengthDrawing;
            var uy = dy / hostLengthDrawing;
            var angle = CadGeometryGuard.Finite(Math.Atan2(dy, dx), host.Id + "/angle");
            var maximumOffsetDrawing = CadGeometryGuard.ToDrawingUnits(document, hostThicknessM / 2d + 0.25d, host.Id + "/host proximity tolerance");
            var prepared = new PreparedHost
            {
                FingerprintPrefix = HostFingerprint(hostLine, hostThicknessM, hostHeightM, hostBottomOffsetM, hostPlacement)
            };

            foreach (var opening in openings)
            {
                opening.HostedPlacement = CadVerticalPlacementResolver.ResolveHostedOpening(
                    document,
                    project,
                    host,
                    opening.Opening,
                    hostLine.StartPoint.Z,
                    hostHeightM,
                    hostBottomOffsetM,
                    opening.HeightM,
                    opening.SillM);
                var fromStartX = CadGeometryGuard.Finite(opening.CenterXDrawing - hostLine.StartPoint.X, opening.Opening.Id + "/from start X");
                var fromStartY = CadGeometryGuard.Finite(opening.CenterYDrawing - hostLine.StartPoint.Y, opening.Opening.Id + "/from start Y");
                var alongDrawing = CadGeometryGuard.Add(fromStartX * ux, fromStartY * uy, opening.Opening.Id + "/projection along host");
                var perpendicularDrawing = Math.Abs(CadGeometryGuard.Add(fromStartX * -uy, fromStartY * ux, opening.Opening.Id + "/perpendicular distance"));
                if (perpendicularDrawing > maximumOffsetDrawing)
                    throw new InvalidOperationException("Opening " + opening.Opening.Id + " nằm quá xa host centerline để khoét an toàn.");

                var centerAlongHostM = CadGeometryGuard.ToMeters(document, alongDrawing, opening.Opening.Id + "/center along host");
                var plan = OpeningCutPlanner.Plan(new OpeningCutInput
                {
                    HostLengthM = hostLengthM,
                    HostThicknessM = hostThicknessM,
                    HostHeightM = opening.HostedPlacement.Host.HeightM,
                    OpeningWidthM = opening.WidthM,
                    OpeningHeightM = opening.HostedPlacement.Opening.HeightM,
                    SillHeightM = opening.HostedPlacement.RelativeSillM,
                    CenterAlongHostM = centerAlongHostM,
                    ClearanceM = opening.ClearanceM
                });

                var alongCenterDrawing = CadGeometryGuard.ToDrawingUnits(document, plan.CenterAlongHostM, opening.Opening.Id + "/center along host");
                var centerZ = CutterCenterZ(
                    document,
                    opening.HostedPlacement,
                    hostLine.StartPoint.Z,
                    hostBottomOffsetM,
                    plan.CenterElevationM,
                    opening.Opening.Id);
                var targetX = CadGeometryGuard.Add(hostLine.StartPoint.X, ux * alongCenterDrawing, opening.Opening.Id + "/target X");
                var targetY = CadGeometryGuard.Add(hostLine.StartPoint.Y, uy * alongCenterDrawing, opening.Opening.Id + "/target Y");
                prepared.Cuts.Add(new PreparedCut
                {
                    Opening = opening.Opening,
                    Plan = plan,
                    Target = new Point3d(targetX, targetY, centerZ),
                    Angle = angle,
                    FingerprintPart = OpeningFingerprint(opening, plan.CenterAlongHostM, "LINE")
                });
            }
            return prepared;
        }

        private static PreparedHost PreparePolylineHost(
            Document document,
            ProjectState project,
            ProjectElement host,
            Polyline hostPolyline,
            double hostThicknessM,
            double hostHeightM,
            double hostBottomOffsetM,
            IReadOnlyList<OpeningGeometry> openings)
        {
            if (hostPolyline.Closed) throw new InvalidOperationException("Physical opening cut yêu cầu wall centerline POLYLINE open.");
            if (hostPolyline.NumberOfVertices < 2) throw new InvalidOperationException("Wall centerline POLYLINE có ít hơn 2 vertex.");
            var normal = hostPolyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d)
                throw new NotSupportedException("Physical opening cut hiện chỉ hỗ trợ wall POLYLINE plan-view có normal +Z.");

            var centerline = new List<Point2>(hostPolyline.NumberOfVertices);
            for (var index = 0; index < hostPolyline.NumberOfVertices; index++)
            {
                if (index < hostPolyline.NumberOfVertices - 1 && Math.Abs(CadGeometryGuard.Finite(hostPolyline.GetBulgeAt(index), host.Id + "/polyline bulge")) > 1e-12d)
                    throw new NotSupportedException("Physical opening cut trên curved/bulged wall POLYLINE chưa được hỗ trợ an toàn; rebuild/tách host thành đoạn thẳng trước khi khoét.");
                var point = hostPolyline.GetPoint2dAt(index);
                centerline.Add(new Point2(
                    CadGeometryGuard.ToMeters(document, point.X, host.Id + "/polyline X"),
                    CadGeometryGuard.ToMeters(document, point.Y, host.Id + "/polyline Y")));
            }

            var hostPlacement = CadVerticalPlacementResolver.Resolve(
                document, project, host, hostPolyline.Elevation, hostHeightM, hostBottomOffsetM);

            var maximumOffsetM = CadGeometryGuard.Add(hostThicknessM / 2d, 0.25d, host.Id + "/host proximity tolerance");
            var prepared = new PreparedHost
            {
                FingerprintPrefix = HostFingerprint(hostPolyline, centerline, hostThicknessM, hostHeightM, hostBottomOffsetM, hostPlacement)
            };
            foreach (var opening in openings)
            {
                opening.HostedPlacement = CadVerticalPlacementResolver.ResolveHostedOpening(
                    document,
                    project,
                    host,
                    opening.Opening,
                    hostPolyline.Elevation,
                    hostHeightM,
                    hostBottomOffsetM,
                    opening.HeightM,
                    opening.SillM);
                var polylinePlan = PolylineOpeningCutPlanner.Plan(new PolylineOpeningCutInput
                {
                    Centerline = centerline,
                    OpeningCenter = new Point2(opening.CenterXM, opening.CenterYM),
                    HostThicknessM = hostThicknessM,
                    HostHeightM = opening.HostedPlacement.Host.HeightM,
                    OpeningWidthM = opening.WidthM,
                    OpeningHeightM = opening.HostedPlacement.Opening.HeightM,
                    SillHeightM = opening.HostedPlacement.RelativeSillM,
                    ClearanceM = opening.ClearanceM,
                    MaximumCenterlineOffsetM = maximumOffsetM
                });
                var plan = polylinePlan.Cut;
                var targetX = CadGeometryGuard.ToDrawingUnits(document, polylinePlan.ProjectedCenter.X, opening.Opening.Id + "/polyline target X");
                var targetY = CadGeometryGuard.ToDrawingUnits(document, polylinePlan.ProjectedCenter.Y, opening.Opening.Id + "/polyline target Y");
                var centerZ = CutterCenterZ(
                    document,
                    opening.HostedPlacement,
                    hostPolyline.Elevation,
                    hostBottomOffsetM,
                    plan.CenterElevationM,
                    opening.Opening.Id);
                var angle = CadGeometryGuard.Finite(Math.Atan2(polylinePlan.Tangent.Y, polylinePlan.Tangent.X), opening.Opening.Id + "/polyline tangent angle");
                prepared.Cuts.Add(new PreparedCut
                {
                    Opening = opening.Opening,
                    Plan = plan,
                    Target = new Point3d(targetX, targetY, centerZ),
                    Angle = angle,
                    FingerprintPart = OpeningFingerprint(opening, polylinePlan.StationM, "POLY:" + polylinePlan.SegmentIndex.ToString(CultureInfo.InvariantCulture))
                });
            }
            return prepared;
        }

        private static OpeningGeometry ReadOpening(Document document, Transaction transaction, ProjectState project, ProjectElement opening)
        {
            var openingFamily = project.FindFamily(opening.FamilyId);
            var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "WidthM", 0.9d), opening.Id + "/WidthM");
            var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "HeightM", 2.2d), opening.Id + "/HeightM");
            var sillM = CadGeometryGuard.Number(opening, openingFamily, "SillHeightM", CadGeometryGuard.Number(opening, openingFamily, "BottomOffsetM", 0d));
            if (sillM < 0d) throw new InvalidOperationException(opening.Id + "/SillHeightM phải >= 0.");
            var clearanceM = CadGeometryGuard.Number(opening, openingFamily, "BooleanClearanceM", 0.01d);
            if (clearanceM < 0d) throw new InvalidOperationException(opening.Id + "/BooleanClearanceM phải >= 0.");

            var openingId = ResolveSingle(document, transaction, opening.SourceHandles, null, "opening source " + opening.Id);
            if (openingId.IsNull) throw new InvalidOperationException("Opening " + opening.Id + " chưa có live CAD source để xác định vị trí khoét.");
            var sourceEntity = transaction.GetObject(openingId, OpenMode.ForRead, false) as Entity;
            if (sourceEntity == null || sourceEntity.IsErased) throw new InvalidOperationException("Opening source không còn live: " + opening.Id);
            var extents = sourceEntity.GeometricExtents;
            var centerX = CadGeometryGuard.Midpoint(extents.MinPoint.X, extents.MaxPoint.X, opening.Id + "/center X");
            var centerY = CadGeometryGuard.Midpoint(extents.MinPoint.Y, extents.MaxPoint.Y, opening.Id + "/center Y");
            return new OpeningGeometry
            {
                Opening = opening,
                SourceId = openingId,
                WidthM = widthM,
                HeightM = heightM,
                SillM = sillM,
                ClearanceM = clearanceM,
                CenterXDrawing = centerX,
                CenterYDrawing = centerY,
                CenterXM = CadGeometryGuard.ToMeters(document, centerX, opening.Id + "/source center X"),
                CenterYM = CadGeometryGuard.ToMeters(document, centerY, opening.Id + "/source center Y")
            };
        }

        private static string OpeningFingerprint(OpeningGeometry opening, double stationM, string mode)
        {
            var legacy = opening.Opening.Id + ":" + opening.SourceId.Handle.ToString() + ":" + mode + ":" +
                NumberToken(opening.CenterXM) + ":" + NumberToken(opening.CenterYM) + ":" + NumberToken(stationM) + ":" +
                NumberToken(opening.WidthM) + ":" + NumberToken(opening.HeightM) + ":" + NumberToken(opening.SillM) + ":" + NumberToken(opening.ClearanceM);
            return HasLevelPlacement(opening.HostedPlacement.Opening.Semantic)
                ? legacy + ":LEVEL:" + PlacementToken(opening.HostedPlacement.Opening.Semantic)
                : legacy;
        }

        private static string HostFingerprint(Line hostLine, double hostThicknessM, double hostHeightM, double hostBottomOffsetM, CadVerticalPlacement placement)
        {
            var legacy = "HOST-LINE:" + hostLine.Handle.ToString() + ":" + PointToken(hostLine.StartPoint) + ":" + PointToken(hostLine.EndPoint) + ":" +
                NumberToken(hostThicknessM) + ":" + NumberToken(hostHeightM) + ":" + NumberToken(hostBottomOffsetM);
            return HasLevelPlacement(placement.Semantic) ? legacy + ":LEVEL:" + PlacementToken(placement.Semantic) : legacy;
        }

        private static string HostFingerprint(Polyline hostPolyline, IReadOnlyList<Point2> centerline, double hostThicknessM, double hostHeightM, double hostBottomOffsetM, CadVerticalPlacement placement)
        {
            var legacy = "HOST-POLY:" + hostPolyline.Handle.ToString() + ":" + NumberToken(hostPolyline.Elevation) + ":" +
                string.Join(";", centerline.Select(x => NumberToken(x.X) + "," + NumberToken(x.Y))) + ":" +
                NumberToken(hostThicknessM) + ":" + NumberToken(hostHeightM) + ":" + NumberToken(hostBottomOffsetM);
            return HasLevelPlacement(placement.Semantic) ? legacy + ":LEVEL:" + PlacementToken(placement.Semantic) : legacy;
        }

        private static double CutterCenterZ(
            Document document,
            CadHostedOpeningPlacement placement,
            double legacySourceBaseDrawing,
            double legacyBottomOffsetM,
            double centerElevationM,
            string label)
        {
            if (!HasLevelPlacement(placement.Host.Semantic) && !HasLevelPlacement(placement.Opening.Semantic))
            {
                var relativeCenterM = CadGeometryGuard.Add(legacyBottomOffsetM, centerElevationM, label + "/relative cutter center Z");
                return CadGeometryGuard.Add(
                    legacySourceBaseDrawing,
                    CadGeometryGuard.ToDrawingUnits(document, relativeCenterM, label + "/cutter center Z"),
                    label + "/world cutter center Z");
            }
            return CadGeometryGuard.Add(
                placement.Host.BottomDrawingUnits,
                CadGeometryGuard.ToDrawingUnits(document, centerElevationM, label + "/resolved cutter center Z"),
                label + "/resolved world cutter center Z");
        }

        private static bool HasLevelPlacement(ElementVerticalPlacement placement) =>
            placement.UsesBottomLevel || placement.UsesTopLevel;

        private static string PlacementToken(ElementVerticalPlacement placement) =>
            NumberToken(placement.BottomElevationM) + ":" + NumberToken(placement.TopElevationM) + ":" + NumberToken(placement.HeightM);

        private static string PointToken(Point3d point) => NumberToken(point.X) + "," + NumberToken(point.Y) + "," + NumberToken(point.Z);

        private static string NumberToken(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Opening boolean fingerprint contains a non-finite value.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static ObjectId ResolveSingle(Document document, Transaction transaction, IEnumerable<string> handles, Type? expectedType, string label)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count == 0) return ObjectId.Null;
            if (ids.Count > 1) throw new InvalidOperationException(label + " resolves to multiple live CAD objects.");
            var entity = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
            if (entity == null || entity.IsErased) return ObjectId.Null;
            if (expectedType != null && !expectedType.IsInstanceOfType(entity)) return ObjectId.Null;
            return ids[0];
        }

        private static bool IsSupportedHost(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;

        private static bool IsPolylineHost(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier;
    }
}
