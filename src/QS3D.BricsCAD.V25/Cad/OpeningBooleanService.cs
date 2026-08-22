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
                        if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle)) continue;

                        var solidId = ResolveSingle(document, transaction, new[] { generatedHandle }, typeof(Solid3d), "generated host solid " + host.Id);
                        if (solidId.IsNull) continue;
                        var hostSourceId = ResolveSingle(document, transaction, host.SourceHandles, null, "host source " + host.Id);
                        if (hostSourceId.IsNull) continue;
                        var hostSolid = transaction.GetObject(solidId, OpenMode.ForWrite, false) as Solid3d;
                        var hostSource = transaction.GetObject(hostSourceId, OpenMode.ForRead, false) as Entity;
                        if (hostSolid == null || hostSource == null || hostSolid.IsErased || hostSource.IsErased) continue;
                        if (host.IsGeneratedSolidStale())
                            throw new InvalidOperationException(
                                "Host " + host.Id + " có generated geometry stale. Hãy Build 3D lại trước khi khoét opening.");

                        var currentSolidHandle = solidId.Handle.ToString();
                        GeneratedGeometryService.RequireMatchingOwnership(hostSolid, project, host, "boolean-cut generated host solid " + currentSolidHandle);
                        var family = project.FindFamily(host.FamilyId);
                        var hostThicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "ThicknessM", 0.2d), host.Id + "/ThicknessM");
                        var hostPlacement = CadElementVerticalPlacement.Resolve(
                            document,
                            project,
                            host,
                            family,
                            SourceBaseDrawing(hostSource, host.Id),
                            "HeightM",
                            3.6d);
                        var requestedElements = group.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
                        var requestedOpenings = requestedElements
                            .Select(x => ReadOpening(document, transaction, project, x, hostPlacement))
                            .ToList();
                        var requestedPrepared = PrepareHost(document, host, hostSource, hostThicknessM, hostPlacement, requestedOpenings);
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
                                    .Select(x => ReadOpening(document, transaction, project, x, hostPlacement))
                                    .ToList();
                                var previousPrepared = PrepareHost(document, host, hostSource, hostThicknessM, hostPlacement, previousOpenings);
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
                                    .Select(x => ReadOpening(document, transaction, project, x, hostPlacement))
                                    .ToList();
                                finalElements = finalById;
                                finalPrepared = PrepareHost(document, host, hostSource, hostThicknessM, hostPlacement, finalOpenings);
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
            var requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in openingIds)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Target opening id cannot be empty.");
                requested.Add(raw.Trim());
            }
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
            ProjectElement host,
            Entity hostSource,
            double hostThicknessM,
            CadElementVerticalPlacement hostPlacement,
            IReadOnlyList<OpeningGeometry> openings)
        {
            if (hostSource is Line hostLine)
                return PrepareLineHost(document, host, hostLine, hostThicknessM, hostPlacement, openings);
            if (hostSource is Polyline hostPolyline && IsPolylineHost(host.Category))
                return PreparePolylineHost(document, host, hostPolyline, hostThicknessM, hostPlacement, openings);
            throw new InvalidOperationException("Host source type chưa hỗ trợ physical opening cut: " + host.Id + " / " + hostSource.GetType().Name);
        }

        private static string Fingerprint(PreparedHost prepared) =>
            prepared.FingerprintPrefix + "|" + string.Join("|", prepared.Cuts.Select(x => x.FingerprintPart));

        private static PreparedHost PrepareLineHost(
            Document document,
            ProjectElement host,
            Line hostLine,
            double hostThicknessM,
            CadElementVerticalPlacement hostPlacement,
            IReadOnlyList<OpeningGeometry> openings)
        {
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
                FingerprintPrefix = HostFingerprint(
                    hostLine,
                    hostThicknessM,
                    hostPlacement.HeightM,
                    hostPlacement.FingerprintBottomM)
            };

            foreach (var opening in openings)
            {
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
                    HostHeightM = hostPlacement.HeightM,
                    OpeningWidthM = opening.WidthM,
                    OpeningHeightM = opening.HeightM,
                    SillHeightM = opening.SillM,
                    CenterAlongHostM = centerAlongHostM,
                    ClearanceM = opening.ClearanceM
                });

                var alongCenterDrawing = CadGeometryGuard.ToDrawingUnits(document, plan.CenterAlongHostM, opening.Opening.Id + "/center along host");
                var centerZ = CadGeometryGuard.Add(
                    hostPlacement.BottomDrawing,
                    CadGeometryGuard.ToDrawingUnits(document, plan.CenterElevationM, opening.Opening.Id + "/cutter center Z"),
                    opening.Opening.Id + "/world cutter center Z");
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
            ProjectElement host,
            Polyline hostPolyline,
            double hostThicknessM,
            CadElementVerticalPlacement hostPlacement,
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

            var maximumOffsetM = CadGeometryGuard.Add(hostThicknessM / 2d, 0.25d, host.Id + "/host proximity tolerance");
            var prepared = new PreparedHost
            {
                FingerprintPrefix = HostFingerprint(
                    hostPolyline,
                    centerline,
                    hostThicknessM,
                    hostPlacement.HeightM,
                    hostPlacement.FingerprintBottomM)
            };
            foreach (var opening in openings)
            {
                var polylinePlan = PolylineOpeningCutPlanner.Plan(new PolylineOpeningCutInput
                {
                    Centerline = centerline,
                    OpeningCenter = new Point2(opening.CenterXM, opening.CenterYM),
                    HostThicknessM = hostThicknessM,
                    HostHeightM = hostPlacement.HeightM,
                    OpeningWidthM = opening.WidthM,
                    OpeningHeightM = opening.HeightM,
                    SillHeightM = opening.SillM,
                    ClearanceM = opening.ClearanceM,
                    MaximumCenterlineOffsetM = maximumOffsetM
                });
                var plan = polylinePlan.Cut;
                var targetX = CadGeometryGuard.ToDrawingUnits(document, polylinePlan.ProjectedCenter.X, opening.Opening.Id + "/polyline target X");
                var targetY = CadGeometryGuard.ToDrawingUnits(document, polylinePlan.ProjectedCenter.Y, opening.Opening.Id + "/polyline target Y");
                var centerZ = CadGeometryGuard.Add(
                    hostPlacement.BottomDrawing,
                    CadGeometryGuard.ToDrawingUnits(document, plan.CenterElevationM, opening.Opening.Id + "/cutter center Z"),
                    opening.Opening.Id + "/world cutter center Z");
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

        private static OpeningGeometry ReadOpening(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement opening,
            CadElementVerticalPlacement hostPlacement)
        {
            var openingFamily = project.FindFamily(opening.FamilyId);
            var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "WidthM", 0.9d), opening.Id + "/WidthM");
            var clearanceM = CadGeometryGuard.Number(opening, openingFamily, "BooleanClearanceM", 0.01d);
            if (clearanceM < 0d) throw new InvalidOperationException(opening.Id + "/BooleanClearanceM phải >= 0.");

            var openingId = ResolveSingle(document, transaction, opening.SourceHandles, null, "opening source " + opening.Id);
            if (openingId.IsNull) throw new InvalidOperationException("Opening " + opening.Id + " chưa có live CAD source để xác định vị trí khoét.");
            var sourceEntity = transaction.GetObject(openingId, OpenMode.ForRead, false) as Entity;
            if (sourceEntity == null || sourceEntity.IsErased) throw new InvalidOperationException("Opening source không còn live: " + opening.Id);
            var extents = sourceEntity.GeometricExtents;
            var verticalPlacement = CadHostedOpeningVerticalPlacement.Resolve(
                document,
                project,
                opening,
                openingFamily,
                extents.MinPoint.Z,
                hostPlacement,
                2.2d,
                0d);
            var centerX = CadGeometryGuard.Midpoint(extents.MinPoint.X, extents.MaxPoint.X, opening.Id + "/center X");
            var centerY = CadGeometryGuard.Midpoint(extents.MinPoint.Y, extents.MaxPoint.Y, opening.Id + "/center Y");
            return new OpeningGeometry
            {
                Opening = opening,
                SourceId = openingId,
                WidthM = widthM,
                HeightM = verticalPlacement.HeightM,
                SillM = verticalPlacement.SillHeightM,
                ClearanceM = clearanceM,
                CenterXDrawing = centerX,
                CenterYDrawing = centerY,
                CenterXM = CadGeometryGuard.ToMeters(document, centerX, opening.Id + "/source center X"),
                CenterYM = CadGeometryGuard.ToMeters(document, centerY, opening.Id + "/source center Y")
            };
        }

        private static double SourceBaseDrawing(Entity source, string label)
        {
            if (source is Line line) return CadGeometryGuard.Finite(line.StartPoint.Z, label + "/source base Z");
            if (source is Polyline polyline) return CadGeometryGuard.Finite(polyline.Elevation, label + "/source base Z");
            throw new InvalidOperationException("Host source type does not expose a Level base elevation: " + label + " / " + source.GetType().Name);
        }

        private static string OpeningFingerprint(OpeningGeometry opening, double stationM, string mode) =>
            opening.Opening.Id + ":" + opening.SourceId.Handle.ToString() + ":" + mode + ":" +
            NumberToken(opening.CenterXM) + ":" + NumberToken(opening.CenterYM) + ":" + NumberToken(stationM) + ":" +
            NumberToken(opening.WidthM) + ":" + NumberToken(opening.HeightM) + ":" + NumberToken(opening.SillM) + ":" + NumberToken(opening.ClearanceM);

        private static string HostFingerprint(Line hostLine, double hostThicknessM, double hostHeightM, double hostBottomOffsetM) =>
            "HOST-LINE:" + hostLine.Handle.ToString() + ":" + PointToken(hostLine.StartPoint) + ":" + PointToken(hostLine.EndPoint) + ":" +
            NumberToken(hostThicknessM) + ":" + NumberToken(hostHeightM) + ":" + NumberToken(hostBottomOffsetM);

        private static string HostFingerprint(Polyline hostPolyline, IReadOnlyList<Point2> centerline, double hostThicknessM, double hostHeightM, double hostBottomOffsetM) =>
            "HOST-POLY:" + hostPolyline.Handle.ToString() + ":" + NumberToken(hostPolyline.Elevation) + ":" +
            string.Join(";", centerline.Select(x => NumberToken(x.X) + "," + NumberToken(x.Y))) + ":" +
            NumberToken(hostThicknessM) + ":" + NumberToken(hostHeightM) + ":" + NumberToken(hostBottomOffsetM);

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
