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
    internal static class OpeningBooleanService
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
            var totalCuts = 0;
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
                    var sourceLineId = ResolveSingle(document, transaction, host.SourceHandles, typeof(Line), "host source LINE " + host.Id);
                    if (sourceLineId.IsNull) continue;
                    var hostSolid = transaction.GetObject(solidId, OpenMode.ForWrite, false) as Solid3d;
                    var hostLine = transaction.GetObject(sourceLineId, OpenMode.ForRead, false) as Line;
                    if (hostSolid == null || hostLine == null || hostSolid.IsErased || hostLine.IsErased) continue;

                    var openings = group.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
                    var fingerprint = BuildFingerprint(openings);
                    var currentSolidHandle = solidId.Handle.ToString();
                    if (host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle", out var cutSolidHandle) &&
                        host.Properties.TryGetValue("PhysicalOpeningCutFingerprint", out var cutFingerprint) &&
                        string.Equals(cutSolidHandle, currentSolidHandle, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(cutFingerprint, fingerprint, StringComparison.Ordinal)) continue;
                        throw new InvalidOperationException("Host " + host.Id + " đã được khoét với opening-set khác trên cùng generated solid. Hãy Build 3D lại host trước khi khoét lại.");
                    }

                    var family = project.FindFamily(host.FamilyId);
                    var hostThicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "ThicknessM", 0.2d), host.Id + "/ThicknessM");
                    var hostHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(host, family, "HeightM", 3.6d), host.Id + "/HeightM");
                    var hostBottomOffsetM = CadGeometryGuard.Number(host, family, "BottomOffsetM", 0d);
                    var dx = CadGeometryGuard.Finite(hostLine.EndPoint.X - hostLine.StartPoint.X, host.Id + "/dx");
                    var dy = CadGeometryGuard.Finite(hostLine.EndPoint.Y - hostLine.StartPoint.Y, host.Id + "/dy");
                    var hostLengthDrawing = CadGeometryGuard.Hypot(dx, dy, host.Id + "/source length");
                    if (hostLengthDrawing <= 1e-6d) throw new InvalidOperationException("Host source LINE quá ngắn: " + host.Id);
                    var hostLengthM = CadGeometryGuard.ToMeters(document, hostLengthDrawing, host.Id + "/source length");
                    var ux = dx / hostLengthDrawing;
                    var uy = dy / hostLengthDrawing;
                    var angle = Math.Atan2(dy, dx);

                    foreach (var opening in openings)
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
                        var fromStartX = centerX - hostLine.StartPoint.X;
                        var fromStartY = centerY - hostLine.StartPoint.Y;
                        var alongDrawing = CadGeometryGuard.Add(fromStartX * ux, fromStartY * uy, opening.Id + "/projection along host");
                        var perpendicularDrawing = Math.Abs(fromStartX * -uy + fromStartY * ux);
                        var maximumOffsetDrawing = CadGeometryGuard.ToDrawingUnits(document, hostThicknessM / 2d + 0.25d, opening.Id + "/host proximity tolerance");
                        if (perpendicularDrawing > maximumOffsetDrawing)
                            throw new InvalidOperationException("Opening " + opening.Id + " nằm quá xa host centerline để khoét an toàn.");

                        var plan = OpeningCutPlanner.Plan(new OpeningCutInput
                        {
                            HostLengthM = hostLengthM,
                            HostThicknessM = hostThicknessM,
                            HostHeightM = hostHeightM,
                            OpeningWidthM = widthM,
                            OpeningHeightM = heightM,
                            SillHeightM = sillM,
                            CenterAlongHostM = CadGeometryGuard.ToMeters(document, alongDrawing, opening.Id + "/center along host"),
                            ClearanceM = clearanceM
                        });

                        var cutterWidth = CadGeometryGuard.ToDrawingUnits(document, plan.CutterWidthM, opening.Id + "/cutter width");
                        var cutterDepth = CadGeometryGuard.ToDrawingUnits(document, plan.CutterDepthM, opening.Id + "/cutter depth");
                        var cutterHeight = CadGeometryGuard.ToDrawingUnits(document, plan.CutterHeightM, opening.Id + "/cutter height");
                        var alongCenterDrawing = CadGeometryGuard.ToDrawingUnits(document, plan.CenterAlongHostM, opening.Id + "/center along host");
                        var centerZ = CadGeometryGuard.Add(hostLine.StartPoint.Z, CadGeometryGuard.ToDrawingUnits(document, hostBottomOffsetM + plan.CenterElevationM, opening.Id + "/cutter center Z"), opening.Id + "/world cutter center Z");
                        var target = new Point3d(
                            hostLine.StartPoint.X + ux * alongCenterDrawing,
                            hostLine.StartPoint.Y + uy * alongCenterDrawing,
                            centerZ);

                        using (var cutter = new Solid3d())
                        {
                            cutter.SetDatabaseDefaults(document.Database);
                            cutter.CreateBox(cutterWidth, cutterDepth, cutterHeight);
                            cutter.TransformBy(Matrix3d.Displacement(new Vector3d(-cutterWidth / 2d, -cutterDepth / 2d, -cutterHeight / 2d)));
                            cutter.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                            cutter.TransformBy(Matrix3d.Displacement(new Vector3d(target.X, target.Y, target.Z)));
                            hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
                        }
                        totalCuts++;
                    }

                    pending.Add(new PendingHostUpdate { Host = host, SolidHandle = currentSolidHandle, Fingerprint = fingerprint, OpeningCount = openings.Count });
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                update.Host.Properties["PhysicalOpeningCutSolidHandle"] = update.SolidHandle;
                update.Host.Properties["PhysicalOpeningCutFingerprint"] = update.Fingerprint;
                update.Host.Properties["PhysicalOpeningCutCount"] = update.OpeningCount.ToString(CultureInfo.InvariantCulture);
                AuditTrail.ForProject(project).Record("geometry.opening.boolean", update.Host.Id, update.OpeningCount.ToString(CultureInfo.InvariantCulture) + " opening(s) • solid " + update.SolidHandle);
            }
            if (pending.Count > 0)
            {
                document.Editor.Regen();
                project.Touch();
            }
            return totalCuts;
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

        private static string BuildFingerprint(IReadOnlyList<ProjectElement> openings)
        {
            return string.Join("|", openings.Select(x =>
                x.Id + ":" + Text(x, "WidthM") + ":" + Text(x, "HeightM") + ":" + Text(x, "SillHeightM") + ":" + Text(x, "BottomOffsetM") + ":" + Text(x, "BooleanClearanceM")));
        }

        private static string Text(ProjectElement element, string key) => element.Properties.TryGetValue(key, out var value) ? (value ?? string.Empty).Trim() : string.Empty;

        private static bool IsSupportedHost(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall || category == ElementCategory.StructuralWall;
    }
}
